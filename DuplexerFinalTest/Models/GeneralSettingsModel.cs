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
        public string RESULTS_FOLDER { get; set; }
        public string RESOURCES_FOLDER { get; set; }
        public string USE_SIMULATORS { get; set; }
        public string USE_LOCAL_DATABASE { get; set; }
        public string LOCAL_DATABASE_CONNECTION_STRING { get; set; }
        public string SAVE_RESULTS_TO_DB_AUTO { get; set; }
    }
}
