using DuplexerFinalTest.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DuplexerFinalTest.Equipment
{
    public class ElectricalSwitch : IElectricalSwitch
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
                _visa.Write("ROUT:OPEN:ALL");
                Thread.Sleep(100);
                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        public bool CloseChannels(List<int> channels, bool openAllFirst = true)
        {
            try
            {
                string channelList = string.Join(",", channels.Select(c => $"{c}"));
                if (openAllFirst)
                {
                    _visa.Write($"ROUT:CLOS:EXCL (@{channelList})");
                }
                else
                {
                    _visa.Write($"ROUT:CLOS (@{channelList})");
                }
                Thread.Sleep(50);
                return true;
            }
            catch { MarkLinkLost(); return false; }
        }

        public double MeasureTemperature(TemperatureMeasureMode mode, int channel)
        {
            try
            {
                // Configure the measurement type for this channel
                if (mode == TemperatureMeasureMode.ThermoCouple)
                    _visa.Write($"CONF:TEMP TC,K,(@{channel})");
                else
                    _visa.Write($"CONF:TEMP THER,5000,(@{channel})"); // 5 kΩ thermistor

                // Set scan list and trigger source, then initiate
                _visa.Write($"ROUT:SCAN (@{channel})");
                _visa.Write("TRIG:SOUR IMM");
                _visa.Write("TRIG:COUN 1");
                _visa.Write("INIT");

                // Allow time for the scan to complete (typical 34972A conversion ~1 s)
                Thread.Sleep(1500);

                string result = _visa.Query("FETC?").Trim();
                if (double.TryParse(result, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double temp))
                {
                    return temp;
                }
                return double.NaN;
            }
            catch { MarkLinkLost(); return double.NaN; }
        }
    }
}
