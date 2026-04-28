using DuplexerFinalTest.Helpers;

namespace DuplexerFinalTest.Models
{
    public class SMUSettingsModel
    {
        public int Channel { get; set; }
        public SMUMeasureMode MeasureMode { get; set; }
        public SMUMeasureMode SourceMode { get; set; }
        public SweepRangeModel SweepRange { get; set; }
        public double Compliance { get; set; }
        public bool IsSourceRangeAuto { get; set; }
        public double SourceRange { get; set; }
        public bool IsMeasureRangeAuto { get; set; }
        public double MeasureRange { get; set; }
    }

    public class SweepRangeModel
    {
        public double Start { get; set; }
        public double Stop { get; set; }
        public int Steps { get; set; }
    }
}
