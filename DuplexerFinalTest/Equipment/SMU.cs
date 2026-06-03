using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        // Marks the link as stale after a runtime SCPI failure so Shared.ReconnectDisconnectedEquipment
        // will actually reconnect on the next retry instead of trusting an out-of-date IsConnected flag.
        private void MarkLinkLost()
        {
            try { _visa.CloseSession(); } catch { }
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
                Thread.Sleep(500);
                _visa.Write("*CLS");
                Thread.Sleep(100);
                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        // SetSweepChannel and SetReadingChannel share the same full B2902A configuration
        // sequence.  A "reading" channel in this system is simply a sweep channel where
        // start == stop (constant source), so the setup is identical.
        public bool SetSweepChannel(SMUSettingsModel settings)
        {
            return ConfigureChannel(settings);
        }

        public bool SetReadingChannel(SMUSettingsModel settings)
        {
            return ConfigureChannel(settings);
        }

        // Full Keysight B2902A channel setup matching the LabVIEW SCPI trace sequence.
        // ARM source = IMM, TRIG source = TIM (minimum), so INIT starts the sweep
        // immediately without needing an external BUS / GPIB-GET trigger.
        private bool ConfigureChannel(SMUSettingsModel settings)
        {
            try
            {
                int ch = settings.Channel;
                bool isCurrSource = settings.SourceMode == SMUMeasureMode.CURR;
                string srcType  = isCurrSource ? "CURR" : "VOLT";
                string compType = isCurrSource ? "VOLT" : "CURR"; // compliance is always opposite of source

                // Guard: B2902A does not accept POIN 0 or TRIG:COUN 0 — clamp to minimum of 1.
                // Reading channels in test-flow JSON may have SweepNumPoints=0 to indicate
                // "passive" mode; the JSON should be corrected to match the sweep channel count,
                // but this guard prevents silent data loss if the JSON is not yet updated.
                int steps = Math.Max(1, settings.SweepRange.Steps);

                // ── Auto-wait / calculation setup ─────────────────────────────
                _visa.Write($"SOUR{ch}:WAIT:AUTO ON");
                _visa.Write($"SENS{ch}:WAIT:AUTO ON");
                _visa.Write($"CALC{ch}:MATH:STAT OFF");
                _visa.Write($"CALC{ch}:CLIM:STAT OFF");
                _visa.Write($"SENS{ch}:RES:MODE MAN");

                // ── Source range ──────────────────────────────────────────────
                if (isCurrSource)
                {
                    _visa.Write($"SOUR{ch}:SWE:RANG FIX");
                    _visa.Write($"SOUR{ch}:CURR:RANG:AUTO ON");
                    if (!settings.IsSourceRangeAuto)
                        _visa.Write($"SOUR{ch}:CURR:RANG {F(settings.SourceRange)}");
                }
                else
                {
                    _visa.Write($"SOUR{ch}:SWE:RANG AUTO");
                    _visa.Write($"SOUR{ch}:VOLT:RANG:AUTO ON");
                    if (!settings.IsSourceRangeAuto)
                    {
                        _visa.Write($"SOUR{ch}:SWE:RANG FIX");
                        _visa.Write($"SOUR{ch}:VOLT:RANG {F(settings.SourceRange)}");
                    }
                }

                // ── Source function, initial value, compliance, sweep params ──
                _visa.Write($"SOUR{ch}:FUNC DC");
                _visa.Write($"SOUR{ch}:{srcType} {F(settings.SweepRange.Start)}");
                _visa.Write($"SOUR{ch}:{srcType}:TRIG {F(settings.SweepRange.Start)}");
                _visa.Write($"SENS{ch}:{compType}:PROT {F(settings.Compliance)}");
                _visa.Write($"SOUR{ch}:FUNC:MODE {srcType}");
                _visa.Write($"SOUR{ch}:SWE:STA SING");
                _visa.Write($"SOUR{ch}:{srcType}:POIN {steps}");
                _visa.Write($"SOUR{ch}:{srcType}:MODE SWE");
                _visa.Write($"SOUR{ch}:{srcType}:STAR {F(settings.SweepRange.Start)}");
                _visa.Write($"SOUR{ch}:{srcType}:STOP {F(settings.SweepRange.Stop)}");
                _visa.Write($"SOUR{ch}:SWE:SPAC LIN");
                _visa.Write($"TRIG{ch}:TRAN:DEL 0");

                // ── Data format — ASCII for straightforward string parsing ────
                _visa.Write("FORM ASC");
                _visa.Write("FORM:ELEM:SENS VOLT,CURR,TIME");

                // ── Sense function setup (VOLT then CURR) ─────────────────────
                _visa.Write($"SENS{ch}:FUNC:OFF:ALL");

                _visa.Write($"SENS{ch}:FUNC:ON \"VOLT\"");
                _visa.Write($"SENS{ch}:VOLT:APER:AUTO ON");
                _visa.Write($"SENS{ch}:VOLT:NPLC 1");
                if (isCurrSource && !settings.IsMeasureRangeAuto)
                    _visa.Write($"SENS{ch}:VOLT:RANG {F(settings.MeasureRange)}");
                else
                    _visa.Write($"SENS{ch}:VOLT:RANG:AUTO ON");
                _visa.Write($"TRIG{ch}:ACQ:DEL 0.0005");

                _visa.Write($"SENS{ch}:FUNC:ON \"CURR\"");
                _visa.Write($"SENS{ch}:CURR:APER:AUTO ON");
                _visa.Write($"SENS{ch}:CURR:NPLC 1");
                if (!isCurrSource && !settings.IsMeasureRangeAuto)
                    _visa.Write($"SENS{ch}:CURR:RANG {F(settings.MeasureRange)}");
                else
                {
                    _visa.Write($"SENS{ch}:CURR:RANG:AUTO ON");
                    if (settings.IsMeasureRangeAuto && settings.MeasureRange > 0)
                        _visa.Write($"SENS{ch}:CURR:RANG:AUTO:LLIM {F(settings.MeasureRange)}");
                }
                _visa.Write($"TRIG{ch}:ACQ:DEL 0.0005");

                // ── 4-wire (Kelvin) sense, output filter, high-cap off ────────
                _visa.Write($"SENS{ch}:REM ON");
                _visa.Write($"OUTP{ch}:HCAP OFF");
                _visa.Write($"OUTP{ch}:FILT ON");
                _visa.Write($"OUTP{ch}:FILT:AUTO OFF");
                _visa.Write($"OUTP{ch}:FILT:TCON 5E-06");

                // ── Source/sense wait gain and offset ─────────────────────────
                _visa.Write($"SOUR{ch}:WAIT:GAIN 1");
                _visa.Write($"SOUR{ch}:WAIT:OFFS 0");
                _visa.Write($"SENS{ch}:WAIT:GAIN 1");
                _visa.Write($"SENS{ch}:WAIT:OFFS 0");
                _visa.Write($"SOUR{ch}:FUNC:TRIG:CONT ON");

                // ── ARM / TRIGGER — immediate arm, timer-paced sweep ──────────
                // ARM source IMM  → no external trigger needed to arm
                // TRIG source TIM → instrument runs all sweep steps back-to-back
                _visa.Write($"ARM{ch}:ALL:COUN 1");
                _visa.Write($"ARM{ch}:ALL:DEL 0");
                _visa.Write($"ARM{ch}:ALL:SOUR IMM");
                _visa.Write($"TRIG{ch}:ALL:COUN {steps}");
                _visa.Write($"TRIG{ch}:ALL:SOUR TIM");
                _visa.Write($"TRIG{ch}:ALL:TIM MIN");

                _visa.Write($"SOUR{ch}:WAIT ON");
                _visa.Write($"SENS{ch}:WAIT ON");
                _visa.Write($"OUTP{ch}:STAT ON");

                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        public bool InitiateReading(List<int> channels, SMUSettingsModel triggerSettings)
        {
            try
            {
                // Group channels on this instrument so they trigger simultaneously
                if (channels.Count > 1)
                    _visa.Write($"SYST:GRO (@{string.Join(",", channels)})");

                // Enable Service Request on operation status change
                _visa.Write("STAT:OPER:PTR 7020");
                _visa.Write("STAT:OPER:NTR 7020");
                _visa.Write("STAT:OPER:ENAB 7020");
                _visa.Write("*SRE 128");

                // Reset internal time counter
                _visa.Write("SYST:TIME:TIM:COUN:RES");

                // Initiate — ARM source is IMM so measurement starts immediately
                _visa.Write($"INIT (@{string.Join(",", channels)})");

                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        public bool ReadData(int ch, bool fromStart, int len, ref double[,] data, out int actrow)
        {
            actrow = 0;

            // The entire channel buffer is read on the first call (fromStart=true).
            // Subsequent calls with fromStart=false return 0 rows, ending the outer loop.
            if (!fromStart)
                return true;

            try
            {
                // Poll for sweep completion (up to 60 s)
                int timeout = 60000;
                int elapsed = 0;
                while (elapsed < timeout)
                {
                    string opc = _visa.Query("*OPC?").Trim();
                    if (opc == "1" || opc == "+1") break;  // IEEE 488.2 allows either format
                    Thread.Sleep(200);
                    elapsed += 200;
                }

                // Read all buffered data for this channel in one ASCII block
                string response = _visa.Query($"SENS{ch}:DATA?").Trim();
                if (string.IsNullOrEmpty(response))
                    return true;

                string[] tokens = response.Split(',');
                // Each reading contains 3 values: VOLT, CURR, TIME
                int rows = tokens.Length / 3;
                actrow = Math.Min(rows, len);

                for (int i = 0; i < actrow; i++)
                {
                    double.TryParse(tokens[i * 3],     NumberStyles.Any, CultureInfo.InvariantCulture, out data[i, 0]); // Voltage
                    double.TryParse(tokens[i * 3 + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out data[i, 1]); // Current
                    double.TryParse(tokens[i * 3 + 2], NumberStyles.Any, CultureInfo.InvariantCulture, out data[i, 2]); // Time
                }
                return true;
            }
            catch { MarkLinkLost(); actrow = 0; return false; }
        }

        public bool CloseAllChannels()
        {
            try
            {
                _visa.Write("OUTP1 OFF");
                _visa.Write("OUTP2 OFF");
                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        // Formats a double for SCPI using InvariantCulture (e.g. "0.045" not "0,045")
        private static string F(double value)
        {
            return value.ToString("G", CultureInfo.InvariantCulture);
        }
    }
}
