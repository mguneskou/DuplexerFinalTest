using DuplexerFinalTest.Helpers;

namespace DuplexerFinalTest.Models
{
    public class MeasMainModel
    {
        public string DeviceCode { get; set; }
        public string SerialNo { get; set; }
        public string Operator { get; set; }
        public string TestDate { get; set; }
        public string TestTime { get; set; }
        public string TestRig { get; set; }
        public string SoftwareRev { get; set; }
        public string ItemNo { get; set; }
        public int ItemNoRev { get; set; }
        public bool Passed { get; set; }
        public DuplexerTestTypes TestType { get; set; }
    }

    public class MeasManualTestModel
    {
        public string DeviceCode { get; set; }
        public int TestID { get; set; }
        public double TestData { get; set; }
        public bool Passed { get; set; }
    }
}
