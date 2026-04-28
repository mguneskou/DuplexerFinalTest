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
            catch { return false; }
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
            catch { return false; }
        }

        public double MeasureTemperature(TemperatureMeasureMode mode, int channel)
        {
            try
            {
                _visa.Write($"CONF:TEMP THER,10000,1,0.1,(@{channel})");
                Thread.Sleep(50);
                _visa.Write($"ROUT:SCAN (@{channel})");
                Thread.Sleep(50);
                _visa.Write("INIT");
                Thread.Sleep(1000);
                string result = _visa.Query("FETC?").Trim();
                if (double.TryParse(result, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double temp))
                {
                    return temp;
                }
                return double.NaN;
            }
            catch { return double.NaN; }
        }
    }
}
