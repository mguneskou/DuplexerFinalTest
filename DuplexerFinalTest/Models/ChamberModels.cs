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
}
