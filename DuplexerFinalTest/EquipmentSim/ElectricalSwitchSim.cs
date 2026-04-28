using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using System.Collections.Generic;

namespace DuplexerFinalTest.EquipmentSim
{
    public class ElectricalSwitchSim : IElectricalSwitch
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
            return "SIMULATED ELECTRICAL SWITCH";
        }

        public bool Reset()
        {
            return true;
        }

        public bool CloseChannels(List<int> channels, bool openAllFirst = true)
        {
            return true;
        }

        public double MeasureTemperature(TemperatureMeasureMode mode, int channel)
        {
            if (Shared.ClimaticChamber != null && Shared.ClimaticChamber.currentTemperature != null)
                return Shared.ClimaticChamber.currentTemperature.MeasuredTemperature;
            return 25.0;
        }
    }
}
