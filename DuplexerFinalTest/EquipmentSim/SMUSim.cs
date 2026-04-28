using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;

namespace DuplexerFinalTest.EquipmentSim
{
    public class SMUSim : ISMU
    {
        public bool IsConnected { get; private set; }

        public bool Connect(string resource)
        {
            IsConnected = true;
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

        public bool SetSweepChannel(SMUSettingsModel settings)
        {
            return true;
        }

        public bool SetReadingChannel(SMUSettingsModel settings)
        {
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
            var rng = new Random();
            for (int i = 0; i < len; i++)
            {
                data[i, 0] = 1.2 + rng.NextDouble() * 0.3;        // Voltage
                data[i, 1] = 0.001 + (i / (double)len) * 0.044;   // Current (sweep 0→45mA)
                data[i, 2] = i * 0.001;                            // Time
            }
            return true;
        }

        public bool CloseAllChannels()
        {
            return true;
        }
    }
}
