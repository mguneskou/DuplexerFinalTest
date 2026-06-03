using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public partial class SettingsForm : Form
    {
        // Row indices in dgvEquipment (must match PopulateEquipmentGrid order)
        private const int ROW_ELEC1       = 0;
        private const int ROW_ELEC2       = 1;
        private const int ROW_ELEC3       = 2;
        private const int ROW_ELEC4       = 3;
        private const int ROW_ELEC5       = 4;
        private const int ROW_ELEC6       = 5;
        private const int ROW_OPT1X4_1   = 6;
        private const int ROW_OPT1X4_2   = 7;
        private const int ROW_OPT1X13_1  = 8;
        private const int ROW_OPT1X13_2  = 9;
        private const int ROW_SMU_MASTER  = 10;
        private const int ROW_SMU_SLAVE   = 11;
        private const int ROW_CHAMBER_IP  = 12;
        private const int ROW_CHAMBER_PORT = 13;
        private const int ROW_CHAMBER_SAFE_MAX = 14;
        private const int ROW_CHAMBER_SAFE_MIN = 15;
        private const int ROW_CHAMBER_HWPROT_MARGIN = 16;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                lblSettingsPath.Text = $"Editing: {Shared.GeneralSettingsPath}";

                Shared.sharedGeneralSettings = ReadGeneralSettings();
                var s = Shared.sharedGeneralSettings.GeneralSettings[0];

                // General tab
                txtPcName.Text         = s.PC_NAME;
                txtBaseItemNo.Text     = s.BASE_ITEM_NUMBER;
                txtRemoteItemNo.Text   = s.REMOTE_ITEM_NUMBER;
                txtSerialNoLength.Text = s.SERIAL_NO_LENGTH;
                txtPlotUpdate.Text     = s.PLOT_UPDATE_IN_MINUTES;

                // Equipment tab
                PopulateEquipmentGrid(s);

                // Paths tab
                txtResultsFolder.Text   = s.RESULTS_FOLDER;
                txtResourcesFolder.Text = s.RESOURCES_FOLDER;

                // Database & Simulation tab
                chkUseSimulators.Checked    = IsTrue(s.USE_SIMULATORS);
                chkUseLocalDatabase.Checked = IsTrue(s.USE_LOCAL_DATABASE);
                txtConnectionString.Text    = s.LOCAL_DATABASE_CONNECTION_STRING;
                chkSaveAuto.Checked         = IsTrue(s.SAVE_RESULTS_TO_DB_AUTO);
                // Simulator tuning
                txtSimPartSpread.Text       = s.SIM_PART_SPREAD_PCT ?? "1.0";
                txtSimMeasNoise.Text        = s.SIM_MEAS_NOISE_PCT ?? "0.05";
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError("Settings form load failed", ex);
                MessageBox.Show($"Settings form load failed:\n{ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateEquipmentGrid(GeneralSetting s)
        {
            dgvEquipment.Rows.Clear();
            dgvEquipment.Rows.Add("Electrical Switch #1",    s.ELECTRICAL_SWITCH1_RESOURCE);
            dgvEquipment.Rows.Add("Electrical Switch #2",    s.ELECTRICAL_SWITCH2_RESOURCE);
            dgvEquipment.Rows.Add("Electrical Switch #3",    s.ELECTRICAL_SWITCH3_RESOURCE);
            dgvEquipment.Rows.Add("Electrical Switch #4",    s.ELECTRICAL_SWITCH4_RESOURCE);
            dgvEquipment.Rows.Add("Electrical Switch #5",    s.ELECTRICAL_SWITCH5_RESOURCE);
            dgvEquipment.Rows.Add("Electrical Switch #6",    s.ELECTRICAL_SWITCH6_RESOURCE);
            dgvEquipment.Rows.Add("Optical Switch 1\u00d74 #1",  s.OPTICAL_SWITCH1x4_1_RESOURCE);
            dgvEquipment.Rows.Add("Optical Switch 1\u00d74 #2",  s.OPTICAL_SWITCH1x4_2_RESOURCE);
            dgvEquipment.Rows.Add("Optical Switch 1\u00d713 #1", s.OPTICAL_SWITCH1x13_1_RESOURCE);
            dgvEquipment.Rows.Add("Optical Switch 1\u00d713 #2", s.OPTICAL_SWITCH1x13_2_RESOURCE);
            dgvEquipment.Rows.Add("SMU - Master",             s.SMU_MASTER_RESOURCE);
            dgvEquipment.Rows.Add("SMU - Slave",              s.SMU_SLAVE_RESOURCE);
            dgvEquipment.Rows.Add("Climatic Chamber IP",      s.CLIMATIC_CHAMBER_IP_ADDRESS);
            dgvEquipment.Rows.Add("Climatic Chamber Port",    s.CLIMATIC_CHAMBER_PORT);
            dgvEquipment.Rows.Add("Chamber Safe Max Temp (\u00b0C)", s.CHAMBER_SAFE_MAX_TEMP ?? "100");
            dgvEquipment.Rows.Add("Chamber Safe Min Temp (\u00b0C)", s.CHAMBER_SAFE_MIN_TEMP ?? "-70");
            dgvEquipment.Rows.Add("Chamber HW Prot Margin (\u00b0C)", s.CHAMBER_HWPROT_MARGIN_C ?? "5");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                dgvEquipment.EndEdit();

                var model = new GeneralSettingsModel();
                model.GeneralSettings = new List<GeneralSetting>();
                model.GeneralSettings.Add(new GeneralSetting()
                {
                    PC_NAME                          = txtPcName.Text.Trim(),
                    BASE_ITEM_NUMBER                 = txtBaseItemNo.Text.Trim(),
                    REMOTE_ITEM_NUMBER               = txtRemoteItemNo.Text.Trim(),
                    SERIAL_NO_LENGTH                 = txtSerialNoLength.Text.Trim(),
                    PLOT_UPDATE_IN_MINUTES           = txtPlotUpdate.Text.Trim(),
                    ELECTRICAL_SWITCH1_RESOURCE      = CellValue(ROW_ELEC1),
                    ELECTRICAL_SWITCH2_RESOURCE      = CellValue(ROW_ELEC2),
                    ELECTRICAL_SWITCH3_RESOURCE      = CellValue(ROW_ELEC3),
                    ELECTRICAL_SWITCH4_RESOURCE      = CellValue(ROW_ELEC4),
                    ELECTRICAL_SWITCH5_RESOURCE      = CellValue(ROW_ELEC5),
                    ELECTRICAL_SWITCH6_RESOURCE      = CellValue(ROW_ELEC6),
                    OPTICAL_SWITCH1x4_1_RESOURCE     = CellValue(ROW_OPT1X4_1),
                    OPTICAL_SWITCH1x4_2_RESOURCE     = CellValue(ROW_OPT1X4_2),
                    OPTICAL_SWITCH1x13_1_RESOURCE    = CellValue(ROW_OPT1X13_1),
                    OPTICAL_SWITCH1x13_2_RESOURCE    = CellValue(ROW_OPT1X13_2),
                    SMU_MASTER_RESOURCE              = CellValue(ROW_SMU_MASTER),
                    SMU_SLAVE_RESOURCE               = CellValue(ROW_SMU_SLAVE),
                    CLIMATIC_CHAMBER_IP_ADDRESS      = CellValue(ROW_CHAMBER_IP),
                    CLIMATIC_CHAMBER_PORT            = CellValue(ROW_CHAMBER_PORT),
                    CHAMBER_SAFE_MAX_TEMP            = CellValue(ROW_CHAMBER_SAFE_MAX),
                    CHAMBER_SAFE_MIN_TEMP            = CellValue(ROW_CHAMBER_SAFE_MIN),
                    CHAMBER_HWPROT_MARGIN_C          = CellValue(ROW_CHAMBER_HWPROT_MARGIN),
                    RESULTS_FOLDER                   = txtResultsFolder.Text.Trim(),
                    RESOURCES_FOLDER                 = txtResourcesFolder.Text.Trim(),
                    USE_SIMULATORS                   = BoolStr(chkUseSimulators.Checked),
                    USE_LOCAL_DATABASE               = BoolStr(chkUseLocalDatabase.Checked),
                    LOCAL_DATABASE_CONNECTION_STRING = txtConnectionString.Text.Trim(),
                    SAVE_RESULTS_TO_DB_AUTO          = BoolStr(chkSaveAuto.Checked),
                    SIM_PART_SPREAD_PCT              = txtSimPartSpread.Text.Trim(),
                    SIM_MEAS_NOISE_PCT               = txtSimMeasNoise.Text.Trim(),
                });

                WriteGeneralSettings(model);
                Shared.sharedGeneralSettings = ReadGeneralSettings();
                Shared.InitializeEquipment(Shared.sharedGeneralSettings);

                MessageBox.Show("Settings saved successfully.", "Settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Settings save: {ex.Message}", MessageType.Error);
                MessageBox.Show($"Failed to save settings:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnBrowseResults_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description  = "Select Results Folder";
                dlg.SelectedPath = txtResultsFolder.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtResultsFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnBrowseResources_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description  = "Select Resources Folder";
                dlg.SelectedPath = txtResourcesFolder.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtResourcesFolder.Text = dlg.SelectedPath;
            }
        }

        public GeneralSettingsModel ReadGeneralSettings()
        {
            try
            {
                string json;
                using (var sr = new StreamReader(Shared.GeneralSettingsPath))
                    json = sr.ReadToEnd();
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

        // ── Helpers ──────────────────────────────────────────────────────
        private string CellValue(int row) =>
            dgvEquipment.Rows[row].Cells["colResource"].Value?.ToString() ?? string.Empty;

        private static bool IsTrue(string value) =>
            value?.Trim().ToLower() == "true";

        private static string BoolStr(bool value) =>
            value ? "true" : "false";
    }
}

