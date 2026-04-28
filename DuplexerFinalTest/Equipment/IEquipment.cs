namespace DuplexerFinalTest.Equipment
{
    public interface IEquipment
    {
        bool Connect(string resource);
        void Disconnect();
        bool IsConnected { get; }
        string GetID();
        void SendCommand(string command);
        bool WaitForComplete(int timeoutSec);
    }
}
