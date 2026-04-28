using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DuplexerFinalTest.Tests
{
    public delegate void TestCompletedDelegate();
    public delegate void TestUpdatedDelegate(string updateMessage, int progressPercentage);

    public class TestRun
    {
        private BackgroundWorker _testWorker = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        public event TestCompletedDelegate TestCompleted;
        public event TestUpdatedDelegate TestUpdate;
        public TestSequenceModel sequence;

        private TestResultModel _testResults;
        private double _plotUpdateInterval_sec = 0;

        public TestRun()
        {
            _testWorker.DoWork += TestWorker_DoWork;
            _testWorker.ProgressChanged += TestWorker_ProgressChanged;
            _testWorker.RunWorkerCompleted += TestWorker_RunWorkerCompleted;
        }

        public void StartTest(TestSequenceModel test)
        {
            if (_testWorker.IsBusy) return;

            sequence = test.Clone();

            if (!double.TryParse(Shared.sharedGeneralSettings.GeneralSettings[0].PLOT_UPDATE_IN_MINUTES,
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
                out double plotIntervalMin))
                plotIntervalMin = 1.0;
            _plotUpdateInterval_sec = plotIntervalMin * 60.0;

            // Verify chamber program if needed
            if (test.CallsChamberProgram)
            {
                if (!Shared.ClimaticChamber.VerifyProgram(test.ChamberProgram.ProgramNumber,
                    test.ChamberProgram.ExpectedProgramName, test.ChamberProgram.ExpectedNumberOfSteps))
                {
                    MessageBox.Show($"Cannot verify chamber program: {test.ChamberProgram.ExpectedProgramName}");
                    return;
                }
            }

            // Init test results
            _testResults?.Dispose();
            _testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };

            // Archive old result files
            ArchiveResults(Shared.BaseResultsPath);
            ArchiveResults(Shared.RemoteResultsPath);

            // Start chamber
            if (Shared.ClimaticChamber is EquipmentSim.ClimaticChamberSim sim)
                sim.Power(true);

            // Start timer
            Shared.testTimer = new System.Diagnostics.Stopwatch();
            Shared.testTimer.Start();

            _testWorker.RunWorkerAsync(test);
        }

        public void StopTest()
        {
            if (_testWorker.IsBusy)
                _testWorker.CancelAsync();
        }

        private void ArchiveResults(string folder)
        {
            try
            {
                if (!Directory.Exists(folder)) return;
                string archiveFolder = Path.Combine(folder, "Archive");
                Directory.CreateDirectory(archiveFolder);
                var di = new DirectoryInfo(folder);
                foreach (var file in di.GetFiles())
                    File.Move(file.FullName, Path.Combine(archiveFolder, file.Name), true);
            }
            catch { }
        }

        private void TestWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = (BackgroundWorker)sender;
            int sweepNo = 0;

            try
            {
                if (sequence.CallsChamberProgram)
                {
                    // Local chamber program
                    Shared.ClimaticChamber.RunLocalProgram(
                        sequence.ChamberProgram.ProgramNumber,
                        sequence.ChamberProgram.StartStepNumber);
                    // TODO: implement wait-for-chamber-step feedback loop when protocol is known
                }
                else
                {
                    // Manual chamber run steps
                    for (int i = 0; i < sequence.ChamberManualRun.ChamberRunSteps.Count; i++)
                    {
                        if (bgw.CancellationPending) { e.Cancel = true; return; }

                        var step = sequence.ChamberManualRun.ChamberRunSteps[i];
                        var action = (ChamberManualRunActions)Enum.Parse(typeof(ChamberManualRunActions), step.Action, true);
                        double tempTolerance = step.GoTemperature == 0
                            ? 1.5
                            : Math.Abs(step.TemperatureTolerenceInPercent * step.GoTemperature / 100.0);

                        // Command the chamber
                        Shared.ClimaticChamber.RunRemoteProgram(step.StartTemperature, step.GoTemperature, step.RampDwellMinutes);
                        if (!step.HumidityOff) Shared.ClimaticChamber.SetHumidity(step.GoHumidity);

                        if (action == ChamberManualRunActions.RAMP)
                        {
                            bgw.ReportProgress(i, $"Step {step.StepNo}: RAMP: {step.StartTemperature}°C → {step.GoTemperature}°C");
                            // Wait until chamber reaches target
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (!Shared.ClimaticChamber.IsReady(step.GoTemperature, tempTolerance));
                        }
                        else // SOAK
                        {
                            sweepNo++;
                            bgw.ReportProgress(i, $"Step {step.StepNo}: SOAK @ {step.GoTemperature}°C");

                            // Delay before sweeps
                            bgw.ReportProgress(i, $"►Delay before sweeps | {TimeSpan.FromMinutes(step.DelayBeforeSweepsMinutes)}");
                            long preDelay = Shared.testTimer.ElapsedMilliseconds;
                            long preDelayMs = (long)(step.DelayBeforeSweepsMinutes * 60000.0);
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (Shared.testTimer.ElapsedMilliseconds < preDelay + preDelayMs);

                            // Run tests for this step
                            if (!string.IsNullOrWhiteSpace(step.Tests))
                            {
                                foreach (var testName in step.Tests.Split(','))
                                {
                                    if (bgw.CancellationPending) { e.Cancel = true; return; }
                                    var testType = (TestSequences)Enum.Parse(typeof(TestSequences), testName.Trim(), true);
                                    bool stepPassed = step.Passed;
                                    RunTest(testType, bgw, i, sweepNo, step.GoTemperature, ref stepPassed, ref e);
                                    step.Passed = stepPassed;
                                    if (e.Cancel) return;
                                    Thread.Sleep(1000);
                                }
                            }

                            // Delay after sweeps
                            bgw.ReportProgress(i, $"►Delay after sweeps | {TimeSpan.FromMinutes(step.DelayAfterSweepsMinutes)}");
                            long postDelay = Shared.testTimer.ElapsedMilliseconds;
                            long postDelayMs = (long)(step.DelayAfterSweepsMinutes * 60000.0);
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (Shared.testTimer.ElapsedMilliseconds < postDelay + postDelayMs);
                        }

                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("TestRun.DoWork", ex);
            }
        }

        private void RunTest(TestSequences testType, BackgroundWorker bgw, int stepIndex, int sweepNo,
            double temperature, ref bool stepPassed, ref DoWorkEventArgs e)
        {
            _testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
            bool result = false;
            bool cancelled = false;

            switch (testType)
            {
                case TestSequences.Base_Z_IB_IOP:
                    result = IndividualTestRun.RunBase_Z_IB_IOP(sequence, _testResults, bgw, stepIndex, sweepNo, temperature, out cancelled);
                    break;
                case TestSequences.Base_Z_IPD:
                    result = IndividualTestRun.RunBase_Z_IPD(sequence, _testResults, bgw, stepIndex, sweepNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_IOP:
                    result = IndividualTestRun.RunRemote_Z_IOP(sequence, _testResults, bgw, stepIndex, sweepNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_IPV:
                    result = IndividualTestRun.RunRemote_Z_IPV(sequence, _testResults, bgw, stepIndex, sweepNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_VPV:
                    result = IndividualTestRun.RunRemote_Z_VPV(sequence, _testResults, bgw, stepIndex, sweepNo, temperature, out cancelled);
                    break;
            }

            stepPassed = result;
            if (cancelled) e.Cancel = true;
        }

        private void TestWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                int total = sequence?.ChamberManualRun?.ChamberRunSteps?.Count ?? 1;
                int percentage = total > 0 ? e.ProgressPercentage * 100 / total : 0;
                TestUpdate?.Invoke(e.UserState?.ToString() ?? "", percentage);
            }
            catch { }
        }

        private void TestWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                _testWorker.DoWork -= TestWorker_DoWork;
                _testWorker.ProgressChanged -= TestWorker_ProgressChanged;
                _testWorker.RunWorkerCompleted -= TestWorker_RunWorkerCompleted;

                // Stop chamber
                if (Shared.ClimaticChamber is EquipmentSim.ClimaticChamberSim sim)
                    sim.Power(false);

                Shared.testTimer?.Stop();

                if (e.Cancelled)
                    Shared.logger?.Log("Test cancelled by operator.");
                else if (e.Error != null)
                    Shared.logger?.LogError("Test completed with error", e.Error);

                TestCompleted?.Invoke();
            }
            catch { }
        }

        public double GetAverageDUTTemp()
        {
            double total = 0;
            int count = 0;
            foreach (var dut in sequence.BaseDUTs) { total += dut.ReadThermistor; count++; }
            foreach (var dut in sequence.RemoteDUTs) { total += dut.ReadThermistor; count++; }
            return count > 0 ? total / count : 0;
        }
    }
}
