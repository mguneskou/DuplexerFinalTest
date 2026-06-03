using System.Collections.Generic;
using System.Data;

namespace DuplexerFinalTest.Models
{
    public class GeneralSettingsModel
    {
        public List<GeneralSetting> GeneralSettings { get; set; }

        public DataTable Transpose()
        {
            var dt = new DataTable();
            dt.Columns.Add("Field");
            dt.Columns.Add("Value");
            var s = GeneralSettings[0];
            dt.Rows.Add("PC Name", s.PC_NAME);
            dt.Rows.Add("Base Item Number", s.BASE_ITEM_NUMBER);
            dt.Rows.Add("Remote Item Number", s.REMOTE_ITEM_NUMBER);
            dt.Rows.Add("Serial No Length", s.SERIAL_NO_LENGTH);
            dt.Rows.Add("Plot Update in Minutes", s.PLOT_UPDATE_IN_MINUTES);
            dt.Rows.Add("Climatic Chamber IP Address", s.CLIMATIC_CHAMBER_IP_ADDRESS);
            dt.Rows.Add("Climatic Chamber Port", s.CLIMATIC_CHAMBER_PORT);
            dt.Rows.Add("Chamber Safe Max Temp (°C)", s.CHAMBER_SAFE_MAX_TEMP);
            dt.Rows.Add("Chamber Safe Min Temp (°C)", s.CHAMBER_SAFE_MIN_TEMP);
            dt.Rows.Add("Chamber HW Prot Margin (°C)", s.CHAMBER_HWPROT_MARGIN_C);
            dt.Rows.Add("Electrical Switch #1", s.ELECTRICAL_SWITCH1_RESOURCE);
            dt.Rows.Add("Electrical Switch #2", s.ELECTRICAL_SWITCH2_RESOURCE);
            dt.Rows.Add("Electrical Switch #3", s.ELECTRICAL_SWITCH3_RESOURCE);
            dt.Rows.Add("Electrical Switch #4", s.ELECTRICAL_SWITCH4_RESOURCE);
            dt.Rows.Add("Electrical Switch #5", s.ELECTRICAL_SWITCH5_RESOURCE);
            dt.Rows.Add("Electrical Switch #6", s.ELECTRICAL_SWITCH6_RESOURCE);
            dt.Rows.Add("Optical Switch 1x4 #1", s.OPTICAL_SWITCH1x4_1_RESOURCE);
            dt.Rows.Add("Optical Switch 1x4 #2", s.OPTICAL_SWITCH1x4_2_RESOURCE);
            dt.Rows.Add("Optical Switch 1x13 #1", s.OPTICAL_SWITCH1x13_1_RESOURCE);
            dt.Rows.Add("Optical Switch 1x13 #2", s.OPTICAL_SWITCH1x13_2_RESOURCE);
            dt.Rows.Add("SMU - Master", s.SMU_MASTER_RESOURCE);
            dt.Rows.Add("SMU - Slave", s.SMU_SLAVE_RESOURCE);
            dt.Rows.Add("Results Folder", s.RESULTS_FOLDER);
            dt.Rows.Add("Resources Folder", s.RESOURCES_FOLDER);
            dt.Rows.Add("Use Simulators", s.USE_SIMULATORS);
            dt.Rows.Add("Use Local Database", s.USE_LOCAL_DATABASE);
            dt.Rows.Add("Local DB Connection String", s.LOCAL_DATABASE_CONNECTION_STRING);
            dt.Rows.Add("Save Results to DB Automatically", s.SAVE_RESULTS_TO_DB_AUTO);
            dt.Rows.Add("Enable Diagnostic UI", s.ENABLE_DIAG_UI);
            return dt;
        }
    }

    public class GeneralSetting
    {
        public string PC_NAME { get; set; }
        public string BASE_ITEM_NUMBER { get; set; }
        public string REMOTE_ITEM_NUMBER { get; set; }
        public string SERIAL_NO_LENGTH { get; set; }
        public string PLOT_UPDATE_IN_MINUTES { get; set; }
        public string OPTICAL_SWITCH1x4_1_RESOURCE { get; set; }
        public string OPTICAL_SWITCH1x4_2_RESOURCE { get; set; }
        public string OPTICAL_SWITCH1x13_1_RESOURCE { get; set; }
        public string OPTICAL_SWITCH1x13_2_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH1_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH2_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH3_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH4_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH5_RESOURCE { get; set; }
        public string ELECTRICAL_SWITCH6_RESOURCE { get; set; }
        public string SMU_MASTER_RESOURCE { get; set; }
        public string SMU_SLAVE_RESOURCE { get; set; }
        public string CLIMATIC_CHAMBER_IP_ADDRESS { get; set; }
        public string CLIMATIC_CHAMBER_PORT { get; set; }
        // Absolute temperature safety limits monitored in software during all chamber polling loops.
        // If the measured chamber temperature goes outside this range, STANDBY is commanded immediately.
        // Defaults if missing from JSON: +100 °C max, -70 °C min.
        public string CHAMBER_SAFE_MAX_TEMP { get; set; }
        public string CHAMBER_SAFE_MIN_TEMP { get; set; }
        // Additional margin (°C) applied beyond the software safety limits when programming
        // the chamber's own hardware protection limits via TEMP,H / TEMP,L on connect.
        // Hardware limit = SAFE_MAX + margin (high) / SAFE_MIN - margin (low).
        // Default if missing from JSON: 5 °C.
        public string CHAMBER_HWPROT_MARGIN_C { get; set; }
        public string RESULTS_FOLDER { get; set; }
        public string RESOURCES_FOLDER { get; set; }
        public string USE_SIMULATORS { get; set; }
        public string USE_LOCAL_DATABASE { get; set; }
        public string LOCAL_DATABASE_CONNECTION_STRING { get; set; }
        public string SAVE_RESULTS_TO_DB_AUTO { get; set; }
        public string ENABLE_DIAG_UI { get; set; }
        // Simulator tuning parameters (optional)
        public string SIM_PART_SPREAD_PCT { get; set; }
        public string SIM_MEAS_NOISE_PCT { get; set; }
    }
}
