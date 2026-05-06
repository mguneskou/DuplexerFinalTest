using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace DuplexerFinalTest.EquipmentSim
{
    public delegate void UpdateDelegate(ChamberTemperatureModel chamberTemp, double avgDUTTemp);

    public class ClimaticChamberSim : IClimaticChamber
    {
        private BackgroundWorker _bgw;
        private double _startTemperature = 25.0;
        private double _goTemperature = 25.0;
        private double _rampDurationMinutes = 5.0;

        public bool IsConnected { get; private set; }
        public ChamberTemperatureModel currentTemperature { get; set; } = new ChamberTemperatureModel();

        public event UpdateDelegate Update;

        public bool Connect(string ipAddress, int port)
        {
            IsConnected = true;
            _startTemperature = 25.0;
            _goTemperature = 25.0;
            currentTemperature = new ChamberTemperatureModel()
            {
                MeasuredTemperature = 25.0,
                SetPointTemperature = 25.0,
                HigherLimitTemperature = 100.0,
                LowerLimitTemperature = -60.0
            };
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public string GetID()
        {
            return "ESPEC SIMULATED CHAMBER SN:00000000";
        }

        public ChamberTemperatureModel GetTemperature()
        {
            return currentTemperature;
        }

        public ChamberHumidityModel GetHumidity()
        {
            return new ChamberHumidityModel() { MeasuredHumidity = 50.0, SetPointHumidity = 50.0 };
        }

        public bool SetTemperature(double temperature)
        {
            currentTemperature.SetPointTemperature = temperature;
            return true;
        }

        public bool SetHumidity(double humidity) { return true; }

        public bool SetMode(ChamberModes mode) { return true; }

        // Simulator: just store the protection limits in the temperature model so the
        // Safety limit check and the displayed values see the configured range.
        public bool SetTemperatureProtection(double highLimit, double lowLimit)
        {
            currentTemperature.HigherLimitTemperature = highLimit;
            currentTemperature.LowerLimitTemperature  = lowLimit;
            return true;
        }

        public bool IsReady(double targetTemperature, double tolerance)
        {
            return Math.Abs(currentTemperature.MeasuredTemperature - targetTemperature) <= tolerance;
        }

        public bool VerifyProgram(int programNumber, string expectedName, int expectedSteps)
        {
            return true;
        }

        public bool RunLocalProgram(int programNumber, int startStep)
        {
            return true;
        }

        public bool RunRemoteProgram(double startTemperature, double endTemperature, double rampDurationMinutes = 5.0)
        {
            _startTemperature = startTemperature;
            _goTemperature = endTemperature;
            _rampDurationMinutes = rampDurationMinutes > 0 ? rampDurationMinutes : 5.0;
            currentTemperature.MeasuredTemperature = startTemperature;
            currentTemperature.SetPointTemperature = endTemperature;
            return true;
        }

        public void Power(bool on)
        {
            if (on)
            {
                if (_bgw != null && _bgw.IsBusy) return;
                _bgw = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                _bgw.DoWork += Bgw_DoWork;
                _bgw.ProgressChanged += Bgw_ProgressChanged;
                _bgw.RunWorkerCompleted += Bgw_RunWorkerCompleted;
                _bgw.RunWorkerAsync();
            }
            else
            {
                _bgw?.CancelAsync();
            }
        }

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = (BackgroundWorker)sender;
            while (!bgw.CancellationPending)
            {
                bgw.ReportProgress(0);
                Thread.Sleep(1000);
            }
            e.Cancel = true;
        }

        private void Bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Advance temperature one second's worth toward the goal
            double rampSeconds = _rampDurationMinutes * 60.0;
            double step = rampSeconds > 0 ? (_goTemperature - _startTemperature) / rampSeconds : 0;
            currentTemperature.MeasuredTemperature += step;
            currentTemperature.SetPointTemperature = _goTemperature;

            // Clamp to goal to prevent overshoot
            if (step > 0 && currentTemperature.MeasuredTemperature > _goTemperature)
                currentTemperature.MeasuredTemperature = _goTemperature;
            else if (step < 0 && currentTemperature.MeasuredTemperature < _goTemperature)
                currentTemperature.MeasuredTemperature = _goTemperature;

            // Average DUT temperature from thermistors (sim electrical switch returns chamber temp)
            double totalDUT = 0;
            int dutCount = 0;
            try
            {
                var seq = Shared.testRun?.sequence;
                if (seq != null)
                {
                    foreach (var dut in seq.BaseDUTs) { totalDUT += dut.ReadThermistor; dutCount++; }
                    foreach (var dut in seq.RemoteDUTs) { totalDUT += dut.ReadThermistor; dutCount++; }
                }
            }
            catch { }
            double avgDUT = dutCount > 0 ? totalDUT / dutCount : currentTemperature.MeasuredTemperature;

            Update?.Invoke(currentTemperature, avgDUT);
        }

        private void Bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_bgw != null)
            {
                _bgw.DoWork -= Bgw_DoWork;
                _bgw.ProgressChanged -= Bgw_ProgressChanged;
                _bgw.RunWorkerCompleted -= Bgw_RunWorkerCompleted;
            }
        }

        public bool ProgramPause() { return true; }
        public bool ProgramContinue() { return true; }
        public bool ProgramAdvance() { return true; }
        public bool ProgramEnd(ChamberProgramEndConditions endCondition) { return true; }
    }
}
