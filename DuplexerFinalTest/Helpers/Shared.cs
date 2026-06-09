using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.EquipmentSim;
using DuplexerFinalTest.Models;
using DuplexerFinalTest.Tests;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DuplexerFinalTest.Helpers
{
    public static class Shared
    {
        // Equipment (via interfaces)
        public static IOpticalSwitch OpticalSwitch1x4 { get; set; }
        public static IOpticalSwitch OpticalSwitch1x13_Base { get; set; }
        public static IOpticalSwitch OpticalSwitch1x13_Remote { get; set; }
        public static IElectricalSwitch ElectricalSwitchBase1 { get; set; }
        public static IElectricalSwitch ElectricalSwitchBase2 { get; set; }
        public static IElectricalSwitch ElectricalSwitchBase3 { get; set; }
        public static IElectricalSwitch ElectricalSwitchRemote1 { get; set; }
        public static IElectricalSwitch ElectricalSwitchRemote2 { get; set; }
        public static IElectricalSwitch ElectricalSwitchRemote3 { get; set; }
        public static ISMU SMU_master { get; set; }
        public static ISMU SMU_slave { get; set; }
        public static IClimaticChamber ClimaticChamber { get; set; }

        // Application services
        public static ProductionDatabase productionDatabase { get; set; } = new ProductionDatabase();
        public static Logger logger { get; set; }
        public static TestRun testRun { get; set; } = new TestRun();
        public static GeneralSettingsModel sharedGeneralSettings { get; set; }
        public static InfoModel infoModel { get; set; } = new InfoModel();
        public static RunMetricsModel currentRunMetrics { get; set; } = new RunMetricsModel();
        public static TestResultSaver testResultSaver { get; set; } = new TestResultSaver();
        public static Tests.Pretest pretest { get; set; } = new Tests.Pretest();

        // Form references
        public static MainForm mainForm { get; set; }
        public static StartForm startForm { get; set; }
        // Do NOT instantiate UI forms during static initialization — create on demand.
        public static SettingsForm settingsForm { get; set; }

        // Images
        public static Image ConnectedImage { get; set; }
        public static Image DisconnectedImage { get; set; }
        public static Image PassImage { get; set; }
        public static Image FailImage { get; set; }

        // Test flow models
        public static TestFlowModel Base_Z_IB_IOP { get; set; }
        public static TestFlowModel Base_Z_IPD { get; set; }
        public static TestFlowModel Remote_Z_IOP { get; set; }
        public static TestFlowModel Remote_Z_IPV { get; set; }
        public static TestFlowModel Remote_Z_VPV { get; set; }

        // Sequences and specs
        public static List<TestSequenceModel> AllAvailableTestSequences { get; set; } = new List<TestSequenceModel>();
        public static List<FinalTestSpecModel> testSpecsBase { get; set; }
        public static List<FinalTestSpecModel> testSpecsRemote { get; set; }

        // Calibration
        public static CalibrationModel calibrationModel { get; set; }

        // Paths
        public static string GeneralSettingsPath { get; set; }
        public static string BaseResultsPath { get; set; }
        public static string RemoteResultsPath { get; set; }
        public static string loggingPath { get; set; }
        public static string SoftwareVersion { get; set; }

        // Timer
        public static System.Diagnostics.Stopwatch testTimer { get; set; }

        // Current DUT serial for simulator offset assignment
        public static string CurrentSimPartSerial { get; set; }

        public static void InitializeEquipment(GeneralSettingsModel settings)
        {
            if (settings?.GeneralSettings == null || settings.GeneralSettings.Count == 0) return;
            var s = settings.GeneralSettings[0];
            bool useSim = s.USE_SIMULATORS?.Trim().ToLower() == "true";

            if (useSim)
            {
                OpticalSwitch1x4 = new OpticalSwitchSim();
                OpticalSwitch1x13_Base = new OpticalSwitchSim();
                OpticalSwitch1x13_Remote = new OpticalSwitchSim();
                ElectricalSwitchBase1 = new ElectricalSwitchSim();
                ElectricalSwitchBase2 = new ElectricalSwitchSim();
                ElectricalSwitchBase3 = new ElectricalSwitchSim();
                ElectricalSwitchRemote1 = new ElectricalSwitchSim();
                ElectricalSwitchRemote2 = new ElectricalSwitchSim();
                ElectricalSwitchRemote3 = new ElectricalSwitchSim();
                SMU_master = new SMUSim();
                SMU_slave = new SMUSim();
                ClimaticChamber = new ClimaticChamberSim();
            }
            else
            {
                OpticalSwitch1x4 = new Equipment.OpticalSwitch();
                OpticalSwitch1x13_Base = new Equipment.OpticalSwitch();
                OpticalSwitch1x13_Remote = new Equipment.OpticalSwitch();
                ElectricalSwitchBase1 = new Equipment.ElectricalSwitch();
                ElectricalSwitchBase2 = new Equipment.ElectricalSwitch();
                ElectricalSwitchBase3 = new Equipment.ElectricalSwitch();
                ElectricalSwitchRemote1 = new Equipment.ElectricalSwitch();
                ElectricalSwitchRemote2 = new Equipment.ElectricalSwitch();
                ElectricalSwitchRemote3 = new Equipment.ElectricalSwitch();
                SMU_master = new Equipment.SMU();
                SMU_slave = new Equipment.SMU();
                ClimaticChamber = new Equipment.ClimaticChamber();
            }
        }

        public static void CheckEquipmentConnections(
            Panel pnlOptical1x4, Panel pnlOptical1x13Base, Panel pnlOptical1x13Remote,
            Panel pnlElecBase1, Panel pnlElecBase2, Panel pnlElecBase3,
            Panel pnlElecRemote1, Panel pnlElecRemote2, Panel pnlElecRemote3,
            Panel pnlSMUMaster, Panel pnlSMUSlave, Panel pnlChamber,
            Panel pnlDB)
        {
            if (sharedGeneralSettings?.GeneralSettings == null || sharedGeneralSettings.GeneralSettings.Count == 0) return;
            var s = sharedGeneralSettings.GeneralSettings[0];
            bool useSim = s.USE_SIMULATORS?.Trim().ToLower() == "true";
            bool useLocalDB = s.USE_LOCAL_DATABASE?.Trim().ToLower() == "true";

            System.Threading.Tasks.Task.Run(() =>
            {
                void SetPanel(Panel p, bool ok)
                {
                    if (p == null) return;
                    p.Invoke((Action)(() => p.BackgroundImage = ok ? ConnectedImage : DisconnectedImage));
                }

                bool ok;
                if (useSim)
                {
                    SetPanel(pnlOptical1x4, OpticalSwitch1x4.Connect("SIM"));
                    SetPanel(pnlOptical1x13Base, OpticalSwitch1x13_Base.Connect("SIM"));
                    SetPanel(pnlOptical1x13Remote, OpticalSwitch1x13_Remote.Connect("SIM"));
                    SetPanel(pnlElecBase1, ElectricalSwitchBase1.Connect("SIM"));
                    SetPanel(pnlElecBase2, ElectricalSwitchBase2.Connect("SIM"));
                    SetPanel(pnlElecBase3, ElectricalSwitchBase3.Connect("SIM"));
                    SetPanel(pnlElecRemote1, ElectricalSwitchRemote1.Connect("SIM"));
                    SetPanel(pnlElecRemote2, ElectricalSwitchRemote2.Connect("SIM"));
                    SetPanel(pnlElecRemote3, ElectricalSwitchRemote3.Connect("SIM"));
                    SetPanel(pnlSMUMaster, SMU_master.Connect("SIM"));
                    SetPanel(pnlSMUSlave, SMU_slave.Connect("SIM"));
                    int.TryParse(s.CLIMATIC_CHAMBER_PORT, out int port);
                    SetPanel(pnlChamber, ClimaticChamber.Connect(s.CLIMATIC_CHAMBER_IP_ADDRESS ?? "127.0.0.1", port > 0 ? port : 5000));
                    ApplyChamberProtectionLimits(s);
                }
                else
                {
                    SetPanel(pnlOptical1x4, OpticalSwitch1x4.Connect(s.OPTICAL_SWITCH1x4_1_RESOURCE));
                    SetPanel(pnlOptical1x13Base, OpticalSwitch1x13_Base.Connect(s.OPTICAL_SWITCH1x13_1_RESOURCE));
                    SetPanel(pnlOptical1x13Remote, OpticalSwitch1x13_Remote.Connect(s.OPTICAL_SWITCH1x13_2_RESOURCE));
                    SetPanel(pnlElecBase1, ElectricalSwitchBase1.Connect(s.ELECTRICAL_SWITCH1_RESOURCE));
                    SetPanel(pnlElecBase2, ElectricalSwitchBase2.Connect(s.ELECTRICAL_SWITCH2_RESOURCE));
                    SetPanel(pnlElecBase3, ElectricalSwitchBase3.Connect(s.ELECTRICAL_SWITCH3_RESOURCE));
                    SetPanel(pnlElecRemote1, ElectricalSwitchRemote1.Connect(s.ELECTRICAL_SWITCH4_RESOURCE));
                    SetPanel(pnlElecRemote2, ElectricalSwitchRemote2.Connect(s.ELECTRICAL_SWITCH5_RESOURCE));
                    SetPanel(pnlElecRemote3, ElectricalSwitchRemote3.Connect(s.ELECTRICAL_SWITCH6_RESOURCE));
                    SetPanel(pnlSMUMaster, SMU_master.Connect(s.SMU_MASTER_RESOURCE));
                    SetPanel(pnlSMUSlave, SMU_slave.Connect(s.SMU_SLAVE_RESOURCE));
                    int.TryParse(s.CLIMATIC_CHAMBER_PORT, out int chamberPort);
                    SetPanel(pnlChamber, ClimaticChamber.Connect(s.CLIMATIC_CHAMBER_IP_ADDRESS, chamberPort > 0 ? chamberPort : 5000));
                    ApplyChamberProtectionLimits(s);
                }

                // DB connection panel
                bool dbOk = false;
                try { productionDatabase?.ConnectToServer(); dbOk = true; logger?.Log("Database connected", MessageType.Success); }
                catch (Exception ex) { logger?.Log($"Database connection failed: {ex.Message}", MessageType.Error); }
                SetPanel(pnlDB, dbOk);

                // Load test specs once DB is connected
                if (dbOk)
                {
                    try
                    {
                        testSpecsBase   = productionDatabase.GetFinalTestSpecs(DUTType.Base);
                        testSpecsRemote = productionDatabase.GetFinalTestSpecs(DUTType.Remote);
                        logger?.Log($"Test specs loaded: Base={testSpecsBase?.Count ?? 0} specs, Remote={testSpecsRemote?.Count ?? 0} specs", MessageType.Success);
                    }
                    catch (Exception ex) { logger?.Log($"Get test specs failed: {ex.Message}", MessageType.Error); }
                }
            });
        }

        public static int ReconnectDisconnectedEquipment()
        {
            var s = sharedGeneralSettings?.GeneralSettings?.FirstOrDefault();
            if (s == null || string.Equals(s.USE_SIMULATORS?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                return 0;

            int reconnectCount = 0;
            reconnectCount += TryReconnect(OpticalSwitch1x4, s.OPTICAL_SWITCH1x4_1_RESOURCE);
            reconnectCount += TryReconnect(OpticalSwitch1x13_Base, s.OPTICAL_SWITCH1x13_1_RESOURCE);
            reconnectCount += TryReconnect(OpticalSwitch1x13_Remote, s.OPTICAL_SWITCH1x13_2_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchBase1, s.ELECTRICAL_SWITCH1_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchBase2, s.ELECTRICAL_SWITCH2_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchBase3, s.ELECTRICAL_SWITCH3_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchRemote1, s.ELECTRICAL_SWITCH4_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchRemote2, s.ELECTRICAL_SWITCH5_RESOURCE);
            reconnectCount += TryReconnect(ElectricalSwitchRemote3, s.ELECTRICAL_SWITCH6_RESOURCE);
            reconnectCount += TryReconnect(SMU_master, s.SMU_MASTER_RESOURCE);
            reconnectCount += TryReconnect(SMU_slave, s.SMU_SLAVE_RESOURCE);

            int.TryParse(s.CLIMATIC_CHAMBER_PORT, out int chamberPort);
            reconnectCount += TryReconnect(ClimaticChamber, s.CLIMATIC_CHAMBER_IP_ADDRESS, chamberPort > 0 ? chamberPort : 5000, s);

            return reconnectCount;
        }

        public static TestFlowModel ParseTestFlow(string fileName)
        {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<TestFlowModel>(json);
        }

        private static int TryReconnect(IOpticalSwitch equipment, string resource)
        {
            if (equipment == null || equipment.IsConnected || string.IsNullOrWhiteSpace(resource))
                return 0;

            return equipment.Connect(resource) ? 1 : 0;
        }

        private static int TryReconnect(IElectricalSwitch equipment, string resource)
        {
            if (equipment == null || equipment.IsConnected || string.IsNullOrWhiteSpace(resource))
                return 0;

            return equipment.Connect(resource) ? 1 : 0;
        }

        private static int TryReconnect(ISMU equipment, string resource)
        {
            if (equipment == null || equipment.IsConnected || string.IsNullOrWhiteSpace(resource))
                return 0;

            return equipment.Connect(resource) ? 1 : 0;
        }

        private static int TryReconnect(IClimaticChamber equipment, string ipAddress, int port, GeneralSetting settings)
        {
            if (equipment == null || equipment.IsConnected || string.IsNullOrWhiteSpace(ipAddress))
                return 0;

            bool connected = equipment.Connect(ipAddress, port);
            if (connected && settings != null)
                ApplyChamberProtectionLimits(settings);

            return connected ? 1 : 0;
        }

        public static TestSequenceModel ParseTestSequence(string fileName)
        {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<TestSequenceModel>(json);
        }

        // Reads CHAMBER_SAFE_MAX/MIN_TEMP and CHAMBER_HWPROT_MARGIN_C from settings,
        // then programs the chamber controller's firmware temperature protection limits
        // via SetTemperatureProtection(high, low).  Called once right after Connect.
        // Hardware high = safeMax + margin,  hardware low = safeMin - margin.
        private static void ApplyChamberProtectionLimits(Models.GeneralSetting s)
        {
            if (ClimaticChamber == null || !ClimaticChamber.IsConnected) return;
            try
            {
                double safeMax = 100.0;
                double safeMin = -70.0;
                double margin  = 5.0;
                if (double.TryParse(s.CHAMBER_SAFE_MAX_TEMP, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsedMax)) safeMax = parsedMax;
                if (double.TryParse(s.CHAMBER_SAFE_MIN_TEMP, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsedMin)) safeMin = parsedMin;
                if (double.TryParse(s.CHAMBER_HWPROT_MARGIN_C, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsedMargin)) margin = parsedMargin;

                double hwHigh = safeMax + margin;
                double hwLow  = safeMin - margin;
                ClimaticChamber.SetTemperatureProtection(hwHigh, hwLow);
                logger?.Log(
                    $"Chamber HW protection limits set: High={hwHigh:F1}\u00b0C, Low={hwLow:F1}\u00b0C " +
                    $"(software safety: [{safeMin:F1}, {safeMax:F1}]\u00b0C, margin: \u00b1{margin:F1}\u00b0C)",
                    MessageType.Message);
            }
            catch (Exception ex)
            {
                logger?.Log($"Chamber HW protection limits: failed to set \u2014 {ex.Message}", MessageType.Warning);
            }
        }

        public static int GetThermistorChannel(DUTType dutType, int slot)
        {
            return 200 + slot;
        }

        public static int GetThermistorChannel(int slot)
        {
            return 200 + slot;
        }

        public static void LoadCalibration(string resourcesFolder)
        {
            try
            {
                calibrationModel = LoadLatestCalibrationModel(null, resourcesFolder);
            }
            catch
            {
                calibrationModel = CreateDefaultCalibration();
            }
        }

        public static string GetCalibrationRootPath(string preferredRoot = null)
        {
            var candidateRoots = new List<string>();

            if (!string.IsNullOrWhiteSpace(preferredRoot))
                candidateRoots.Add(preferredRoot);

            string resultsRoot = sharedGeneralSettings?.GeneralSettings?[0]?.RESULTS_FOLDER;
            if (!string.IsNullOrWhiteSpace(resultsRoot))
            {
                string suiteRoot = Path.GetDirectoryName(resultsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(suiteRoot))
                    candidateRoots.Add(Path.Combine(suiteRoot, "Calibration"));
            }

            string resourcesRoot = sharedGeneralSettings?.GeneralSettings?[0]?.RESOURCES_FOLDER;
            if (!string.IsNullOrWhiteSpace(resourcesRoot))
            {
                string suiteRoot = Path.GetDirectoryName(resourcesRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(suiteRoot))
                    candidateRoots.Add(Path.Combine(suiteRoot, "Calibration"));
            }

            candidateRoots.Add(@"P:\MGunes\DuplexerTestSuite\Calibration");
            candidateRoots.Add(@"P:\MGunes\Duplexer\Calibration");

            foreach (string candidateRoot in candidateRoots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Directory.Exists(candidateRoot))
                    return candidateRoot;
            }

            return candidateRoots.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        }

        public static CalibrationModel LoadLatestCalibrationModel(DateTime? beforeTimestamp = null, string preferredRoot = null)
        {
            string calibrationRoot = GetCalibrationRootPath(preferredRoot);
            if (string.IsNullOrWhiteSpace(calibrationRoot) || !Directory.Exists(calibrationRoot))
                throw new DirectoryNotFoundException("Calibration folder was not found.");

            List<CalibrationCandidate> candidates = EnumerateCalibrationCandidates(calibrationRoot);
            if (candidates.Count == 0)
                throw new FileNotFoundException($"No calibration files were found under '{calibrationRoot}'.");

            CalibrationCandidate selectedCandidate;
            if (beforeTimestamp.HasValue)
            {
                selectedCandidate = candidates
                    .Where(candidate => candidate.Timestamp <= beforeTimestamp.Value)
                    .OrderByDescending(candidate => candidate.Timestamp)
                    .FirstOrDefault();

                if (selectedCandidate == null)
                {
                    throw new FileNotFoundException(
                        $"No calibration file was found earlier than {beforeTimestamp.Value:yyyy-MM-dd HH:mm:ss} in '{calibrationRoot}'.");
                }
            }
            else
            {
                selectedCandidate = candidates
                    .OrderByDescending(candidate => candidate.Timestamp)
                    .First();
            }

            calibrationModel = ParseCalibrationFile(selectedCandidate.FilePath, selectedCandidate.Timestamp);
            return calibrationModel;
        }

        public static double GetCalibrationValueForResultFile(string resultFilePath, TestSequences testSequence, int slot)
        {
            if (string.IsNullOrWhiteSpace(resultFilePath))
                throw new ArgumentException("Result file path is required.", nameof(resultFilePath));

            DateTime resultTimestamp = TryParseResultTimestamp(resultFilePath, out DateTime parsedTimestamp)
                ? parsedTimestamp
                : File.GetLastWriteTime(resultFilePath);

            CalibrationModel model = LoadLatestCalibrationModel(resultTimestamp);
            double calibrationValue = model.GetValue(testSequence, slot);
            if (double.IsNaN(calibrationValue))
            {
                throw new InvalidOperationException(
                    $"Calibration value is missing for {testSequence} slot {slot} in '{model.SourcePath}'.");
            }

            return calibrationValue;
        }

        private static CalibrationModel CreateDefaultCalibration()
        {
            var model = new CalibrationModel();
            for (int i = 1; i <= 24; i++)
            {
                model.Z_IB_IOP[$"path{i}"] = double.NaN;
                model.Z_IPD[$"path{i}"] = double.NaN;
                model.Z_IOP[$"path{i}"] = double.NaN;
                model.Z_VPV[$"path{i}"] = double.NaN;
            }
            return model;
        }

        private static List<CalibrationCandidate> EnumerateCalibrationCandidates(string calibrationRoot)
        {
            var candidates = new List<CalibrationCandidate>();

            foreach (string calibrationFile in Directory.GetFiles(calibrationRoot, "*.cal", SearchOption.TopDirectoryOnly))
            {
                if (TryParseCalibrationTimestamp(calibrationFile, out DateTime timestamp))
                    candidates.Add(new CalibrationCandidate(calibrationFile, timestamp));
            }

            foreach (string calibrationDirectory in Directory.GetDirectories(calibrationRoot))
            {
                foreach (string calibrationFile in Directory.GetFiles(calibrationDirectory, "*.cal", SearchOption.TopDirectoryOnly))
                {
                    if (TryParseCalibrationTimestamp(calibrationFile, out DateTime timestamp))
                        candidates.Add(new CalibrationCandidate(calibrationFile, timestamp));
                }
            }

            return candidates;
        }

        private static CalibrationModel ParseCalibrationFile(string calibrationFilePath, DateTime effectiveTimestamp)
        {
            CalibrationModel model = CreateDefaultCalibration();
            model.EffectiveTimestamp = effectiveTimestamp;
            model.SourcePath = calibrationFilePath;

            string currentSection = string.Empty;
            foreach (string rawLine in File.ReadAllLines(calibrationFilePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("!") || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0 || string.IsNullOrWhiteSpace(currentSection))
                    continue;

                string key = line.Substring(0, equalsIndex).Trim();
                string valueText = line.Substring(equalsIndex + 1).Trim().Trim('"');
                if (!double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    continue;

                Dictionary<string, double> section = GetCalibrationSection(model, currentSection);
                if (section != null)
                    section[key] = value;
            }

            return model;
        }

        private static Dictionary<string, double> GetCalibrationSection(CalibrationModel model, string sectionName)
        {
            switch (sectionName.ToUpperInvariant())
            {
                case "Z-IB-IOP":
                    return model.Z_IB_IOP;
                case "Z-IPD":
                    return model.Z_IPD;
                case "Z-IOP":
                    return model.Z_IOP;
                case "Z-VPV":
                    return model.Z_VPV;
                default:
                    return null;
            }
        }

        private static bool TryParseCalibrationTimestamp(string calibrationPath, out DateTime timestamp)
        {
            string[] candidates =
            {
                Path.GetFileNameWithoutExtension(calibrationPath),
                Path.GetFileName(Path.GetDirectoryName(calibrationPath) ?? string.Empty)
            };

            foreach (string candidate in candidates)
            {
                if (DateTime.TryParseExact(candidate, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out timestamp))
                {
                    return true;
                }
            }

            timestamp = DateTime.MinValue;
            return false;
        }

        private static bool TryParseResultTimestamp(string resultFilePath, out DateTime timestamp)
        {
            string fileName = Path.GetFileNameWithoutExtension(resultFilePath);
            Match match = Regex.Match(fileName, @"_(\d{8})_(\d{6})(?:_FAILED)?$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string token = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
                if (DateTime.TryParseExact(token, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out timestamp))
                {
                    return true;
                }
            }

            timestamp = DateTime.MinValue;
            return false;
        }

        private sealed class CalibrationCandidate
        {
            public CalibrationCandidate(string filePath, DateTime timestamp)
            {
                FilePath = filePath;
                Timestamp = timestamp;
            }

            public string FilePath { get; }
            public DateTime Timestamp { get; }
        }

        // ── Result file helpers (verbatim logic from reference) ──────────────

        public static string FindFileByTemperature(System.Collections.Generic.List<System.IO.FileInfo> files, string keyword, double minTemp, double maxTemp, int sweepNo)
        {
            foreach (var fi in files)
            {
                string file = fi.FullName;
                string name = Path.GetFileName(file);
                var match = Regex.Match(name, $@"{keyword}_(-?\d+(\.\d+)?)C");
                if (!match.Success) continue;
                if (!double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double temp)) continue;
                if (temp < minTemp || temp > maxTemp) continue;
                if (!name.Contains($"sweep_{sweepNo}_")) continue;
                return file;
            }
            return null;
        }


        public static double FindReadingByClosestValue(string csvFile, int searchColumn, int readColumn, double targetCurrent_mA)
        {
            if (string.IsNullOrEmpty(csvFile) || !File.Exists(csvFile)) return double.NaN;
            double targetA = targetCurrent_mA / 1000.0;
            double closestVal = double.NaN;
            double minDiff = double.MaxValue;
            bool firstLine = true;
            foreach (var line in File.ReadAllLines(csvFile))
            {
                if (firstLine) { firstLine = false; continue; }  // skip header
                var parts = line.Split(',');
                if (parts.Length <= Math.Max(searchColumn, readColumn)) continue;
                if (!double.TryParse(parts[searchColumn].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double searchVal)) continue;
                double diff = Math.Abs(searchVal - targetA);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    if (double.TryParse(parts[readColumn].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double readVal))
                        closestVal = readVal;
                }
            }
            return closestVal;
        }

        public static double FindReadingByClosestValue(string csvFile, string searchColumnName, string readColumnName, double targetCurrent_mA)
        {
            if (string.IsNullOrEmpty(csvFile) || !File.Exists(csvFile)) return double.NaN;
            var lines = File.ReadAllLines(csvFile);
            if (lines.Length < 2) return double.NaN;
            var headers = lines[0].Split(',');
            int searchColumn = Array.IndexOf(headers.Select(h => h.Trim()).ToArray(), searchColumnName.Trim());
            int readColumn = Array.IndexOf(headers.Select(h => h.Trim()).ToArray(), readColumnName.Trim());
            if (searchColumn < 0 || readColumn < 0) return double.NaN;
            double targetA = targetCurrent_mA / 1000.0;
            double closestVal = double.NaN;
            double minDiff = double.MaxValue;
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length <= Math.Max(searchColumn, readColumn)) continue;
                if (!double.TryParse(parts[searchColumn].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double searchVal)) continue;
                double diff = Math.Abs(searchVal - targetA);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    if (double.TryParse(parts[readColumn].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double readVal))
                        closestVal = readVal;
                }
            }
            return closestVal;
        }

        public static double Extract_Z_IPD_Value(System.Collections.Generic.List<System.IO.FileInfo> files, string keyword, double minTemp, double maxTemp,
            int compareColIdx, double calValue, int resultColIdx, int sweepNo)
        {
            string file = FindFileByTemperature(files, keyword, minTemp, maxTemp, sweepNo);
            if (string.IsNullOrEmpty(file)) return double.NaN;
            double closestVal = double.NaN;
            double minDiff = double.MaxValue;
            bool firstLine = true;
            foreach (var line in File.ReadAllLines(file))
            {
                if (firstLine) { firstLine = false; continue; }
                var parts = line.Split(',');
                if (parts.Length <= Math.Max(compareColIdx, resultColIdx)) continue;
                if (!double.TryParse(parts[compareColIdx].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double cmpVal)) continue;
                double diff = Math.Abs(cmpVal - calValue);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    if (double.TryParse(parts[resultColIdx].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double resVal))
                        closestVal = resVal;
                }
            }
            return closestVal;
        }

        public static double ExtractRemoteVPV(System.Collections.Generic.List<System.IO.FileInfo> files, string keyword, double minTemp, double maxTemp,
            int sweepNo, int compareColIdx, double calValue, int voltageColIdx)
        {
            return Extract_Z_IPD_Value(files, keyword, minTemp, maxTemp, compareColIdx, calValue, voltageColIdx, sweepNo);
        }

        public static double ExtractRemoteZIOP(System.Collections.Generic.List<System.IO.FileInfo> files, string keyword, double minTemp, double maxTemp,
            int sweepNo, int compareColIdx, double compareValue, int resultColIdx)
        {
            string file = FindFileByTemperature(files, keyword, minTemp, maxTemp, sweepNo);
            if (string.IsNullOrEmpty(file)) return double.NaN;
            double closestVal = double.NaN;
            double minDiff = double.MaxValue;
            bool firstLine = true;
            foreach (var line in File.ReadAllLines(file))
            {
                if (firstLine) { firstLine = false; continue; }
                var parts = line.Split(',');
                if (parts.Length <= Math.Max(compareColIdx, resultColIdx)) continue;
                if (!double.TryParse(parts[compareColIdx].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double cmpVal)) continue;
                double diff = Math.Abs(cmpVal - compareValue);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    if (double.TryParse(parts[resultColIdx].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double resVal))
                        closestVal = resVal;
                }
            }
            return closestVal;
        }

        public static void MirrorResultsToLegacy()
        {
            try
            {
                var s = sharedGeneralSettings?.GeneralSettings?.FirstOrDefault();
                if (s == null) return;
                var legacyRoot = s.LEGACY_RESULTS_ROOT?.Trim();
                if (string.IsNullOrEmpty(legacyRoot)) return;

                // Ensure root exists
                Directory.CreateDirectory(legacyRoot);

                var baseSrc = BaseResultsPath;
                var remoteSrc = RemoteResultsPath;

                var summaryLines = new List<string>();
                summaryLines.Add("Serial,Type,RelativePath,FileName,FullPath,Timestamp");

                void ProcessSource(string src, string type)
                {
                    if (string.IsNullOrWhiteSpace(src) || !Directory.Exists(src)) return;
                    var files = Directory.GetFiles(src, "*.csv", SearchOption.TopDirectoryOnly);
                    foreach (var f in files)
                    {
                        var filename = Path.GetFileName(f);
                        if (string.IsNullOrWhiteSpace(filename)) continue;
                        var parts = filename.Split('_');
                        var serial = parts.Length > 0 ? parts[0] : "UNKNOWN";
                        var destDir = Path.Combine(legacyRoot, type, serial);
                        Directory.CreateDirectory(destDir);
                        var destPath = Path.Combine(destDir, filename);
                        try { File.Copy(f, destPath, true); }
                        catch (Exception) { continue; }
                        var rel = Path.Combine(type, serial, filename);
                        summaryLines.Add($"{serial},{type},{rel},{filename},{destPath},{File.GetCreationTime(f).ToString("o")}");
                    }
                }

                ProcessSource(baseSrc, "Base");
                ProcessSource(remoteSrc, "Remote");

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var summaryFile = Path.Combine(legacyRoot, $"ZODIAC-TS_{timestamp}.csv");
                File.WriteAllLines(summaryFile, summaryLines);

                logger?.Log($"Mirrored results to legacy root: {legacyRoot}", MessageType.Success);
            }
            catch (Exception ex)
            {
                logger?.Log($"MirrorResultsToLegacy failed: {ex.Message}", MessageType.Warning);
            }
        }
    }
}
