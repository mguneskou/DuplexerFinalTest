using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DuplexerFinalTest.Equipment
{
    public class ClimaticChamber : IClimaticChamber
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public bool IsConnected { get; private set; }
        public ChamberTemperatureModel currentTemperature { get; set; } = new ChamberTemperatureModel();

        public bool Connect(string ipAddress, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.ConnectAsync(ipAddress, port).Wait(5000);
                if (!_client.Connected) return false;
                _stream = _client.GetStream();
                IsConnected = true;
                return true;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            try { _stream?.Close(); _client?.Close(); }
            catch { }
            IsConnected = false;
        }

        public string GetID()
        {
            return SendQuery("$D1");
        }

        private string SendQuery(string command)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(command + "\r\n");
                _stream.Write(data, 0, data.Length);
                Thread.Sleep(100);
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            }
            catch { return string.Empty; }
        }

        private void SendCommand(string command)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(command + "\r\n");
                _stream.Write(data, 0, data.Length);
                Thread.Sleep(100);
            }
            catch { }
        }

        public ChamberTemperatureModel GetTemperature()
        {
            try
            {
                string response = SendQuery("$D1");
                // Parse Espec response: temperature data
                var model = new ChamberTemperatureModel();
                var parts = response.Split(',');
                if (parts.Length >= 2)
                {
                    double.TryParse(parts[0].Trim().TrimStart('$'), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double measured);
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double setpoint);
                    model.MeasuredTemperature = measured;
                    model.SetPointTemperature = setpoint;
                }
                currentTemperature = model;
                return model;
            }
            catch { return new ChamberTemperatureModel(); }
        }

        public ChamberHumidityModel GetHumidity()
        {
            try
            {
                string response = SendQuery("$D2");
                var model = new ChamberHumidityModel();
                var parts = response.Split(',');
                if (parts.Length >= 2)
                {
                    double.TryParse(parts[0].Trim().TrimStart('$'), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double measured);
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double setpoint);
                    model.MeasuredHumidity = measured;
                    model.SetPointHumidity = setpoint;
                }
                return model;
            }
            catch { return new ChamberHumidityModel(); }
        }

        public bool SetTemperature(double temperature)
        {
            SendCommand($"$S1{temperature.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            return true;
        }

        public bool SetHumidity(double humidity)
        {
            SendCommand($"$S2{humidity.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            return true;
        }

        public bool SetMode(ChamberModes mode)
        {
            switch (mode)
            {
                case ChamberModes.OFF: SendCommand("$POFF"); break;
                case ChamberModes.STANDBY: SendCommand("$PSTB"); break;
                case ChamberModes.CONSTANT: SendCommand("$PCON"); break;
            }
            return true;
        }

        public bool IsReady(double targetTemperature, double tolerance)
        {
            var temp = GetTemperature();
            return Math.Abs(temp.MeasuredTemperature - targetTemperature) <= tolerance;
        }

        public bool VerifyProgram(int programNumber, string expectedName, int expectedSteps)
        {
            return true;
        }

        public bool RunLocalProgram(int programNumber, int startStep)
        {
            SendCommand($"$RUNProgramNumber{programNumber},StepNumber{startStep}");
            return true;
        }

        public bool RunRemoteProgram(double startTemperature, double endTemperature, double rampDurationMinutes = 5.0)
        {
            currentTemperature = new ChamberTemperatureModel()
            {
                MeasuredTemperature = startTemperature,
                SetPointTemperature = startTemperature
            };
            SetTemperature(endTemperature);
            return true;
        }

        public bool ProgramPause()
        {
            SendCommand("$PAU");
            return true;
        }

        public bool ProgramContinue()
        {
            SendCommand("$CONT");
            return true;
        }

        public bool ProgramAdvance()
        {
            SendCommand("$NEXT");
            return true;
        }

        public bool ProgramEnd(ChamberProgramEndConditions endCondition)
        {
            switch (endCondition)
            {
                case ChamberProgramEndConditions.OFF: SendCommand("$POFF"); break;
                case ChamberProgramEndConditions.STANDBY: SendCommand("$PSTB"); break;
                case ChamberProgramEndConditions.HOLD: SendCommand("$PHLD"); break;
                case ChamberProgramEndConditions.CONST: SendCommand("$PCON"); break;
            }
            return true;
        }
    }
}
