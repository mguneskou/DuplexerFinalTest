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
            // Initialise logger
            try
            {
                Shared.loggingPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    @"DuplexerFinalTest\logs");
                Directory.CreateDirectory(Shared.loggingPath);
                Shared.logger = new Logger(Path.Combine(Shared.loggingPath,
                    $"log-{DateTime.Now:dd_MM_yyyy-HH_mm_ss}.txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Initialise message viewer
            try
            {
                Shared.InitialiseMessageViewer(tlpBottom);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Read general settings
            try
            {
                Shared.GeneralSettingsPath = Path.Combine(
                    Application.StartupPath, "Resources", "Settings", "SettingsGeneral.json");
                Shared.sharedGeneralSettings = Shared.settingsForm.ReadGeneralSettings();
                Shared.InitializeEquipment(Shared.sharedGeneralSettings);
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
                Shared.logger?.Log($"Image load failed: {ex.Message}");
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
                Shared.messageViewer?.AddNewMessage($"Cannot connect equipment: {ex.Message}", MessageType.Error);
            }

            // Database connection and test specs are handled by CheckEquipmentConnections (background task)

            waitForm.Close();
            waitForm.Dispose();
            waitForm = null;
            this.Enabled = true;

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
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Form closing error: {ex.Message}");
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
                Shared.messageViewer?.AddNewMessage($"Update equipment status: {ex.Message}", MessageType.Error);
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

                            // Subscribe climatic chamber update for chart
                            if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                                sim.Update += ClimaticChamber_Update;

                            Shared.testRun.StartTest(Shared.infoModel.Test);
                            Shared.testTimer = new System.Diagnostics.Stopwatch();
                            Shared.testTimer.Start();
                            timerElapsed.Start();
                        }
                    }
                }
                else
                {
                    mnuNewTest.Text = "New Test";
                    timerElapsed.Stop();
                    Shared.testTimer?.Stop();
                    Shared.testRun.StopTest();
                    // Unsubscribe
                    Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                    Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                    if (Shared.ClimaticChamber is ClimaticChamberSim simStop)
                        simStop.Update -= ClimaticChamber_Update;
                }
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"New test menu item: {ex.Message}", MessageType.Error);
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
                Shared.messageViewer?.AddNewMessage($"Cannot load settings form: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuViewLogFiles_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Shared.loggingPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"View log files: {ex.Message}", MessageType.Error);
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
                Shared.messageViewer?.AddNewMessage($"Test procedure: {ex.Message}", MessageType.Error);
            }
        }

        private void MnuHelp_Click(object sender, EventArgs e)
        {
            try
            {
                var dir = Path.Combine(Application.StartupPath, "Resources", "Help");
                if (Directory.Exists(dir))
                {
                    var helpFile = Directory.GetFiles(dir).ToList().Find(r => r.EndsWith(".pdf"));
                    if (helpFile != null)
                        Process.Start(new ProcessStartInfo { FileName = helpFile, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"Help: {ex.Message}", MessageType.Error);
            }
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
                Shared.messageViewer?.AddNewMessage($"Chamber update: {ex.Message}", MessageType.Error);
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
                Shared.messageViewer?.AddNewMessageThreadSafe($"Test update: {ex.Message}", MessageType.Error);
            }
        }

        private void UpdateTestProgress(string updateMessage, int progressPercentage)
        {
            var parts = updateMessage.Split('|');
            var step = parts.Length > 0 ? parts[0].Trim() : updateMessage;
            var status = parts.Length > 1 ? parts[1].Trim() : "";
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

                // Evaluate base results
                var diBase = new DirectoryInfo(Shared.BaseResultsPath);
                var allBaseResults = diBase.GetFiles().ToList();
                foreach (var serialNo in Shared.infoModel.Test.BaseDUTs.Select(d => d.SerialNumber))
                {
                    var results = allBaseResults.Where(a => a.Name.Contains(serialNo)).ToList();
                    int finalIncrement = 0;
                    var deviceCode = $"M{serialNo}A";
                    var passed = !results.Any(r => r.Name.Contains("FAIL"));

                    if (Shared.productionDatabase.IsExistingRecord(deviceCode, out finalIncrement))
                        Shared.productionDatabase.UpdateExistingRecord(deviceCode, finalIncrement);

                    var testTables = Shared.productionDatabase.GetTestDataTables(
                        serialNo, Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER,
                        new List<int>() { 1 }, out int specRevision);

                    var measMainModel = new MeasMainModel()
                    {
                        DeviceCode = deviceCode,
                        SerialNo = serialNo,
                        TestType = DuplexerTestTypes.A,
                        Operator = Shared.infoModel.Operator,
                        TestDate = Shared.infoModel.TestDate,
                        TestTime = Shared.infoModel.TestTime,
                        TestRig = "Manual",
                        SoftwareRev = Shared.SoftwareVersion,
                        ItemNo = Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER,
                        ItemNoRev = specRevision,
                        Passed = passed
                    };
                    Shared.productionDatabase.InsertIntoMeasMain(measMainModel);

                    if (testTables != null && testTables.Count > 0)
                    {
                        DataTable dtManualTest = testTables[0].Copy();
                        for (int i = 0; i < dtManualTest.Rows.Count; i++)
                        {
                            var testIDStr = dtManualTest.Rows[i].ItemArray[0]?.ToString();
                            var testID = !string.IsNullOrEmpty(testIDStr) ? int.Parse(testIDStr) : 0;
                            var testData = ExtractBaseTestResults(testID, results);
                            Shared.productionDatabase.InsertIntoMeasManualTest(new MeasManualTestModel()
                            {
                                DeviceCode = deviceCode,
                                TestID = testID,
                                TestData = testData,
                                Passed = passed
                            });
                        }
                    }
                    foreach (var result in results)
                    {
                        File.Move(result.FullName,
                            Path.Combine(Shared.BaseResultsPath, "Archive", result.Name));
                    }
                }

                // Evaluate remote results
                var diRemote = new DirectoryInfo(Shared.RemoteResultsPath);
                var allRemoteResults = diRemote.GetFiles().ToList();
                foreach (var serialNo in Shared.infoModel.Test.RemoteDUTs.Select(d => d.SerialNumber))
                {
                    var results = allRemoteResults.Where(a => a.Name.Contains(serialNo)).ToList();
                    int finalIncrement = 0;
                    var deviceCode = $"M{serialNo}A";
                    var passed = !results.Any(r => r.Name.Contains("FAIL"));

                    if (Shared.productionDatabase.IsExistingRecord(deviceCode, out finalIncrement))
                        Shared.productionDatabase.UpdateExistingRecord(deviceCode, finalIncrement);

                    var testTables = Shared.productionDatabase.GetTestDataTables(
                        serialNo, Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER,
                        new List<int>() { 1 }, out int specRevision);

                    var measMainModel = new MeasMainModel()
                    {
                        DeviceCode = deviceCode,
                        SerialNo = serialNo,
                        TestType = DuplexerTestTypes.A,
                        Operator = Shared.infoModel.Operator,
                        TestDate = Shared.infoModel.TestDate,
                        TestTime = Shared.infoModel.TestTime,
                        TestRig = "Manual",
                        SoftwareRev = Shared.SoftwareVersion,
                        ItemNo = Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER,
                        ItemNoRev = specRevision,
                        Passed = passed
                    };
                    Shared.productionDatabase.InsertIntoMeasMain(measMainModel);

                    if (testTables != null && testTables.Count > 0)
                    {
                        DataTable dtManualTest = testTables[0].Copy();
                        for (int i = 0; i < dtManualTest.Rows.Count; i++)
                        {
                            var testIDStr = dtManualTest.Rows[i].ItemArray[0]?.ToString();
                            var testID = !string.IsNullOrEmpty(testIDStr) ? int.Parse(testIDStr) : 0;
                            var testData = ExtractRemoteTestResults(testID, results);
                            Shared.productionDatabase.InsertIntoMeasManualTest(new MeasManualTestModel()
                            {
                                DeviceCode = deviceCode,
                                TestID = testID,
                                TestData = testData,
                                Passed = passed
                            });
                        }
                    }
                    foreach (var result in results)
                    {
                        File.Move(result.FullName,
                            Path.Combine(Shared.RemoteResultsPath, "Archive", result.Name));
                    }
                }

                if (InvokeRequired)
                    BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        prgTestProgress.Value = 100;
                        mnuNewTest.Text = "New Test";
                        Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                        Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                        if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                            sim.Update -= ClimaticChamber_Update;
                    });
                else
                {
                    prgTestProgress.Value = 100;
                    mnuNewTest.Text = "New Test";
                    Shared.testRun.TestUpdate -= TestRun_TestUpdate;
                    Shared.testRun.TestCompleted -= TestRun_TestCompleted;
                    if (Shared.ClimaticChamber is ClimaticChamberSim sim)
                        sim.Update -= ClimaticChamber_Update;
                }
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessageThreadSafe($"Test completed: {ex.Message}", MessageType.Error);
            }
        }

        #endregion

        #region Result Extraction

        private double ExtractBaseTestResults(int testID, List<System.IO.FileInfo> resultFiles)
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
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", -60.5, -49.5, 1, 0.002, 4, 2) * 1000.0d; break; }
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
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", 76.5, 93.5, 1, 0.002, 4, 3) * 1000.0d; break; }
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
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", -60.5, -49.5, 1, 0.002, 4, 4) * 1000.0d; break; }
                    case 1849:
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", 76.5, 93.5, 1, 0.002, 4, 5) * 1000.0d; break; }
                    case 1850:
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", -60.5, -49.5, 1, 0.002, 4, 6) * 1000.0d; break; }
                    case 1851:
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", 76.5, 93.5, 1, 0.002, 4, 7) * 1000.0d; break; }
                    case 1852:
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", 22.5, 27.5, 1, 0.002, 4, 8) * 1000.0d; break; }
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
                        { value = Shared.Extract_Z_IPD_Value(resultFiles, "Z_IPD", -27.5, -22.5, 1, 0.002, 4, 1) * 1000.0d; break; }
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

        private double ExtractRemoteTestResults(int testID, List<System.IO.FileInfo> resultFiles)
        {
            try
            {
                const double loadRes = 13000;
                const double calValue = 0.03d;
                double value = 0;
                switch (testID)
                {
                    case 1648: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 2, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1649: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 2, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1654: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 3, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1655: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 3, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1751: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.002, 0); break; }
                    case 1752: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.003, 0); break; }
                    case 1753: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.002, 0); break; }
                    case 1754: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.003, 0); break; }
                    case 1771: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1772: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -60.5, -49.5, 2, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1773: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.002, 6) * 1000000.0d; break; }
                    case 1774: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", 76.5, 93.5, 3, 1, 0.003, 6) * 1000000.0d; break; }
                    case 1790: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 4, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1791: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 5, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1792: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 6, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1793: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 7, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1794: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 22.5, 27.5, 8, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 1795: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 4, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1796: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 5, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1797: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -60.5, -49.5, 6, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1798: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 76.5, 93.5, 7, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
                    case 1799: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", 22.5, 27.5, 8, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
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
                    case 2223: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -27.5, -22.5, 1, 1, calValue, 3); value = (VPV * VPV) / loadRes * 10000; break; }
                    case 2224: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.002, 6) * 1000000.0d; break; }
                    case 2225: { value = Shared.ExtractRemoteZIOP(resultFiles, "Z_IOP", -27.5, -22.5, 1, 1, 0.003, 6) * 1000000.0d; break; }
                    case 2226: { var VPV = Shared.ExtractRemoteVPV(resultFiles, "Z_VPV", -27.5, -22.5, 1, 1, calValue, 3); value = VPV / loadRes * 100000; break; }
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
