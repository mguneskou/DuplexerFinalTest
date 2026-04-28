using System;
using System.Threading;

namespace DuplexerFinalTest.Equipment
{
    public class OpticalSwitch : IOpticalSwitch
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
                Thread.Sleep(500);
                return true;
            }
            catch { return false; }
        }

        public bool CloseChannel(int channel)
        {
            try
            {
                _visa.Write($"ROUT1:CHAN1 A,{channel}");
                Thread.Sleep(100);
                return true;
            }
            catch { return false; }
        }
    }
}
