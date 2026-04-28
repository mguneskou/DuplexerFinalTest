using DuplexerFinalTest.Equipment;

namespace DuplexerFinalTest.EquipmentSim
{
    public class OpticalSwitchSim : IOpticalSwitch
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
            return "SIMULATED OPTICAL SWITCH";
        }

        public bool Reset()
        {
            return true;
        }

        public bool CloseChannel(int channel)
        {
            return true;
        }
    }
}
