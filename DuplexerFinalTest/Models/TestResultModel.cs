using DuplexerFinalTest.Helpers;
using System;
using System.Collections.Generic;

namespace DuplexerFinalTest.Models
{
    public class TestResultModel : IDisposable
    {
        public OverallPassFail OverallPassFail { get; set; }
        public bool SaveIntoProductionDB { get; set; }
        public Result_Base_Z_IB_IOP Base_Z_IB_IOP_Results { get; set; } = new Result_Base_Z_IB_IOP();
        public Result_Base_Z_IB_IPD Base_Z_IPD_Results { get; set; } = new Result_Base_Z_IB_IPD();
        public Result_Remote_Z_IOP Remote_Z_IOP_Results { get; set; } = new Result_Remote_Z_IOP();
        public Result_Remote_Z_IPV Remote_Z_IPV_Results { get; set; } = new Result_Remote_Z_IPV();
        public Result_Remote_Z_VPV Remote_Z_VPV_Results { get; set; } = new Result_Remote_Z_VPV();

        public TestResultModel Clone()
        {
            return new TestResultModel()
            {
                OverallPassFail = this.OverallPassFail,
                SaveIntoProductionDB = this.SaveIntoProductionDB,
                Base_Z_IB_IOP_Results = this.Base_Z_IB_IOP_Results.Clone(),
                Base_Z_IPD_Results = this.Base_Z_IPD_Results.Clone(),
                Remote_Z_IOP_Results = this.Remote_Z_IOP_Results.Clone(),
                Remote_Z_IPV_Results = this.Remote_Z_IPV_Results.Clone(),
                Remote_Z_VPV_Results = this.Remote_Z_VPV_Results.Clone()
            };
        }

        public void Dispose()
        {
            Base_Z_IB_IOP_Results?.Dispose(); Base_Z_IB_IOP_Results = null;
            Base_Z_IPD_Results?.Dispose(); Base_Z_IPD_Results = null;
            Remote_Z_IOP_Results?.Dispose(); Remote_Z_IOP_Results = null;
            Remote_Z_IPV_Results?.Dispose(); Remote_Z_IPV_Results = null;
            Remote_Z_VPV_Results?.Dispose(); Remote_Z_VPV_Results = null;
        }
    }

    public class Result_Base_Z_IB_IOP : IDisposable
    {
        public string SerialNumber { get; set; }
        public double Temperature { get; set; }
        public bool Pass { get; set; }
        public List<double> CH1_Voltage = new List<double>();
        public List<double> CH1_Current = new List<double>();
        public List<string> CH1_Time = new List<string>();
        public List<double> CH2_Voltage = new List<double>();
        public List<double> CH2_Current = new List<double>();
        public List<string> CH2_Time = new List<string>();
        public List<double> CH4_Voltage = new List<double>();
        public List<double> CH4_Current = new List<double>();
        public List<string> CH4_Time = new List<string>();
        public List<double> CH4_Power = new List<double>();

        public Result_Base_Z_IB_IOP Clone()
        {
            var r = new Result_Base_Z_IB_IOP() { SerialNumber = SerialNumber, Temperature = Temperature, Pass = Pass };
            for (int i = 0; i < CH1_Current.Count; i++)
            {
                r.CH1_Voltage.Add(CH1_Voltage[i]); r.CH1_Current.Add(CH1_Current[i]); r.CH1_Time.Add(CH1_Time[i]);
                r.CH2_Voltage.Add(CH2_Voltage[i]); r.CH2_Current.Add(CH2_Current[i]); r.CH2_Time.Add(CH2_Time[i]);
                r.CH4_Voltage.Add(CH4_Voltage[i]); r.CH4_Current.Add(CH4_Current[i]); r.CH4_Time.Add(CH4_Time[i]);
                r.CH4_Power.Add(CH4_Power[i]);
            }
            return r;
        }

        public void Dispose()
        {
            Pass = false; SerialNumber = string.Empty; Temperature = double.NaN;
            CH1_Voltage.Clear(); CH1_Current.Clear(); CH1_Time.Clear();
            CH2_Voltage.Clear(); CH2_Current.Clear(); CH2_Time.Clear();
            CH4_Voltage.Clear(); CH4_Current.Clear(); CH4_Time.Clear(); CH4_Power.Clear();
        }
    }

    public class Result_Base_Z_IB_IPD : IDisposable
    {
        public string SerialNumber { get; set; }
        public double Temperature { get; set; }
        public bool Pass { get; set; }
        public List<double> CH3_Voltage = new List<double>();
        public List<double> CH3_Current = new List<double>();
        public List<string> CH3_Time = new List<string>();
        public List<double> CH2_Voltage = new List<double>();
        public List<double> CH2_Current = new List<double>();
        public List<string> CH2_Time = new List<string>();

        public Result_Base_Z_IB_IPD Clone()
        {
            var r = new Result_Base_Z_IB_IPD() { SerialNumber = SerialNumber, Temperature = Temperature, Pass = Pass };
            for (int i = 0; i < CH3_Current.Count; i++)
            {
                r.CH3_Voltage.Add(CH3_Voltage[i]); r.CH3_Current.Add(CH3_Current[i]); r.CH3_Time.Add(CH3_Time[i]);
                r.CH2_Voltage.Add(CH2_Voltage[i]); r.CH2_Current.Add(CH2_Current[i]); r.CH2_Time.Add(CH2_Time[i]);
            }
            return r;
        }

        public void Dispose()
        {
            Pass = false; SerialNumber = string.Empty; Temperature = double.NaN;
            CH3_Voltage.Clear(); CH3_Current.Clear(); CH3_Time.Clear();
            CH2_Voltage.Clear(); CH2_Current.Clear(); CH2_Time.Clear();
        }
    }

    public class Result_Remote_Z_IOP : IDisposable
    {
        public string SerialNumber { get; set; }
        public double Temperature { get; set; }
        public bool Pass { get; set; }
        public List<double> CH1_Voltage = new List<double>();
        public List<double> CH1_Current = new List<double>();
        public List<string> CH1_Time = new List<string>();
        public List<double> CH4_Voltage = new List<double>();
        public List<double> CH4_Current = new List<double>();
        public List<string> CH4_Time = new List<string>();
        public List<double> CH4_Power = new List<double>();

        public Result_Remote_Z_IOP Clone()
        {
            var r = new Result_Remote_Z_IOP() { SerialNumber = SerialNumber, Temperature = Temperature, Pass = Pass };
            for (int i = 0; i < CH1_Current.Count; i++)
            {
                r.CH1_Voltage.Add(CH1_Voltage[i]); r.CH1_Current.Add(CH1_Current[i]); r.CH1_Time.Add(CH1_Time[i]);
                r.CH4_Voltage.Add(CH4_Voltage[i]); r.CH4_Current.Add(CH4_Current[i]); r.CH4_Time.Add(CH4_Time[i]);
                r.CH4_Power.Add(CH4_Power[i]);
            }
            return r;
        }

        public void Dispose()
        {
            Pass = false; SerialNumber = string.Empty; Temperature = double.NaN;
            CH1_Voltage.Clear(); CH1_Current.Clear(); CH1_Time.Clear();
            CH4_Voltage.Clear(); CH4_Current.Clear(); CH4_Time.Clear(); CH4_Power.Clear();
        }
    }

    public class Result_Remote_Z_IPV : IDisposable
    {
        public string SerialNumber { get; set; }
        public double Temperature { get; set; }
        public bool Pass { get; set; }
        public List<double> CH3_Voltage = new List<double>();
        public List<double> CH3_Current = new List<double>();
        public List<string> CH3_Time = new List<string>();
        public List<double> CH2_Voltage = new List<double>();
        public List<double> CH2_Current = new List<double>();
        public List<string> CH2_Time = new List<string>();

        public Result_Remote_Z_IPV Clone()
        {
            var r = new Result_Remote_Z_IPV() { SerialNumber = SerialNumber, Temperature = Temperature, Pass = Pass };
            for (int i = 0; i < CH3_Current.Count; i++)
            {
                r.CH3_Voltage.Add(CH3_Voltage[i]); r.CH3_Current.Add(CH3_Current[i]); r.CH3_Time.Add(CH3_Time[i]);
                r.CH2_Voltage.Add(CH2_Voltage[i]); r.CH2_Current.Add(CH2_Current[i]); r.CH2_Time.Add(CH2_Time[i]);
            }
            return r;
        }

        public void Dispose()
        {
            Pass = false; SerialNumber = string.Empty; Temperature = double.NaN;
            CH3_Voltage.Clear(); CH3_Current.Clear(); CH3_Time.Clear();
            CH2_Voltage.Clear(); CH2_Current.Clear(); CH2_Time.Clear();
        }
    }

    public class Result_Remote_Z_VPV : IDisposable
    {
        public string SerialNumber { get; set; }
        public double Temperature { get; set; }
        public bool Pass { get; set; }
        public List<double> CH3_Voltage = new List<double>();
        public List<double> CH3_Current = new List<double>();
        public List<string> CH3_Time = new List<string>();
        public List<double> CH2_Voltage = new List<double>();
        public List<double> CH2_Current = new List<double>();
        public List<string> CH2_Time = new List<string>();
        public List<double> CH5_Current = new List<double>();
        public List<double> Power = new List<double>();

        public Result_Remote_Z_VPV Clone()
        {
            var r = new Result_Remote_Z_VPV() { SerialNumber = SerialNumber, Temperature = Temperature, Pass = Pass };
            for (int i = 0; i < CH3_Current.Count; i++)
            {
                r.CH3_Voltage.Add(CH3_Voltage[i]); r.CH3_Current.Add(CH3_Current[i]); r.CH3_Time.Add(CH3_Time[i]);
                r.CH2_Voltage.Add(CH2_Voltage[i]); r.CH2_Current.Add(CH2_Current[i]); r.CH2_Time.Add(CH2_Time[i]);
                r.CH5_Current.Add(CH5_Current[i]); r.Power.Add(Power[i]);
            }
            return r;
        }

        public void Dispose()
        {
            Pass = false; SerialNumber = string.Empty; Temperature = double.NaN;
            CH3_Voltage.Clear(); CH3_Current.Clear(); CH3_Time.Clear();
            CH2_Voltage.Clear(); CH2_Current.Clear(); CH2_Time.Clear();
            CH5_Current.Clear(); Power.Clear();
        }
    }
}
