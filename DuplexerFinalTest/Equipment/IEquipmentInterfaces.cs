using System.Collections.Generic;
using DuplexerFinalTest.Helpers;

namespace DuplexerFinalTest.Equipment
{
    public interface IOpticalSwitch
    {
        bool Connect(string resource);
        void Disconnect();
        bool IsConnected { get; }
        string GetID();
        bool CloseChannel(int channel);
        bool Reset();
    }

    public interface IElectricalSwitch
    {
        bool Connect(string resource);
        void Disconnect();
        bool IsConnected { get; }
        string GetID();
        bool CloseChannels(List<int> channels, bool openAllFirst = true);
        bool Reset();
        double MeasureTemperature(TemperatureMeasureMode mode, int channel);
    }

    public interface ISMU
    {
        bool Connect(string resource);
        void Disconnect();
        bool IsConnected { get; }
        string GetID();
        bool SetSweepChannel(Models.SMUSettingsModel settings);
        bool SetReadingChannel(Models.SMUSettingsModel settings);
        bool InitiateReading(List<int> channels, Models.SMUSettingsModel triggerSettings);
        bool ReadData(int ch, bool fromStart, int len, ref double[,] data, out int actrow);
        bool CloseAllChannels();
        bool Reset();
    }

    public interface IClimaticChamber
    {
        bool Connect(string ipAddress, int port);
        void Disconnect();
        bool IsConnected { get; }
        string GetID();
        bool IsReady(double targetTemperature, double tolerance);
        Models.ChamberTemperatureModel GetTemperature();
        Models.ChamberHumidityModel GetHumidity();
        bool SetTemperature(double temperature);
        bool SetHumidity(double humidity);
        bool SetMode(ChamberModes mode);
        bool VerifyProgram(int programNumber, string expectedName, int expectedSteps);
        bool RunLocalProgram(int programNumber, int startStep);
        bool RunRemoteProgram(double startTemperature, double endTemperature, double rampDurationMinutes = 5.0);
        bool ProgramPause();
        bool ProgramContinue();
        bool ProgramAdvance();
        bool ProgramEnd(ChamberProgramEndConditions endCondition);
        // Programs the chamber controller's own firmware protection limits (TEMP,H / TEMP,L).
        // These mirror the front-panel "Temperature Protection" settings and are applied once
        // on connect so they always match the software safety limits plus a configured margin.
        bool SetTemperatureProtection(double highLimit, double lowLimit);
        // Queries the chamber program monitor (PRGM MON?) to get current step and run status.
        Models.ChamberProgramMonitorModel GetProgramMonitor();
        Models.ChamberTemperatureModel currentTemperature { get; set; }
    }
}
