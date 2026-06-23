using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DuplexerTestSuite.Helpers
{
    public class LogbookReportGenerator
    {
        private readonly string outputDir = Shared.GetFinalTestChamberLogbookRootPath() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FinalTestChamberLogbook");
        public event Action<string, int> ProgressUpdate;
        private int _scannedFiles;
        private static readonly Regex ResultFileNameRegex = new Regex("^(?<serial>[^_]+?)_(?<test>.+?)_(?<temp>-?\\d+(?:\\.\\d+)?)C_sweep_(?<sweep>\\d+)_(?<ts>\\d{8}_\\d{6})(?<failed>_FAILED)?\\.csv$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LogbookReportGenerator()
        {
            try
            {
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Shared.logger?.Log($"Created logbook output directory: {outputDir}", MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError($"Failed to create output directory {outputDir}", ex);
            }
        }

        public string GenerateReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                startDate = startDate.Date;
                endDate = endDate.Date;
                ProgressUpdate?.Invoke("Loading archived result files...", 5);
                var entries = LoadLogbookData(startDate, endDate);
                if (entries.Count == 0)
                {
                    Shared.logger?.Log("No archived or current result files found for selected date range", MessageType.Warning);
                    return null;
                }
                ProgressUpdate?.Invoke($"Found {entries.Count} result entries from {_scannedFiles} files. Preparing report...", 60);
                ArchiveExistingReports();
                string html = BuildReportHtml(entries, startDate, endDate);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filename = $"LogbookReport_{timestamp}.html";
                string filepath = Path.Combine(outputDir, filename);
                File.WriteAllText(filepath, html, Encoding.UTF8);
                Shared.logger?.Log($"Logbook report generated: {filepath}", MessageType.Success);
                ProgressUpdate?.Invoke("Finalizing report file...", 98);
                ProgressUpdate?.Invoke("Report generation complete", 100);
                return filepath;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("GenerateReport failed", ex);
                return null;
            }
        }

        private string BuildReportHtml(List<ResultEntry> entries, DateTime startDate, DateTime endDate)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>Final Test Chamber Logbook Report - {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</title>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js'></script>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f4f6fb; }");
            sb.AppendLine("h1 { color: #1f3057; border-bottom: 3px solid #3b82f6; padding-bottom: 10px; }");
            sb.AppendLine("h2 { color: #3b82f6; margin-top: 30px; }");
            sb.AppendLine(".chart-container { position: relative; width: 95%; height: 340px; margin: 20px auto; background: #ffffff; padding: 20px; border-radius: 10px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }");
            sb.AppendLine(".chart-container canvas { width: 100% !important; height: 100% !important; }");
            sb.AppendLine(".summary-box { display: flex; flex-wrap: wrap; gap: 18px; margin: 20px 0; }");
            sb.AppendLine(".summary-card { flex: 1; min-width: 220px; background: #ffffff; padding: 18px; border-radius: 10px; box-shadow: 0 1px 8px rgba(0,0,0,0.08); }");
            sb.AppendLine(".summary-card h3 { margin: 0 0 10px 0; color: #111827; font-size: 14px; text-transform: uppercase; letter-spacing: .05em; }");
            sb.AppendLine(".summary-card .value { font-size: 32px; font-weight: 700; color: #111827; }");
            sb.AppendLine(".table-container { background: #ffffff; padding: 18px; border-radius: 10px; box-shadow: 0 1px 10px rgba(0,0,0,0.08); margin: 20px 0; overflow-x: auto; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; font-size: 13px; }");
            sb.AppendLine("th, td { padding: 12px 10px; text-align: left; border-bottom: 1px solid #e5e7eb; }");
            sb.AppendLine("th { background-color: #2563eb; color: white; position: sticky; top: 0; z-index: 2; }");
            sb.AppendLine("tr:hover { background-color: #eff6ff; }");
            sb.AppendLine(".date-range { font-size: 14px; color: #4b5563; margin-bottom: 10px; }");
            sb.AppendLine(".legend-label { display: inline-block; margin-right: 16px; font-size: 14px; color: #334155; }");
            sb.AppendLine(".legend-badge { display: inline-block; width: 14px; height: 14px; border-radius: 4px; margin-right: 6px; vertical-align: middle; }");
            sb.AppendLine(".table-controls { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-bottom: 16px; }");
            sb.AppendLine(".table-controls input, .table-controls select, .table-controls button { border: 1px solid #cbd5e1; border-radius: 6px; padding: 8px 10px; font-size: 13px; }");
            sb.AppendLine(".table-controls button { background: #2563eb; color: #ffffff; cursor: pointer; }");
            sb.AppendLine(".table-controls button:hover { background: #1d4ed8; }");
            sb.AppendLine(".note { font-size: 13px; color: #64748b; margin-top: 10px; }");
            sb.AppendLine("@media (max-width: 900px) { .summary-box { flex-direction: column; } .chart-container { width: 100%; } .table-controls { flex-direction: column; align-items: stretch; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<h1>Final Test Chamber Logbook Report</h1>");
            sb.AppendLine($"<div class='date-range'>Report generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Data range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</div>");
            GenerateSummarySection(sb, entries, startDate, endDate);
            GenerateThroughputChart(sb, entries);
            GeneratePassFailTrendChart(sb, entries);
            GenerateSlotTrendChart(sb, entries);
            GenerateOperatorTrendChart(sb, entries);
            GenerateFailureReasonSummary(sb, entries);
            GenerateDailyVolumeChart(sb, entries);
            GenerateDetailedTable(sb, entries);
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }


        private void ArchiveExistingReports()
        {
            try
            {
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                string archiveRoot = Path.Combine(outputDir, "Archive");
                if (!Directory.Exists(archiveRoot))
                {
                    Directory.CreateDirectory(archiveRoot);
                }
                var existingReports = Directory.GetFiles(outputDir, "*.html", SearchOption.TopDirectoryOnly);
                if (existingReports.Length == 0)
                {
                    return;
                }
                string archiveFolder = Path.Combine(archiveRoot, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                Directory.CreateDirectory(archiveFolder);
                foreach (var report in existingReports)
                {
                    var dest = Path.Combine(archiveFolder, Path.GetFileName(report));
                    File.Move(report, dest);
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("ArchiveExistingReports failed", ex);
            }
        }

        private List<ResultEntry> LoadLogbookData(DateTime startDate, DateTime endDate)
        {
            var entries = new List<ResultEntry>();
            try
            {
                var searchRoots = new List<string>();
                if (!string.IsNullOrWhiteSpace(Shared.BaseResultsPath))
                {
                    searchRoots.Add(Shared.BaseResultsPath);
                }
                if (!string.IsNullOrWhiteSpace(Shared.RemoteResultsPath))
                {
                    searchRoots.Add(Shared.RemoteResultsPath);
                }
                if (searchRoots.Count == 0)
                {
                    var root = Shared.GetResultsRootPath();
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        searchRoots.Add(Path.Combine(root, "Base"));
                        searchRoots.Add(Path.Combine(root, "Remote"));
                    }
                }
                if (searchRoots.Count == 0)
                {
                    Shared.logger?.Log("No results folder configured for logbook generation", MessageType.Warning);
                    return entries;
                }
                var topLevelOperatorMap = LoadTopLevelOperatorMap();
                var fileEntries = new List<(string Root, string Path)>();
                foreach (var root in searchRoots.Where(Directory.Exists))
                {
                    foreach (var path in Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories))
                    {
                        fileEntries.Add((root, path));
                    }
                }
                int totalFiles = fileEntries.Count;
                _scannedFiles = 0;
                foreach (var item in fileEntries)
                {
                    _scannedFiles++;
                    if (totalFiles > 0 && (_scannedFiles % 20 == 0 || _scannedFiles == totalFiles))
                    {
                        int progress = 5 + (int)Math.Round(75.0 * _scannedFiles / totalFiles);
                        ProgressUpdate?.Invoke($"Scanning {_scannedFiles}/{totalFiles} result files...", Math.Min(progress, 80));
                    }
                    try
                    {
                        var fileInfo = new FileInfo(item.Path);
                        var result = ParseResultFile(fileInfo, item.Root, topLevelOperatorMap);
                        if (result == null)
                        {
                            continue;
                        }
                        if (result.RunDate.Date < startDate || result.RunDate.Date > endDate)
                        {
                            continue;
                        }
                        entries.Add(result);
                    }
                    catch { }
                }
                var operatorMap = LoadOperatorMap(startDate, endDate);
                foreach (var entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Operator) && operatorMap.TryGetValue(entry.SerialNumber, out var op))
                    {
                        entry.Operator = op;
                    }
                }
                if (totalFiles == 0)
                {
                    ProgressUpdate?.Invoke("No result files found.", 15);
                }
                else
                {
                    ProgressUpdate?.Invoke($"Loaded {entries.Count} matching entries from {_scannedFiles} files.", 25);
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("LoadLogbookData failed", ex);
            }
            return entries.OrderBy(e => e.RunDate).ToList();
        }

        internal ResultEntry ParseResultFile(FileInfo file, string rootPath, Dictionary<string, string> topLevelOperatorMap)
        {
            var entry = new ResultEntry
            {
                Passed = !file.Name.EndsWith("_FAILED.csv", StringComparison.OrdinalIgnoreCase),
                Source = rootPath.IndexOf("Base", StringComparison.OrdinalIgnoreCase) >= 0 ? "Base" : "Remote",
                FullPath = file.FullName,
                RunDate = file.LastWriteTime,
                Operator = string.Empty
            };
            var match = ResultFileNameRegex.Match(file.Name);
            if (match.Success)
            {
                entry.SerialNumber = match.Groups["serial"].Value.Trim();
                entry.TestName = match.Groups["test"].Value.Trim();
                var timestamp = match.Groups["ts"].Value;
                var parsedDate = ParseFileTimestamp(timestamp);
                if (parsedDate.HasValue)
                {
                    entry.RunDate = parsedDate.Value;
                }
            }
            try
            {
                using (var reader = new StreamReader(file.FullName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim().Equals("###COLUMNINFO", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                        if (line.StartsWith("Serial Number", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2 && string.IsNullOrWhiteSpace(entry.SerialNumber))
                            {
                                entry.SerialNumber = parts[1].Trim();
                            }
                        }
                        else if (line.StartsWith("UUT Position", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out var slot))
                            {
                                entry.Slot = slot;
                            }
                        }
                        else if (line.StartsWith("Top Level Result FIle", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                entry.TopLevelFile = parts[1].Trim();
                            }
                        }
                        else if (line.StartsWith("Failure Reason Code", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                entry.FailureReasonCode = parts[1].Trim();
                            }
                        }
                        else if (line.StartsWith("Failure Reason", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                entry.FailureReasonText = parts[1].Trim();
                            }
                        }
                    }
                }
                bool isFailedFile = !entry.Passed;
                if (isFailedFile && (string.IsNullOrWhiteSpace(entry.FailureReasonCode) || string.Equals(entry.FailureReasonCode, "P", StringComparison.OrdinalIgnoreCase) || string.Equals(entry.FailureReasonCode, "PASS", StringComparison.OrdinalIgnoreCase)))
                {
                    entry.FailureReasonCode = "G";
                }
                if (isFailedFile && (string.IsNullOrWhiteSpace(entry.FailureReasonText) || string.Equals(entry.FailureReasonText, "Pass", StringComparison.OrdinalIgnoreCase) || string.Equals(entry.FailureReasonText, "PASS", StringComparison.OrdinalIgnoreCase)))
                {
                    entry.FailureReasonText = "General failure";
                }
                if (string.IsNullOrWhiteSpace(entry.FailureReasonCode))
                {
                    entry.FailureReasonCode = entry.Passed ? "P" : "G";
                }
                if (string.IsNullOrWhiteSpace(entry.FailureReasonText))
                {
                    entry.FailureReasonText = entry.Passed ? "Pass" : "General failure";
                }
                if (!string.IsNullOrWhiteSpace(entry.TopLevelFile))
                {
                    var topLevelKey = NormalizeTopLevelFileKey(entry.TopLevelFile);
                    if (topLevelOperatorMap != null && !string.IsNullOrWhiteSpace(topLevelKey) && topLevelOperatorMap.TryGetValue(topLevelKey, out var operatorFromTopLevel) && !string.IsNullOrWhiteSpace(operatorFromTopLevel))
                    {
                        entry.Operator = operatorFromTopLevel;
                    }
                    else if (string.IsNullOrWhiteSpace(entry.Operator))
                    {
                        operatorFromTopLevel = TryGetOperatorFromTopLevelFile(entry.TopLevelFile, file.DirectoryName);
                        if (!string.IsNullOrWhiteSpace(operatorFromTopLevel))
                        {
                            entry.Operator = operatorFromTopLevel;
                        }
                    }
                }
            }
            catch { }
            if (string.IsNullOrWhiteSpace(entry.SerialNumber))
            {
                return null;
            }
            if (!entry.Slot.HasValue)
            {
                entry.Slot = ExtractSlotFromFilePath(file.FullName);
            }
            if (entry.Slot.HasValue && string.Equals(entry.Source, "Remote", StringComparison.OrdinalIgnoreCase) && entry.Slot.Value >= 1 && entry.Slot.Value <= 12)
            {
                entry.Slot += 12;
            }
            if (string.IsNullOrWhiteSpace(entry.Operator))
            {
                entry.Operator = "Unknown";
            }
            return entry;
        }

        private string TryGetOperatorFromTopLevelFile(string topLevelFile, string sourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(topLevelFile))
            {
                return null;
            }
            var fileName = Path.GetFileName(topLevelFile);
            var candidates = new List<string>();
            if (Path.IsPathRooted(topLevelFile))
            {
                candidates.Add(topLevelFile);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    candidates.Add(Path.Combine(sourceDirectory, fileName));
                    var currentDir = sourceDirectory;
                    while (!string.IsNullOrWhiteSpace(currentDir))
                    {
                        candidates.Add(Path.Combine(currentDir, fileName));
                        var parent = Directory.GetParent(currentDir);
                        currentDir = parent?.FullName;
                    }
                }
                var searchRoots = GetTopLevelResultSearchRoots();
                foreach (var root in searchRoots)
                {
                    if (string.IsNullOrWhiteSpace(root))
                    {
                        continue;
                    }
                    candidates.Add(Path.Combine(root, topLevelFile));
                    candidates.Add(Path.Combine(root, fileName));
                    candidates.Add(Path.Combine(root, "Base", fileName));
                    candidates.Add(Path.Combine(root, "Remote", fileName));
                    candidates.Add(Path.Combine(root, "Results", fileName));
                    candidates.Add(Path.Combine(root, "Results", "Base", fileName));
                    candidates.Add(Path.Combine(root, "Results", "Remote", fileName));
                }
                candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                while (!string.IsNullOrWhiteSpace(baseDir))
                {
                    candidates.Add(Path.Combine(baseDir, fileName));
                    var parent = Directory.GetParent(baseDir);
                    baseDir = parent?.FullName;
                }
            }
            foreach (var candidate in candidates.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }
                    using (var reader = new StreamReader(candidate))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!line.StartsWith("Sequence information", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            var parts = line.Split(new[] { ',' }, 3);
                            if (parts.Length < 3)
                            {
                                continue;
                            }
                            var key = parts[1].Trim();
                            if (key.Equals("Operator ID", StringComparison.OrdinalIgnoreCase) || key.Equals("Operator", StringComparison.OrdinalIgnoreCase))
                            {
                                return parts[2].Trim();
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private IEnumerable<string> GetTopLevelResultSearchRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var configuredRoot = GetResultsRoot();
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                roots.Add(configuredRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            if (!string.IsNullOrWhiteSpace(Shared.BaseResultsPath))
            {
                var baseParent = Path.GetDirectoryName(Shared.BaseResultsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(baseParent))
                {
                    roots.Add(baseParent);
                }
            }
            if (!string.IsNullOrWhiteSpace(Shared.RemoteResultsPath))
            {
                var remoteParent = Path.GetDirectoryName(Shared.RemoteResultsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(remoteParent))
                {
                    roots.Add(remoteParent);
                }
            }
            return roots;
        }

        internal Dictionary<string, string> LoadTopLevelOperatorMap()
        {
            var operatorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in GetTopLevelResultSearchRoots().Where(Directory.Exists))
            {
                try
                {
                    foreach (var topLevelFile in Directory.GetFiles(root, "ZODIAC-TS_*.csv", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            string operatorName = ReadTopLevelOperator(topLevelFile);
                            if (!string.IsNullOrWhiteSpace(operatorName))
                            {
                                var key = NormalizeTopLevelFileKey(topLevelFile);
                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    operatorMap[key] = operatorName;
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return operatorMap;
        }

        private string ReadTopLevelOperator(string path)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("Sequence information", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        var parts = line.Split(',');
                        if (parts.Length < 3)
                        {
                            continue;
                        }
                        var key = parts[1].Trim();
                        if (key.Equals("Operator ID", StringComparison.OrdinalIgnoreCase) || key.Equals("Operator", StringComparison.OrdinalIgnoreCase))
                        {
                            return parts[2].Trim();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string GetResultsRoot()
        {
            return Shared.GetResultsRootPath();
        }

        private string NormalizeTopLevelFileKey(string topLevelFile)
        {
            if (string.IsNullOrWhiteSpace(topLevelFile))
            {
                return null;
            }
            return Path.GetFileName(topLevelFile).Trim();
        }

        private int? ExtractSlotFromFilePath(string path)
        {
            var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var segment in segments.Reverse())
            {
                if (int.TryParse(segment, out var slot))
                {
                    return slot;
                }
            }
            return null;
        }

        private DateTime? ParseFileTimestamp(string timestamp)
        {
            if (DateTime.TryParseExact(timestamp, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            return null;
        }

        private Dictionary<string, string> LoadOperatorMap(DateTime startDate, DateTime endDate)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var db = Shared.productionDatabase;
                if (db == null)
                {
                    return map;
                }
                var assignments = db.GetOperatorBySerial(startDate, endDate);
                if (assignments != null)
                {
                    foreach (var kvp in assignments)
                    {
                        if (!map.ContainsKey(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                        {
                            map[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        private void GenerateSummarySection(StringBuilder sb, List<ResultEntry> entries, DateTime startDate, DateTime endDate)
        {
            var deviceLatest = entries
                .GroupBy(e => new { Serial = (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant(), e.Source })
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .ToList();
            var devicesBySerial = entries
                .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                .ToList();
            int totalDevices = deviceLatest.Count;
            int passedDevices = deviceLatest.Count(e => e.Passed);
            int failedDevices = totalDevices - passedDevices;
            double overallPassRate = totalDevices > 0 ? passedDevices * 100.0 / totalDevices : 0;
            int baseDevices = deviceLatest.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase));
            int remoteDevices = deviceLatest.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase));
            int baseDevicePassed = deviceLatest.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase) && e.Passed);
            int remoteDevicePassed = deviceLatest.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase) && e.Passed);
            double basePassRate = baseDevices > 0 ? baseDevicePassed * 100.0 / baseDevices : 0;
            double remotePassRate = remoteDevices > 0 ? remoteDevicePassed * 100.0 / remoteDevices : 0;
            int activeDays = entries.Select(e => e.RunDate.Date).Distinct().Count();
            int reportDays = Math.Max(1, (endDate.Date - startDate.Date).Days + 1);
            var dailyBaseCounts = entries
                .Where(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.RunDate.Date)
                .Select(g => g.Select(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant()).Distinct().Count())
                .ToList();
            var dailyRemoteCounts = entries
                .Where(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.RunDate.Date)
                .Select(g => g.Select(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant()).Distinct().Count())
                .ToList();
            double avgBaseDevicesPerDay = activeDays > 0 ? Math.Round(dailyBaseCounts.DefaultIfEmpty(0).Average(), 1) : 0;
            double avgRemoteDevicesPerDay = activeDays > 0 ? Math.Round(dailyRemoteCounts.DefaultIfEmpty(0).Average(), 1) : 0;
            int firstPassDevices = devicesBySerial.Count(g => g.OrderBy(e => e.RunDate).First().Passed);
            double firstPassYield = totalDevices > 0 ? Math.Round(firstPassDevices * 100.0 / totalDevices, 1) : 0;
            int retestDevices = devicesBySerial.Count(g =>
            {
                var sessionKeys = g
                    .Select(e => !string.IsNullOrWhiteSpace(e.TopLevelFile)
                        ? NormalizeTopLevelFileKey(e.TopLevelFile)
                        : Path.GetDirectoryName(e.FullPath) ?? string.Empty)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                return sessionKeys > 1;
            });
            double reworkRate = totalDevices > 0 ? Math.Round(retestDevices * 100.0 / totalDevices, 1) : 0;
            int recoveredDevices = devicesBySerial.Count(g => g.Count() > 1 && !g.OrderBy(e => e.RunDate).First().Passed && g.OrderBy(e => e.RunDate).Last().Passed);
            double recoveredRate = totalDevices > 0 ? Math.Round(recoveredDevices * 100.0 / totalDevices, 1) : 0;
            int baseSlotsUsed = deviceLatest.Where(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase) && e.Slot.HasValue).Select(e => e.Slot.Value).Distinct().Count();
            int remoteSlotsUsed = deviceLatest.Where(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase) && e.Slot.HasValue).Select(e => e.Slot.Value).Distinct().Count();
            double baseSlotUtilization = Math.Round(baseSlotsUsed * 100.0 / 12.0, 1);
            double remoteSlotUtilization = Math.Round(remoteSlotsUsed * 100.0 / 12.0, 1);
            sb.AppendLine("<h2>Summary</h2>");
            sb.AppendLine("<div class='summary-box'>");
            sb.AppendLine($"<div class='summary-card'><h3>Total DUTs</h3><div class='value'>{totalDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Base DUTs</h3><div class='value'>{baseDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Remote DUTs</h3><div class='value'>{remoteDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Passed DUTs</h3><div class='value'>{passedDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Failed DUTs</h3><div class='value'>{failedDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Overall Pass Rate</h3><div class='value'>{overallPassRate:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>First-pass Yield</h3><div class='value'>{firstPassYield:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Retest Rate</h3><div class='value'>{reworkRate:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Recovered DUTs</h3><div class='value'>{recoveredDevices}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Recovered Rate</h3><div class='value'>{recoveredRate:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Avg Base DUTs / day</h3><div class='value'>{avgBaseDevicesPerDay:F1}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Avg Remote DUTs / day</h3><div class='value'>{avgRemoteDevicesPerDay:F1}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Base Slot Use</h3><div class='value'>{baseSlotUtilization:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Remote Slot Use</h3><div class='value'>{remoteSlotUtilization:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Active Days</h3><div class='value'>{activeDays}/{reportDays}</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Base Pass Rate</h3><div class='value'>{basePassRate:F1}%</div></div>");
            sb.AppendLine($"<div class='summary-card'><h3>Remote Pass Rate</h3><div class='value'>{remotePassRate:F1}%</div></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='note'>Pass/fail counts are based on unique device serial numbers and latest result per device.</div>");
        }

        private void GenerateThroughputChart(StringBuilder sb, List<ResultEntry> entries)
        {
            var daily = entries
                .GroupBy(e => new { Date = e.RunDate.Date, Serial = (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant() })
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .GroupBy(e => e.RunDate.Date)
                .OrderBy(g => g.Key)
                .ToList();
            var labels = daily.Select(g => g.Key.ToString("yyyy-MM-dd")).ToList();
            var totalCounts = daily.Select(g => g.Count()).ToList();
            var baseCounts = daily.Select(g => g.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase))).ToList();
            var remoteCounts = daily.Select(g => g.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase))).ToList();
            sb.AppendLine("<h2>Daily Throughput</h2>");
            sb.AppendLine("<div class='chart-container'><canvas id='throughputChart'></canvas></div>");
            sb.AppendLine("<script>");
            sb.AppendLine($"const throughputLabels = [{ToJsArray(labels)}];");
            sb.AppendLine($"const throughputTotal = [{string.Join(",", totalCounts)}];");
            sb.AppendLine($"const throughputBase = [{string.Join(",", baseCounts)}];");
            sb.AppendLine($"const throughputRemote = [{string.Join(",", remoteCounts)}];");
            sb.AppendLine("new Chart(document.getElementById('throughputChart'), { type: 'bar', data: { labels: throughputLabels, datasets: [{ label: 'Total DUTs', data: throughputTotal, backgroundColor: '#1f77b4' }, { label: 'Base DUTs', data: throughputBase, backgroundColor: '#ff7f0e' }, { label: 'Remote DUTs', data: throughputRemote, backgroundColor: '#10b981' }] }, options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true, precision: 0 } } } });");
            sb.AppendLine("</script>");
        }

        private void GeneratePassFailTrendChart(StringBuilder sb, List<ResultEntry> entries)
        {
            var daily = entries
                .GroupBy(e => new { Date = e.RunDate.Date, Serial = (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant() })
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .GroupBy(e => e.RunDate.Date)
                .OrderBy(g => g.Key)
                .ToList();
            var labels = daily.Select(g => g.Key.ToString("yyyy-MM-dd")).ToList();
            var basePass = daily.Select(g => g.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase) && e.Passed)).ToList();
            var baseFail = daily.Select(g => g.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase) && !e.Passed)).ToList();
            var remotePass = daily.Select(g => g.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase) && e.Passed)).ToList();
            var remoteFail = daily.Select(g => g.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase) && !e.Passed)).ToList();
            sb.AppendLine("<h2>Pass/Fail Trend</h2>");
            sb.AppendLine("<div class='chart-container'><canvas id='passFailChart'></canvas></div>");
            sb.AppendLine("<script>");
            sb.AppendLine($"const passFailLabels = [{ToJsArray(labels)}];");
            sb.AppendLine($"const basePassTrend = [{string.Join(",", basePass)}];");
            sb.AppendLine($"const baseFailTrend = [{string.Join(",", baseFail)}];");
            sb.AppendLine($"const remotePassTrend = [{string.Join(",", remotePass)}];");
            sb.AppendLine($"const remoteFailTrend = [{string.Join(",", remoteFail)}];");
            sb.AppendLine("new Chart(document.getElementById('passFailChart'), { type: 'bar', data: { labels: passFailLabels, datasets: [{ label: 'Base Pass', data: basePassTrend, backgroundColor: '#16a34a', stack: 'base' }, { label: 'Base Fail', data: baseFailTrend, backgroundColor: '#dc2626', stack: 'base' }, { label: 'Remote Pass', data: remotePassTrend, backgroundColor: '#4ade80', stack: 'remote' }, { label: 'Remote Fail', data: remoteFailTrend, backgroundColor: '#f87171', stack: 'remote' }] }, options: { responsive: true, maintainAspectRatio: false, scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true } } } });");
            sb.AppendLine("</script>");
        }

        private void GenerateSlotTrendChart(StringBuilder sb, List<ResultEntry> entries)
        {
            var slotLabels = Enumerable.Range(1, 24).Select(i => i.ToString()).ToList();
            var basePassRates = new List<double?>();
            var remotePassRates = new List<double?>();
            foreach (var slot in Enumerable.Range(1, 24))
            {
                var slotEntries = entries.Where(e => e.Slot == slot)
                    .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                    .Select(g => g.OrderByDescending(e => e.RunDate).First())
                    .ToList();
                var slotPassRate = slotEntries.Count > 0 ? Math.Round(slotEntries.Count(e => e.Passed) * 100.0 / slotEntries.Count, 1) : (double?)null;
                if (slot <= 12)
                {
                    basePassRates.Add(slotPassRate);
                    remotePassRates.Add(null);
                }
                else
                {
                    basePassRates.Add(null);
                    remotePassRates.Add(slotPassRate);
                }
            }
            sb.AppendLine("<h2>Slot Performance</h2>");
            sb.AppendLine("<div class='legend-label'><span class='legend-badge' style='background:#2563eb'></span>Base slots</div>");
            sb.AppendLine("<div class='legend-label'><span class='legend-badge' style='background:#10b981'></span>Remote slots</div>");
            sb.AppendLine("<div class='chart-container'><canvas id='slotChart'></canvas></div>");
            sb.AppendLine("<script>");
            sb.AppendLine($"const slotLabels = [{ToJsArray(slotLabels)}];");
            sb.AppendLine($"const baseSlotPassRates = [{string.Join(",", basePassRates.Select(v => v.HasValue ? v.Value.ToString("F1", CultureInfo.InvariantCulture) : "null"))}];");
            sb.AppendLine($"const remoteSlotPassRates = [{string.Join(",", remotePassRates.Select(v => v.HasValue ? v.Value.ToString("F1", CultureInfo.InvariantCulture) : "null"))}];");
            sb.AppendLine("new Chart(document.getElementById('slotChart'), { type: 'line', data: { labels: slotLabels, datasets: [{ label: 'Base slot pass rate', data: baseSlotPassRates, borderColor: '#2563eb', backgroundColor: 'rgba(59,130,246,0.25)', fill: false, tension: 0.3, spanGaps: true }, { label: 'Remote slot pass rate', data: remoteSlotPassRates, borderColor: '#10b981', backgroundColor: 'rgba(16,185,129,0.25)', fill: false, tension: 0.3, spanGaps: true }] }, options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true, max: 100 } } } });");
            sb.AppendLine("</script>");
        }

        private void GenerateOperatorTrendChart(StringBuilder sb, List<ResultEntry> entries)
        {
            var operatorGroups = entries.GroupBy(e => string.IsNullOrWhiteSpace(e.Operator) ? "Unknown" : e.Operator).OrderBy(g => g.Key).ToList();
            var labels = operatorGroups.Select(g => g.Key).ToList();
            var passRate = operatorGroups.Select(g => Math.Round(g.Count(e => e.Passed) * 100.0 / Math.Max(1, g.Count()), 1)).ToList();
            sb.AppendLine("<h2>Operator Performance</h2>");
            sb.AppendLine("<div class='chart-container'><canvas id='operatorChart'></canvas></div>");
            sb.AppendLine("<script>");
            sb.AppendLine($"const operatorLabels = [{ToJsArray(labels)}];");
            sb.AppendLine($"const operatorPassRates = [{string.Join(",", passRate)}];");
            sb.AppendLine("new Chart(document.getElementById('operatorChart'), { type: 'bar', data: { labels: operatorLabels, datasets: [{ label: 'Pass Rate %', data: operatorPassRates, backgroundColor: '#f59e0b' }] }, options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true, max: 100 } } } });");
            sb.AppendLine("</script>");
        }

        private void GenerateFailureReasonSummary(StringBuilder sb, List<ResultEntry> entries)
        {
            var latestFailedPerDevice = entries
                .Where(e => !e.Passed)
                .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .ToList();
            var reasonGroups = latestFailedPerDevice
                .GroupBy(e => string.IsNullOrWhiteSpace(e.FailureReasonText) ? "Unknown" : e.FailureReasonText)
                .Select(g => new
                {
                    Reason = g.Key,
                    BaseCount = g.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase)),
                    RemoteCount = g.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase)),
                    TotalCount = g.Count()
                })
                .OrderByDescending(g => g.TotalCount)
                .Take(8)
                .ToList();
            sb.AppendLine("<h2>Top Failure Reasons</h2>");
            sb.AppendLine("<div class='table-container'><table>");
            sb.AppendLine("<thead><tr><th>Reason</th><th>Base Occurrences</th><th>Remote Occurrences</th><th>Total Occurrences</th></tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var reason in reasonGroups)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{HtmlEncode(reason.Reason)}</td>");
                sb.AppendLine($"<td>{reason.BaseCount}</td>");
                sb.AppendLine($"<td>{reason.RemoteCount}</td>");
                sb.AppendLine($"<td>{reason.TotalCount}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        private void GenerateDailyVolumeChart(StringBuilder sb, List<ResultEntry> entries)
        {
            var daily = entries
                .GroupBy(e => new { e.RunDate.Date, Serial = (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant() })
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .GroupBy(e => e.RunDate.Date)
                .OrderBy(g => g.Key)
                .ToList();
            var labels = daily.Select(g => g.Key.ToString("yyyy-MM-dd")).ToList();
            var baseCounts = daily.Select(g => g.Count(e => string.Equals(e.Source, "Base", StringComparison.OrdinalIgnoreCase))).ToList();
            var remoteCounts = daily.Select(g => g.Count(e => string.Equals(e.Source, "Remote", StringComparison.OrdinalIgnoreCase))).ToList();
            sb.AppendLine("<h2>Daily Run Volume</h2>");
            sb.AppendLine("<div class='legend-label'><span class='legend-badge' style='background:#2563eb'></span>Base devices</div>");
            sb.AppendLine("<div class='legend-label'><span class='legend-badge' style='background:#10b981'></span>Remote devices</div>");
            sb.AppendLine("<div class='chart-container'><canvas id='dailyVolumeChart'></canvas></div>");
            sb.AppendLine("<script>");
            sb.AppendLine($"const dailyVolumeLabels = [{ToJsArray(labels)}];");
            sb.AppendLine($"const dailyBaseCounts = [{string.Join(",", baseCounts)}];");
            sb.AppendLine($"const dailyRemoteCounts = [{string.Join(",", remoteCounts)}];");
            sb.AppendLine("new Chart(document.getElementById('dailyVolumeChart'), { type: 'bar', data: { labels: dailyVolumeLabels, datasets: [{ label: 'Base devices', data: dailyBaseCounts, backgroundColor: '#2563eb' }, { label: 'Remote devices', data: dailyRemoteCounts, backgroundColor: '#10b981' }] }, options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true, precision: 0 } } } });");
            sb.AppendLine("</script>");
        }

        private void GenerateDetailedTable(StringBuilder sb, List<ResultEntry> entries)
        {
            var latestEntries = entries
                .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                .Select(g => g.OrderByDescending(e => e.RunDate).First())
                .OrderByDescending(e => e.RunDate)
                .ToList();
            var operators = latestEntries.Select(e => e.Operator ?? "Unknown").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(o => o).ToList();
            sb.AppendLine("<h2>DUT Results</h2>");
            sb.AppendLine("<div class='note'>Showing one row per serial number, latest result only.</div>");
            sb.AppendLine("<div class='table-controls'>");
            sb.AppendLine("<input id='searchInput' type='text' placeholder='Search serial / operator / test / reason' oninput='filterTable()' />");
            sb.AppendLine("<input id='slotInput' type='text' placeholder='Slot' oninput='filterTable()' style='width:80px;' />");
            sb.AppendLine("<select id='sourceFilter' onchange='filterTable()'>");
            sb.AppendLine("<option value=''>All sources</option>");
            sb.AppendLine("<option value='Base'>Base</option>");
            sb.AppendLine("<option value='Remote'>Remote</option>");
            sb.AppendLine("</select>");
            sb.AppendLine("<select id='resultFilter' onchange='filterTable()'>");
            sb.AppendLine("<option value=''>All results</option>");
            sb.AppendLine("<option value='PASS'>PASS</option>");
            sb.AppendLine("<option value='FAIL'>FAIL</option>");
            sb.AppendLine("</select>");
            sb.AppendLine("<select id='operatorFilter' onchange='filterTable()'>");
            sb.AppendLine("<option value=''>All operators</option>");
            foreach (var op in operators)
            {
                sb.AppendLine($"<option value='{HtmlEncode(op)}'>{HtmlEncode(op)}</option>");
            }
            sb.AppendLine("</select>");
            sb.AppendLine("<button type='button' onclick='resetFilters()'>Reset filters</button>");
            sb.AppendLine("<button type='button' onclick='exportCsv()'>Export CSV</button>");
            sb.AppendLine("<button type='button' onclick='printReport()'>Export PDF</button>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='table-container'><table id='resultsTable'>");
            sb.AppendLine("<thead><tr><th>Date</th><th>Serial</th><th>Slot</th><th>Source</th><th>Result</th><th>Retest Status</th><th>Failure Code</th><th>Failure Reason</th><th>Operator</th><th>File Location</th></tr></thead>");
            sb.AppendLine("<tbody>");
            var serialGroups = entries
                .GroupBy(e => (e.SerialNumber ?? string.Empty).Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.RunDate).ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in latestEntries)
            {
                string serialKey = (entry.SerialNumber ?? string.Empty).Trim().ToUpperInvariant();
                string retestStatus;
                if (!serialGroups.TryGetValue(serialKey, out var history) || history.Count == 1)
                {
                    retestStatus = entry.Passed ? "First-pass PASS" : "Fail";
                }
                else
                {
                    bool firstPassed = history.First().Passed;
                    if (entry.Passed)
                    {
                        retestStatus = firstPassed ? "First-pass PASS" : "Pass after retest";
                    }
                    else
                    {
                        retestStatus = firstPassed ? "Fail after retest" : "Fail";
                    }
                }
                string runDateText = HtmlEncode(entry.RunDate.ToString("yyyy-MM-dd HH:mm:ss"));
                string slotText = HtmlEncode(entry.Slot.HasValue ? entry.Slot.Value.ToString() : "—");
                string resultText = entry.Passed ? "PASS" : "FAIL";
                string failureCodeText = entry.Passed ? "-" : HtmlEncode(entry.FailureReasonCode);
                string failureReasonText = entry.Passed ? "-" : HtmlEncode(entry.FailureReasonText);
                string folderPath = string.Empty;
                if (!string.IsNullOrWhiteSpace(entry.FullPath))
                {
                    var dir = Path.GetDirectoryName(entry.FullPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        folderPath = dir;
                    }
                }
                var fileUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    fileUrl = HtmlEncode("file:///" + folderPath.Replace("\\", "/").Replace(" ", "%20"));
                }
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{runDateText}</td>");
                sb.AppendLine($"<td>{HtmlEncode(entry.SerialNumber)}</td>");
                sb.AppendLine($"<td>{slotText}</td>");
                sb.AppendLine($"<td>{HtmlEncode(entry.Source)}</td>");
                sb.AppendLine($"<td>{HtmlEncode(resultText)}</td>");
                sb.AppendLine($"<td>{HtmlEncode(retestStatus)}</td>");
                sb.AppendLine($"<td>{failureCodeText}</td>");
                sb.AppendLine($"<td>{failureReasonText}</td>");
                sb.AppendLine($"<td>{HtmlEncode(entry.Operator)}</td>");
                if (!string.IsNullOrWhiteSpace(fileUrl))
                {
                    sb.AppendLine($"<td><a href='{fileUrl}' target='_blank'>Open folder</a></td>");
                }
                else
                {
                    sb.AppendLine("<td>Unknown</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table></div>");
            sb.AppendLine("<script>");
            sb.AppendLine("function filterTable() {");
            sb.AppendLine("  const searchText = document.getElementById('searchInput').value.toLowerCase();");
            sb.AppendLine("  const slotText = document.getElementById('slotInput').value.trim();");
            sb.AppendLine("  const sourceFilter = document.getElementById('sourceFilter').value;");
            sb.AppendLine("  const resultFilter = document.getElementById('resultFilter').value;");
            sb.AppendLine("  const operatorFilter = document.getElementById('operatorFilter').value;");
            sb.AppendLine("  const tbody = document.getElementById('resultsTable').tBodies[0];");
            sb.AppendLine("  Array.from(tbody.rows).forEach(row => {");
            sb.AppendLine("    const cells = row.cells;");
            sb.AppendLine("    const rowText = Array.from(cells).map(cell => cell.innerText.toLowerCase()).join(' ');");
            sb.AppendLine("    const matchesSearch = !searchText || rowText.includes(searchText);");
            sb.AppendLine("    const matchesSlot = !slotText || cells[2].innerText.trim() === slotText;");
            sb.AppendLine("    const matchesSource = !sourceFilter || cells[3].innerText.trim() === sourceFilter;");
            sb.AppendLine("    const matchesResult = !resultFilter || cells[5].innerText.trim() === resultFilter;");
            sb.AppendLine("    const matchesOperator = !operatorFilter || cells[8].innerText.trim() === operatorFilter;");
            sb.AppendLine("    row.style.display = matchesSearch && matchesSlot && matchesSource && matchesResult && matchesOperator ? '' : 'none';");
            sb.AppendLine("  });");
            sb.AppendLine("}");
            sb.AppendLine("function resetFilters() {");
            sb.AppendLine("  document.getElementById('searchInput').value = '';" );
            sb.AppendLine("  document.getElementById('slotInput').value = '';" );
            sb.AppendLine("  document.getElementById('sourceFilter').value = '';" );
            sb.AppendLine("  document.getElementById('resultFilter').value = '';" );
            sb.AppendLine("  document.getElementById('operatorFilter').value = '';" );
            sb.AppendLine("  filterTable();" );
            sb.AppendLine("}");
            sb.AppendLine("function sortTable(table, colIndex) {");
            sb.AppendLine("  const tbody = table.tBodies[0];");
            sb.AppendLine("  const rows = Array.from(tbody.rows).filter(r => r.style.display !== 'none');");
            sb.AppendLine("  const asc = table.dataset.sortCol == colIndex && table.dataset.sortDir == 'asc' ? false : true;");
            sb.AppendLine("  rows.sort((a,b) => {");
            sb.AppendLine("    const aText = a.cells[colIndex].innerText.trim();");
            sb.AppendLine("    const bText = b.cells[colIndex].innerText.trim();");
            sb.AppendLine("    const aNum = parseFloat(aText.replace(/[^0-9.-]/g, ''));");
            sb.AppendLine("    const bNum = parseFloat(bText.replace(/[^0-9.-]/g, ''));");
            sb.AppendLine("    if (!isNaN(aNum) && !isNaN(bNum)) return asc ? aNum - bNum : bNum - aNum;");
            sb.AppendLine("    return asc ? aText.localeCompare(bText) : bText.localeCompare(aText);");
            sb.AppendLine("  });");
            sb.AppendLine("  rows.forEach(row => tbody.appendChild(row));");
            sb.AppendLine("  table.dataset.sortCol = colIndex;");
            sb.AppendLine("  table.dataset.sortDir = asc ? 'asc' : 'desc';");
            sb.AppendLine("}");
            sb.AppendLine("function exportCsv() {");
            sb.AppendLine("  const table = document.getElementById('resultsTable');");
            sb.AppendLine("  const rows = Array.from(table.querySelectorAll('tr')).filter(row => row.style.display !== 'none');");
            sb.AppendLine("  const csv = rows.map(row => Array.from(row.cells).map(cell => '\"' + cell.innerText.replace(/\"/g, '\"\"') + '\"').join(',' )).join('\\n');");
            sb.AppendLine("  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });");
            sb.AppendLine("  const url = URL.createObjectURL(blob);" );
            sb.AppendLine("  const a = document.createElement('a');");
            sb.AppendLine("  a.href = url;");
            sb.AppendLine("  a.download = 'LogbookReport.csv';");
            sb.AppendLine("  document.body.appendChild(a);");
            sb.AppendLine("  a.click();");
            sb.AppendLine("  document.body.removeChild(a);");
            sb.AppendLine("  URL.revokeObjectURL(url);");
            sb.AppendLine("}");
            sb.AppendLine("function printReport() {");
            sb.AppendLine("  window.print();");
            sb.AppendLine("}");
            sb.AppendLine("document.addEventListener('DOMContentLoaded', function() {");
            sb.AppendLine("  const table = document.getElementById('resultsTable');");
            sb.AppendLine("  const headers = table.querySelectorAll('th');");
            sb.AppendLine("  headers.forEach((header, index) => {");
            sb.AppendLine("    header.style.cursor = 'pointer';");
            sb.AppendLine("    header.addEventListener('click', () => sortTable(table, index));");
            sb.AppendLine("  });");
            sb.AppendLine("});");
            sb.AppendLine("</script>");
        }

        private static string ToJsArray(IEnumerable<string> values)
        {
            return string.Join(",", values.Select(v => "\"" + HtmlEncode(v).Replace("\"", "\\\"") + "\""));
        }

        private static string HtmlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        internal class ResultEntry
        {
            internal string SerialNumber { get; set; }
            internal int? Slot { get; set; }
            internal bool Passed { get; set; }
            internal string TestName { get; set; }
            internal DateTime RunDate { get; set; }
            internal string Source { get; set; }
            internal string FullPath { get; set; }
            internal string Operator { get; set; }
            internal string TopLevelFile { get; set; }
            internal string FailureReasonCode { get; set; }
            internal string FailureReasonText { get; set; }
        }
    }
}