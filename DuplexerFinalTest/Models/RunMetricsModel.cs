using System;

namespace DuplexerFinalTest.Models
{
    public class RunMetricsModel
    {
        public DateTime? SerialEntryCompletedAt { get; set; }
        public DateTime? TestStartRequestedAt { get; set; }
        public int DuplicateScanCorrectionCount { get; private set; }
        public int PretestFailedDutCount { get; private set; }
        public int EquipmentRetryCount { get; private set; }
        public int EquipmentReconnectCount { get; private set; }
        public int ForcedOperatorResumeCount { get; private set; }
        public double ChamberTemperatureErrorTotalC { get; private set; }
        public int ChamberTemperatureSampleCount { get; private set; }
        public double ChamberMaxTemperatureDeviationC { get; private set; }
        public double SoakSettleTotalMinutes { get; private set; }
        public int SoakStepCount { get; private set; }

        public double? AverageChamberTemperatureErrorC => ChamberTemperatureSampleCount > 0
            ? Math.Round(ChamberTemperatureErrorTotalC / ChamberTemperatureSampleCount, 3)
            : (double?)null;

        public double? MaxChamberTemperatureDeviationC => ChamberTemperatureSampleCount > 0
            ? Math.Round(ChamberMaxTemperatureDeviationC, 3)
            : (double?)null;

        public double? AverageSoakSettleMinutes => SoakStepCount > 0
            ? Math.Round(SoakSettleTotalMinutes / SoakStepCount, 2)
            : (double?)null;

        public double? ScanCompleteToTestStartMinutes => SerialEntryCompletedAt.HasValue
            && TestStartRequestedAt.HasValue
            && TestStartRequestedAt.Value >= SerialEntryCompletedAt.Value
            ? Math.Round((TestStartRequestedAt.Value - SerialEntryCompletedAt.Value).TotalMinutes, 2)
            : (double?)null;

        public void RecordDuplicateCorrection()
        {
            DuplicateScanCorrectionCount++;
        }

        public void RecordPretestAttempt(int failedDutCount)
        {
            if (failedDutCount > 0)
                PretestFailedDutCount += failedDutCount;
        }

        public void RecordEquipmentRetry()
        {
            EquipmentRetryCount++;
        }

        public void RecordEquipmentReconnect(int reconnectCount)
        {
            if (reconnectCount > 0)
                EquipmentReconnectCount += reconnectCount;
        }

        public void RecordForcedOperatorResume()
        {
            ForcedOperatorResumeCount++;
        }

        public void RecordChamberTemperature(double measuredTemperature, double targetTemperature)
        {
            if (double.IsNaN(measuredTemperature) || double.IsNaN(targetTemperature))
                return;

            double error = Math.Abs(measuredTemperature - targetTemperature);
            ChamberTemperatureErrorTotalC += error;
            ChamberTemperatureSampleCount++;
            if (error > ChamberMaxTemperatureDeviationC)
                ChamberMaxTemperatureDeviationC = error;
        }

        public void RecordSoakSettle(TimeSpan settleDuration)
        {
            if (settleDuration.TotalMilliseconds < 0)
                return;

            SoakSettleTotalMinutes += settleDuration.TotalMinutes;
            SoakStepCount++;
        }
    }
}