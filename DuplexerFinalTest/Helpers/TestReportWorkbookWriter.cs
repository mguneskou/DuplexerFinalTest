using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace DuplexerFinalTest.Helpers
{
    public sealed class TestReportWorkbookData
    {
        public TestReportRunSummary Summary { get; set; }
        public List<TestReportDutEntry> Entries { get; set; } = new List<TestReportDutEntry>();
    }

    public sealed class TestReportRunSummary
    {
        public string RunId { get; set; }
        public DateTime RunStartedAt { get; set; }
        public DateTime RunCompletedAt { get; set; }
        public string SequenceName { get; set; }
        public string SequenceRevision { get; set; }
        public string OperatorName { get; set; }
        public string TestRig { get; set; }
        public string SoftwareVersion { get; set; }
        public bool IsSimulationMode { get; set; }
        public int BaseUnitCount { get; set; }
        public int RemoteUnitCount { get; set; }
        public int TotalDutCount { get; set; }
        public int PassedDutCount { get; set; }
        public int FailedDutCount { get; set; }
        public double PassRatePercent { get; set; }
        public int TotalSpecCount { get; set; }
        public int FailedSpecCount { get; set; }
        public DateTime? CalibrationTimestamp { get; set; }
        public double? CalibrationAgeDays { get; set; }
        public double? AverageChamberTemperatureErrorC { get; set; }
        public double? MaxChamberTemperatureDeviationC { get; set; }
        public double? AverageSoakSettleMinutes { get; set; }
        public int EquipmentRetryCount { get; set; }
        public int EquipmentReconnectCount { get; set; }
        public int ForcedOperatorResumeCount { get; set; }
        public int PretestFailedDutCount { get; set; }
        public int DuplicateScanCorrectionCount { get; set; }
        public double? ScanCompleteToTestStartMinutes { get; set; }
        public bool OverallPassed { get; set; }
    }

    public sealed class TestReportDutEntry
    {
        public string RunId { get; set; }
        public DateTime RunStartedAt { get; set; }
        public DateTime RunCompletedAt { get; set; }
        public string SequenceName { get; set; }
        public string SequenceRevision { get; set; }
        public string OperatorName { get; set; }
        public string TestRig { get; set; }
        public string SoftwareVersion { get; set; }
        public bool IsSimulationMode { get; set; }
        public DateTime? CalibrationTimestamp { get; set; }
        public double? CalibrationAgeDays { get; set; }
        public string DutType { get; set; }
        public int Slot { get; set; }
        public string SerialNumber { get; set; }
        public bool Passed { get; set; }
        public int FailedSpecCount { get; set; }
        public int TotalSpecCount { get; set; }
        public string FailedSpecIds { get; set; }
        public int ResultFileCount { get; set; }
        public string OverallRunResult { get; set; }
        public int BaseUnitCount { get; set; }
        public int RemoteUnitCount { get; set; }
    }

    public static class TestReportWorkbookWriter
    {
        private const string ReportFolderPath = @"P:\MGunes\DuplexerTestSuite\TestReport";
        private const string WorkbookFileName = "DuplexerFinalTest_TestLog.xlsx";
        private const string DutLogSheetName = "DUT Log";
        private const string RunSummarySheetName = "Run Summary";
        private const string PerformanceSheetName = "Performance Graph";
        private const double CalibrationReviewMinPassRatePct = 95.0d;
        private const double CalibrationReviewMaxAgeDays = 30.0d;

        private static readonly string[] DutLogHeaders =
        {
            "Run ID",
            "Run Start",
            "Run Completed",
            "Sequence",
            "Revision",
            "Operator",
            "Test Rig",
            "Software Version",
            "Simulation Mode",
            "Calibration Timestamp",
            "Calibration Age (days)",
            "DUT Type",
            "Slot",
            "Serial Number",
            "Result",
            "Failed Specs",
            "Total Specs",
            "Failed Spec IDs",
            "Result File Count",
            "Overall Run Result",
            "Base Units",
            "Remote Units"
        };

        private static readonly string[] RunSummaryHeaders =
        {
            "Run ID",
            "Run Start",
            "Run Completed",
            "Sequence",
            "Revision",
            "Operator",
            "Test Rig",
            "Simulation Mode",
            "Base Units",
            "Remote Units",
            "Total DUTs",
            "Passed DUTs",
            "Failed DUTs",
            "Pass Rate %",
            "Failed Specs",
            "Total Specs",
            "Calibration Timestamp",
            "Calibration Age (days)",
            "Calibration Review Status",
            "Overall Run Result",
            "Software Version",
            "Avg Chamber Temp Error (C)",
            "Max Chamber Temp Deviation (C)",
            "Avg Soak Settle (min)",
            "Equipment Retries",
            "Equipment Reconnects",
            "Forced Operator Resumes",
            "Pretest Failed DUTs",
            "Duplicate Scan Corrections",
            "Scan Complete To Start (min)"
        };

        public static string WorkbookPath => Path.Combine(ReportFolderPath, WorkbookFileName);

        public static int PurgeSimulationRuns()
        {
            if (!File.Exists(WorkbookPath))
                return 0;

            using (var workbook = new XLWorkbook(WorkbookPath))
            {
                IXLWorksheet dutLogSheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == DutLogSheetName);
                IXLWorksheet runSummarySheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == RunSummarySheetName);
                int removedRows = PurgeSimulationRuns(dutLogSheet, runSummarySheet);

                if (removedRows > 0)
                {
                    if (dutLogSheet != null)
                    {
                        GetOrCreateWorksheet(workbook, DutLogSheetName, DutLogHeaders);
                        FinalizeWorksheet(dutLogSheet, DutLogHeaders.Length);
                    }

                    if (runSummarySheet != null)
                    {
                        GetOrCreateWorksheet(workbook, RunSummarySheetName, RunSummaryHeaders);
                        FinalizeWorksheet(runSummarySheet, RunSummaryHeaders.Length);
                        RebuildPerformanceSheet(workbook, runSummarySheet);
                    }

                    workbook.SaveAs(WorkbookPath);
                }

                return removedRows;
            }
        }

        public static void AppendRunReport(TestReportWorkbookData reportData)
        {
            if (reportData == null)
                throw new ArgumentNullException(nameof(reportData));
            if (reportData.Summary == null)
                throw new ArgumentException("Run summary is required.", nameof(reportData));

            Directory.CreateDirectory(ReportFolderPath);

            using (var workbook = File.Exists(WorkbookPath) ? new XLWorkbook(WorkbookPath) : new XLWorkbook())
            {
                IXLWorksheet dutLogSheet = GetOrCreateWorksheet(workbook, DutLogSheetName, DutLogHeaders);
                IXLWorksheet runSummarySheet = GetOrCreateWorksheet(workbook, RunSummarySheetName, RunSummaryHeaders);

                PurgeSimulationRuns(dutLogSheet, runSummarySheet);

                AppendDutLogRows(dutLogSheet, reportData.Entries);
                AppendRunSummaryRow(runSummarySheet, reportData.Summary);
                RebuildPerformanceSheet(workbook, runSummarySheet);

                workbook.SaveAs(WorkbookPath);
            }
        }

        private static IXLWorksheet GetOrCreateWorksheet(XLWorkbook workbook, string sheetName, string[] headers)
        {
            IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);
            if (worksheet == null)
                worksheet = workbook.AddWorksheet(sheetName);

            for (int i = 0; i < headers.Length; i++)
                worksheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
            worksheet.SheetView.FreezeRows(1);

            return worksheet;
        }

        private static void AppendDutLogRows(IXLWorksheet worksheet, List<TestReportDutEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return;

            int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
            foreach (var entry in entries)
            {
                int col = 1;
                worksheet.Cell(nextRow, col++).Value = entry.RunId;
                worksheet.Cell(nextRow, col++).Value = entry.RunStartedAt;
                worksheet.Cell(nextRow, col++).Value = entry.RunCompletedAt;
                worksheet.Cell(nextRow, col++).Value = entry.SequenceName;
                worksheet.Cell(nextRow, col++).Value = entry.SequenceRevision;
                worksheet.Cell(nextRow, col++).Value = entry.OperatorName;
                worksheet.Cell(nextRow, col++).Value = entry.TestRig;
                worksheet.Cell(nextRow, col++).Value = entry.SoftwareVersion;
                worksheet.Cell(nextRow, col++).Value = entry.IsSimulationMode ? "Yes" : "No";
                if (entry.CalibrationTimestamp.HasValue)
                    worksheet.Cell(nextRow, col).Value = entry.CalibrationTimestamp.Value;
                else
                    worksheet.Cell(nextRow, col).Value = string.Empty;
                col++;
                if (entry.CalibrationAgeDays.HasValue)
                    worksheet.Cell(nextRow, col).Value = entry.CalibrationAgeDays.Value;
                else
                    worksheet.Cell(nextRow, col).Value = string.Empty;
                col++;
                worksheet.Cell(nextRow, col++).Value = entry.DutType;
                worksheet.Cell(nextRow, col++).Value = entry.Slot;
                worksheet.Cell(nextRow, col++).Value = entry.SerialNumber;
                worksheet.Cell(nextRow, col++).Value = entry.Passed ? "PASS" : "FAIL";
                worksheet.Cell(nextRow, col++).Value = entry.FailedSpecCount;
                worksheet.Cell(nextRow, col++).Value = entry.TotalSpecCount;
                worksheet.Cell(nextRow, col++).Value = entry.FailedSpecIds;
                worksheet.Cell(nextRow, col++).Value = entry.ResultFileCount;
                worksheet.Cell(nextRow, col++).Value = entry.OverallRunResult;
                worksheet.Cell(nextRow, col++).Value = entry.BaseUnitCount;
                worksheet.Cell(nextRow, col++).Value = entry.RemoteUnitCount;

                worksheet.Cell(nextRow, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                worksheet.Cell(nextRow, 3).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                worksheet.Cell(nextRow, 10).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                if (entry.CalibrationAgeDays.HasValue)
                    worksheet.Cell(nextRow, 11).Style.NumberFormat.Format = "0.00";

                ColorRowByResult(worksheet.Row(nextRow), entry.Passed);
                nextRow++;
            }

            FinalizeWorksheet(worksheet, DutLogHeaders.Length);
        }

        private static void AppendRunSummaryRow(IXLWorksheet worksheet, TestReportRunSummary summary)
        {
            int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
            string calibrationStatus = BuildCalibrationReviewStatus(summary);

            int col = 1;
            worksheet.Cell(nextRow, col++).Value = summary.RunId;
            worksheet.Cell(nextRow, col++).Value = summary.RunStartedAt;
            worksheet.Cell(nextRow, col++).Value = summary.RunCompletedAt;
            worksheet.Cell(nextRow, col++).Value = summary.SequenceName;
            worksheet.Cell(nextRow, col++).Value = summary.SequenceRevision;
            worksheet.Cell(nextRow, col++).Value = summary.OperatorName;
            worksheet.Cell(nextRow, col++).Value = summary.TestRig;
            worksheet.Cell(nextRow, col++).Value = summary.IsSimulationMode ? "Yes" : "No";
            worksheet.Cell(nextRow, col++).Value = summary.BaseUnitCount;
            worksheet.Cell(nextRow, col++).Value = summary.RemoteUnitCount;
            worksheet.Cell(nextRow, col++).Value = summary.TotalDutCount;
            worksheet.Cell(nextRow, col++).Value = summary.PassedDutCount;
            worksheet.Cell(nextRow, col++).Value = summary.FailedDutCount;
            worksheet.Cell(nextRow, col++).Value = summary.PassRatePercent;
            worksheet.Cell(nextRow, col++).Value = summary.FailedSpecCount;
            worksheet.Cell(nextRow, col++).Value = summary.TotalSpecCount;
            if (summary.CalibrationTimestamp.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.CalibrationTimestamp.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;
            col++;
            if (summary.CalibrationAgeDays.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.CalibrationAgeDays.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;
            col++;
            worksheet.Cell(nextRow, col++).Value = calibrationStatus;
            worksheet.Cell(nextRow, col++).Value = summary.OverallPassed ? "PASS" : "FAIL";
            worksheet.Cell(nextRow, col++).Value = summary.SoftwareVersion;
            if (summary.AverageChamberTemperatureErrorC.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.AverageChamberTemperatureErrorC.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;
            col++;
            if (summary.MaxChamberTemperatureDeviationC.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.MaxChamberTemperatureDeviationC.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;
            col++;
            if (summary.AverageSoakSettleMinutes.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.AverageSoakSettleMinutes.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;
            col++;
            worksheet.Cell(nextRow, col++).Value = summary.EquipmentRetryCount;
            worksheet.Cell(nextRow, col++).Value = summary.EquipmentReconnectCount;
            worksheet.Cell(nextRow, col++).Value = summary.ForcedOperatorResumeCount;
            worksheet.Cell(nextRow, col++).Value = summary.PretestFailedDutCount;
            worksheet.Cell(nextRow, col++).Value = summary.DuplicateScanCorrectionCount;
            if (summary.ScanCompleteToTestStartMinutes.HasValue)
                worksheet.Cell(nextRow, col).Value = summary.ScanCompleteToTestStartMinutes.Value;
            else
                worksheet.Cell(nextRow, col).Value = string.Empty;

            worksheet.Cell(nextRow, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell(nextRow, 3).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell(nextRow, 14).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(nextRow, 17).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            if (summary.CalibrationAgeDays.HasValue)
                worksheet.Cell(nextRow, 18).Style.NumberFormat.Format = "0.00";
            if (summary.AverageChamberTemperatureErrorC.HasValue)
                worksheet.Cell(nextRow, 22).Style.NumberFormat.Format = "0.000";
            if (summary.MaxChamberTemperatureDeviationC.HasValue)
                worksheet.Cell(nextRow, 23).Style.NumberFormat.Format = "0.000";
            if (summary.AverageSoakSettleMinutes.HasValue)
                worksheet.Cell(nextRow, 24).Style.NumberFormat.Format = "0.00";
            if (summary.ScanCompleteToTestStartMinutes.HasValue)
                worksheet.Cell(nextRow, 30).Style.NumberFormat.Format = "0.00";

            ColorRowByResult(worksheet.Row(nextRow), summary.OverallPassed);
            FinalizeWorksheet(worksheet, RunSummaryHeaders.Length);
        }

        private static int PurgeSimulationRuns(IXLWorksheet dutLogSheet, IXLWorksheet runSummarySheet)
        {
            int removedRows = 0;

            if (dutLogSheet != null)
                removedRows += RemoveSimulationRows(dutLogSheet, 9);

            if (runSummarySheet != null)
                removedRows += RemoveSimulationRows(runSummarySheet, 8);

            return removedRows;
        }

        private static int RemoveSimulationRows(IXLWorksheet worksheet, int simulationModeColumnIndex)
        {
            int removedRows = 0;
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int rowNo = lastRow; rowNo >= 2; rowNo--)
            {
                if (string.Equals(worksheet.Cell(rowNo, simulationModeColumnIndex).GetString(), "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Row(rowNo).Delete();
                    removedRows++;
                }
            }

            return removedRows;
        }

        private static void FinalizeWorksheet(IXLWorksheet worksheet, int headerCount)
        {
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
                usedRange.SetAutoFilter();

            for (int i = 1; i <= headerCount; i++)
                worksheet.Column(i).AdjustToContents();
        }

        private static void ColorRowByResult(IXLRow row, bool passed)
        {
            row.Style.Fill.BackgroundColor = passed
                ? XLColor.FromHtml("#E2F0D9")
                : XLColor.FromHtml("#FCE4D6");
        }

        private static void RebuildPerformanceSheet(XLWorkbook workbook, IXLWorksheet runSummarySheet)
        {
            IXLWorksheet existingSheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == PerformanceSheetName);
            if (existingSheet != null)
                workbook.Worksheets.Delete(existingSheet.Name);

            IXLWorksheet worksheet = workbook.AddWorksheet(PerformanceSheetName);
            List<HistoricalRunSummary> summaries = ReadRunSummaries(runSummarySheet);
            List<HistoricalRunSummary> hardwareSummaries = summaries
                .Where(summary => !summary.IsSimulationMode)
                .OrderBy(summary => summary.RunCompletedAt)
                .ToList();

            worksheet.Cell("A1").Value = "Duplexer Final Test Rig Performance";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A3").Value = "Workbook updated";
            worksheet.Cell("B3").Value = DateTime.Now;
            worksheet.Cell("B3").Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell("A4").Value = "Review rule";
            worksheet.Cell("B4").Value = $"Recommend calibration review when hardware pass rate drops below {CalibrationReviewMinPassRatePct:0.#}% or calibration age exceeds {CalibrationReviewMaxAgeDays:0.#} days.";
            worksheet.Cell("A5").Value = "Chart scope";
            worksheet.Cell("B5").Value = "Hardware runs only. Simulation runs are not stored in this workbook.";

            if (hardwareSummaries.Count == 0)
            {
                worksheet.Cell("A7").Value = "No hardware test runs have been logged yet.";
                worksheet.Column(1).AdjustToContents();
                worksheet.Column(2).Width = 120;
                return;
            }

            HistoricalRunSummary latest = hardwareSummaries.Last();
            worksheet.Cell("A7").Value = "Latest hardware run";
            worksheet.Cell("B7").Value = latest.RunCompletedAt;
            worksheet.Cell("B7").Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell("A8").Value = "Latest run ID";
            worksheet.Cell("B8").Value = latest.RunId;
            worksheet.Cell("A9").Value = "Latest pass rate";
            worksheet.Cell("B9").Value = latest.PassRatePercent;
            worksheet.Cell("B9").Style.NumberFormat.Format = "0.00";
            worksheet.Cell("A10").Value = "Calibration timestamp";
            if (latest.CalibrationTimestamp.HasValue)
                worksheet.Cell("B10").Value = latest.CalibrationTimestamp.Value;
            else
                worksheet.Cell("B10").Value = "N/A";
            if (latest.CalibrationTimestamp.HasValue)
                worksheet.Cell("B10").Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell("A11").Value = "Calibration age (days)";
            if (latest.CalibrationAgeDays.HasValue)
                worksheet.Cell("B11").Value = latest.CalibrationAgeDays.Value;
            else
                worksheet.Cell("B11").Value = "N/A";
            if (latest.CalibrationAgeDays.HasValue)
                worksheet.Cell("B11").Style.NumberFormat.Format = "0.00";
            worksheet.Cell("A12").Value = "Current status";
            worksheet.Cell("B12").Value = BuildCalibrationReviewStatus(latest);
            worksheet.Cell("B12").Style.Font.Bold = true;
            worksheet.Cell("B12").Style.Fill.BackgroundColor = latest.NeedsCalibrationReview
                ? XLColor.FromHtml("#FCE4D6")
                : XLColor.FromHtml("#E2F0D9");

            string tempChartPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
            try
            {
                CreatePerformanceChartImage(hardwareSummaries, tempChartPath);
                worksheet.AddPicture(tempChartPath).MoveTo(worksheet.Cell("A14"));
            }
            finally
            {
                try
                {
                    if (File.Exists(tempChartPath))
                        File.Delete(tempChartPath);
                }
                catch { }
            }

            worksheet.Column(1).AdjustToContents();
            worksheet.Column(2).Width = 120;
        }

        private static List<HistoricalRunSummary> ReadRunSummaries(IXLWorksheet worksheet)
        {
            var summaries = new List<HistoricalRunSummary>();
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                if (row.Cell(1).IsEmpty())
                    continue;

                summaries.Add(new HistoricalRunSummary()
                {
                    RunId = row.Cell(1).GetString(),
                    RunStartedAt = TryGetDateTime(row.Cell(2)),
                    RunCompletedAt = TryGetDateTime(row.Cell(3)),
                    IsSimulationMode = string.Equals(row.Cell(8).GetString(), "Yes", StringComparison.OrdinalIgnoreCase),
                    PassedDutCount = TryGetInt(row.Cell(12)),
                    FailedDutCount = TryGetInt(row.Cell(13)),
                    PassRatePercent = TryGetDouble(row.Cell(14)),
                    CalibrationTimestamp = TryGetNullableDateTime(row.Cell(17)),
                    CalibrationAgeDays = TryGetNullableDouble(row.Cell(18)),
                    NeedsCalibrationReview = string.Equals(row.Cell(19).GetString(), "Calibration review recommended", StringComparison.OrdinalIgnoreCase)
                });
            }

            return summaries;
        }

        private static DateTime TryGetDateTime(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            if (DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;

            return DateTime.MinValue;
        }

        private static DateTime? TryGetNullableDateTime(IXLCell cell)
        {
            if (cell.IsEmpty())
                return null;

            DateTime value = TryGetDateTime(cell);
            return value == DateTime.MinValue ? (DateTime?)null : value;
        }

        private static int TryGetInt(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
                return Convert.ToInt32(cell.GetDouble());

            int.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed);
            return parsed;
        }

        private static double TryGetDouble(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
                return cell.GetDouble();

            double.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed);
            return parsed;
        }

        private static double? TryGetNullableDouble(IXLCell cell)
        {
            if (cell.IsEmpty())
                return null;

            double value = TryGetDouble(cell);
            return double.IsNaN(value) ? (double?)null : value;
        }

        private static void CreatePerformanceChartImage(List<HistoricalRunSummary> hardwareSummaries, string outputPath)
        {
            using (var chart = new Chart())
            {
                chart.Width = 1400;
                chart.Height = 720;
                chart.BackColor = Color.White;
                chart.Palette = ChartColorPalette.None;

                var chartArea = new ChartArea("RigPerformance")
                {
                    BackColor = Color.White
                };
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -45;
                chartArea.AxisX.MajorGrid.LineColor = Color.Gainsboro;
                chartArea.AxisY.Minimum = 0;
                chartArea.AxisY.Maximum = 100;
                chartArea.AxisY.Title = "Pass Rate (%)";
                chartArea.AxisY.MajorGrid.LineColor = Color.Gainsboro;
                chartArea.AxisY2.Enabled = AxisEnabled.True;
                chartArea.AxisY2.Title = "Calibration Age (days)";
                chartArea.AxisY2.MajorGrid.Enabled = false;
                chart.ChartAreas.Add(chartArea);

                chart.Titles.Add(new Title
                {
                    Text = "Test Rig Performance / Calibration Watch",
                    Font = new Font("Segoe UI", 14.0f, FontStyle.Bold)
                });

                var passRateSeries = new Series("Pass Rate %")
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 3,
                    Color = Color.FromArgb(33, 150, 83),
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 7
                };

                var thresholdSeries = new Series("Review Threshold")
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.FromArgb(192, 57, 43)
                };

                var calibrationAgeSeries = new Series("Calibration Age (days)")
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    Color = Color.FromArgb(52, 120, 246),
                    YAxisType = AxisType.Secondary,
                    MarkerStyle = MarkerStyle.Diamond,
                    MarkerSize = 6
                };

                foreach (var summary in hardwareSummaries)
                {
                    string label = summary.RunCompletedAt == DateTime.MinValue
                        ? summary.RunId
                        : summary.RunCompletedAt.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);

                    var passPoint = passRateSeries.Points.AddXY(label, summary.PassRatePercent);
                    passRateSeries.Points[passPoint].ToolTip = $"{summary.RunId}: {summary.PassRatePercent:0.00}% pass";
                    if (summary.NeedsCalibrationReview)
                        passRateSeries.Points[passPoint].Color = Color.FromArgb(192, 57, 43);

                    thresholdSeries.Points.AddXY(label, CalibrationReviewMinPassRatePct);

                    if (summary.CalibrationAgeDays.HasValue)
                        calibrationAgeSeries.Points.AddXY(label, summary.CalibrationAgeDays.Value);
                    else
                        calibrationAgeSeries.Points.AddXY(label, double.NaN);
                }

                chart.Series.Add(passRateSeries);
                chart.Series.Add(thresholdSeries);
                chart.Series.Add(calibrationAgeSeries);
                chart.Legends.Add(new Legend("MainLegend")
                {
                    Docking = Docking.Top,
                    Alignment = StringAlignment.Center,
                    Font = new Font("Segoe UI", 9.0f)
                });

                chart.SaveImage(outputPath, ChartImageFormat.Png);
            }
        }

        private static string BuildCalibrationReviewStatus(TestReportRunSummary summary)
        {
            return BuildCalibrationReviewStatus(new HistoricalRunSummary()
            {
                IsSimulationMode = summary.IsSimulationMode,
                PassRatePercent = summary.PassRatePercent,
                CalibrationAgeDays = summary.CalibrationAgeDays
            });
        }

        private static string BuildCalibrationReviewStatus(HistoricalRunSummary summary)
        {
            if (summary.IsSimulationMode)
                return "Simulation run";
            if (!summary.CalibrationAgeDays.HasValue)
                return "Calibration data missing";
            if (summary.PassRatePercent < CalibrationReviewMinPassRatePct || summary.CalibrationAgeDays.Value > CalibrationReviewMaxAgeDays)
                return "Calibration review recommended";
            return "Monitor";
        }

        private sealed class HistoricalRunSummary
        {
            public string RunId { get; set; }
            public DateTime RunStartedAt { get; set; }
            public DateTime RunCompletedAt { get; set; }
            public bool IsSimulationMode { get; set; }
            public int PassedDutCount { get; set; }
            public int FailedDutCount { get; set; }
            public double PassRatePercent { get; set; }
            public DateTime? CalibrationTimestamp { get; set; }
            public double? CalibrationAgeDays { get; set; }
            public bool NeedsCalibrationReview { get; set; }
        }
    }
}