namespace DuplexerFinalTest.Models
{
    public class ChamberTemperatureModel
    {
        public double MeasuredTemperature { get; set; }
        public double SetPointTemperature { get; set; }
        public double HigherLimitTemperature { get; set; }
        public double LowerLimitTemperature { get; set; }
    }

    public class ChamberHumidityModel
    {
        public double MeasuredHumidity { get; set; }
        public double SetPointHumidity { get; set; }
        public double HigherLimitHumidity { get; set; }
        public double LowerLimitHumidity { get; set; }
    }

    // Parsed response from PRGM MON? — "MON, RUN, 3/15, 0:45:23"
    // Status values: RUN (soak timer active), WAIT (waiting for GRANTY), PAUSE, END, STANDBY
    public class ChamberProgramMonitorModel
    {
        public string Status { get; set; } = string.Empty;
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string RemainingTime { get; set; } = string.Empty;
    }
}
