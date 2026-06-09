using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;

namespace DuplexerFinalTest.EquipmentSim
{
    public class SMUSim : ISMU
    {
        public bool IsConnected { get; private set; }

        // Simulation configuration
        private double _simPartSpreadPct = 1.0;   // percent, e.g. 1.0 => ±1%
        private double _simMeasNoisePct = 0.05;   // percent measurement noise
        private Random _rng = new Random();

        // Per-assigned 'part' offsets (assigned on SetReadingChannel)
        private int _currentPartId = 0;
        private int _lastAssignedPartId = 0;
        private Dictionary<int, double> _partOffsets = new Dictionary<int, double>();
        private Dictionary<string, double> _partOffsetsBySerial = new Dictionary<string, double>();
        private string _lastAssignedPartSerial = null;

        public bool Connect(string resource)
        {
            IsConnected = true;
            try
            {
                var gs = Shared.sharedGeneralSettings?.GeneralSettings?[0];
                if (gs != null)
                {
                    string spRaw = gs.SIM_PART_SPREAD_PCT ?? string.Empty;
                    string mnRaw = gs.SIM_MEAS_NOISE_PCT ?? string.Empty;
                    // Allow values like "1", "1.0" or "1%"
                    spRaw = spRaw.Trim().TrimEnd('%');
                    mnRaw = mnRaw.Trim().TrimEnd('%');
                    if (double.TryParse(spRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sp))
                        _simPartSpreadPct = sp;
                    if (double.TryParse(mnRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mn))
                        _simMeasNoisePct = mn;
                    Shared.logger?.Log($"SMUSim: SIM_PART_SPREAD_PCT={_simPartSpreadPct}, SIM_MEAS_NOISE_PCT={_simMeasNoisePct}", MessageType.Message);
                }
            }
            catch { }
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public string GetID()
        {
            return "SIMULATED SMU";
        }

        public bool Reset()
        {
            return true;
        }

        private readonly Dictionary<int, SMUSettingsModel> _channelSettings = new Dictionary<int, SMUSettingsModel>();

        public bool SetSweepChannel(SMUSettingsModel settings)
        {
            _channelSettings[settings.Channel] = settings;
            return true;
        }

        public bool SetReadingChannel(SMUSettingsModel settings)
        {
            _channelSettings[settings.Channel] = settings;
            // Assign a simulated per-part offset for this reading call so repeated reads
            // within the same logical part use the same offset for the duration of the call.
            _currentPartId++;
            double spread = _simPartSpreadPct / 100.0;
            double factor = 1.0 + (_rng.NextDouble() * 2.0 - 1.0) * spread; // uniform in [1-spread,1+spread]
            // Prefer deterministic per-part offsets if a DUT serial is available in Shared
            string serial = Shared.CurrentSimPartSerial;
            if (!string.IsNullOrEmpty(serial))
            {
                if (!_partOffsetsBySerial.TryGetValue(serial, out double serialFactor))
                {
                    // Derive deterministic seed from serial (numeric if possible, otherwise simple hashing)
                    long seed = 0;
                    if (!long.TryParse(serial, out seed))
                    {
                        seed = 0;
                        foreach (char c in serial)
                            seed = seed * 31 + c;
                    }
                    var r = new Random((int)(seed & 0x7FFFFFFF));
                    serialFactor = 1.0 + (r.NextDouble() * 2.0 - 1.0) * ( _simPartSpreadPct / 100.0 );
                    _partOffsetsBySerial[serial] = serialFactor;
                }
                _lastAssignedPartSerial = serial;
                _partOffsets[_currentPartId] = serialFactor;
                _lastAssignedPartId = _currentPartId;
                try { Shared.logger?.Log($"SMUSim: Assigned serial={serial}, partId={_currentPartId}, offsetFactor={serialFactor:F6}, spreadPct={_simPartSpreadPct}", MessageType.Message); } catch { }
            }
            else
            {
                _partOffsets[_currentPartId] = factor;
                _lastAssignedPartId = _currentPartId;
                try
                {
                    Shared.logger?.Log($"SMUSim: Assigned partId={_currentPartId}, offsetFactor={factor:F6}, spreadPct={_simPartSpreadPct}", MessageType.Message);
                }
                catch { }
            }
            return true;
        }

        public bool InitiateReading(List<int> channels, SMUSettingsModel triggerSettings)
        {
            return true;
        }

        public bool ReadData(int ch, bool fromStart, int len, ref double[,] data, out int actrow)
        {
            // Simulate paged read: return all data on first call, signal done on subsequent calls
            if (!fromStart)
            {
                actrow = 0;
                return true;
            }
            actrow = len;
            _channelSettings.TryGetValue(ch, out SMUSettingsModel s);
            bool isBias = s != null && s.SweepRange != null && s.SweepRange.Start == s.SweepRange.Stop;
            // Determine offset factor for this 'part' (assigned at SetReadingChannel)
            double offsetFactor = 1.0;
            if (!_partOffsets.TryGetValue(_lastAssignedPartId, out offsetFactor)) offsetFactor = 1.0;
            double measNoiseFrac = _simMeasNoisePct / 100.0;

            for (int i = 0; i < len; i++)
            {
                double volt = 0.0;
                double curr = 0.0;
                if (isBias && s != null)
                {
                    // Constant-source channel: return the fixed source value with small noise and part offset
                    if (s.SourceMode == SMUMeasureMode.VOLT)
                    {
                        volt = s.SweepRange.Start * offsetFactor;
                        volt += volt * ((_rng.NextDouble() * 2.0 - 1.0) * measNoiseFrac);
                        curr = (_rng.NextDouble() * 1e-6);
                    }
                    else
                    {
                        curr = s.SweepRange.Start * offsetFactor;
                        curr += curr * ((_rng.NextDouble() * 2.0 - 1.0) * measNoiseFrac);
                        volt = (_rng.NextDouble() * 1e-3);
                    }
                }
                else
                {
                    // Sweep: generate base ramp then apply part offset and measurement noise
                    volt = 1.2 + _rng.NextDouble() * 0.3;
                    curr = 0.001 + (i / (double)len) * 0.044;   // 0 -> 0.045 A
                    volt *= offsetFactor;
                    curr *= offsetFactor;
                    // measurement noise
                    volt += volt * ((_rng.NextDouble() * 2.0 - 1.0) * measNoiseFrac);
                    curr += curr * ((_rng.NextDouble() * 2.0 - 1.0) * measNoiseFrac);
                }

                data[i, 0] = volt;
                data[i, 1] = curr;
                data[i, 2] = i * 0.001; // Time
            }
            try
            {
                if (!string.IsNullOrEmpty(_lastAssignedPartSerial))
                    Shared.logger?.Log($"SMUSim Read: ch={ch}, partId={_lastAssignedPartId}, serial={_lastAssignedPartSerial}, offsetFactor={offsetFactor:F6}, measNoisePct={_simMeasNoisePct}", MessageType.Message);
                else
                    Shared.logger?.Log($"SMUSim Read: ch={ch}, partId={_lastAssignedPartId}, offsetFactor={offsetFactor:F6}, measNoisePct={_simMeasNoisePct}", MessageType.Message);
            }
            catch { }
            return true;
        }

        public bool CloseAllChannels()
        {
            return true;
        }
    }
}
