using System.Collections.Generic;
using System.ComponentModel;

namespace DuplexerFinalTest.Models
{
    public class TestFlowModel
    {
        public string TestName { get; set; }
        public SMUModel SMU1 { get; set; }
        public ElectricalSwitchModel ElectricalSwitch1 { get; set; }
        public string Measurement1 { get; set; }
        public OpticalSwitchModel OpticalSwitch1 { get; set; }
        public OpticalSwitchModel OpticalSwitch2 { get; set; }
        public string Measurement2 { get; set; }
        public ElectricalSwitchModel ElectricalSwitch2 { get; set; }
        public SMUModel SMU2 { get; set; }
    }

    public class OpticalSwitchModel
    {
        public string OpticalSwitch_ID { get; set; }
        public string MakeModel { get; set; }
        public string OpticalSwitchType { get; set; }
        public List<OpticalSwitchRoutesModel> Routes { get; set; }
    }

    public class OpticalSwitchRoutesModel
    {
        public string RouteName { get; set; }
        public int FromChannel { get; set; }
        public int ToChannel { get; set; }
    }

    public class ElectricalSwitchModel
    {
        public string ElectricalSwitch_ID { get; set; }
        public string MakeModel { get; set; }
        public List<ElectricalSwitchPositionModel> Positions { get; set; }
    }

    public class ElectricalSwitchPositionModel
    {
        public string PositionName { get; set; }
        public int FromChannel { get; set; }
        public int ToChannel { get; set; }
    }

    public class SMUModel
    {
        public string SMU_ID { get; set; }
        public string MakeModel { get; set; }
        public string SMUType { get; set; }
        public bool FourWireSense { get; set; }
        public List<SMUChannelModel> Channels { get; set; }
    }

    public class SMUChannelModel
    {
        public int ChannelNumber { get; set; }
        public SMUMeasureModel MeasureModel { get; set; }
    }

    public class SMUMeasureModel
    {
        public string MeasureMode { get; set; }
        public string SourceMode { get; set; }
        public int SweepNumPoints { get; set; }
        public double Start { get; set; }
        public double Stop { get; set; }
        public double Compliance { get; set; }
        public bool IsSourceRangeAuto { get; set; }
        public double SourceRange { get; set; }
        public bool IsMeasureRangeAuto { get; set; }
        public double MeasureRange { get; set; }
    }
}
