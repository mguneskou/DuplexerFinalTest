using System;

namespace DuplexerFinalTest.Models
{
    public class InfoModel
    {
        public string Operator { get; set; }
        public string TestDate { get; set; }
        public string TestTime { get; set; }
        public TestSequenceModel Test { get; set; }
        public int NumberOfBaseUnits { get; set; }
        public int NumberOfRemoteUnits { get; set; }
    }
}
