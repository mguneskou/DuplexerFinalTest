using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                Shared.sharedGeneralSettings = ReadGeneralSettings();
                dgvGeneralSettings.DataSource = Shared.sharedGeneralSettings.Transpose();
                dgvGeneralSettings.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dgvGeneralSettings.Columns[0].ReadOnly = true;
                dgvGeneralSettings.Columns[0].DefaultCellStyle.BackColor = Color.Silver;
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"Settings form load: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                dgvGeneralSettings.EndEdit();
                var dt = dgvGeneralSettings.DataSource as DataTable;

                var model = new GeneralSettingsModel();
                model.GeneralSettings = new List<GeneralSetting>();
                model.GeneralSettings.Add(new GeneralSetting()
                {
                    PC_NAME                          = dt.Rows[0][1].ToString(),
                    BASE_ITEM_NUMBER                 = dt.Rows[1][1].ToString(),
                    REMOTE_ITEM_NUMBER               = dt.Rows[2][1].ToString(),
                    SERIAL_NO_LENGTH                 = dt.Rows[3][1].ToString(),
                    PLOT_UPDATE_IN_MINUTES           = dt.Rows[4][1].ToString(),
                    CLIMATIC_CHAMBER_IP_ADDRESS      = dt.Rows[5][1].ToString(),
                    CLIMATIC_CHAMBER_PORT            = dt.Rows[6][1].ToString(),
                    ELECTRICAL_SWITCH1_RESOURCE      = dt.Rows[7][1].ToString(),
                    ELECTRICAL_SWITCH2_RESOURCE      = dt.Rows[8][1].ToString(),
                    ELECTRICAL_SWITCH3_RESOURCE      = dt.Rows[9][1].ToString(),
                    ELECTRICAL_SWITCH4_RESOURCE      = dt.Rows[10][1].ToString(),
                    ELECTRICAL_SWITCH5_RESOURCE      = dt.Rows[11][1].ToString(),
                    ELECTRICAL_SWITCH6_RESOURCE      = dt.Rows[12][1].ToString(),
                    OPTICAL_SWITCH1x4_1_RESOURCE     = dt.Rows[13][1].ToString(),
                    OPTICAL_SWITCH1x4_2_RESOURCE     = dt.Rows[14][1].ToString(),
                    OPTICAL_SWITCH1x13_1_RESOURCE    = dt.Rows[15][1].ToString(),
                    OPTICAL_SWITCH1x13_2_RESOURCE    = dt.Rows[16][1].ToString(),
                    SMU_MASTER_RESOURCE              = dt.Rows[17][1].ToString(),
                    SMU_SLAVE_RESOURCE               = dt.Rows[18][1].ToString(),
                    RESULTS_FOLDER                   = dt.Rows[19][1].ToString(),
                    RESOURCES_FOLDER                 = dt.Rows[20][1].ToString(),
                    USE_SIMULATORS                   = dt.Rows[21][1].ToString(),
                    USE_LOCAL_DATABASE               = dt.Rows[22][1].ToString(),
                    LOCAL_DATABASE_CONNECTION_STRING = dt.Rows[23][1].ToString(),
                });

                WriteGeneralSettings(model);
                Shared.sharedGeneralSettings = ReadGeneralSettings();

                // Re-initialize equipment if simulator/database settings changed
                Shared.InitializeEquipment(Shared.sharedGeneralSettings);

                MessageBox.Show("Settings saved successfully.");
                this.Close();
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"Settings save: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public GeneralSettingsModel ReadGeneralSettings()
        {
            try
            {
                string json;
                using (var sr = new StreamReader(Shared.GeneralSettingsPath))
                {
                    json = sr.ReadToEnd();
                }
                return JsonConvert.DeserializeObject<GeneralSettingsModel>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot read general settings. Make sure 'SettingsGeneral.json' exists. => {ex.Message}");
            }
        }

        public void WriteGeneralSettings(GeneralSettingsModel settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(Shared.GeneralSettingsPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot write general settings. => {ex.Message}");
            }
        }
    }
}
