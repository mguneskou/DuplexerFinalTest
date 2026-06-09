using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DuplexerFinalTest.Tests
{
    public class Pretest
    {
        public event EventHandler PretestCompleted;
        public event EventHandler PretestCancelled;

        private WaitForm _waitForm;

        public void Run(List<DUTModel> DUTs)
        {
            _waitForm = new WaitForm(null, "Running Pretest...", true);
            ThemeManager.ApplyDarkThemeToForm(_waitForm);
            _waitForm.DoWork += (bgw, e) => DoPretest(bgw, e, DUTs);
            _waitForm.WorkCompleted += (result) =>
            {
                if (result == DialogResult.Cancel)
                    PretestCancelled?.Invoke(this, EventArgs.Empty);
                else
                    PretestCompleted?.Invoke(this, EventArgs.Empty);
            };
            _waitForm.ShowDialog();
        }

        private void DoPretest(BackgroundWorker bgw, DoWorkEventArgs e, List<DUTModel> DUTs)
        {
            try
            {
                #region Base DUTs
                var baseDUTs = DUTs.Where(d => d.DUTType == DUTType.Base).ToList();
                if (baseDUTs.Count > 0)
                {
                    // Reset base optical switches
                    Shared.OpticalSwitch1x13_Base.Reset();
                    Shared.OpticalSwitch1x4.Reset();
                    // Route 1x4 to power head (CH1)
                    Shared.OpticalSwitch1x4.CloseChannel(1);
                    // Reset electrical switch 2 (base DUTs 7-12)
                    Shared.ElectricalSwitchBase2.Reset();

                    // SMU sweep settings for pretest
                    var SMUSettings_ch1 = new SMUSettingsModel()
                    {
                        Channel = 1,
                        MeasureMode = SMUMeasureMode.VOLT,
                        SourceMode = SMUMeasureMode.CURR,
                        SweepRange = new SweepRangeModel() { Start = 0.0001, Stop = 0.0451, Steps = 451 },
                        Compliance = 4.0,
                        IsSourceRangeAuto = false,
                        SourceRange = 0.045,
                        IsMeasureRangeAuto = false,
                        MeasureRange = 20
                    };
                    var SMUSettings_ch2 = new SMUSettingsModel()
                    {
                        Channel = 2,
                        SourceMode = SMUMeasureMode.VOLT,
                        MeasureMode = SMUMeasureMode.CURR,
                        SweepRange = new SweepRangeModel() { Start = 0, Stop = 0, Steps = 451 },
                        Compliance = 0.045,
                        IsSourceRangeAuto = false,
                        SourceRange = 0,
                        IsMeasureRangeAuto = false,
                        MeasureRange = 0
                    };

                    var Current = new List<double>();

                    foreach (var d in baseDUTs)
                    {
                        if (bgw.CancellationPending) { e.Cancel = true; return; }
                        Current.Clear();

                        // Route 1x13 to base DUT slot
                        Shared.OpticalSwitch1x13_Base.CloseChannel(d.Slot);
                        // Close electrical switch 1 for this DUT slot
                        Shared.ElectricalSwitchBase1.CloseChannels(new List<int>()
                        {
                            Shared.Base_Z_IB_IOP.ElectricalSwitch1.Positions[d.Slot - 1].FromChannel
                        });
                        // Close electrical switch 3 to power head
                        Shared.ElectricalSwitchBase3.CloseChannels(new List<int>()
                        {
                            Shared.Base_Z_IB_IOP.ElectricalSwitch2.Positions[0].FromChannel
                        });

                        // Master SMU sweep
                        Shared.SMU_master.Reset();
                        Thread.Sleep(20);
                        Shared.SMU_master.SetSweepChannel(SMUSettings_ch1);
                        Thread.Sleep(20);
                        Shared.SMU_master.SetReadingChannel(SMUSettings_ch2);
                        Thread.Sleep(20);
                        Shared.SMU_master.InitiateReading(new List<int>() { 1, 2 }, SMUSettings_ch2);

                        int READ_SIZE = SMUSettings_ch1.SweepRange.Steps;
                        double[,] data = new double[READ_SIZE, 3];
                        bool isFirst = true;
                        int actrow;
                        do
                        {
                            if (bgw.CancellationPending) { e.Cancel = true; return; }
                            if (!Shared.SMU_master.ReadData(1, isFirst, READ_SIZE, ref data, out actrow))
                                break;
                            Thread.Sleep(20);
                            isFirst = false;
                            if (actrow == 0) break;
                            for (int i = 0; i < actrow; i++)
                                Current.Add(data[i, 0]);
                        } while (true);

                        if (Current.Any(dt => dt > 0))
                            d.ReadyToTest = true;

                        Thread.Sleep(250);
                    }
                }
                #endregion

                #region Remote DUTs
                var remoteDUTs = DUTs.Where(d => d.DUTType == DUTType.Remote).ToList();
                if (remoteDUTs.Count <= 0) return;

                // Reset remote optical switches
                Shared.OpticalSwitch1x13_Remote.Reset();
                Shared.OpticalSwitch1x4.Reset();
                // Route 1x4 to power head
                Shared.OpticalSwitch1x4.CloseChannel(1);
                // Reset remote electrical switch 2
                Shared.ElectricalSwitchRemote2.Reset();

                var SMUSettings_remote_ch1 = new SMUSettingsModel()
                {
                    Channel = 1,
                    MeasureMode = SMUMeasureMode.VOLT,
                    SourceMode = SMUMeasureMode.CURR,
                    SweepRange = new SweepRangeModel() { Start = 5, Stop = 5, Steps = 1 },
                    Compliance = 4.0,
                    IsSourceRangeAuto = false,
                    SourceRange = 0.045,
                    IsMeasureRangeAuto = false,
                    MeasureRange = 20
                };
                var SMUSettings_remote_ch2 = new SMUSettingsModel()
                {
                    Channel = 2,
                    SourceMode = SMUMeasureMode.VOLT,
                    MeasureMode = SMUMeasureMode.CURR,
                    SweepRange = new SweepRangeModel() { Start = 0, Stop = 0, Steps = 0 },
                    Compliance = 0.045,
                    IsSourceRangeAuto = false,
                    SourceRange = 0,
                    IsMeasureRangeAuto = false,
                    MeasureRange = 0
                };

                foreach (var d in remoteDUTs)
                {
                    if (bgw.CancellationPending) { e.Cancel = true; return; }

                    // Route 1x13 to remote DUT (Slot is 1-based per type, direct mapping)
                    Shared.OpticalSwitch1x13_Remote.CloseChannel(d.Slot);
                    // Close remote electrical switch 1 for this DUT slot
                    Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>()
                    {
                        Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[d.Slot - 1].FromChannel,
                        Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[d.Slot - 1].ToChannel
                    });
                    // Close remote electrical switch 3 to power head
                    Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>()
                    {
                        Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].FromChannel,
                        Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].ToChannel
                    });

                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(SMUSettings_remote_ch1);
                    Shared.SMU_master.SetReadingChannel(SMUSettings_remote_ch2);
                    Shared.SMU_master.InitiateReading(new List<int>() { 1 }, SMUSettings_remote_ch1);

                    int READ_SIZE_R = 1;
                    double[,] dataR = new double[READ_SIZE_R, 3];
                    int actrowR;
                    Shared.SMU_master.ReadData(1, true, READ_SIZE_R, ref dataR, out actrowR);

                    if (actrowR > 0 && dataR[0, 1] > 0)
                        d.ReadyToTest = true;

                    Thread.Sleep(250);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("Pretest.DoPretest", ex);
                throw;
            }
        }
    }
}
