using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DuplexerFinalTest.Equipment
{
    public class SMU : ISMU
    {
        private readonly VisaController _visa = new VisaController();

        public bool IsConnected { get; private set; }

        public bool Connect(string resource)
        {
            try
            {
                IsConnected = _visa.OpenSession(resource);
                return IsConnected;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            _visa.CloseSession();
            IsConnected = false;
        }

        public string GetID()
        {
            return _visa.Query("*IDN?").Trim();
        }

        public bool Reset()
        {
            try
            {
                _visa.Write("*RST");
                Thread.Sleep(100);
                _visa.Write("*CLS");
                Thread.Sleep(100);
                return true;
            }
            catch { return false; }
        }

        public bool SetSweepChannel(SMUSettingsModel settings)
        {
            try
            {
                int ch = settings.Channel;
                string sourceMode = settings.SourceMode == SMUMeasureMode.CURR ? "CURR" : "VOLT";
                string measureMode = settings.MeasureMode == SMUMeasureMode.CURR ? "CURR" : "VOLT";
                _visa.Write($":SOUR{ch}:FUNC:MODE {sourceMode}");
                _visa.Write($":SOUR{ch}:FUNC:SHAP SWE");
                if (settings.IsSourceRangeAuto)
                    _visa.Write($":SOUR{ch}:{sourceMode}:RANG:AUTO ON");
                else
                    _visa.Write($":SOUR{ch}:{sourceMode}:RANG {settings.SourceRange.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                _visa.Write($":SOUR{ch}:{sourceMode}:SWE:STAR {settings.SweepRange.Start.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                _visa.Write($":SOUR{ch}:{sourceMode}:SWE:STOP {settings.SweepRange.Stop.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                _visa.Write($":SOUR{ch}:{sourceMode}:SWE:POIN {settings.SweepRange.Steps}");
                _visa.Write($":SENS{ch}:FUNC \"{measureMode}\"");
                _visa.Write($":SENS{ch}:{measureMode}:PROT {settings.Compliance.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                if (settings.IsMeasureRangeAuto)
                    _visa.Write($":SENS{ch}:{measureMode}:RANG:AUTO ON");
                else
                    _visa.Write($":SENS{ch}:{measureMode}:RANG {settings.MeasureRange.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                _visa.Write($":FORM:ELEM:SENS VOLT,CURR,TIME");
                _visa.Write($":OUTP{ch} ON");
                return true;
            }
            catch { return false; }
        }

        public bool SetReadingChannel(SMUSettingsModel settings)
        {
            try
            {
                int ch = settings.Channel;
                string measureMode = settings.MeasureMode == SMUMeasureMode.CURR ? "CURR" : "VOLT";
                _visa.Write($":SENS{ch}:FUNC \"{measureMode}\"");
                if (settings.IsMeasureRangeAuto)
                    _visa.Write($":SENS{ch}:{measureMode}:RANG:AUTO ON");
                else
                    _visa.Write($":SENS{ch}:{measureMode}:RANG {settings.MeasureRange.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                _visa.Write($":OUTP{ch} ON");
                return true;
            }
            catch { return false; }
        }

        public bool InitiateReading(List<int> channels, SMUSettingsModel triggerSettings)
        {
            try
            {
                string chList = string.Join(",", channels);
                _visa.Write($":ARM:TRIG:COUNT {triggerSettings.SweepRange.Steps}");
                _visa.Write(":TRIG:ACQ:COUNT 1");
                _visa.Write($":TRIG:TRAN:SOURCE TIM");
                _visa.Write($":TRIG:TRAN:COUNT {triggerSettings.SweepRange.Steps}");
                _visa.Write(":INIT (@" + chList + ")");
                return true;
            }
            catch { return false; }
        }

        public bool ReadData(int ch, bool fromStart, int len, ref double[,] data, out int actrow)
        {
            actrow = 0;
            try
            {
                string readStart = fromStart ? "STAR" : "CURR";
                // Poll for OPC
                int timeout = 30000;
                int elapsed = 0;
                while (elapsed < timeout)
                {
                    string opc = _visa.Query("*OPC?").Trim();
                    if (opc == "1") break;
                    Thread.Sleep(100);
                    elapsed += 100;
                }
                string response = _visa.Query($"SENS{ch}:DATA? {readStart},{len}").Trim();
                if (string.IsNullOrEmpty(response)) { actrow = 0; return true; }
                string[] tokens = response.Split(',');
                // Each reading = 3 values (VOLT, CURR, TIME)
                int rows = tokens.Length / 3;
                actrow = Math.Min(rows, len);
                for (int i = 0; i < actrow; i++)
                {
                    double.TryParse(tokens[i * 3], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out data[i, 0]);     // Voltage
                    double.TryParse(tokens[i * 3 + 1], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out data[i, 1]);     // Current
                    double.TryParse(tokens[i * 3 + 2], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out data[i, 2]);     // Time
                }
                return true;
            }
            catch { actrow = 0; return false; }
        }

        public bool CloseAllChannels()
        {
            try
            {
                _visa.Write(":OUTP1 OFF");
                _visa.Write(":OUTP2 OFF");
                return true;
            }
            catch { return false; }
        }
    }
}
