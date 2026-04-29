using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
                        Shared.logger?.Log($"Step {step.StepNo} [{action}]: {step.StartTemperature}°C→{step.GoTemperature}°C | RampDwell={step.RampDwellMinutes}min DelayBefore={step.DelayBeforeSweepsMinutes}min DelayAfter={step.DelayAfterSweepsMinutes}min");
                        if (!step.HumidityOff) Shared.ClimaticChamber.SetHumidity(step.GoHumidity);

                        if (action == ChamberManualRunActions.RAMP)
                        {
                            bgw.ReportProgress(i, $"Step {step.StepNo}: RAMP: {step.StartTemperature}°C → {step.GoTemperature}°C | {TimeSpan.FromMinutes(step.RampDwellMinutes):hh\\:mm\\:ss}");
                            // Wait until chamber reaches target
                            long rampStart = Shared.testTimer.ElapsedMilliseconds;
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (!Shared.ClimaticChamber.IsReady(step.GoTemperature, tempTolerance));
                            Shared.logger?.Log($"Step {step.StepNo}: RAMP complete in {Shared.testTimer.ElapsedMilliseconds - rampStart}ms | Temp={Shared.ClimaticChamber.GetTemperature()?.MeasuredTemperature:0.0}°C");
                        }
                        else // SOAK
                        {
                            sweepNo++;
                            bgw.ReportProgress(i, $"Step {step.StepNo}: SOAK @ {step.GoTemperature}°C");

                            // Delay before sweeps
                            long preDelayMs = (long)(step.DelayBeforeSweepsMinutes * 60000.0);
                            bgw.ReportProgress(i, $"►Delay before sweeps | {TimeSpan.FromMilliseconds(preDelayMs)}");
                            Shared.logger?.Log($"Step {step.StepNo}: pre-delay {preDelayMs}ms ({step.DelayBeforeSweepsMinutes}min)");
                            long preDelay = Shared.testTimer.ElapsedMilliseconds;
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (Shared.testTimer.ElapsedMilliseconds < preDelay + preDelayMs);
                            Shared.logger?.Log($"Step {step.StepNo}: pre-delay done (actual {Shared.testTimer.ElapsedMilliseconds - preDelay}ms)");

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
                            long postDelayMs = (long)(step.DelayAfterSweepsMinutes * 60000.0);
                            bgw.ReportProgress(i, $"►Delay after sweeps | {TimeSpan.FromMilliseconds(postDelayMs)}");
                            Shared.logger?.Log($"Step {step.StepNo}: post-delay {postDelayMs}ms ({step.DelayAfterSweepsMinutes}min)");
                            long postDelay = Shared.testTimer.ElapsedMilliseconds;
                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);
                            } while (Shared.testTimer.ElapsedMilliseconds < postDelay + postDelayMs);
                            Shared.logger?.Log($"Step {step.StepNo}: post-delay done (actual {Shared.testTimer.ElapsedMilliseconds - postDelay}ms)");
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
            int retryCount = 0;

            while (true)
            {
                _testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                bool result = false;
                bool cancelled = false;

                try
                {
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
                    return; // success — exit retry loop
                }
                catch (EquipmentCommunicationException ex) when (!bgw.CancellationPending)
                {
                    retryCount++;
                    Shared.logger?.Log(
                        $"Equipment communication failure (attempt {retryCount}): {ex.Message}",
                        MessageType.Error);

                    // Delay schedule: attempt 1 → 10 min, attempt 2 → 15 min, attempt 3+ → wait for user
                    TimeSpan delay = retryCount == 1 ? TimeSpan.FromMinutes(10)
                                   : retryCount == 2 ? TimeSpan.FromMinutes(15)
                                   : TimeSpan.Zero;

                    bgw.ReportProgress(stepIndex,
                        retryCount <= 2
                            ? $"⚠ Comm failure — retry {retryCount} in {(int)delay.TotalMinutes} min: {ex.Message}"
                            : $"⚠ Comm failure — waiting for operator: {ex.Message}");

                    var dialogResult = ShowRetryCountdownDialog(ex.Message, delay, retryCount);

                    if (dialogResult == RetryCountdownResult.Cancel || bgw.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    // ResumeNow → loop back and retry the same test step from the top
                    Shared.logger?.Log($"Operator resumed — retrying {testType}", MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Marshals a RetryCountdownForm onto the UI thread and blocks the background worker
        /// until the operator clicks Resume Now (or the countdown expires) or Cancel Test.
        /// </summary>
        private RetryCountdownResult ShowRetryCountdownDialog(string errorMessage, TimeSpan delay, int attemptNumber)
        {
            var result = RetryCountdownResult.Cancel;
            var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            if (mainForm == null || mainForm.IsDisposed)
                return RetryCountdownResult.Cancel;

            mainForm.Invoke((MethodInvoker)delegate
            {
                using (var form = new RetryCountdownForm(errorMessage, delay, attemptNumber))
                {
                    form.ShowDialog(mainForm);
                    result = form.Result;
                }
            });

            return result;
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
