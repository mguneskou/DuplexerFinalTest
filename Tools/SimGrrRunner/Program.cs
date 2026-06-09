using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using DuplexerFinalTest.Tests;
using Newtonsoft.Json;

namespace SimGrrRunner
{
    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Loading general settings...");
                string settingsPath = Path.GetFullPath("D:\\VSCodeRepo\\DuplexerFinalTest\\DuplexerFinalTest\\Resources\\Settings\\SettingsGeneral.json");
                if (!File.Exists(settingsPath))
                {
                    Console.Error.WriteLine($"Settings file not found: {settingsPath}");
                    return 2;
                }

                var gsJson = File.ReadAllText(settingsPath);
                var generalSettings = JsonConvert.DeserializeObject<GeneralSettingsModel>(gsJson);
                Shared.sharedGeneralSettings = generalSettings;

                // Initialize simulators or real equipment based on settings
                Shared.InitializeEquipment(generalSettings);

                var s = generalSettings.GeneralSettings[0];
                string resultsRoot = s.RESULTS_FOLDER ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuplexerResults");
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dataRoot = Path.Combine(resultsRoot, "GaugeRR_SimRun_" + stamp + "_Data");
                Directory.CreateDirectory(dataRoot);

                // Load test flows from resources folder if present
                string resourcesFolder = s.RESOURCES_FOLDER;
                if (!string.IsNullOrWhiteSpace(resourcesFolder))
                {
                    string tf = Path.Combine(resourcesFolder, "TestFlows");
                    try
                    {
                        Shared.Base_Z_IB_IOP = Shared.ParseTestFlow(Path.Combine(tf, "Base_Z_IB_IOP.json"));
                        Shared.Base_Z_IPD = Shared.ParseTestFlow(Path.Combine(tf, "Base_Z_IPD.json"));
                        Shared.Remote_Z_IOP = Shared.ParseTestFlow(Path.Combine(tf, "Remote_Z_IOP.json"));
                        Shared.Remote_Z_IPV = Shared.ParseTestFlow(Path.Combine(tf, "Remote_Z_IPV.json"));
                        Shared.Remote_Z_VPV = Shared.ParseTestFlow(Path.Combine(tf, "Remote_Z_VPV.json"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: failed to load test flows: " + ex.Message);
                    }
                }

                // Build GRR-like study parameters
                int baseParts = 3;
                int remoteParts = 3;
                var basePartsList = new List<DUTModel>();
                var remotePartsList = new List<DUTModel>();
                for (int i = 0; i < baseParts; i++)
                    basePartsList.Add(new DUTModel { SerialNumber = $"SIMB{i+1:000}", DUTType = DUTType.Base, Slot = i+1, ReadyToTest = true });
                for (int i = 0; i < remoteParts; i++)
                    remotePartsList.Add(new DUTModel { SerialNumber = $"SIMR{i+1:000}", DUTType = DUTType.Remote, Slot = i+1, ReadyToTest = true });

                var operators = new List<string> { "OpA", "OpB", "OpC" };
                int replicates = 2;

                // Create a runtime sequence containing parts
                var runtimeSequence = new TestSequenceModel
                {
                    SequenceName = "Simulated-GRR",
                    BaseDUTs = basePartsList.Select(p => p.Clone()).ToList(),
                    RemoteDUTs = remotePartsList.Select(p => p.Clone()).ToList()
                };

                // Run steps: for each replicate and operator
                int stepIndex = 0;
                foreach (int replicate in Enumerable.Range(1, replicates))
                {
                    foreach (int opIndex in Enumerable.Range(0, operators.Count))
                    {
                        string operatorFolder = $"{opIndex + 1:00}_{Sanitize(operators[opIndex])}";
                        string stepFolder = Path.Combine(dataRoot, operatorFolder, $"Replicate_{replicate:00}");
                        string baseFolder = Path.Combine(stepFolder, "Base");
                        string remoteFolder = Path.Combine(stepFolder, "Remote");
                        Directory.CreateDirectory(baseFolder);
                        Directory.CreateDirectory(remoteFolder);

                        // Set Shared result paths for this step
                        string origBase = Shared.BaseResultsPath;
                        string origRemote = Shared.RemoteResultsPath;
                        Shared.BaseResultsPath = baseFolder;
                        Shared.RemoteResultsPath = remoteFolder;

                        Console.WriteLine($"Running step {stepIndex}: Operator={operators[opIndex]}, Replicate={replicate}");
                        // Run Base plans
                        var bw = new System.ComponentModel.BackgroundWorker { WorkerReportsProgress = true };
                        if (runtimeSequence.BaseDUTs.Count > 0 && Shared.Base_Z_IB_IOP != null)
                        {
                            var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
                            bool cancelled;
                            IndividualTestRun.RunBase_Z_IB_IOP(runtimeSequence, tr, bw, stepIndex, replicate, 25.0, out cancelled);
                        }

                        if (runtimeSequence.BaseDUTs.Count > 0 && Shared.Base_Z_IPD != null)
                        {
                            var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
                            bool cancelled;
                            IndividualTestRun.RunBase_Z_IPD(runtimeSequence, tr, bw, stepIndex, replicate, 25.0, out cancelled);
                        }

                        // Run Remote plans
                        if (runtimeSequence.RemoteDUTs.Count > 0 && Shared.Remote_Z_IOP != null)
                        {
                            var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
                            bool cancelled;
                            IndividualTestRun.RunRemote_Z_IOP(runtimeSequence, tr, bw, stepIndex, replicate, 25.0, out cancelled);
                        }

                        if (runtimeSequence.RemoteDUTs.Count > 0 && Shared.Remote_Z_IPV != null)
                        {
                            var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
                            bool cancelled;
                            IndividualTestRun.RunRemote_Z_IPV(runtimeSequence, tr, bw, stepIndex, replicate, 25.0, out cancelled);
                        }

                        if (runtimeSequence.RemoteDUTs.Count > 0 && Shared.Remote_Z_VPV != null)
                        {
                            var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
                            bool cancelled;
                            IndividualTestRun.RunRemote_Z_VPV(runtimeSequence, tr, bw, stepIndex, replicate, 25.0, out cancelled);
                        }

                        // Restore
                        Shared.BaseResultsPath = origBase;
                        Shared.RemoteResultsPath = origRemote;

                        stepIndex++;
                    }
                }

                Console.WriteLine("Capture complete. Scanning generated CSVs and computing per-part means...");
                var csvResults = ScanCsvMeans(dataRoot);
                string outCsv = Path.Combine(Directory.GetCurrentDirectory(), "gauge_rr_csv_check_sim.csv");
                using (var sw = new StreamWriter(outCsv))
                {
                    sw.WriteLine("PartSerial,Group,NumFiles,MeanOfMeans,VarianceBetweenParts");
                    foreach (var g in csvResults.GroupBy(x => x.Group))
                    {
                        var groups = g.GroupBy(x => x.PartSerial).Select(pg => new { Part = pg.Key, Mean = pg.Average(x => x.FileMean), Count = pg.Count() }).ToList();
                        double variance = Variance(groups.Select(x => x.Mean).ToList());
                        foreach (var row in groups)
                            sw.WriteLine($"{row.Part},{g.Key},{row.Count},{row.Mean.ToString(CultureInfo.InvariantCulture)},{variance.ToString(CultureInfo.InvariantCulture)}");
                    }
                }

                Console.WriteLine($"Finished. Summary written to {outCsv}");
                // Now invoke the app-native DiagnosticForm analysis to produce the full ANOVA+Range report
                try
                {
                    Console.WriteLine("Invoking DiagnosticForm analysis to write official HTML/CSV report...");
                    InvokeDiagnosticReport(dataRoot, Path.GetDirectoryName(dataRoot), runtimeSequence, operators, replicates);
                    Console.WriteLine("DiagnosticForm report generation completed.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("DiagnosticForm invocation failed: " + ex.Message);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        private class CsvMeanRow { public string PartSerial; public string Group; public double FileMean; }

        private static List<CsvMeanRow> ScanCsvMeans(string dataRoot)
        {
            var results = new List<CsvMeanRow>();
            foreach (string csv in Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories))
            {
                try
                {
                    string[] lines = File.ReadAllLines(csv);
                    if (lines.Length < 2) continue;
                    var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
                    int idx = Array.FindIndex(headers, h => string.Equals(h, "CH1 Voltage(V)", StringComparison.OrdinalIgnoreCase));
                    if (idx < 0) continue;
                    double sum = 0; int count = 0;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var cols = lines[i].Split(',');
                        if (idx >= cols.Length) continue;
                        if (double.TryParse(cols[idx].Trim(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) { sum += v; count++; }
                    }
                    if (count == 0) continue;
                    double mean = sum / count;
                    // Extract serial from filename pattern: SERIAL_TestType_... or SERIAL_TestSequences...
                    string fileName = Path.GetFileNameWithoutExtension(csv);
                    string partSerial = fileName.Split('_')[0];
                    // determine group by folder name (Base/Remote)
                    string group = Path.GetDirectoryName(csv).Split(Path.DirectorySeparatorChar).LastOrDefault() ?? "";
                    results.Add(new CsvMeanRow { PartSerial = partSerial, Group = group, FileMean = mean });
                }
                catch { }
            }
            return results;
        }

        private static double Variance(List<double> values)
        {
            if (values == null || values.Count <= 1) return 0.0;
            double avg = values.Average();
            double sumsq = values.Sum(v => (v - avg) * (v - avg));
            return sumsq / (values.Count - 1);
        }

        // --- GRR Analysis and report generation (ANOVA + Range) ----------------
        private class GrrStudy
        {
            public string StudyName;
            public string OutputFolder;
            public string DataFolderRoot;
            public List<string> Operators = new List<string>();
            public int Replicates;
            public List<GrrPlan> Plans = new List<GrrPlan>();
            public List<GrrObservation> Observations = new List<GrrObservation>();
        }

        private class GrrPlan
        {
            public string GroupName;
            public DuplexerFinalTest.Helpers.DUTType DutType;
            public string ColumnName;
            public double Tolerance;
            public List<string> Parts = new List<string>();
        }

        private class GrrObservation
        {
            public string GroupName;
            public string ColumnName;
            public string OperatorName;
            public int ReplicateNo;
            public string PartSerial;
            public double Value;
            public string CsvPath;
        }

        private class GrrGroupResult
        {
            public GrrPlan Plan;
            public List<GrrObservation> Observations = new List<GrrObservation>();
            public MethodResult Anova;
            public MethodResult Range;
        }

        private class MethodResult
        {
            public string MethodName;
            public double EVVariance;
            public double AVVariance;
            public double InteractionVariance;
            public double ReproducibilityVariance;
            public double PartVariance;
            public double GrrVariance;
            public double TotalVariance;
            public double EVPctStudyVar;
            public double AVPctStudyVar;
            public double ReproPctStudyVar;
            public double PartPctStudyVar;
            public double GrrPctStudyVar;
            public double EVPctTolerance;
            public double AVPctTolerance;
            public double ReproPctTolerance;
            public double PartPctTolerance;
            public double GrrPctTolerance;
            public double Ndc;
            public string Verdict;
            public string Notes;
            public List<ComponentRow> Components = new List<ComponentRow>();
        }

        private class ComponentRow { public string Name; public double Variance; public double StandardDeviation; public double StudyVariation; public double PercentStudyVariation; public double PercentTolerance; }

        private static void InvokeDiagnosticReport(string dataRoot, string outputFolder, TestSequenceModel sequence, List<string> operators, int replicates)
        {
            // Use reflection to create a DiagnosticForm, populate its private GaugeRrStudyDefinition, then call AnalyzeGrrStudy and WriteGrrReports
            var diagType = typeof(DuplexerFinalTest.DiagnosticForm);
            var diag = Activator.CreateInstance(diagType);

            var nestedStudyType = diagType.GetNestedType("GaugeRrStudyDefinition", System.Reflection.BindingFlags.NonPublic);
            var nestedPlanType = diagType.GetNestedType("GaugeRrMeasurePlan", System.Reflection.BindingFlags.NonPublic);
            var nestedObsType = diagType.GetNestedType("GaugeRrObservation", System.Reflection.BindingFlags.NonPublic);

            var study = Activator.CreateInstance(nestedStudyType);
            nestedStudyType.GetProperty("StudyName").SetValue(study, "SimRun");
            nestedStudyType.GetProperty("CreatedAt").SetValue(study, DateTime.Now);
            nestedStudyType.GetProperty("SequenceName").SetValue(study, sequence.SequenceName ?? "SimSequence");
            nestedStudyType.GetProperty("Sequence").SetValue(study, sequence);
            nestedStudyType.GetProperty("Replicates").SetValue(study, replicates);
            nestedStudyType.GetProperty("OutputFolder").SetValue(study, outputFolder);
            nestedStudyType.GetProperty("DataFolderRoot").SetValue(study, dataRoot);

            // Operators list
            var opsList = Activator.CreateInstance(typeof(List<string>));
            var opsAdd = opsList.GetType().GetMethod("Add");
            foreach (var op in operators) opsAdd.Invoke(opsList, new object[] { op });
            nestedStudyType.GetProperty("Operators").SetValue(study, opsList);

            // Build plans (Base and Remote) using nested plan type
            var plansList = Activator.CreateInstance(typeof(List<>).MakeGenericType(nestedPlanType));
            var plansAdd = plansList.GetType().GetMethod("Add");

            string[] groups = new[] { "Base", "Remote" };
            foreach (var grp in groups)
            {
                // create plan
                var plan = Activator.CreateInstance(nestedPlanType);
                nestedPlanType.GetProperty("GroupName").SetValue(plan, grp);
                nestedPlanType.GetProperty("ColumnName").SetValue(plan, "CH1 Voltage(V)");
                nestedPlanType.GetProperty("Tolerance").SetValue(plan, 0.0);
                // TestType: infer from group
                var testTypeProp = nestedPlanType.GetProperty("TestType");
                var dutTypeProp = nestedPlanType.GetProperty("DutType");
                if (string.Equals(grp, "Base", StringComparison.OrdinalIgnoreCase))
                {
                    // Base -> Base_Z_IB_IOP
                    testTypeProp.SetValue(plan, DuplexerFinalTest.Helpers.TestSequences.Base_Z_IB_IOP);
                    dutTypeProp.SetValue(plan, DuplexerFinalTest.Helpers.DUTType.Base);
                    // parts
                    var parts = new List<DuplexerFinalTest.Models.DUTModel>();
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMB001" });
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMB002" });
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMB003" });
                    nestedPlanType.GetProperty("Parts").SetValue(plan, parts);
                }
                else
                {
                    testTypeProp.SetValue(plan, DuplexerFinalTest.Helpers.TestSequences.Remote_Z_IOP);
                    dutTypeProp.SetValue(plan, DuplexerFinalTest.Helpers.DUTType.Remote);
                    var parts = new List<DuplexerFinalTest.Models.DUTModel>();
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMR001" });
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMR002" });
                    parts.Add(new DuplexerFinalTest.Models.DUTModel { SerialNumber = "SIMR003" });
                    nestedPlanType.GetProperty("Parts").SetValue(plan, parts);
                }

                plansAdd.Invoke(plansList, new object[] { plan });
            }

            nestedStudyType.GetProperty("Plans").SetValue(study, plansList);

            // Now build Observations by scanning CSV files under dataRoot
            var obsList = Activator.CreateInstance(typeof(List<>).MakeGenericType(nestedObsType));
            var obsAdd = obsList.GetType().GetMethod("Add");

            foreach (string csv in Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories))
            {
                // path like ...\01_OpA\Replicate_01\Base\SIMB001_Base_Z_IB_IOP_25.0C_sweep_1_20260604_094207.csv
                string fname = Path.GetFileNameWithoutExtension(csv);
                string[] nameParts = fname.Split('_');
                string partSerial = nameParts.Length > 0 ? nameParts[0] : "";
                string testMarker = fname.ToUpperInvariant();
                DuplexerFinalTest.Helpers.TestSequences testSeq = DuplexerFinalTest.Helpers.TestSequences.Base_Z_IB_IOP;
                if (testMarker.Contains("REMOTE_Z_IPV")) testSeq = DuplexerFinalTest.Helpers.TestSequences.Remote_Z_IPV;
                else if (testMarker.Contains("REMOTE_Z_VPV")) testSeq = DuplexerFinalTest.Helpers.TestSequences.Remote_Z_VPV;
                else if (testMarker.Contains("REMOTE_Z_IOP")) testSeq = DuplexerFinalTest.Helpers.TestSequences.Remote_Z_IOP;
                else if (testMarker.Contains("BASE_Z_IPD")) testSeq = DuplexerFinalTest.Helpers.TestSequences.Base_Z_IPD;
                else if (testMarker.Contains("BASE_Z_IB_IOP")) testSeq = DuplexerFinalTest.Helpers.TestSequences.Base_Z_IB_IOP;

                // parse operator and replicate from path
                var dir = new DirectoryInfo(Path.GetDirectoryName(csv));
                string folderRemote = null;
                int replicateNo = 1;
                string operatorName = "OpA";
                try
                {
                    // dir -> Base or Remote
                    var baseDir = dir;
                    if (baseDir.Parent != null && baseDir.Parent.Name.StartsWith("Replicate_", StringComparison.OrdinalIgnoreCase))
                    {
                        var repDir = baseDir.Parent;
                        replicateNo = int.Parse(repDir.Name.Split('_').Last());
                        var opDir = repDir.Parent;
                        if (opDir != null) operatorName = opDir.Name.Split('_').Last();
                    }
                }
                catch { }

                // compute mean of CH1 Voltage(V)
                double val = 0; int cnt = 0;
                try
                {
                    var lines = File.ReadAllLines(csv);
                    if (lines.Length > 1)
                    {
                        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
                        int idx = Array.FindIndex(headers, h => string.Equals(h, "CH1 Voltage(V)", StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0)
                        {
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var cols = lines[i].Split(',');
                                if (idx < cols.Length && double.TryParse(cols[idx].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) { val += d; cnt++; }
                            }
                        }
                    }
                }
                catch { }
                if (cnt == 0) continue;
                double mean = val / cnt;

                var obs = Activator.CreateInstance(nestedObsType);
                nestedObsType.GetProperty("GroupName").SetValue(obs, Path.GetFileName(Path.GetDirectoryName(csv))); // Base/Remote
                nestedObsType.GetProperty("TestType").SetValue(obs, testSeq);
                nestedObsType.GetProperty("ColumnName").SetValue(obs, "CH1 Voltage(V)");
                nestedObsType.GetProperty("OperatorName").SetValue(obs, operatorName);
                nestedObsType.GetProperty("ReplicateNo").SetValue(obs, replicateNo);
                nestedObsType.GetProperty("PartSerial").SetValue(obs, partSerial);
                nestedObsType.GetProperty("Value").SetValue(obs, mean);
                nestedObsType.GetProperty("CsvPath").SetValue(obs, csv);

                obsAdd.Invoke(obsList, new object[] { obs });
            }

            nestedStudyType.GetProperty("Observations").SetValue(study, obsList);

            // assign study into DiagnosticForm instance
            var grrField = diagType.GetField("_grrStudy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            grrField.SetValue(diag, study);

            // call AnalyzeGrrStudy
            var analyzeMethod = diagType.GetMethod("AnalyzeGrrStudy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var grrResults = analyzeMethod.Invoke(diag, new object[] { study });

            // assign _grrResults and call WriteGrrReports
            var resultsField = diagType.GetField("_grrResults", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resultsField.SetValue(diag, grrResults);

            var writeMethod = diagType.GetMethod("WriteGrrReports", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            writeMethod.Invoke(diag, null);
        }
    }
}
