namespace DuplexerFinalTest.Helpers
{
    public enum TestSequences
    {
        All = 0,
        Base_Z_IB_IOP = 1,
        Base_Z_IPD = 2,
        Remote_Z_IOP = 3,
        Remote_Z_IPV = 4,
        Remote_Z_VPV = 5
    }

    public enum DuplexerTestTypes
    {
        L = 0,
        A = 1,
        Q = 2,
        N = 3,
        PK = 4,
        M = 5,
        B = 6
    }

    public enum SMUMeasureMode
    {
        CURR = 0,
        VOLT = 1
    }

    public enum DUTType
    {
        Base = 0,
        Remote = 1,
        Unknown = 2
    }

    public enum TimerSettings : int
    {
        QuickTimer = 0,
        StartTimer = 1,
        Endtimer = 2
    }

    public enum ChamberModes
    {
        OFF = 0,
        STANDBY = 1,
        CONSTANT = 2
    }

    public enum ChamberProgramEndConditions
    {
        HOLD = 0,
        CONST = 1,
        OFF = 2,
        STANDBY = 3
    }

    public enum ChamberRelayMode
    {
        RELAYON = 0,
        RELAYOFF = 1
    }

    public enum TemperatureMeasureMode
    {
        Thermistor = 0,
        ThermoCouple = 1
    }

    public enum OverallPassFail
    {
        PASS = 0,
        FAIL = 1
    }

    public enum MessageType
    {
        Message = 0,
        Warning = 1,
        Error = 2,
        Success = 3
    }
}
