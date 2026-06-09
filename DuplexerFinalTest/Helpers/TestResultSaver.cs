using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DuplexerFinalTest.Helpers
{
    public class TestResultSaver
    {
        public void SaveResults(string serialNumber, TestResultModel testResults, TestSequences test, int sweepNo, double temperature)
        {
            try
            {
                string passFail = (testResults.OverallPassFail == OverallPassFail.PASS) ? "" : "_FAILED";
                string dateStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Write per-serial results into subfolder to match legacy structure: <ResultsRoot>\Base\<serial>\...
                string folder = (test == TestSequences.Base_Z_IB_IOP || test == TestSequences.Base_Z_IPD)
                    ? Path.Combine(Shared.BaseResultsPath, serialNumber)
                    : Path.Combine(Shared.RemoteResultsPath, serialNumber);

                Directory.CreateDirectory(folder);
                string fileName = $"{serialNumber}_{test}_{temperature:F1}C_sweep_{sweepNo}_{dateStr}{passFail}.csv";
                string filePath = Path.Combine(folder, fileName);

                var sb = new StringBuilder();

                // Legacy-style header block (FileFormatVersion + ###HEAD info)
                try
                {
                    var s = Shared.sharedGeneralSettings?.GeneralSettings?.FirstOrDefault();
                    string partNumber = string.Empty;
                    if (s != null)
                        partNumber = (test == TestSequences.Base_Z_IB_IOP || test == TestSequences.Base_Z_IPD)
                            ? s.BASE_ITEM_NUMBER ?? string.Empty
                            : s.REMOTE_ITEM_NUMBER ?? string.Empty;

                    // Determine UUT slot (position) by looking up the DUT in the active test plan
                    int uutSlot = 0;
                    DUTModel foundDut = null;
                    var plan = Shared.infoModel?.Test;
                    if (plan != null)
                    {
                        var list = (test == TestSequences.Base_Z_IB_IOP || test == TestSequences.Base_Z_IPD)
                            ? plan.BaseDUTs
                            : plan.RemoteDUTs;
                        if (list != null)
                        {
                            foundDut = list.FirstOrDefault(d => string.Equals(d.SerialNumber?.Trim(), serialNumber?.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (foundDut != null) uutSlot = foundDut.Slot;
                        }
                    }

                    // Top-level result filename: use the shared run-level name if already established.
                    string topLevelFile = Shared.TopLevelResultFileName;
                    if (string.IsNullOrWhiteSpace(topLevelFile))
                    {
                        if (!string.IsNullOrWhiteSpace(Shared.infoModel?.TestDate) && !string.IsNullOrWhiteSpace(Shared.infoModel?.TestTime))
                        {
                            if (DateTime.TryParseExact($"{Shared.infoModel.TestDate} {Shared.infoModel.TestTime}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtStart))
                                topLevelFile = $"ZODIAC-TS_{dtStart:yyyy-MM-dd_HH-mm-ss}.csv";
                        }
                        if (string.IsNullOrWhiteSpace(topLevelFile))
                            topLevelFile = $"ZODIAC-TS_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                        Shared.TopLevelResultFileName = topLevelFile;
                    }

                    // Thermistor reading (best-effort, non-fatal)
                    string thermistorStr = string.Empty;
                    try
                    {
                        if (foundDut != null)
                        {
                            double therm = foundDut.ReadThermistor;
                            thermistorStr = therm.ToString("F1", CultureInfo.InvariantCulture);
                        }
                    }
                    catch { thermistorStr = string.Empty; }

                    // Calibration value (best-effort)
                    string calibStr = string.Empty;
                    try
                    {
                        if (uutSlot > 0)
                        {
                            double calib = Shared.GetCalibrationValueForResultFile(filePath, test, uutSlot);
                            calibStr = calib.ToString("F6", CultureInfo.InvariantCulture);
                        }
                    }
                    catch { calibStr = string.Empty; }

                    sb.AppendLine("FileFormatVersion,1.0");
                    sb.AppendLine("###HEAD");
                    sb.AppendLine($"Part Number,{partNumber}");
                    sb.AppendLine($"Serial Number,{serialNumber}");
                    sb.AppendLine($"UUT Position,{(uutSlot > 0 ? uutSlot.ToString() : string.Empty)}");
                    sb.AppendLine($"Top Level Result FIle,{topLevelFile}");
                    sb.AppendLine($"Chamber Temperature DegC,{temperature.ToString("F1", CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"Thermistor Temperature DegC,{thermistorStr}");
                    sb.AppendLine($"Calibration Value: ,{calibStr}");
                    sb.AppendLine("###COLUMNINFO");
                }
                catch { }

                // Write measurement data with column headers
                switch (test)
                {
                    case TestSequences.Base_Z_IB_IOP:
                        WriteBase_Z_IB_IOP(sb, testResults.Base_Z_IB_IOP_Results);
                        break;
                    case TestSequences.Base_Z_IPD:
                        WriteBase_Z_IPD(sb, testResults.Base_Z_IPD_Results);
                        break;
                    case TestSequences.Remote_Z_IOP:
                        WriteRemote_Z_IOP(sb, testResults.Remote_Z_IOP_Results);
                        break;
                    case TestSequences.Remote_Z_IPV:
                        WriteRemote_Z_IPV(sb, testResults.Remote_Z_IPV_Results);
                        break;
                    case TestSequences.Remote_Z_VPV:
                        WriteRemote_Z_VPV(sb, testResults.Remote_Z_VPV_Results);
                        break;
                }

                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("TestResultSaver.SaveResults", ex);
            }
        }

        

        private void WriteBase_Z_IB_IOP(StringBuilder sb, Result_Base_Z_IB_IOP r)
        {
            sb.AppendLine("CH1 Voltage(V),CH1 Current(A),CH1 Time,CH2 Voltage(V),CH2 Current(A),CH2 Time,CH4 Voltage(V),CH4 Current(A),CH4 Time,CH4 Power(W)");
            int count = r.CH1_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{r.CH1_Voltage[i]},{r.CH1_Current[i]},{r.CH1_Time[i]}," +
                    $"{SafeGet(r.CH2_Voltage, i)},{SafeGet(r.CH2_Current, i)},{SafeGetStr(r.CH2_Time, i)}," +
                    $"{SafeGet(r.CH4_Voltage, i)},{SafeGet(r.CH4_Current, i)},{SafeGetStr(r.CH4_Time, i)},{SafeGet(r.CH4_Power, i)}");
            }
        }

        private void WriteBase_Z_IPD(StringBuilder sb, Result_Base_Z_IB_IPD r)
        {
            sb.AppendLine("CH3 Voltage(V),CH3 Current(A),CH3 Time,CH2 Voltage(V),CH2 Current(A),CH2 Time");
            int count = r.CH3_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{r.CH3_Voltage[i]},{r.CH3_Current[i]},{r.CH3_Time[i]}," +
                    $"{SafeGet(r.CH2_Voltage, i)},{SafeGet(r.CH2_Current, i)},{SafeGetStr(r.CH2_Time, i)}");
            }
        }

        private void WriteRemote_Z_IOP(StringBuilder sb, Result_Remote_Z_IOP r)
        {
            sb.AppendLine("CH1 Voltage(V),CH1 Current(A),CH1 Time,CH4 Voltage(V),CH4 Current(A),CH4 Time,CH4 Power(W)");
            int count = r.CH1_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{r.CH1_Voltage[i]},{r.CH1_Current[i]},{r.CH1_Time[i]}," +
                    $"{SafeGet(r.CH4_Voltage, i)},{SafeGet(r.CH4_Current, i)},{SafeGetStr(r.CH4_Time, i)},{SafeGet(r.CH4_Power, i)}");
            }
        }

        private void WriteRemote_Z_IPV(StringBuilder sb, Result_Remote_Z_IPV r)
        {
            sb.AppendLine("CH3 Voltage(V),CH3 Current(A),CH3 Time,CH2 Voltage(V),CH2 Current(A),CH2 Time");
            int count = r.CH3_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{r.CH3_Voltage[i]},{r.CH3_Current[i]},{r.CH3_Time[i]}," +
                    $"{SafeGet(r.CH2_Voltage, i)},{SafeGet(r.CH2_Current, i)},{SafeGetStr(r.CH2_Time, i)}");
            }
        }

        private void WriteRemote_Z_VPV(StringBuilder sb, Result_Remote_Z_VPV r)
        {
            sb.AppendLine("CH3 Voltage(V),CH3 Current(A),CH3 Time,CH2 Voltage(V),CH2 Current(A),CH2 Time,CH5 Current(A),Power (VxA)");
            int count = r.CH3_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{r.CH3_Voltage[i]},{r.CH3_Current[i]},{r.CH3_Time[i]}," +
                    $"{SafeGet(r.CH2_Voltage, i)},{SafeGet(r.CH2_Current, i)},{SafeGetStr(r.CH2_Time, i)}," +
                    $"{SafeGet(r.CH5_Current, i)},{SafeGet(r.Power, i)}");
            }
        }

        private double SafeGet(System.Collections.Generic.List<double> list, int i)
            => (list != null && i < list.Count) ? list[i] : 0.0;
        private string SafeGetStr(System.Collections.Generic.List<string> list, int i)
            => (list != null && i < list.Count) ? list[i] : "";
    }
}
