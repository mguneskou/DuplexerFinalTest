using DuplexerFinalTest.EquipmentSim;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace DuplexerFinalTest
{
    public partial class MainForm : Form
    {
        private WaitForm waitForm = null;

        public MainForm()
        {
            InitializeComponent();
            // Double-buffer the table layout panels to reduce flickering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, tlpMain, new object[] { true });
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, tlpBottom, new object[] { true });
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, tlpEquipment, new object[] { true });
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Initialise logger - writes to P:\MGunes\DuplexerTestSuite\Logs with session timestamp
            try
            {
                string logFolder = @"P:\MGunes\DuplexerTestSuite\Logs";
                Shared.logger = new Logger(logFolder);
                Shared.logger.Log("Application started");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logger init failed: {ex.Message}");
            }

            // Attach the event-log ListView (handle is ready after InitializeComponent)
            try
            {
                Shared.logger?.AttachListView(lstEventLog);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Read general settings
            try
            {
                Shared.GeneralSettingsPath = @"P:\MGunes\DuplexerTestSuite\Resources\Settings\SettingsGeneral.json";
                // Ensure the SettingsForm instance is created on the UI thread before use
                if (Shared.settingsForm == null)
                {
                    try
                    {
                        Shared.settingsForm = new SettingsForm();
                    }
                    catch (Exception ex)
                    {
                        Shared.logger?.LogError("SettingsForm construction failed", ex);
                        MessageBox.Show($"Settings form construction failed:\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw;
                    }
                }
                try
                {
                    Shared.sharedGeneralSettings = Shared.settingsForm.ReadGeneralSettings();
                }
                catch (Exception ex)
                {
                    Shared.logger?.LogError("Settings reading failed", ex);
                    MessageBox.Show($"Settings reading failed:\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw;
                }
                Shared.InitializeEquipment(Shared.sharedGeneralSettings);

                // Configure "Save to Database" menu item based on auto-save setting
                bool autoSave = Shared.sharedGeneralSettings?.GeneralSettings[0]
                    .SAVE_RESULTS_TO_DB_AUTO?.Trim().ToLower() == "true";
                mnuSaveToDatabase.Visible = false;   // hidden until test ends (in both modes)
                mnuSaveToDatabase.Enabled = false;
                Shared.logger?.Log($"DB auto-save: {(autoSave ? "ON" : "OFF")}");

                bool diagEnabled = Shared.sharedGeneralSettings?.GeneralSettings[0]
                    .ENABLE_DIAG_UI?.Trim().ToLower() == "true";
                mnuDiagnostics.Visible = diagEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Settings reading failed: {ex.Message}");
            }

            // Assign results folders
            try
            {
                Shared.BaseResultsPath = Path.Combine(
                    Shared.sharedGeneralSettings.GeneralSettings[0].RESULTS_FOLDER, "Base");
                Shared.RemoteResultsPath = Path.Combine(
                    Shared.sharedGeneralSettings.GeneralSettings[0].RESULTS_FOLDER, "Remote");
                Directory.CreateDirectory(Path.Combine(Shared.BaseResultsPath, "Archive"));
                Directory.CreateDirectory(Path.Combine(Shared.RemoteResultsPath, "Archive"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Results path error: {ex.Message}");
            }

            // Parse test flow JSON files
            try
            {
                string tfDir = Path.Combine(
                    Shared.sharedGeneralSettings.GeneralSettings[0].RESOURCES_FOLDER, "TestFlows");
                Shared.Base_Z_IB_IOP = Shared.ParseTestFlow(Path.Combine(tfDir, "Base_Z_IB_IOP.json"));
                Shared.Base_Z_IPD    = Shared.ParseTestFlow(Path.Combine(tfDir, "Base_Z_IPD.json"));
                Shared.Remote_Z_IOP  = Shared.ParseTestFlow(Path.Combine(tfDir, "Remote_Z_IOP.json"));
                Shared.Remote_Z_IPV  = Shared.ParseTestFlow(Path.Combine(tfDir, "Remote_Z_IPV.json"));
                Shared.Remote_Z_VPV  = Shared.ParseTestFlow(Path.Combine(tfDir, "Remote_Z_VPV.json"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test flow files reading failed: {ex.Message}");
            }

            // Read all available test sequence files
            try
            {
                string tsDir = Path.Combine(
                    Shared.sharedGeneralSettings.GeneralSettings[0].RESOURCES_FOLDER, "TestSequences");
                if (Directory.Exists(tsDir))
                {
                    foreach (var file in new DirectoryInfo(tsDir).EnumerateFiles())
                    {
                        Shared.AllAvailableTestSequences.Add(Shared.ParseTestSequence(file.FullName));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test sequence files reading failed: {ex.Message}");
            }

            // Load connected/disconnected/pass/fail images
            try
            {
                string imgDir = Path.Combine(
                    Shared.sharedGeneralSettings.GeneralSettings[0].RESOURCES_FOLDER, "Images");
                Shared.ConnectedImage    = Image.FromFile(Path.Combine(imgDir, "connected.png"));
                Shared.DisconnectedImage = Image.FromFile(Path.Combine(imgDir, "disconnected.png"));
                Shared.PassImage         = Image.FromFile(Path.Combine(imgDir, "success.png"));
                Shared.FailImage         = Image.FromFile(Path.Combine(imgDir, "error.png"));
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Image load failed: {ex.Message}", MessageType.Warning);
            }

            // Connect equipment
            this.Enabled = false;
            waitForm = new WaitForm(null, "Connecting equipment...", false);
            waitForm.Show();
            Application.DoEvents();
            try
            {
                Shared.CheckEquipmentConnections(
                    pnlOpticalSwitch1x4, pnlOpticalSwitch1x13_Base, pnlOpticalSwitch1x13_Remote,
                    pnlElectricalSwitchBase1, pnlElectricalSwitchBase2, pnlElectricalSwitchBase3,
                    pnlElectricalSwitchRemote1, pnlElectricalSwitchRemote2, pnlElectricalSwitchRemote3,
                    pnlSMUMaster, pnlSMUSlave, pnlClimaticChamber, pnlDatabase);
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Cannot connect equipment: {ex.Message}", MessageType.Error);
            }

            // Database connection and test specs are handled by CheckEquipmentConnections (background task)

            waitForm.Close();
            waitForm.Dispose();
            waitForm = null;
            this.Enabled = true;

            Shared.logger?.Log("Application startup complete");

            // Set software version in title bar
            try
            {
                Shared.SoftwareVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
                this.Text = $"Duplexer Final Test | V-{Shared.SoftwareVersion}";
            }
            catch
            {
                this.Text = "Duplexer Final Test";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Shared.logger?.Log("Application closing - disconnecting equipment");
                timerElapsed.Stop();
                Shared.testRun?.StopTest();
                Shared.SMU_master?.Disconnect();
                Shared.SMU_slave?.Disconnect();
                Shared.OpticalSwitch1x4?.Disconnect();
                Shared.OpticalSwitch1x13_Base?.Disconnect();
                Shared.OpticalSwitch1x13_Remote?.Disconnect();
                Shared.ElectricalSwitchBase1?.Disconnect();
                Shared.ElectricalSwitchBase2?.Disconnect();
                Shared.ElectricalSwitchBase3?.Disconnect();
                Shared.ElectricalSwitchRemote1?.Disconnect();
                Shared.ElectricalSwitchRemote2?.Disconnect();
                Shared.ElectricalSwitchRemote3?.Disconnect();
                Shared.ClimaticChamber?.Disconnect();
                Shared.logger?.Log("Application closed");
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Form closing error: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnUpdateEquipmentStatus_Click(object sender, EventArgs e)
        {
            try
            {
                Shared.CheckEquipmentConnections(
                    pnlOpticalSwitch1x4, pnlOpticalSwitch1x13_Base, pnlOpticalSwitch1x13_Remote,
                    pnlElectricalSwitchBase1, pnlElectricalSwitchBase2, pnlElectricalSwitchBase3,
                    pnlElectricalSwitchRemote1, pnlElectricalSwitchRemote2, pnlElectricalSwitchRemote3,
                    pnlSMUMaster, pnlSMUSlave, pnlClimaticChamber, pnlDatabase);
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Update equipment status: {ex.Message}", MessageType.Error);
            }
        }

        private void TimerElapsed_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Shared.testTimer != null && Shared.testTimer.IsRunning)
                {
                    lblElapsedTime.Text = $"Elapsed Time: [{Shared.testTimer.Elapsed:hh\\:mm\\:ss}]";
                }
            }
            catch { }
        }

        #region Cancel Test Button

        private void BtnCancelTest_Click(object sender, EventArgs e)
        {
            try
            {
                Shared.logger?.Log("Cancel test requested by operator", MessageType.Warning);
                Shared.testRun?.StopTest();
                btnCancelTest.Enabled = false;
                lblTestResult.Text = "";
                mnuNewTest.Text = "New Test";
                timerElapsed.Stop();
                Shared.testTimer?.Stop();
                Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                Shared.testRun.TestTemperatureUpdate -= ClimaticChamber_Update;
                if (Shared.ClimaticChamber is ClimaticChamberSim simStop)
                    simStop.Update -= ClimaticChamber_Update;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("BtnCancelTest_Click", ex);
            }
        }

        #endregion

        #region Menu Handlers

        private void MnuNewTest_Click(object sender, EventArgs e)
        {
            try
            {
                if (mnuNewTest.Text == "New Test")
                {
                    using (var sf = new StartForm())
                    {
                        if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            mnuNewTest.Text = "Cancel Test";
                            btnCancelTest.Enabled = true;
                            lblTestResult.Text = "";
                            chartTemperature.Series["ChamberTemperature"].Points.Clear();
                            chartTemperature.Series["DUTTemperature"].Points.Clear();

                            // Setup chart legends
                            chartTemperature.Legends.Clear();
                            chartTemperature.Legends.Add(new Legend("ChamberTemp"));
                            chartTemperature.Legends["ChamberTemp"].Docking = Docking.Bottom;
                            chartTemperature.Legends.Add(new Legend("DUTTemp"));
                            chartTemperature.Legends["DUTTemp"].Docking = Docking.Bottom;
                            chartTemperature.Series["ChamberTemperature"].Legend = "ChamberTemp";
                            chartTemperature.Series["ChamberTemperature"].LegendText = "Chamber Temperature (°C)";
                            chartTemperature.Series["DUTTemperature"].Legend = "DUTTemp";
                            chartTemperature.Series["DUTTemperature"].LegendText = "DUT Temperature (°C)";

                            // Setup test progress list
                            lstTestProgress.Items.Clear();
                            lstTestProgress.Columns.Clear();
                            lstTestProgress.View = System.Windows.Forms.View.Details;
                            lstTestProgress.Columns.Add("Step", (lstTestProgress.Width / 5) * 4);
                            lstTestProgress.Columns.Add("Status", (lstTestProgress.Width / 5) * 1);

                            prgTestProgress.Value = 0;

                            // Subscribe events
                            Shared.testRun.TestUpdate += TestRun_TestUpdate;
                            Shared.testRun.TestCompleted += TestRun_TestCompleted;
                            Shared.testRun.TestTemperatureUpdate += ClimaticChamber_Update;

                            // Subscribe climatic chamber update for chart (simulator only)
                            if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                                sim.Update += ClimaticChamber_Update;

                            Shared.logger?.Log($"Test started: {Shared.infoModel.Test?.SequenceName} | Operator: {Shared.infoModel.Operator}");
                            mnuSaveToDatabase.Visible = false;
                            mnuSaveToDatabase.Enabled = false;
                            Shared.testRun.StartTest(Shared.infoModel.Test);
                            timerElapsed.Start();
                        }
                    }
                }
                else
                {
                    mnuNewTest.Text = "New Test";
                    btnCancelTest.Enabled = false;
                    timerElapsed.Stop();
                    Shared.testTimer?.Stop();
                    Shared.logger?.Log("Test cancelled from menu", MessageType.Warning);
                    Shared.testRun.StopTest();
                    // Unsubscribe
                    Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                    Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                    Shared.testRun.TestTemperatureUpdate -= ClimaticChamber_Update;
                    if (Shared.ClimaticChamber is ClimaticChamberSim simStop)
                        simStop.Update -= ClimaticChamber_Update;
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"New test menu item: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuCalibration_Click(object sender, EventArgs e)
        {
            try
            {
                using (var calibrationForm = new CalibrationForm())
                {
                    calibrationForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Calibration menu: {ex.Message}");
            }
        }

        private void MnuSettings_Click(object sender, EventArgs e)
        {
            try
            {
                Shared.settingsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Cannot load settings form: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuViewLogFiles_Click(object sender, EventArgs e)
        {
            try
            {
                string logDir = Shared.logger?.LogDirectory ?? @"P:\MGunes\DuplexerTestSuite\Logs";
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"View log files: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuTestProcedure_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = Path.Combine(Application.StartupPath, "Resources", "TestProcedure");
                var helpFile = Directory.GetFiles(dir).ToList().Find(r => r.EndsWith(".pdf"));
                if (helpFile != null)
                    Process.Start(new ProcessStartInfo { FileName = helpFile, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Test procedure: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuHelp_Click(object sender, EventArgs e)
        {
            try
            {
                string dir = GetPreferredResourceDirectory("Help");
                if (!Directory.Exists(dir))
                    throw new DirectoryNotFoundException($"Help folder was not found: {dir}");

                string helpFile = Directory.GetFiles(dir)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(path => path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    ?? Directory.GetFiles(dir)
                        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault(path => path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(helpFile))
                    throw new FileNotFoundException($"No HTML or PDF help file was found in '{dir}'.");

                Process.Start(new ProcessStartInfo { FileName = helpFile, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Help: {ex.Message}", MessageType.Error);
            }
        }

        private string GetPreferredResourceDirectory(string subfolderName)
        {
            string resourcesRoot = Shared.sharedGeneralSettings?.GeneralSettings?[0]?.RESOURCES_FOLDER;
            string configuredPath = string.IsNullOrWhiteSpace(resourcesRoot)
                ? null
                : Path.Combine(resourcesRoot, subfolderName);

            if (!string.IsNullOrWhiteSpace(configuredPath) && Directory.Exists(configuredPath))
                return configuredPath;

            return Path.Combine(Application.StartupPath, "Resources", subfolderName);
        }

        private void MnuDiagnostics_Click(object sender, EventArgs e)
        {
            var frm = new DiagnosticForm();
            frm.Show(this);
        }

        private void MnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Test Event Handlers

        private void ClimaticChamber_Update(DuplexerFinalTest.Models.ChamberTemperatureModel chamberTemp, double averageDUTTemperature)
        {
            try
            {
                double chamberTemperature = chamberTemp?.MeasuredTemperature ?? 0;
                var elapsed = Shared.testTimer?.Elapsed.ToString(@"hh\:mm\:ss") ?? "00:00:00";
                if (InvokeRequired)
                {
                    BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        chartTemperature.Series["ChamberTemperature"].Points.AddXY(elapsed, chamberTemperature);
                        chartTemperature.Series["DUTTemperature"].Points.AddXY(elapsed, averageDUTTemperature);
                        lblChamberTemperature.Text = chamberTemperature.ToString("0.000");
                        lblAverageDUTTemperature.Text = averageDUTTemperature.ToString("0.000");
                    });
                }
                else
                {
                    chartTemperature.Series["ChamberTemperature"].Points.AddXY(elapsed, chamberTemperature);
                    chartTemperature.Series["DUTTemperature"].Points.AddXY(elapsed, averageDUTTemperature);
                    lblChamberTemperature.Text = chamberTemperature.ToString("0.000");
                    lblAverageDUTTemperature.Text = averageDUTTemperature.ToString("0.000");
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Chamber update: {ex.Message}", MessageType.Error);
            }
        }

        private void TestRun_TestUpdate(string updateMessage, int progressPercentage)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        UpdateTestProgress(updateMessage, progressPercentage);
                    });
                }
                else
                {
                    UpdateTestProgress(updateMessage, progressPercentage);
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Test update: {ex.Message}", MessageType.Error);
            }
        }

        private void UpdateTestProgress(string updateMessage, int progressPercentage)
        {
            var parts = updateMessage.Split('|');
            var step = parts.Length > 0 ? parts[0].Trim() : updateMessage;
            var status = parts.Length > 1 ? parts[1].Trim() : "";
            Shared.logger?.Log($"{step}{(string.IsNullOrEmpty(status) ? "" : " | " + status)}");
            var item = lstTestProgress.Items.Add(new System.Windows.Forms.ListViewItem(new string[] { step, status }));
            if (status.Contains("Failed") || status.Contains("FAIL"))
                lstTestProgress.Items[lstTestProgress.Items.Count - 1].ForeColor = Color.Red;
            else if (status.Contains("Passed") || status.Contains("PASS"))
                lstTestProgress.Items[lstTestProgress.Items.Count - 1].ForeColor = Color.Green;
            if (progressPercentage >= 0 && progressPercentage <= 100)
                prgTestProgress.Value = progressPercentage;
            lstTestProgress.Items[lstTestProgress.Items.Count - 1].EnsureVisible();
        }

        private void TestRun_TestCompleted()
        {
            try
            {
                Shared.testTimer?.Stop();
                timerElapsed.Stop();

                DateTime runCompletedAt = DateTime.Now;
                bool overallPassed = EvaluateRunOverallPassFail(updateLiveResultFileNames: true);
                WriteExcelTestReport(overallPassed, runCompletedAt);

                bool autoSave = Shared.sharedGeneralSettings?.GeneralSettings[0]
                    .SAVE_RESULTS_TO_DB_AUTO?.Trim().ToLower() == "true";

                if (autoSave)
                {
                    bool saved = SaveResultsToDatabase(out overallPassed);
                    if (saved)
                        Shared.logger?.Log("Results saved to database automatically", MessageType.Success);
                    else
                        Shared.logger?.Log("DB auto-save: one or more DUTs failed to save — see error(s) above", MessageType.Warning);
                }
                else
                {
                    Shared.logger?.Log("Auto-save is OFF — use 'Save to Database' menu item to save results", MessageType.Warning);
                }

                if (InvokeRequired)
                    BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        prgTestProgress.Value = 100;
                        ShowTestResult(overallPassed);
                        btnCancelTest.Enabled = false;
                        mnuNewTest.Text = "New Test";
                        if (!autoSave)
                        {
                            mnuSaveToDatabase.Visible = true;
                            mnuSaveToDatabase.Enabled = true;
                        }
                        Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                        Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                        Shared.testRun.TestTemperatureUpdate -= ClimaticChamber_Update;
                        if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                            sim.Update -= ClimaticChamber_Update;
                    });
                else
                {
                    prgTestProgress.Value = 100;
                    ShowTestResult(overallPassed);
                    btnCancelTest.Enabled = false;
                    mnuNewTest.Text = "New Test";
                    if (!autoSave)
                    {
                        mnuSaveToDatabase.Visible = true;
                        mnuSaveToDatabase.Enabled = true;
                    }
                    Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                    Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                    Shared.testRun.TestTemperatureUpdate -= ClimaticChamber_Update;
                    if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                        sim.Update -= ClimaticChamber_Update;
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Test completed handler: {ex.Message}", MessageType.Error);
            }
        }

        private void ShowTestResult(bool passed)
        {
            if (passed)
            {
                lblTestResult.Text = "\u2714  PASS";
                lblTestResult.ForeColor = Color.Green;
            }
            else
            {
                lblTestResult.Text = "\u2718  FAIL";
                lblTestResult.ForeColor = Color.Red;
            }
        }

        private void WriteExcelTestReport(bool overallPassed, DateTime runCompletedAt)
        {
            try
            {
                if (IsSimulationMode())
                {
                    int purgedRows = TestReportWorkbookWriter.PurgeSimulationRuns();
                    if (purgedRows > 0)
                        Shared.logger?.Log($"Removed {purgedRows} simulation row(s) from the Excel test report.", MessageType.Warning);

                    Shared.logger?.Log("Excel test report skipped for simulation mode.", MessageType.Warning);
                    return;
                }

                TestReportWorkbookData reportData = BuildExcelTestReportData(overallPassed, runCompletedAt);
                TestReportWorkbookWriter.AppendRunReport(reportData);
                Shared.logger?.Log($"Excel test report updated → {TestReportWorkbookWriter.WorkbookPath}", MessageType.Success);
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Excel test report update failed: {ex.Message}", MessageType.Warning);
            }
        }

        private TestReportWorkbookData BuildExcelTestReportData(bool overallPassed, DateTime runCompletedAt)
        {
            bool isSimulationMode = IsSimulationMode();
            RunMetricsModel runMetrics = Shared.currentRunMetrics ?? new RunMetricsModel();
            DateTime runStartedAt = GetCurrentRunStartTimestamp(runCompletedAt);
            string runId = "RUN-" + runStartedAt.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
            int baseUnitCount = Shared.infoModel?.Test?.BaseDUTs?.Count ?? Shared.infoModel?.NumberOfBaseUnits ?? 0;
            int remoteUnitCount = Shared.infoModel?.Test?.RemoteDUTs?.Count ?? Shared.infoModel?.NumberOfRemoteUnits ?? 0;
            DateTime? calibrationTimestamp = Shared.calibrationModel != null && Shared.calibrationModel.EffectiveTimestamp > DateTime.MinValue
                ? Shared.calibrationModel.EffectiveTimestamp
                : (DateTime?)null;
            double? calibrationAgeDays = calibrationTimestamp.HasValue
                ? Math.Round((runCompletedAt - calibrationTimestamp.Value).TotalDays, 2)
                : (double?)null;

            var entries = new List<TestReportDutEntry>();
            int totalSpecCount = 0;
            int failedSpecCount = 0;
            int passedDutCount = 0;
            int failedDutCount = 0;

            BuildExcelReportEntriesForGroup(
                entries,
                Shared.infoModel?.Test?.BaseDUTs,
                Shared.BaseResultsPath,
                "Base",
                Shared.testSpecsBase,
                (testID, files, slot) => ExtractBaseTestResults(testID, files, slot),
                runId,
                runStartedAt,
                runCompletedAt,
                overallPassed,
                isSimulationMode,
                calibrationTimestamp,
                calibrationAgeDays,
                baseUnitCount,
                remoteUnitCount,
                ref passedDutCount,
                ref failedDutCount,
                ref totalSpecCount,
                ref failedSpecCount);

            BuildExcelReportEntriesForGroup(
                entries,
                Shared.infoModel?.Test?.RemoteDUTs,
                Shared.RemoteResultsPath,
                "Remote",
                Shared.testSpecsRemote,
                (testID, files, slot) => ExtractRemoteTestResults(testID, files, slot),
                runId,
                runStartedAt,
                runCompletedAt,
                overallPassed,
                isSimulationMode,
                calibrationTimestamp,
                calibrationAgeDays,
                baseUnitCount,
                remoteUnitCount,
                ref passedDutCount,
                ref failedDutCount,
                ref totalSpecCount,
                ref failedSpecCount);

            return new TestReportWorkbookData()
            {
                Summary = new TestReportRunSummary()
                {
                    RunId = runId,
                    RunStartedAt = runStartedAt,
                    RunCompletedAt = runCompletedAt,
                    SequenceName = Shared.infoModel?.Test?.SequenceName ?? string.Empty,
                    SequenceRevision = Shared.infoModel?.Test?.Revision ?? string.Empty,
                    OperatorName = Shared.infoModel?.Operator ?? string.Empty,
                    TestRig = Environment.MachineName,
                    SoftwareVersion = Shared.SoftwareVersion ?? string.Empty,
                    IsSimulationMode = isSimulationMode,
                    BaseUnitCount = baseUnitCount,
                    RemoteUnitCount = remoteUnitCount,
                    TotalDutCount = entries.Count,
                    PassedDutCount = passedDutCount,
                    FailedDutCount = failedDutCount,
                    PassRatePercent = entries.Count > 0 ? Math.Round((double)passedDutCount * 100.0d / entries.Count, 2) : 0.0d,
                    TotalSpecCount = totalSpecCount,
                    FailedSpecCount = failedSpecCount,
                    CalibrationTimestamp = calibrationTimestamp,
                    CalibrationAgeDays = calibrationAgeDays,
                    AverageChamberTemperatureErrorC = runMetrics.AverageChamberTemperatureErrorC,
                    MaxChamberTemperatureDeviationC = runMetrics.MaxChamberTemperatureDeviationC,
                    AverageSoakSettleMinutes = runMetrics.AverageSoakSettleMinutes,
                    EquipmentRetryCount = runMetrics.EquipmentRetryCount,
                    EquipmentReconnectCount = runMetrics.EquipmentReconnectCount,
                    ForcedOperatorResumeCount = runMetrics.ForcedOperatorResumeCount,
                    PretestFailedDutCount = runMetrics.PretestFailedDutCount,
                    DuplicateScanCorrectionCount = runMetrics.DuplicateScanCorrectionCount,
                    ScanCompleteToTestStartMinutes = runMetrics.ScanCompleteToTestStartMinutes,
                    OverallPassed = overallPassed
                },
                Entries = entries
            };
        }

        private void BuildExcelReportEntriesForGroup(
            List<TestReportDutEntry> entries,
            List<DUTModel> duts,
            string resultsFolder,
            string groupName,
            List<FinalTestSpecModel> specs,
            Func<int, List<FileInfo>, int, double> extractValue,
            string runId,
            DateTime runStartedAt,
            DateTime runCompletedAt,
            bool overallPassed,
            bool isSimulationMode,
            DateTime? calibrationTimestamp,
            double? calibrationAgeDays,
            int baseUnitCount,
            int remoteUnitCount,
            ref int passedDutCount,
            ref int failedDutCount,
            ref int totalSpecCount,
            ref int failedSpecCount)
        {
            if (duts == null || duts.Count == 0)
                return;

            var di = new DirectoryInfo(resultsFolder ?? string.Empty);
            var allResults = di.Exists ? di.GetFiles().ToList() : new List<FileInfo>();

            foreach (var dut in duts)
            {
                var results = allResults.Where(file => file.Name.Contains(dut.SerialNumber)).ToList();
                List<MeasManualTestModel> manualTestModels = new List<MeasManualTestModel>();
                bool passed = isSimulationMode
                    ? !results.Any(file => file.Name.Contains("FAIL"))
                    : BuildManualTestModels(groupName, dut, results, specs, extractValue, out manualTestModels, false);

                int dutFailedSpecCount = isSimulationMode ? 0 : manualTestModels.Count(model => !model.Passed);
                int dutTotalSpecCount = isSimulationMode ? 0 : manualTestModels.Count;
                string failedSpecIds = isSimulationMode
                    ? string.Empty
                    : string.Join(",", manualTestModels
                        .Where(model => !model.Passed)
                        .Select(model => model.TestID.ToString(CultureInfo.InvariantCulture)));

                entries.Add(new TestReportDutEntry()
                {
                    RunId = runId,
                    RunStartedAt = runStartedAt,
                    RunCompletedAt = runCompletedAt,
                    SequenceName = Shared.infoModel?.Test?.SequenceName ?? string.Empty,
                    SequenceRevision = Shared.infoModel?.Test?.Revision ?? string.Empty,
                    OperatorName = Shared.infoModel?.Operator ?? string.Empty,
                    TestRig = Environment.MachineName,
                    SoftwareVersion = Shared.SoftwareVersion ?? string.Empty,
                    IsSimulationMode = isSimulationMode,
                    CalibrationTimestamp = calibrationTimestamp,
                    CalibrationAgeDays = calibrationAgeDays,
                    DutType = groupName,
                    Slot = dut.Slot,
                    SerialNumber = dut.SerialNumber,
                    Passed = passed,
                    FailedSpecCount = dutFailedSpecCount,
                    TotalSpecCount = dutTotalSpecCount,
                    FailedSpecIds = failedSpecIds,
                    ResultFileCount = results.Count,
                    OverallRunResult = overallPassed ? "PASS" : "FAIL",
                    BaseUnitCount = baseUnitCount,
                    RemoteUnitCount = remoteUnitCount
                });

                if (passed)
                    passedDutCount++;
                else
                    failedDutCount++;

                totalSpecCount += dutTotalSpecCount;
                failedSpecCount += dutFailedSpecCount;
            }
        }

        private DateTime GetCurrentRunStartTimestamp(DateTime fallback)
        {
            string dateText = Shared.infoModel?.TestDate ?? string.Empty;
            string timeText = Shared.infoModel?.TestTime ?? string.Empty;
            string combined = string.Join(" ", new[] { dateText.Trim(), timeText.Trim() }.Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(combined)
                && DateTime.TryParseExact(
                    combined,
                    new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd H:mm:ss" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private bool SaveResultsToDatabase(out bool overallPassed)
        {
            bool allSaved = true;
            bool isSimulationMode = IsSimulationMode();
            overallPassed = true;

            // ── Base DUTs ──────────────────────────────────────────────────────
            if (Shared.infoModel.Test.BaseDUTs?.Count > 0)
            {
                var diBase = new DirectoryInfo(Shared.BaseResultsPath);
                var allBaseResults = diBase.Exists ? diBase.GetFiles().ToList() : new System.Collections.Generic.List<FileInfo>();
                foreach (var dut in Shared.infoModel.Test.BaseDUTs)
                {
                    string serialNo = dut.SerialNumber;
                    var results = allBaseResults.Where(a => a.Name.Contains(serialNo)).ToList();
                    bool specPassed = BuildManualTestModels(
                        "Base",
                        dut,
                        results,
                        Shared.testSpecsBase,
                        (testID, files, slot) => ExtractBaseTestResults(testID, files, slot),
                        out System.Collections.Generic.List<MeasManualTestModel> manualTestModels);

                    bool passed = isSimulationMode
                        ? !results.Any(r => r.Name.Contains("FAIL"))
                        : specPassed;
                    overallPassed &= passed;

                    var measMainModel = new MeasMainModel()
                    {
                        SerialNo = serialNo,
                        TestType = DuplexerTestTypes.A,
                        Operator = Shared.infoModel.Operator,
                        TestDate = Shared.infoModel.TestDate,
                        TestTime = Shared.infoModel.TestTime,
                        TestRig = Environment.MachineName,
                        SoftwareRev = Shared.SoftwareVersion,
                        ItemNo = Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER,
                        ItemNoRev = 0,
                        Passed = passed
                    };

                    if (!Shared.productionDatabase.SaveTestResultsWithHistory(measMainModel, manualTestModels))
                        allSaved = false;

                    ArchiveResultFiles(Path.Combine(Shared.BaseResultsPath, "Archive"), results, passed, isSimulationMode);
                }
            }

            // ── Remote DUTs ────────────────────────────────────────────────────
            if (Shared.infoModel.Test.RemoteDUTs?.Count > 0)
            {
                var diRemote = new DirectoryInfo(Shared.RemoteResultsPath);
                var allRemoteResults = diRemote.Exists ? diRemote.GetFiles().ToList() : new System.Collections.Generic.List<FileInfo>();
                foreach (var dut in Shared.infoModel.Test.RemoteDUTs)
                {
                    string serialNo = dut.SerialNumber;
                    var results = allRemoteResults.Where(a => a.Name.Contains(serialNo)).ToList();
                    bool specPassed = BuildManualTestModels(
                        "Remote",
                        dut,
                        results,
                        Shared.testSpecsRemote,
                        (testID, files, slot) => ExtractRemoteTestResults(testID, files, slot),
                        out System.Collections.Generic.List<MeasManualTestModel> manualTestModels);

                    bool passed = isSimulationMode
                        ? !results.Any(r => r.Name.Contains("FAIL"))
                        : specPassed;
                    overallPassed &= passed;

                    var measMainModel = new MeasMainModel()
                    {
                        SerialNo = serialNo,
                        TestType = DuplexerTestTypes.A,
                        Operator = Shared.infoModel.Operator,
                        TestDate = Shared.infoModel.TestDate,
                        TestTime = Shared.infoModel.TestTime,
                        TestRig = Environment.MachineName,
                        SoftwareRev = Shared.SoftwareVersion,
                        ItemNo = Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER,
                        ItemNoRev = 0,
                        Passed = passed
                    };

                    if (!Shared.productionDatabase.SaveTestResultsWithHistory(measMainModel, manualTestModels))
                        allSaved = false;

                    ArchiveResultFiles(Path.Combine(Shared.RemoteResultsPath, "Archive"), results, passed, isSimulationMode);
                }
            }
            return allSaved;
        }

        private void MnuSaveToDatabase_Click(object sender, EventArgs e)
        {
            try
            {
                mnuSaveToDatabase.Enabled = false;
                bool saved = SaveResultsToDatabase(out bool overallPassed);
                if (saved)
                    Shared.logger?.Log("Results saved to database manually", MessageType.Success);
                else
                    Shared.logger?.Log("DB manual save: one or more DUTs failed to save — see error(s) above", MessageType.Warning);
                mnuSaveToDatabase.Visible = false;
                ShowTestResult(overallPassed);
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Manual DB save failed: {ex.Message}", MessageType.Error);
                mnuSaveToDatabase.Enabled = true;
            }
        }

        #endregion

        #region Result Extraction

        /// <summary>
        /// Evaluates pass/fail against limits, matching the reference project logic:
        ///   min!=0 &amp;&amp; max!=0 → value must be between min and max
        ///   min==0 &amp;&amp; max!=0 → value must be &lt;= max (no lower bound)
        ///   min!=0 &amp;&amp; max==0 → value must be &gt;= min (no upper bound)
        ///   min==0 &amp;&amp; max==0 → no validation, always pass
        /// </summary>
        private static bool EvaluateLimit(double value, double limitMin, double limitMax)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return false;

            if (limitMin != 0 && limitMax != 0)
                return value >= limitMin && value <= limitMax;
            if (limitMin == 0 && limitMax != 0)
                return value >= 0 && value <= limitMax;
            if (limitMin != 0 && limitMax == 0)
                return value >= limitMin;
            return true; // both 0 → no validation
        }

        private bool EvaluateRunOverallPassFail(bool updateLiveResultFileNames = false)
        {
            try
            {
                if (IsSimulationMode())
                    return EvaluateRunOverallPassFailFromResultFiles();

                bool overallPassed = true;

                if (Shared.infoModel.Test.BaseDUTs?.Count > 0)
                {
                    var diBase = new DirectoryInfo(Shared.BaseResultsPath);
                    var allBaseResults = diBase.Exists ? diBase.GetFiles().ToList() : new System.Collections.Generic.List<FileInfo>();
                    foreach (var dut in Shared.infoModel.Test.BaseDUTs)
                    {
                        var results = allBaseResults.Where(file => file.Name.Contains(dut.SerialNumber)).ToList();
                        bool dutPassed = BuildManualTestModels(
                            "Base",
                            dut,
                            results,
                            Shared.testSpecsBase,
                            (testID, files, slot) => ExtractBaseTestResults(testID, files, slot),
                            out _);
                        if (updateLiveResultFileNames)
                            UpdateLiveResultFiles(results, dutPassed, false);
                        overallPassed &= dutPassed;
                    }
                }

                if (Shared.infoModel.Test.RemoteDUTs?.Count > 0)
                {
                    var diRemote = new DirectoryInfo(Shared.RemoteResultsPath);
                    var allRemoteResults = diRemote.Exists ? diRemote.GetFiles().ToList() : new System.Collections.Generic.List<FileInfo>();
                    foreach (var dut in Shared.infoModel.Test.RemoteDUTs)
                    {
                        var results = allRemoteResults.Where(file => file.Name.Contains(dut.SerialNumber)).ToList();
                        bool dutPassed = BuildManualTestModels(
                            "Remote",
                            dut,
                            results,
                            Shared.testSpecsRemote,
                            (testID, files, slot) => ExtractRemoteTestResults(testID, files, slot),
                            out _);
                        if (updateLiveResultFileNames)
                            UpdateLiveResultFiles(results, dutPassed, false);
                        overallPassed &= dutPassed;
                    }
                }

                return overallPassed;
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Spec-based pass/fail evaluation failed: {ex.Message}", MessageType.Error);
                return false;
            }
        }

        private bool EvaluateRunOverallPassFailFromResultFiles()
        {
            bool overallPassed = true;
            try
            {
                if (Directory.Exists(Shared.BaseResultsPath))
                    overallPassed &= !Directory.GetFiles(Shared.BaseResultsPath)
                        .Any(f => Path.GetFileName(f).Contains("FAIL"));
                if (Directory.Exists(Shared.RemoteResultsPath))
                    overallPassed &= !Directory.GetFiles(Shared.RemoteResultsPath)
                        .Any(f => Path.GetFileName(f).Contains("FAIL"));
            }
            catch { }

            return overallPassed;
        }

        private bool BuildManualTestModels(
            string groupName,
            DUTModel dut,
            System.Collections.Generic.List<FileInfo> resultFiles,
            System.Collections.Generic.List<FinalTestSpecModel> specs,
            Func<int, System.Collections.Generic.List<FileInfo>, int, double> extractValue,
            out System.Collections.Generic.List<MeasManualTestModel> manualTestModels,
            bool logDetails = true)
        {
            manualTestModels = new System.Collections.Generic.List<MeasManualTestModel>();

            if (resultFiles == null || resultFiles.Count == 0)
            {
                if (logDetails)
                    Shared.logger?.Log($"{groupName} DUT {dut?.SerialNumber}: no result files were found for spec evaluation.", MessageType.Warning);
                return false;
            }

            if (specs == null || specs.Count == 0)
            {
                if (logDetails)
                    Shared.logger?.Log($"{groupName} DUT {dut?.SerialNumber}: no database specs are loaded, so pass/fail cannot be evaluated.", MessageType.Warning);
                return false;
            }

            bool passed = true;
            if (logDetails)
                Shared.logger?.Log($"{groupName} specs for {dut.SerialNumber}: {specs.Count} specs, {resultFiles.Count} result files");

            foreach (var spec in specs)
            {
                double testData = double.NaN;
                bool rowPassed = false;

                try
                {
                    testData = extractValue(spec.TestID, resultFiles, dut.Slot);
                    rowPassed = EvaluateLimit(testData, spec.LimitMin, spec.LimitMax);
                }
                catch (Exception ex)
                {
                    if (logDetails)
                        Shared.logger?.Log($"Extract{groupName}TestResults TestID={spec.TestID}: {ex.Message}", MessageType.Warning);
                }

                if (logDetails)
                {
                    Shared.logger?.Log(
                        $"  {groupName} TestID={spec.TestID} value={(double.IsNaN(testData) || double.IsInfinity(testData) ? "NaN" : testData.ToString("G6"))} pass={rowPassed} [{spec.LimitMin}..{spec.LimitMax}]");
                }

                manualTestModels.Add(new MeasManualTestModel()
                {
                    TestID = spec.TestID,
                    TestData = testData,
                    Passed = rowPassed
                });

                passed &= rowPassed;
            }

            return passed;
        }

        private void ArchiveResultFiles(string archiveFolder, System.Collections.Generic.List<FileInfo> resultFiles, bool passed, bool isSimulationMode)
        {
            Directory.CreateDirectory(archiveFolder);

            foreach (var result in resultFiles)
            {
                string archivedFileName = BuildResultFileNameWithSpecOutcome(result.Name, passed, isSimulationMode);
                File.Move(result.FullName, Path.Combine(archiveFolder, archivedFileName), true);
            }
        }

        private void UpdateLiveResultFiles(System.Collections.Generic.List<FileInfo> resultFiles, bool passed, bool isSimulationMode)
        {
            foreach (var result in resultFiles)
            {
                string updatedFileName = BuildResultFileNameWithSpecOutcome(result.Name, passed, isSimulationMode);
                if (string.Equals(updatedFileName, result.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                string directoryPath = result.DirectoryName ?? Path.GetDirectoryName(result.FullName) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(directoryPath))
                    continue;

                File.Move(result.FullName, Path.Combine(directoryPath, updatedFileName), true);
            }
        }

        private string BuildResultFileNameWithSpecOutcome(string originalFileName, bool passed, bool isSimulationMode)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                return originalFileName;

            if (isSimulationMode || passed || originalFileName.Contains("_FAILED"))
                return originalFileName;

            string extension = Path.GetExtension(originalFileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            return $"{fileNameWithoutExtension}_FAILED{extension}";
        }

        private bool IsSimulationMode()
        {
            return Shared.sharedGeneralSettings?.GeneralSettings[0]
                .USE_SIMULATORS?.Trim().ToLower() == "true";
        }

        private double ExtractBaseZIpdValue(List<System.IO.FileInfo> resultFiles, double minTemp, double maxTemp, int sweepNo, int slot)
        {
            string resultFile = Shared.FindFileByTemperature(resultFiles, "Z_IPD", minTemp, maxTemp, sweepNo);
            double calibrationValue = Shared.GetCalibrationValueForResultFile(resultFile, TestSequences.Base_Z_IPD, slot);
            return Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", minTemp, maxTemp, 1, calibrationValue, 4, sweepNo) * 1000.0d;
        }

        private double ExtractRemoteZVpvValue(List<System.IO.FileInfo> resultFiles, double minTemp, double maxTemp, int sweepNo, int slot, bool returnCurrent)
        {
            const double loadRes = 13000.0d;

            string resultFile = Shared.FindFileByTemperature(resultFiles, "Z_VPV", minTemp, maxTemp, sweepNo);
            double calibrationValue = Shared.GetCalibrationValueForResultFile(resultFile, TestSequences.Remote_Z_VPV, slot);
            double vpv = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", minTemp, maxTemp, sweepNo, 1, calibrationValue, 3);
            if (returnCurrent)
                return vpv / loadRes * 100000.0d;

            return (vpv * vpv) / loadRes * 10000.0d;
        }

        private double ExtractBaseTestResults(int testID, List<System.IO.FileInfo> resultFiles, int slot)
        {
            try
            {
                double value = 0;
                switch (testID)
                {
                    case 1620:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1639:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1664:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1665:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1666:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1667:
                        { value = ExtractBaseZIpdValue(resultFiles, -60.5, -49.5, 2, slot); break; }
                    case 1668:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1669:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 2); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1670:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1671:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1672:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045); break; }
                    case 1673:
                        { value = ExtractBaseZIpdValue(resultFiles, 76.5, 93.5, 3, slot); break; }
                    case 1674:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1675:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 3); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1828:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1829:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1830:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1831:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1832:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 1833:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1834:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1835:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1836:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1837:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 1838:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1839:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1840:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1841:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1842:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 1843:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1844:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1845:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1846:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1847:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 1848:
                        { value = ExtractBaseZIpdValue(resultFiles, -60.5, -49.5, 4, slot); break; }
                    case 1849:
                        { value = ExtractBaseZIpdValue(resultFiles, 76.5, 93.5, 5, slot); break; }
                    case 1850:
                        { value = ExtractBaseZIpdValue(resultFiles, -60.5, -49.5, 6, slot); break; }
                    case 1851:
                        { value = ExtractBaseZIpdValue(resultFiles, 76.5, 93.5, 7, slot); break; }
                    case 1852:
                        { value = ExtractBaseZIpdValue(resultFiles, 22.5, 27.5, 8, slot); break; }
                    case 1853:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1854:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1855:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1856:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1857:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 1858:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 4); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1859:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 5); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1860:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -60.5, -49.5, 6); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1861:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 76.5, 93.5, 7); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 1862:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", 22.5, 27.5, 8); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                    case 2189:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.035); break; }
                    case 2190:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH1 Voltage(V)", 0.045); break; }
                    case 2191:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.035) * 1000000.0d; break; }
                    case 2192:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH2 Current(A)", 0.045) * 1000000.0d; break; }
                    case 2193:
                        { value = ExtractBaseZIpdValue(resultFiles, -27.5, -22.5, 1, slot); break; }
                    case 2194:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.035) * 1000.0d; break; }
                    case 2195:
                        { var f = Shared.FindFileByTemperature(resultFiles, "Z_IB_IOP", -27.5, -22.5, 1); value = Shared.FindReadingByClosestValue(f, "CH1 Current(A)", "CH4 Power(W)", 0.045) * 1000.0d; break; }
                }
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Extract base test results (testID: {testID}): {ex.Message}");
            }
        }

        private double ExtractRemoteTestResults(int testID, List<System.IO.FileInfo> resultFiles, int slot)
        {
            try
            {
                double value = 0;
                switch (testID)
                {
                    case 1648: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 2, slot, true); break; }
                    case 1649: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 2, slot, false); break; }
                    case 1654: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 3, slot, true); break; }
                    case 1655: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 3, slot, false); break; }
                    case 1751: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.002, 0); break; }
                    case 1752: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.003, 0); break; }
                    case 1753: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.002, 0); break; }
                    case 1754: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.003, 0); break; }
                    case 1771: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1772: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1773: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1774: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1790: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 4, slot, false); break; }
                    case 1791: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 5, slot, false); break; }
                    case 1792: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 6, slot, false); break; }
                    case 1793: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 7, slot, false); break; }
                    case 1794: { value = ExtractRemoteZVpvValue(resultFiles, 22.5, 27.5, 8, slot, false); break; }
                    case 1795: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 4, slot, true); break; }
                    case 1796: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 5, slot, true); break; }
                    case 1797: { value = ExtractRemoteZVpvValue(resultFiles, -60.5, -49.5, 6, slot, true); break; }
                    case 1798: { value = ExtractRemoteZVpvValue(resultFiles, 76.5, 93.5, 7, slot, true); break; }
                    case 1799: { value = ExtractRemoteZVpvValue(resultFiles, 22.5, 27.5, 8, slot, true); break; }
                    case 1800: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 4, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1801: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 5, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1802: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 6, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1803: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 7, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1804: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 22.5, 27.5, 8, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1805: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 4, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1806: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 5, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1807: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 6, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1808: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 7, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1809: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 22.5, 27.5, 8, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1810: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 4, 1, 0.002, 0); break; }
                    case 1811: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 5, 1, 0.002, 0); break; }
                    case 1812: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 6, 1, 0.002, 0); break; }
                    case 1813: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 7, 1, 0.002, 0); break; }
                    case 1814: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 22.5, 27.5, 8, 1, 0.002, 0); break; }
                    case 1815: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 4, 1, 0.003, 0); break; }
                    case 1816: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 5, 1, 0.003, 0); break; }
                    case 1817: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 6, 1, 0.003, 0); break; }
                    case 1818: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 7, 1, 0.003, 0); break; }
                    case 1819: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 22.5, 27.5, 8, 1, 0.003, 0); break; }
                    case 1821: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", -60.5, -49.5); break; }
                    case 1822: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", 76.5, 93.5); break; }
                    case 1823: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", -60.5, -49.5); break; }
                    case 1824: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", 76.5, 93.5); break; }
                    case 1825: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", -60.5, -49.5); break; }
                    case 1826: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", 76.5, 93.5); break; }
                    case 1827: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", 22.5, 27.5); break; }
                    case 2221: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.002, 0); break; }
                    case 2222: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.003, 0); break; }
                    case 2223: { value = ExtractRemoteZVpvValue(resultFiles, -27.5, -22.5, 1, slot, false); break; }
                    case 2224: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.002, 6) * 1000000.0d; break; }
                    case 2225: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.003, 6) * 1000000.0d; break; }
                    case 2226: { value = ExtractRemoteZVpvValue(resultFiles, -27.5, -22.5, 1, slot, true); break; }
                    case 2227: { value = ExtractThresholdCurrent(resultFiles, "Z_IOP", -27.5, -22.5); break; }
                }
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Extract remote test results (testID: {testID}): {ex.Message}");
            }
        }

        private double ExtractThresholdCurrent(List<System.IO.FileInfo> resultFiles, string fileType, double tempMin, double tempMax)
        {
            var resultFile = resultFiles.First(r =>
                r.Name.Contains(fileType) &&
                double.Parse(r.Name.Split('_').First(c => c.Contains("C")).Replace("C", string.Empty),
                    NumberStyles.Float, CultureInfo.InvariantCulture) >= tempMin &&
                double.Parse(r.Name.Split('_').First(c => c.Contains("C")).Replace("C", string.Empty),
                    NumberStyles.Float, CultureInfo.InvariantCulture) <= tempMax);
            var allLines = File.ReadAllLines(resultFile.FullName).ToList();
            allLines.RemoveAt(0); // skip header
            var masterPoints = new List<System.Drawing.PointF>();
            foreach (var line in allLines)
            {
                var cols = line.Split(',');
                masterPoints.Add(new System.Drawing.PointF(
                    float.Parse(cols[1], NumberStyles.Float, CultureInfo.InvariantCulture) * 1000,
                    float.Parse(cols[6], NumberStyles.Float, CultureInfo.InvariantCulture) * 1000));
            }
            var smoothedData = new List<System.Drawing.PointF>();
            for (int i = 1; i < masterPoints.Count; i++)
                smoothedData.Add(new System.Drawing.PointF(masterPoints[i].X, masterPoints[i].Y - masterPoints[i - 1].Y));
            var result = smoothedData.Where(p => Math.Abs(p.Y / 0.002) > 1).ToList();
            return result.First(p => p.Y > 0.002).X * 1000.0d;
        }

        #endregion
    }
}
