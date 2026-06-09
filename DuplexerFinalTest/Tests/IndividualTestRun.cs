using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace DuplexerFinalTest.Tests
{
    public static class IndividualTestRun
    {
        private static string SuccessStatusText()
        {
            return Shared.sharedGeneralSettings?.GeneralSettings?[0]
                .USE_SIMULATORS?.Trim().ToLower() == "true"
                ? "Passed"
                : "Completed";
        }

        private static SMUSettingsModel BuildSMUSettings(SMUChannelModel ch)
        {
            return new SMUSettingsModel()
            {
                Channel = ch.ChannelNumber,
                MeasureMode = (SMUMeasureMode)Enum.Parse(typeof(SMUMeasureMode), ch.MeasureModel.MeasureMode),
                SourceMode = (SMUMeasureMode)Enum.Parse(typeof(SMUMeasureMode), ch.MeasureModel.SourceMode),
                SweepRange = new SweepRangeModel()
                {
                    Start = ch.MeasureModel.Start,
                    Stop = ch.MeasureModel.Stop,
                    Steps = ch.MeasureModel.SweepNumPoints
                },
                Compliance = ch.MeasureModel.Compliance,
                IsSourceRangeAuto = ch.MeasureModel.IsSourceRangeAuto,
                SourceRange = ch.MeasureModel.SourceRange,
                IsMeasureRangeAuto = ch.MeasureModel.IsMeasureRangeAuto,
                MeasureRange = ch.MeasureModel.MeasureRange
            };
        }

        private static void ReadIntoLists(ISMU smu, int ch, int readSize, bool fromStart,
            System.Collections.Generic.List<double> voltList,
            System.Collections.Generic.List<double> currList,
            System.Collections.Generic.List<string> timeList)
        {
            double[,] data = new double[readSize, 3];
            bool isFirst = fromStart;
            int actrow;
            do
            {
                if (!smu.ReadData(ch, isFirst, readSize, ref data, out actrow))
                    break;
                Thread.Sleep(20);
                isFirst = false;
                if (actrow == 0) break;
                for (int i = 0; i < actrow; i++)
                {
                    voltList.Add(data[i, 0]);
                    currList.Add(data[i, 1]);
                    timeList.Add(data[i, 2].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            } while (true);
        }

        // ── Base_Z_IB_IOP ──────────────────────────────────────────────────────
        public static bool RunBase_Z_IB_IOP(TestSequenceModel sequence, TestResultModel testResults,
            BackgroundWorker bgw, int index, int sweepNo, double temperature, out bool cancelled)
        {
            cancelled = false;
            try
            {
                var smuMaster_CH1 = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU1.Channels[0]);
                var smuMaster_CH2 = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU1.Channels[1]);
                var smuSlave_CH2 = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU2.Channels[0]);

                if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                    throw new EquipmentCommunicationException("Optical switch 1x4 cannot close channel 1.");

                Shared.ElectricalSwitchBase2.Reset();

                foreach (var DUT in sequence.BaseDUTs)
                {
                    Shared.CurrentSimPartSerial = DUT.SerialNumber;
                    testResults.Base_Z_IB_IOP_Results.Pass = false;
                    if (bgw.CancellationPending) { cancelled = true; break; }

                    if (!Shared.OpticalSwitch1x13_Base.CloseChannel(DUT.Slot))
                        throw new EquipmentCommunicationException($"Base optical switch 1x13 cannot close channel {DUT.Slot}.");

                    if (!Shared.ElectricalSwitchBase1.CloseChannels(
                        new List<int>() {
                            Shared.Base_Z_IB_IOP.ElectricalSwitch1.Positions[DUT.Slot - 1].FromChannel,
                            Shared.Base_Z_IB_IOP.ElectricalSwitch1.Positions[DUT.Slot - 1].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Base electrical switch #1 error.");

                    if (!Shared.ElectricalSwitchBase3.CloseChannels(
                        new List<int>() {
                            Shared.Base_Z_IB_IOP.ElectricalSwitch2.Positions[0].FromChannel,
                            Shared.Base_Z_IB_IOP.ElectricalSwitch2.Positions[0].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Base electrical switch #3 error.");

                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(smuMaster_CH1);
                    Shared.SMU_master.SetReadingChannel(smuMaster_CH2);
                    Shared.SMU_slave.Reset();
                    Shared.SMU_slave.SetReadingChannel(smuSlave_CH2);
                    Shared.SMU_master.InitiateReading(
                        new List<int>() { smuMaster_CH1.Channel, smuMaster_CH2.Channel }, smuMaster_CH2);
                    Shared.SMU_slave.InitiateReading(
                        new List<int>() { smuSlave_CH2.Channel }, smuSlave_CH2);

                    int readSize = smuMaster_CH1.SweepRange.Steps;
                    ReadIntoLists(Shared.SMU_master, smuMaster_CH1.Channel, readSize, true,
                        testResults.Base_Z_IB_IOP_Results.CH1_Voltage,
                        testResults.Base_Z_IB_IOP_Results.CH1_Current,
                        testResults.Base_Z_IB_IOP_Results.CH1_Time);

                    ReadIntoLists(Shared.SMU_master, smuMaster_CH2.Channel, readSize, true,
                        testResults.Base_Z_IB_IOP_Results.CH2_Voltage,
                        testResults.Base_Z_IB_IOP_Results.CH2_Current,
                        testResults.Base_Z_IB_IOP_Results.CH2_Time);

                    ReadIntoLists(Shared.SMU_slave, smuSlave_CH2.Channel, readSize, true,
                        testResults.Base_Z_IB_IOP_Results.CH4_Voltage,
                        testResults.Base_Z_IB_IOP_Results.CH4_Current,
                        testResults.Base_Z_IB_IOP_Results.CH4_Time);

                    // Calculate CH4 Power
                    int pwrCount = Math.Min(testResults.Base_Z_IB_IOP_Results.CH4_Current.Count,
                                           testResults.Base_Z_IB_IOP_Results.CH4_Voltage.Count);
                    for (int i = 0; i < pwrCount; i++)
                        testResults.Base_Z_IB_IOP_Results.CH4_Power.Add(
                            testResults.Base_Z_IB_IOP_Results.CH4_Current[i] *
                            testResults.Base_Z_IB_IOP_Results.CH4_Voltage[i]);

                    testResults.Base_Z_IB_IOP_Results.SerialNumber = DUT.SerialNumber;
                    testResults.Base_Z_IB_IOP_Results.Temperature = temperature;
                    testResults.Base_Z_IB_IOP_Results.Pass = true;
                    testResults.OverallPassFail = OverallPassFail.PASS;

                    Shared.testResultSaver.SaveResults(DUT.SerialNumber, testResults, TestSequences.Base_Z_IB_IOP, sweepNo, temperature);
                    testResults.Dispose(); testResults = null;
                    testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                }

                Shared.SMU_master.CloseAllChannels();
                Shared.SMU_slave.CloseAllChannels();
                bgw.ReportProgress(index, $"►Base_Z_IB_IOP @{temperature}°C | {SuccessStatusText()}");
                return true;
            }
            catch (EquipmentCommunicationException)
            {
                throw; // propagate to TestRun retry loop
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("RunBase_Z_IB_IOP", ex);
                bgw.ReportProgress(index, $"►Base_Z_IB_IOP @{temperature}°C | Failed");
                testResults.OverallPassFail = OverallPassFail.FAIL;
                return false;
            }
        }

        // ── Base_Z_IPD ─────────────────────────────────────────────────────────
        public static bool RunBase_Z_IPD(TestSequenceModel sequence, TestResultModel testResults,
            BackgroundWorker bgw, int index, int sweepNo, double temperature, out bool cancelled)
        {
            cancelled = false;
            try
            {
                var smuMaster = BuildSMUSettings(Shared.Base_Z_IPD.SMU1.Channels[0]);
                var smuSlave = BuildSMUSettings(Shared.Base_Z_IPD.SMU2.Channels[0]);

                if (!Shared.OpticalSwitch1x4.CloseChannel(3))
                    throw new EquipmentCommunicationException("Optical switch 1x4 cannot close channel 3.");

                Shared.ElectricalSwitchBase2.Reset();

                foreach (var DUT in sequence.BaseDUTs)
                {
                    Shared.CurrentSimPartSerial = DUT.SerialNumber;
                    testResults.Base_Z_IPD_Results.Pass = false;
                    if (bgw.CancellationPending) { cancelled = true; break; }

                    if (!Shared.OpticalSwitch1x13_Base.CloseChannel(DUT.Slot))
                        throw new EquipmentCommunicationException($"Base optical switch 1x13 cannot close channel {DUT.Slot}.");

                    if (!Shared.ElectricalSwitchBase1.CloseChannels(new List<int>()
                        {
                            Shared.Base_Z_IPD.ElectricalSwitch1.Positions[0].FromChannel,
                            Shared.Base_Z_IPD.ElectricalSwitch1.Positions[0].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Base electrical switch #1 error (Z_IPD).");

                    if (!Shared.ElectricalSwitchBase3.CloseChannels(new List<int>()
                        {
                            Shared.Base_Z_IPD.ElectricalSwitch2.Positions[DUT.Slot - 1].FromChannel,
                            Shared.Base_Z_IPD.ElectricalSwitch2.Positions[DUT.Slot - 1].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Base electrical switch #3 error (Z_IPD).");

                    // Configure bias channel first — output turns ON so bias is active during sweep
                    Shared.SMU_slave.Reset();
                    Shared.SMU_slave.SetSweepChannel(smuSlave);
                    Shared.SMU_slave.SetReadingChannel(smuSlave);
                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(smuMaster);
                    Shared.SMU_master.SetReadingChannel(smuMaster);
                    Shared.SMU_slave.InitiateReading(new List<int>() { smuSlave.Channel }, smuSlave);
                    Shared.SMU_master.InitiateReading(new List<int>() { smuMaster.Channel }, smuMaster);
                    ReadIntoLists(Shared.SMU_master, smuMaster.Channel, smuMaster.SweepRange.Steps, true,
                        testResults.Base_Z_IPD_Results.CH3_Voltage,
                        testResults.Base_Z_IPD_Results.CH3_Current,
                        testResults.Base_Z_IPD_Results.CH3_Time);
                    ReadIntoLists(Shared.SMU_slave, smuSlave.Channel, smuSlave.SweepRange.Steps, true,
                        testResults.Base_Z_IPD_Results.CH2_Voltage,
                        testResults.Base_Z_IPD_Results.CH2_Current,
                        testResults.Base_Z_IPD_Results.CH2_Time);

                    testResults.Base_Z_IPD_Results.SerialNumber = DUT.SerialNumber;
                    testResults.Base_Z_IPD_Results.Temperature = temperature;
                    testResults.Base_Z_IPD_Results.Pass = true;
                    testResults.OverallPassFail = OverallPassFail.PASS;

                    Shared.testResultSaver.SaveResults(DUT.SerialNumber, testResults, TestSequences.Base_Z_IPD, sweepNo, temperature);
                    testResults.Dispose(); testResults = null;
                    testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                }

                Shared.SMU_master.CloseAllChannels();
                Shared.SMU_slave.CloseAllChannels();
                bgw.ReportProgress(index, $"►Base_Z_IPD @{temperature}°C | {SuccessStatusText()}");
                return true;
            }
            catch (EquipmentCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("RunBase_Z_IPD", ex);
                bgw.ReportProgress(index, $"►Base_Z_IPD @{temperature}°C | Failed");
                testResults.OverallPassFail = OverallPassFail.FAIL;
                return false;
            }
        }

        // ── Remote_Z_IOP ───────────────────────────────────────────────────────
        public static bool RunRemote_Z_IOP(TestSequenceModel sequence, TestResultModel testResults,
            BackgroundWorker bgw, int index, int sweepNo, double temperature, out bool cancelled)
        {
            cancelled = false;
            try
            {
                var smuMaster = BuildSMUSettings(Shared.Remote_Z_IOP.SMU1.Channels[0]);
                var smuSlave = BuildSMUSettings(Shared.Remote_Z_IOP.SMU2.Channels[0]);

                if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                    throw new EquipmentCommunicationException("Optical switch 1x4 cannot close channel 1 (Remote_Z_IOP).");

                Shared.ElectricalSwitchRemote2.Reset();

                foreach (var DUT in sequence.RemoteDUTs)
                {
                    Shared.CurrentSimPartSerial = DUT.SerialNumber;
                    testResults.Remote_Z_IOP_Results.Pass = false;
                    if (bgw.CancellationPending) { cancelled = true; break; }

                    if (!Shared.OpticalSwitch1x13_Remote.CloseChannel(DUT.Slot))
                        throw new EquipmentCommunicationException($"Remote optical switch 1x13 cannot close channel {DUT.Slot}.");

                    // DUT.Slot is 1-based per type (1–12)
                    if (!Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[DUT.Slot - 1].FromChannel,
                            Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[DUT.Slot - 1].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #1 error (Z_IOP).");

                    if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].FromChannel,
                            Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #3 error (Z_IOP).");

                    // Configure bias channel first — output turns ON so bias is active during sweep
                    Shared.SMU_slave.Reset();
                    Shared.SMU_slave.SetSweepChannel(smuSlave);
                    Shared.SMU_slave.SetReadingChannel(smuSlave);
                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(smuMaster);
                    Shared.SMU_master.SetReadingChannel(smuMaster);
                    Shared.SMU_slave.InitiateReading(new List<int>() { smuSlave.Channel }, smuSlave);
                    Shared.SMU_master.InitiateReading(new List<int>() { smuMaster.Channel }, smuMaster);
                    ReadIntoLists(Shared.SMU_master, smuMaster.Channel, smuMaster.SweepRange.Steps, true,
                        testResults.Remote_Z_IOP_Results.CH1_Voltage,
                        testResults.Remote_Z_IOP_Results.CH1_Current,
                        testResults.Remote_Z_IOP_Results.CH1_Time);
                    ReadIntoLists(Shared.SMU_slave, smuSlave.Channel, smuSlave.SweepRange.Steps, true,
                        testResults.Remote_Z_IOP_Results.CH4_Voltage,
                        testResults.Remote_Z_IOP_Results.CH4_Current,
                        testResults.Remote_Z_IOP_Results.CH4_Time);

                    // Calculate CH4 Power
                    int pwrCount = Math.Min(testResults.Remote_Z_IOP_Results.CH4_Current.Count,
                                           testResults.Remote_Z_IOP_Results.CH4_Voltage.Count);
                    for (int i = 0; i < pwrCount; i++)
                        testResults.Remote_Z_IOP_Results.CH4_Power.Add(
                            testResults.Remote_Z_IOP_Results.CH4_Current[i] *
                            testResults.Remote_Z_IOP_Results.CH4_Voltage[i]);

                    testResults.Remote_Z_IOP_Results.SerialNumber = DUT.SerialNumber;
                    testResults.Remote_Z_IOP_Results.Temperature = temperature;
                    testResults.Remote_Z_IOP_Results.Pass = true;
                    testResults.OverallPassFail = OverallPassFail.PASS;

                    Shared.testResultSaver.SaveResults(DUT.SerialNumber, testResults, TestSequences.Remote_Z_IOP, sweepNo, temperature);
                    testResults.Dispose(); testResults = null;
                    testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                }

                Shared.SMU_master.CloseAllChannels();
                Shared.SMU_slave.CloseAllChannels();
                bgw.ReportProgress(index, $"►Remote_Z_IOP @{temperature}°C | {SuccessStatusText()}");
                return true;
            }
            catch (EquipmentCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("RunRemote_Z_IOP", ex);
                bgw.ReportProgress(index, $"►Remote_Z_IOP @{temperature}°C | Failed");
                testResults.OverallPassFail = OverallPassFail.FAIL;
                return false;
            }
        }

        // ── Remote_Z_IPV ───────────────────────────────────────────────────────
        public static bool RunRemote_Z_IPV(TestSequenceModel sequence, TestResultModel testResults,
            BackgroundWorker bgw, int index, int sweepNo, double temperature, out bool cancelled)
        {
            cancelled = false;
            try
            {
                var smuMaster = BuildSMUSettings(Shared.Remote_Z_IPV.SMU1.Channels[0]);
                var smuSlave = BuildSMUSettings(Shared.Remote_Z_IPV.SMU2.Channels[0]);

                if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                    throw new EquipmentCommunicationException("Optical switch 1x4 cannot close channel 1 (Remote_Z_IPV).");

                Shared.ElectricalSwitchRemote2.Reset();

                foreach (var DUT in sequence.RemoteDUTs)
                {
                    Shared.CurrentSimPartSerial = DUT.SerialNumber;
                    testResults.Remote_Z_IPV_Results.Pass = false;
                    if (bgw.CancellationPending) { cancelled = true; break; }

                    if (!Shared.OpticalSwitch1x13_Remote.CloseChannel(DUT.Slot))
                        throw new EquipmentCommunicationException($"Remote optical switch 1x13 cannot close channel {DUT.Slot}.");

                    if (!Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_IPV.ElectricalSwitch1.Positions[0].FromChannel,
                            Shared.Remote_Z_IPV.ElectricalSwitch1.Positions[0].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #1 error (Z_IPV).");

                    // DUT.Slot is 1-based per type (1–12)
                    if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_IPV.ElectricalSwitch2.Positions[DUT.Slot - 1].FromChannel,
                            Shared.Remote_Z_IPV.ElectricalSwitch2.Positions[DUT.Slot - 1].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #3 error (Z_IPV).");

                    // Configure bias channel first — output turns ON so bias is active during sweep
                    Shared.SMU_slave.Reset();
                    Shared.SMU_slave.SetSweepChannel(smuSlave);
                    Shared.SMU_slave.SetReadingChannel(smuSlave);
                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(smuMaster);
                    Shared.SMU_master.SetReadingChannel(smuMaster);
                    Shared.SMU_slave.InitiateReading(new List<int>() { smuSlave.Channel }, smuSlave);
                    Shared.SMU_master.InitiateReading(new List<int>() { smuMaster.Channel }, smuMaster);
                    ReadIntoLists(Shared.SMU_master, smuMaster.Channel, smuMaster.SweepRange.Steps, true,
                        testResults.Remote_Z_IPV_Results.CH3_Voltage,
                        testResults.Remote_Z_IPV_Results.CH3_Current,
                        testResults.Remote_Z_IPV_Results.CH3_Time);
                    ReadIntoLists(Shared.SMU_slave, smuSlave.Channel, smuSlave.SweepRange.Steps, true,
                        testResults.Remote_Z_IPV_Results.CH2_Voltage,
                        testResults.Remote_Z_IPV_Results.CH2_Current,
                        testResults.Remote_Z_IPV_Results.CH2_Time);

                    testResults.Remote_Z_IPV_Results.SerialNumber = DUT.SerialNumber;
                    testResults.Remote_Z_IPV_Results.Temperature = temperature;
                    testResults.Remote_Z_IPV_Results.Pass = true;
                    testResults.OverallPassFail = OverallPassFail.PASS;

                    Shared.testResultSaver.SaveResults(DUT.SerialNumber, testResults, TestSequences.Remote_Z_IPV, sweepNo, temperature);
                    testResults.Dispose(); testResults = null;
                    testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                }

                Shared.SMU_master.CloseAllChannels();
                Shared.SMU_slave.CloseAllChannels();
                bgw.ReportProgress(index, $"►Remote_Z_IPV @{temperature}°C | {SuccessStatusText()}");
                return true;
            }
            catch (EquipmentCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("RunRemote_Z_IPV", ex);
                bgw.ReportProgress(index, $"►Remote_Z_IPV @{temperature}°C | Failed");
                testResults.OverallPassFail = OverallPassFail.FAIL;
                return false;
            }
        }

        // ── Remote_Z_VPV ───────────────────────────────────────────────────────
        public static bool RunRemote_Z_VPV(TestSequenceModel sequence, TestResultModel testResults,
            BackgroundWorker bgw, int index, int sweepNo, double temperature, out bool cancelled)
        {
            cancelled = false;
            try
            {
                var smuMaster = BuildSMUSettings(Shared.Remote_Z_VPV.SMU1.Channels[0]);
                var smuSlave = BuildSMUSettings(Shared.Remote_Z_VPV.SMU2.Channels[0]);

                if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                    throw new EquipmentCommunicationException("Optical switch 1x4 cannot close channel 1 (Remote_Z_VPV).");

                Shared.ElectricalSwitchRemote2.Reset();

                foreach (var DUT in sequence.RemoteDUTs)
                {
                    Shared.CurrentSimPartSerial = DUT.SerialNumber;
                    testResults.Remote_Z_VPV_Results.Pass = false;
                    if (bgw.CancellationPending) { cancelled = true; break; }

                    if (!Shared.OpticalSwitch1x13_Remote.CloseChannel(DUT.Slot))
                        throw new EquipmentCommunicationException($"Remote optical switch 1x13 cannot close channel {DUT.Slot}.");

                    if (!Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_VPV.ElectricalSwitch1.Positions[0].FromChannel,
                            Shared.Remote_Z_VPV.ElectricalSwitch1.Positions[0].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #1 error (Z_VPV).");

                    // DUT.Slot is 1-based per type (1–12)
                    if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>()
                        {
                            Shared.Remote_Z_VPV.ElectricalSwitch2.Positions[DUT.Slot - 1].FromChannel,
                            Shared.Remote_Z_VPV.ElectricalSwitch2.Positions[DUT.Slot - 1].ToChannel
                        }))
                        throw new EquipmentCommunicationException("Remote electrical switch #3 error (Z_VPV).");

                    // Configure bias channel first — output turns ON so bias is active during sweep
                    Shared.SMU_slave.Reset();
                    Shared.SMU_slave.SetSweepChannel(smuSlave);
                    Shared.SMU_master.Reset();
                    Shared.SMU_master.SetSweepChannel(smuMaster);
                    Shared.SMU_slave.InitiateReading(new List<int>() { smuSlave.Channel }, smuSlave);
                    Shared.SMU_master.InitiateReading(new List<int>() { smuMaster.Channel }, smuMaster);
                    ReadIntoLists(Shared.SMU_master, smuMaster.Channel, smuMaster.SweepRange.Steps, true,
                        testResults.Remote_Z_VPV_Results.CH3_Voltage,
                        testResults.Remote_Z_VPV_Results.CH3_Current,
                        testResults.Remote_Z_VPV_Results.CH3_Time);
                    ReadIntoLists(Shared.SMU_slave, smuSlave.Channel, smuSlave.SweepRange.Steps, true,
                        testResults.Remote_Z_VPV_Results.CH2_Voltage,
                        testResults.Remote_Z_VPV_Results.CH2_Current,
                        testResults.Remote_Z_VPV_Results.CH2_Time);

                    // CH5 Current = CH2 Current; Power = CH2_I × CH2_V
                    int vpvCount = Math.Min(testResults.Remote_Z_VPV_Results.CH2_Current.Count,
                                           testResults.Remote_Z_VPV_Results.CH2_Voltage.Count);
                    for (int i = 0; i < vpvCount; i++)
                    {
                        testResults.Remote_Z_VPV_Results.CH5_Current.Add(testResults.Remote_Z_VPV_Results.CH2_Current[i]);
                        testResults.Remote_Z_VPV_Results.Power.Add(
                            testResults.Remote_Z_VPV_Results.CH2_Current[i] *
                            testResults.Remote_Z_VPV_Results.CH2_Voltage[i]);
                    }

                    testResults.Remote_Z_VPV_Results.SerialNumber = DUT.SerialNumber;
                    testResults.Remote_Z_VPV_Results.Temperature = temperature;
                    testResults.Remote_Z_VPV_Results.Pass = true;
                    testResults.OverallPassFail = OverallPassFail.PASS;

                    Shared.testResultSaver.SaveResults(DUT.SerialNumber, testResults, TestSequences.Remote_Z_VPV, sweepNo, temperature);
                    testResults.Dispose(); testResults = null;
                    testResults = new TestResultModel { OverallPassFail = OverallPassFail.FAIL, SaveIntoProductionDB = true };
                }

                Shared.SMU_master.CloseAllChannels();
                Shared.SMU_slave.CloseAllChannels();
                bgw.ReportProgress(index, $"►Remote_Z_VPV @{temperature}°C | {SuccessStatusText()}");
                return true;
            }
            catch (EquipmentCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("RunRemote_Z_VPV", ex);
                bgw.ReportProgress(index, $"►Remote_Z_VPV @{temperature}°C | Failed");
                testResults.OverallPassFail = OverallPassFail.FAIL;
                return false;
            }
        }
    }
}
