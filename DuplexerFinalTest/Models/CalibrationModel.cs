using System.Collections.Generic;

namespace DuplexerFinalTest.Models
{
    public class CalibrationModel
    {
        public BaseCalibration Base { get; set; } = new BaseCalibration();
        public RemoteCalibration Remote { get; set; } = new RemoteCalibration();
    }

    public class BaseCalibration
    {
        public Dictionary<string, double> Z_IB_IOP { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> Z_IPD { get; set; } = new Dictionary<string, double>();
    }

    public class RemoteCalibration
    {
        public Dictionary<string, double> Z_IOP { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> Z_VPV { get; set; } = new Dictionary<string, double>();
    }
}
