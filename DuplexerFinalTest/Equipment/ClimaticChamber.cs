using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DuplexerFinalTest.Equipment
{
    // Espec P-300 Communications Option — ASCII protocol over TCP (port 10001).
    // Command reference confirmed from LabVIEW session traces (program ZODIAC.txt).
    // All commands are terminated with CR+LF (\r\n).
    // Temperature response format: "TEMP, measured,setpoint"   e.g. "TEMP, 25.0,25.0"
    // Humidity  response format:   "HUMI, measured,setpoint"   e.g. "HUMI, 50.0,50.0"
    // Mode      response format:   "MODE, CONSTANT"  /  "MODE, STANDBY"  /  "MODE, OFF"
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

        // Marks the TCP link as lost after a runtime send/query failure so
        // Shared.ReconnectDisconnectedEquipment will actually reconnect on retry
        // instead of trusting an out-of-date IsConnected flag.
        private void MarkLinkLost()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
            IsConnected = false;
        }

        // Query current mode — safe check that confirms the chamber is responding.
        public string GetID()
        {
            string mode = SendQuery("MODE?");
            return string.IsNullOrEmpty(mode) ? "Espec P-300 (no response)" : $"Espec P-300 | {mode}";
        }

        private string SendQuery(string command)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(command + "\r\n");
                _stream.Write(data, 0, data.Length);
                Thread.Sleep(200);
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            }
            catch { MarkLinkLost(); return string.Empty; }
        }

        private void SendCommand(string command)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(command + "\r\n");
                _stream.Write(data, 0, data.Length);
                Thread.Sleep(100);
            }
            catch { MarkLinkLost(); }
        }

        // Response: "TEMP, measured,setpoint"  →  parts[0]="TEMP", parts[1]=measured, parts[2]=setpoint
        public ChamberTemperatureModel GetTemperature()
        {
            var model = new ChamberTemperatureModel();
            try
            {
                string response = SendQuery("TEMP?");
                var parts = response.Split(',');
                if (parts.Length >= 3)
                {
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double measured);
                    double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double setpoint);
                    model.MeasuredTemperature = measured;
                    model.SetPointTemperature = setpoint;
                }
                currentTemperature = model;
            }
            catch { }
            return model;
        }

        // Response: "HUMI, measured,setpoint"  →  same pattern as TEMP?
        public ChamberHumidityModel GetHumidity()
        {
            var model = new ChamberHumidityModel();
            try
            {
                string response = SendQuery("HUMI?");
                var parts = response.Split(',');
                if (parts.Length >= 3)
                {
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double measured);
                    double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double setpoint);
                    model.MeasuredHumidity = measured;
                    model.SetPointHumidity = setpoint;
                }
            }
            catch { }
            return model;
        }

        // Sets temperature setpoint.  Chamber must be in CONSTANT mode for this to take effect.
        public bool SetTemperature(double temperature)
        {
            SendCommand($"TEMP,S{temperature.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            return true;
        }

        public bool SetHumidity(double humidity)
        {
            SendCommand($"HUMI,S{humidity.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            return true;
        }

        // Switches the chamber operating mode.
        public bool SetMode(ChamberModes mode)
        {
            switch (mode)
            {
                case ChamberModes.OFF:      SendCommand("MODE SET,OFF");      break;
                case ChamberModes.STANDBY:  SendCommand("MODE SET,STANDBY");  break;
                case ChamberModes.CONSTANT: SendCommand("MODE SET,CONSTANT"); break;
            }
            return true;
        }

        public bool IsReady(double targetTemperature, double tolerance)
        {
            var temp = GetTemperature();
            return Math.Abs(temp.MeasuredTemperature - targetTemperature) <= tolerance;
        }

        // Queries the stored program header and validates name and step count.
        // P-300 response: "steps,<name>,COUNT,..."  e.g. "15,<ZODIAC>,COUNT,A(0.0.0),..."
        public bool VerifyProgram(int programNumber, string expectedName, int expectedSteps)
        {
            try
            {
                string response = SendQuery($"PRGM DATA?,RAM:{programNumber}");
                if (string.IsNullOrEmpty(response) || response.StartsWith("NA:"))
                    return false;

                var parts = response.Split(',');
                if (parts.Length < 2) return false;

                bool stepsOk = int.TryParse(parts[0].Trim(), out int steps) && steps == expectedSteps;
                bool nameOk  = parts[1].Trim().Equals($"<{expectedName}>", StringComparison.OrdinalIgnoreCase);
                return stepsOk && nameOk;
            }
            catch { return false; }
        }

        // Starts a stored program from its first step (startStep is used for future extension).
        public bool RunLocalProgram(int programNumber, int startStep)
        {
            SendCommand($"PRGM RUN,RAM:{programNumber}");
            return true;
        }

        // Primary direct-control method: switches to CONSTANT mode and sets the target setpoint.
        // The chamber ramps at its own rate; TestRun polls IsReady() to detect arrival.
        public bool RunRemoteProgram(double startTemperature, double endTemperature, double rampDurationMinutes = 5.0)
        {
            SetMode(ChamberModes.CONSTANT);
            currentTemperature.SetPointTemperature = endTemperature;
            SetTemperature(endTemperature);
            return true;
        }

        public bool ProgramPause()
        {
            SendCommand("PRGM PAUS");
            return true;
        }

        public bool ProgramContinue()
        {
            SendCommand("PRGM CONT");
            return true;
        }

        public bool ProgramAdvance()
        {
            SendCommand("PRGM NEXT");
            return true;
        }

        public bool ProgramEnd(ChamberProgramEndConditions endCondition)
        {
            switch (endCondition)
            {
                case ChamberProgramEndConditions.HOLD:    SendCommand("PRGM END,HOLD");     break;
                case ChamberProgramEndConditions.STANDBY: SendCommand("PRGM END,STANDBY");  break;
                case ChamberProgramEndConditions.CONST:   SendCommand("PRGM END,CONSTANT"); break;
                case ChamberProgramEndConditions.OFF:     SendCommand("PRGM END,OFF");      break;
            }
            return true;
        }

        // Programs the chamber controller's own firmware temperature protection limits.
        // Equivalent to setting "Temperature Protection High/Low" from the front panel.
        // P-300 ASCII commands: TEMP,H{val:F1}  /  TEMP,L{val:F1}
        // These are NOT the hardware OTP relay (which requires physical adjustment).
        public bool SetTemperatureProtection(double highLimit, double lowLimit)
        {
            SendCommand($"TEMP,H{highLimit.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            SendCommand($"TEMP,L{lowLimit.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
            return true;
        }

        // Queries the chamber program monitor.
        // P-300 response: "MON, RUN, 3/15, 0:45:23"
        public ChamberProgramMonitorModel GetProgramMonitor()
        {
            var model = new ChamberProgramMonitorModel();
            try
            {
                string response = SendQuery("PRGM MON?");
                if (string.IsNullOrEmpty(response) || response.StartsWith("NA:"))
                    return model;
                var parts = response.Split(',');
                if (parts.Length >= 3)
                {
                    model.Status = parts[1].Trim();
                    var stepParts = parts[2].Trim().Split('/');
                    if (stepParts.Length == 2)
                    {
                        int.TryParse(stepParts[0].Trim(), out int cur);
                        int.TryParse(stepParts[1].Trim(), out int total);
                        model.CurrentStep = cur;
                        model.TotalSteps = total;
                    }
                    if (parts.Length >= 4)
                        model.RemainingTime = parts[3].Trim();
                }
            }
            catch { MarkLinkLost(); }
            return model;
        }
    }
}
