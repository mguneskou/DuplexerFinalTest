using System;

namespace DuplexerFinalTest.Helpers
{
    /// <summary>
    /// Thrown when equipment communication fails during a test step.
    /// Caught by TestRun.RunTest to trigger the auto-retry + countdown dialog.
    /// </summary>
    public class EquipmentCommunicationException : Exception
    {
        public EquipmentCommunicationException(string message) : base(message) { }
        public EquipmentCommunicationException(string message, Exception inner) : base(message, inner) { }
    }
}
