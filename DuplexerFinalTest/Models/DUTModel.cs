using DuplexerFinalTest.Helpers;
using System;
using System.Collections.Generic;

namespace DuplexerFinalTest.Models
{
    public class DUTModel
    {
        public string Name { get; set; }
        public string ItemNumber { get; set; }
        public string SerialNumber { get; set; }
        public DUTType DUTType { get; set; }
        public int Slot { get; set; }
        public bool ReadyToTest { get; set; } = false;
        public string Tag { get; set; }
        public int ThermistorChannel { get; set; }

        public double ReadThermistor
        {
            get
            {
                try
                {
                    if (DUTType == DUTType.Base)
                    {
                        if (!Shared.ElectricalSwitchBase3.CloseChannels(new List<int>() { ThermistorChannel }))
                            throw new Exception($"Base electrical switch #3 cannot close channel {ThermistorChannel}");
                        return Shared.ElectricalSwitchBase3.MeasureTemperature(TemperatureMeasureMode.Thermistor, ThermistorChannel);
                    }
                    else
                    {
                        if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>() { ThermistorChannel }))
                            throw new Exception($"Remote electrical switch #6 cannot close channel {ThermistorChannel}");
                        return Shared.ElectricalSwitchRemote3.MeasureTemperature(TemperatureMeasureMode.Thermistor, ThermistorChannel);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"DUT=>ReadThermistor - {ex.Message}");
                }
            }
        }

        public DUTModel Clone()
        {
            return new DUTModel()
            {
                Name = this.Name,
                ItemNumber = this.ItemNumber,
                SerialNumber = this.SerialNumber,
                DUTType = this.DUTType,
                Slot = this.Slot,
                ReadyToTest = this.ReadyToTest,
                Tag = this.Tag,
                ThermistorChannel = this.ThermistorChannel
            };
        }
    }
}
