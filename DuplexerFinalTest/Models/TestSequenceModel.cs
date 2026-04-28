using DuplexerFinalTest.Helpers;
using System.Collections.Generic;

namespace DuplexerFinalTest.Models
{
    public class TestSequenceModel
    {
        public string SequenceName { get; set; }
        public string Revision { get; set; }
        public string Comments { get; set; }
        public List<DUTModel> RemoteDUTs { get; set; } = new List<DUTModel>();
        public List<DUTModel> BaseDUTs { get; set; } = new List<DUTModel>();
        public bool CallsChamberProgram { get; set; }
        public ChamberProgramModel ChamberProgram { get; set; } = new ChamberProgramModel();
        public ChamberManualRunModel ChamberManualRun { get; set; } = new ChamberManualRunModel();

        public TestSequenceModel Clone()
        {
            var returnModel = new TestSequenceModel()
            {
                SequenceName = this.SequenceName,
                Revision = this.Revision,
                Comments = this.Comments,
                CallsChamberProgram = this.CallsChamberProgram
            };
            if (this.ChamberProgram != null) returnModel.ChamberProgram = this.ChamberProgram.Clone();
            if (this.ChamberManualRun != null) returnModel.ChamberManualRun = this.ChamberManualRun.Clone();
            foreach (var b in this.BaseDUTs) returnModel.BaseDUTs.Add(b.Clone());
            foreach (var r in this.RemoteDUTs) returnModel.RemoteDUTs.Add(r.Clone());
            return returnModel;
        }
    }

    public class ChamberProgramModel
    {
        public int ProgramNumber { get; set; }
        public string ExpectedProgramName { get; set; }
        public int ExpectedNumberOfSteps { get; set; }
        public int StartStepNumber { get; set; }
        public List<ChamberProgramTestsModel> TestsForEachStep { get; set; } = new List<ChamberProgramTestsModel>();

        public ChamberProgramModel Clone()
        {
            var returnModel = new ChamberProgramModel()
            {
                ProgramNumber = this.ProgramNumber,
                ExpectedProgramName = this.ExpectedProgramName,
                ExpectedNumberOfSteps = this.ExpectedNumberOfSteps,
                StartStepNumber = this.StartStepNumber
            };
            foreach (var t in TestsForEachStep) returnModel.TestsForEachStep.Add(t.Clone());
            return returnModel;
        }
    }

    public class ChamberProgramTestsModel
    {
        public int StepNumber { get; set; }
        public int DelayBeforeSweepsMinutes { get; set; }
        public int DelayAfterSweepsMinutes { get; set; }
        public string Tests { get; set; }

        public ChamberProgramTestsModel Clone()
        {
            return new ChamberProgramTestsModel()
            {
                StepNumber = this.StepNumber,
                DelayBeforeSweepsMinutes = this.DelayBeforeSweepsMinutes,
                DelayAfterSweepsMinutes = this.DelayAfterSweepsMinutes,
                Tests = this.Tests
            };
        }
    }

    public class ChamberManualRunModel
    {
        public List<ChamberManualRunRowModel> ChamberRunSteps { get; set; } = new List<ChamberManualRunRowModel>();

        public ChamberManualRunModel Clone()
        {
            var returnModel = new ChamberManualRunModel();
            foreach (var s in this.ChamberRunSteps) returnModel.ChamberRunSteps.Add(s.Clone());
            return returnModel;
        }
    }

    public class ChamberManualRunRowModel
    {
        public int StepNo { get; set; }
        public double StartTemperature { get; set; }
        public double GoTemperature { get; set; }
        public bool HumidityOff { get; set; }
        public double StartHumidity { get; set; }
        public double GoHumidity { get; set; }
        public int RefrigerationCapacity { get; set; } = 9;
        public string RelayMode { get; set; }
        public List<int> RelayNumbers { get; set; } = new List<int>();
        public string Action { get; set; }
        public double TemperatureTolerenceInPercent { get; set; }
        public double RampDwellMinutes { get; set; }
        public double DelayBeforeSweepsMinutes { get; set; }
        public double DelayAfterSweepsMinutes { get; set; }
        public string Tests { get; set; }
        public bool Passed { get; set; }

        public ChamberManualRunRowModel Clone()
        {
            var returnModel = new ChamberManualRunRowModel()
            {
                StepNo = this.StepNo,
                StartTemperature = this.StartTemperature,
                GoTemperature = this.GoTemperature,
                HumidityOff = this.HumidityOff,
                StartHumidity = this.StartHumidity,
                GoHumidity = this.GoHumidity,
                RefrigerationCapacity = this.RefrigerationCapacity,
                RelayMode = this.RelayMode,
                Action = this.Action,
                TemperatureTolerenceInPercent = this.TemperatureTolerenceInPercent,
                RampDwellMinutes = this.RampDwellMinutes,
                DelayBeforeSweepsMinutes = this.DelayBeforeSweepsMinutes,
                DelayAfterSweepsMinutes = this.DelayAfterSweepsMinutes,
                Tests = this.Tests,
                Passed = this.Passed
            };
            foreach (var rn in this.RelayNumbers) returnModel.RelayNumbers.Add(rn);
            return returnModel;
        }
    }

    public enum ChamberManualRunActions
    {
        RAMP = 0,
        SOAK = 1
    }
}
