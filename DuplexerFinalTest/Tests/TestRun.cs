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
    public delegate void TestTemperatureUpdatedDelegate(ChamberTemperatureModel chamberTemp, double avgDUTTemp);

    public class TestRun
    {
        private BackgroundWorker _testWorker = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        public event TestCompletedDelegate TestCompleted;
        public event TestUpdatedDelegate TestUpdate;
        public event TestTemperatureUpdatedDelegate TestTemperatureUpdate;
        public TestSequenceModel sequence;

        private TestResultModel _testResults;
        private double _plotUpdateInterval_sec = 0;
        private DateTime _lastPlotUpdate = DateTime.MinValue;

        public TestRun()
        {
            _testWorker.DoWork += TestWorker_DoWork;
            _testWorker.ProgressChanged += TestWorker_ProgressChanged;
            _testWorker.RunWorkerCompleted += TestWorker_RunWorkerCompleted;
        }

        public void StartTest(TestSequenceModel test)
        {
            if (_testWorker.IsBusy) return;

            if (Shared.currentRunMetrics == null)
                Shared.currentRunMetrics = new RunMetricsModel();

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

            _lastPlotUpdate = DateTime.MinValue; // ensures first temperature update fires immediately
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
                string archiveTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var di = new DirectoryInfo(folder);
                foreach (var file in di.GetFiles())
                {
                    string archivedName = Path.GetFileNameWithoutExtension(file.Name)
                        + "_arch_" + archiveTimestamp
                        + file.Extension;
                    File.Move(file.FullName, Path.Combine(archiveFolder, archivedName));
                }
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
                    // Start the stored chamber program
                    Shared.ClimaticChamber.RunLocalProgram(
                        sequence.ChamberProgram.ProgramNumber,
                        sequence.ChamberProgram.StartStepNumber);
                    Shared.logger?.Log($"Chamber program {sequence.ChamberProgram.ProgramNumber} ({sequence.ChamberProgram.ExpectedProgramName}) started.");

                    bool programEndedEarly = false;

                    // For each configured test step: wait for the chamber to reach that step in RUN
                    // state (GRANTY satisfied, soak timer counting), pause the timer, run tests,
                    // then resume so the remaining soak time expires and the chamber advances.
                    for (int s = 0; s < sequence.ChamberProgram.TestsForEachStep.Count; s++)
                    {
                        if (bgw.CancellationPending) { e.Cancel = true; return; }

                        var stepConfig = sequence.ChamberProgram.TestsForEachStep[s];
                        sweepNo++;

                        bgw.ReportProgress(0, $"Waiting for chamber step {stepConfig.StepNumber}...");
                        Shared.logger?.Log($"Waiting for chamber program step {stepConfig.StepNumber}.");

                        // 3-hour ceiling per step (accounts for long RAMP + SOAK cycles)
                        const long stepWaitTimeoutMs = 180L * 60 * 1000;
                        long stepWaitStart = Shared.testTimer.ElapsedMilliseconds;
                        bool stepReached = false;
                        while (!stepReached)
                        {
                            if (bgw.CancellationPending) { e.Cancel = true; return; }
                            Thread.Sleep(2000);

                            var currentTemp = Shared.ClimaticChamber.GetTemperature();
                            Shared.currentRunMetrics?.RecordChamberTemperature(currentTemp?.MeasuredTemperature ?? double.NaN, double.NaN);
                            CheckChamberSafetyLimits(currentTemp?.MeasuredTemperature ?? double.NaN);
                            FireTemperatureUpdateIfDue(currentTemp);

                            var monitor = Shared.ClimaticChamber.GetProgramMonitor();
                            if (monitor == null) continue;

                            bgw.ReportProgress(0, $"Step {monitor.CurrentStep}/{monitor.TotalSteps} [{monitor.Status}] — waiting for step {stepConfig.StepNumber}");

                            if (monitor.Status == "END")
                            {
                                Shared.logger?.Log($"Chamber program ended before reaching step {stepConfig.StepNumber}.", MessageType.Warning);
                                programEndedEarly = true;
                                break;
                            }

                            if (monitor.CurrentStep == stepConfig.StepNumber && monitor.Status == "RUN")
                                stepReached = true;

                            if (Shared.testTimer.ElapsedMilliseconds - stepWaitStart > stepWaitTimeoutMs)
                                throw new EquipmentCommunicationException(
                                    $"Timeout (3 hours) waiting for chamber step {stepConfig.StepNumber}. " +
                                    $"Last status: step {monitor.CurrentStep} [{monitor.Status}]");
                        }

                        if (programEndedEarly) break;

                        // Freeze the soak timer while we run tests
                        Shared.ClimaticChamber.ProgramPause();
                        double soakTemp = Shared.ClimaticChamber.GetTemperature()?.MeasuredTemperature ?? double.NaN;
                        Shared.logger?.Log($"Chamber paused at step {stepConfig.StepNumber}, temp={soakTemp:F1}°C.");
                        bgw.ReportProgress(s, $"Step {stepConfig.StepNumber}: SOAK @ {soakTemp:F1}°C");

                        // Pre-delay
                        long preDelayMs = (long)(stepConfig.DelayBeforeSweepsMinutes * 60000.0);
                        if (preDelayMs > 0)
                        {
                            bgw.ReportProgress(s, $"Step {stepConfig.StepNumber}: delay before sweeps | {TimeSpan.FromMilliseconds(preDelayMs)}");
                            Shared.logger?.Log($"Step {stepConfig.StepNumber}: pre-delay {stepConfig.DelayBeforeSweepsMinutes}min");
                            long preStart = Shared.testTimer.ElapsedMilliseconds;
                            do
                            {
                                if (bgw.CancellationPending) { Shared.ClimaticChamber.ProgramContinue(); e.Cancel = true; return; }
                                Thread.Sleep(500);
                                var t = Shared.ClimaticChamber.GetTemperature();
                                Shared.currentRunMetrics?.RecordChamberTemperature(t?.MeasuredTemperature ?? double.NaN, soakTemp);
                                CheckChamberSafetyLimits(t?.MeasuredTemperature ?? double.NaN);
                                FireTemperatureUpdateIfDue(t);
                            } while (Shared.testTimer.ElapsedMilliseconds < preStart + preDelayMs);
                        }

                        // Run tests for this step
                        if (!string.IsNullOrWhiteSpace(stepConfig.Tests))
                        {
                            foreach (var testName in stepConfig.Tests.Split(','))
                            {
                                if (bgw.CancellationPending) { Shared.ClimaticChamber.ProgramContinue(); e.Cancel = true; return; }
                                var testType = (TestSequences)Enum.Parse(typeof(TestSequences), testName.Trim(), true);
                                bool stepPassed = true;
                                RunTest(testType, bgw, s, sweepNo, soakTemp, ref stepPassed, ref e);
                                if (e.Cancel) { Shared.ClimaticChamber.ProgramContinue(); return; }
                                Thread.Sleep(1000);
                            }
                        }

                        // Post-delay
                        long postDelayMs = (long)(stepConfig.DelayAfterSweepsMinutes * 60000.0);
                        if (postDelayMs > 0)
                        {
                            bgw.ReportProgress(s, $"Step {stepConfig.StepNumber}: delay after sweeps | {TimeSpan.FromMilliseconds(postDelayMs)}");
                            Shared.logger?.Log($"Step {stepConfig.StepNumber}: post-delay {stepConfig.DelayAfterSweepsMinutes}min");
                            long postStart = Shared.testTimer.ElapsedMilliseconds;
                            do
                            {
                                if (bgw.CancellationPending) { Shared.ClimaticChamber.ProgramContinue(); e.Cancel = true; return; }
                                Thread.Sleep(500);
                                var t = Shared.ClimaticChamber.GetTemperature();
                                Shared.currentRunMetrics?.RecordChamberTemperature(t?.MeasuredTemperature ?? double.NaN, soakTemp);
                                CheckChamberSafetyLimits(t?.MeasuredTemperature ?? double.NaN);
                                FireTemperatureUpdateIfDue(t);
                            } while (Shared.testTimer.ElapsedMilliseconds < postStart + postDelayMs);
                        }

                        // Resume the soak timer — chamber advances to next step once
                        // the remaining soak time expires.
                        Shared.ClimaticChamber.ProgramContinue();
                        Shared.logger?.Log($"Chamber resumed from step {stepConfig.StepNumber}.");
                    }

                    // Wait for the program to reach END (all steps complete)
                    if (!programEndedEarly)
                        bgw.ReportProgress(0, "Waiting for chamber program to complete...");
                    while (!programEndedEarly)
                    {
                        if (bgw.CancellationPending) { e.Cancel = true; return; }
                        Thread.Sleep(2000);
                        var t2 = Shared.ClimaticChamber.GetTemperature();
                        Shared.currentRunMetrics?.RecordChamberTemperature(t2?.MeasuredTemperature ?? double.NaN, double.NaN);
                        CheckChamberSafetyLimits(t2?.MeasuredTemperature ?? double.NaN);
                        FireTemperatureUpdateIfDue(t2);
                        var endMonitor = Shared.ClimaticChamber.GetProgramMonitor();
                        if (endMonitor?.Status == "END") break;
                    }
                    Shared.ClimaticChamber.ProgramEnd(ChamberProgramEndConditions.STANDBY);
                    Shared.logger?.Log("Chamber program complete. Chamber set to STANDBY.");
                }
                else
                {
                    // Manual chamber run steps
                    for (int i = 0; i < sequence.ChamberManualRun.ChamberRunSteps.Count; i++)
                    {
                        if (bgw.CancellationPending) { e.Cancel = true; return; }

                        var step = sequence.ChamberManualRun.ChamberRunSteps[i];
                        var action = (ChamberManualRunActions)Enum.Parse(typeof(ChamberManualRunActions), step.Action, true);

                        // TemperatureTolerenceInPercent is used as absolute ±°C.
                        // Percentage units make no physical sense for temperature (breaks at 0 °C,
                        // and gives inconsistent bands at hot/cold extremes).
                        // Existing JSON values of "5" become ±5 °C — tune per product requirement.
                        double tempTolerance = step.TemperatureTolerenceInPercent > 0
                            ? step.TemperatureTolerenceInPercent
                            : 2.0;   // 2 °C fallback if field is 0 or missing

                        // Command the chamber
                        Shared.ClimaticChamber.RunRemoteProgram(step.StartTemperature, step.GoTemperature, step.RampDwellMinutes);
                        Shared.logger?.Log($"Step {step.StepNo} [{action}]: {step.StartTemperature}°C→{step.GoTemperature}°C | Tolerance=±{tempTolerance:F1}°C RampDwell={step.RampDwellMinutes}min DelayBefore={step.DelayBeforeSweepsMinutes}min DelayAfter={step.DelayAfterSweepsMinutes}min");
                        if (!step.HumidityOff) Shared.ClimaticChamber.SetHumidity(step.GoHumidity);

                        if (action == ChamberManualRunActions.RAMP)
                        {
                            bgw.ReportProgress(i, $"Step {step.StepNo}: RAMP: {step.StartTemperature}°C → {step.GoTemperature}°C  (±{tempTolerance:F1}°C, 5 stable readings required)");

                            // RAMP completion requires 5 consecutive readings within tolerance
                            // (5 × 500 ms = 2.5 s of stability) to avoid declaring success on a
                            // transient reading while the chamber is still moving through the target.
                            const int requiredConsecutive = 5;
                            int consecutiveOk = 0;
                            double measuredTemp = double.NaN;

                            // 120-minute hard ceiling — if the chamber hasn't arrived by then,
                            // throw EquipmentCommunicationException so the operator is prompted
                            // via the existing retry countdown dialog.
                            const long rampTimeoutMs = 120L * 60 * 1000;
                            long rampStart = Shared.testTimer.ElapsedMilliseconds;

                            do
                            {
                                if (bgw.CancellationPending) { e.Cancel = true; return; }
                                Thread.Sleep(500);

                                // Single GetTemperature call per iteration — reused for safety
                                // check, stability check, and chart update to avoid redundant
                                // TCP round-trips to the chamber.
                                var currentTemp = Shared.ClimaticChamber.GetTemperature();
                                measuredTemp = currentTemp?.MeasuredTemperature ?? double.NaN;
                                Shared.currentRunMetrics?.RecordChamberTemperature(measuredTemp, step.GoTemperature);

                                CheckChamberSafetyLimits(measuredTemp);
                                FireTemperatureUpdateIfDue(currentTemp);

                                if (Shared.testTimer.ElapsedMilliseconds - rampStart > rampTimeoutMs)
                                    throw new EquipmentCommunicationException(
                                        $"Chamber RAMP timeout (120 min): step {step.StepNo} target {step.GoTemperature:F1}°C not reached. " +
                                        $"Last reading: {measuredTemp:F1}°C");

                                if (!double.IsNaN(measuredTemp) && Math.Abs(measuredTemp - step.GoTemperature) <= tempTolerance)
                                    consecutiveOk++;
                                else
                                    consecutiveOk = 0;

                                if (consecutiveOk > 0 && consecutiveOk < requiredConsecutive)
                                    bgw.ReportProgress(i, $"Step {step.StepNo}: RAMP — within tolerance ({consecutiveOk}/{requiredConsecutive})");

                            } while (consecutiveOk < requiredConsecutive);

                            Shared.logger?.Log($"Step {step.StepNo}: RAMP complete in {Shared.testTimer.ElapsedMilliseconds - rampStart}ms | Temp={measuredTemp:0.0}°C");
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
                                var currentTemp = Shared.ClimaticChamber.GetTemperature();
                                Shared.currentRunMetrics?.RecordChamberTemperature(currentTemp?.MeasuredTemperature ?? double.NaN, step.GoTemperature);
                                CheckChamberSafetyLimits(currentTemp?.MeasuredTemperature ?? double.NaN);
                                FireTemperatureUpdateIfDue(currentTemp);
                            } while (Shared.testTimer.ElapsedMilliseconds < preDelay + preDelayMs);
                            Shared.logger?.Log($"Step {step.StepNo}: pre-delay done (actual {Shared.testTimer.ElapsedMilliseconds - preDelay}ms)");
                            Shared.currentRunMetrics?.RecordSoakSettle(TimeSpan.FromMilliseconds(Shared.testTimer.ElapsedMilliseconds - preDelay));

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
                                var currentTemp = Shared.ClimaticChamber.GetTemperature();
                                Shared.currentRunMetrics?.RecordChamberTemperature(currentTemp?.MeasuredTemperature ?? double.NaN, step.GoTemperature);
                                CheckChamberSafetyLimits(currentTemp?.MeasuredTemperature ?? double.NaN);
                                FireTemperatureUpdateIfDue(currentTemp);
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
                    Shared.currentRunMetrics?.RecordEquipmentRetry();
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

                    if (retryCount > 2)
                        Shared.currentRunMetrics?.RecordForcedOperatorResume();

                    int reconnectCount = Shared.ReconnectDisconnectedEquipment();
                    if (reconnectCount > 0)
                    {
                        Shared.currentRunMetrics?.RecordEquipmentReconnect(reconnectCount);
                        Shared.logger?.Log($"Recovered {reconnectCount} disconnected equipment session(s) before retry.", MessageType.Warning);
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

        private void FireTemperatureUpdateIfDue(ChamberTemperatureModel chamberTemp = null)
        {
            if (_plotUpdateInterval_sec > 0 && (DateTime.UtcNow - _lastPlotUpdate).TotalSeconds < _plotUpdateInterval_sec)
                return;
            _lastPlotUpdate = DateTime.UtcNow;
            try
            {
                if (chamberTemp == null)
                    chamberTemp = Shared.ClimaticChamber.GetTemperature();
                double total = 0;
                int count = 0;
                if (sequence?.BaseDUTs != null)
                    foreach (var dut in sequence.BaseDUTs)
                    {
                        try { total += dut.ReadThermistor; count++; } catch { }
                    }
                if (sequence?.RemoteDUTs != null)
                    foreach (var dut in sequence.RemoteDUTs)
                    {
                        try { total += dut.ReadThermistor; count++; } catch { }
                    }
                double avgDUTTemp = count > 0 ? total / count : 0;
                TestTemperatureUpdate?.Invoke(chamberTemp, avgDUTTemp);
            }
            catch { }
        }

        // Checks the measured chamber temperature against the configured safety limits.
        // If outside the envelope: commands STANDBY immediately, logs the event, and throws
        // EquipmentCommunicationException — which routes to the operator retry/countdown dialog.
        private void CheckChamberSafetyLimits(double measuredTemperature)
        {
            if (double.IsNaN(measuredTemperature)) return;  // unreadable — skip, do not false-alarm

            var s = Shared.sharedGeneralSettings?.GeneralSettings?[0];
            double safeMax = 100.0;   // default: +100 °C
            double safeMin = -70.0;   // default: -70 °C
            if (s != null)
            {
                if (double.TryParse(s.CHAMBER_SAFE_MAX_TEMP, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsedMax))
                    safeMax = parsedMax;
                if (double.TryParse(s.CHAMBER_SAFE_MIN_TEMP, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsedMin))
                    safeMin = parsedMin;
            }

            if (measuredTemperature > safeMax || measuredTemperature < safeMin)
            {
                try { Shared.ClimaticChamber?.SetMode(ChamberModes.STANDBY); } catch { }
                Shared.logger?.Log(
                    $"SAFETY LIMIT: chamber {measuredTemperature:F1}°C outside [{safeMin:F1}, {safeMax:F1}]°C — STANDBY commanded",
                    MessageType.Error);
                throw new EquipmentCommunicationException(
                    $"SAFETY LIMIT EXCEEDED: Chamber temperature {measuredTemperature:F1}°C is outside the " +
                    $"configured safe range [{safeMin:F1}°C to {safeMax:F1}°C]. " +
                    "Chamber set to STANDBY. Verify CHAMBER_SAFE_MAX_TEMP / CHAMBER_SAFE_MIN_TEMP in Settings.");
            }
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
                // Do NOT unsubscribe the BackgroundWorker handlers here — they must persist
                // so that subsequent test runs (New Test → StartTest again) work correctly.

                // Stop chamber
                if (Shared.ClimaticChamber is EquipmentSim.ClimaticChamberSim sim)
                    sim.Power(false);
                else
                    try { Shared.ClimaticChamber?.SetMode(ChamberModes.STANDBY); } catch { }

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
