namespace DuplexerFinalTest
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── Outer layout ──────────────────────────────────────────────
            this.tlpOuter = new System.Windows.Forms.TableLayoutPanel();

            // ── Top info label ────────────────────────────────────────────
            this.lblSettingsPath = new System.Windows.Forms.Label();

            // ── Tab control ───────────────────────────────────────────────
            this.tabControl      = new System.Windows.Forms.TabControl();
            this.tabGeneral      = new System.Windows.Forms.TabPage();
            this.tabEquipment    = new System.Windows.Forms.TabPage();
            this.tabPaths        = new System.Windows.Forms.TabPage();
            this.tabDatabase     = new System.Windows.Forms.TabPage();

            // ── General tab ───────────────────────────────────────────────
            this.tlpGeneral        = new System.Windows.Forms.TableLayoutPanel();
            this.lblPcName         = new System.Windows.Forms.Label();
            this.txtPcName         = new System.Windows.Forms.TextBox();
            this.lblBaseItemNo     = new System.Windows.Forms.Label();
            this.txtBaseItemNo     = new System.Windows.Forms.TextBox();
            this.lblRemoteItemNo   = new System.Windows.Forms.Label();
            this.txtRemoteItemNo   = new System.Windows.Forms.TextBox();
            this.lblSerialNoLength = new System.Windows.Forms.Label();
            this.txtSerialNoLength = new System.Windows.Forms.TextBox();
            this.lblPlotUpdate     = new System.Windows.Forms.Label();
            this.txtPlotUpdate     = new System.Windows.Forms.TextBox();

            // ── Equipment tab ─────────────────────────────────────────────
            this.dgvEquipment  = new System.Windows.Forms.DataGridView();
            this.colDevice     = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResource   = new System.Windows.Forms.DataGridViewTextBoxColumn();

            // ── Paths tab ─────────────────────────────────────────────────
            this.tlpPaths            = new System.Windows.Forms.TableLayoutPanel();
            this.lblResultsFolder    = new System.Windows.Forms.Label();
            this.txtResultsFolder    = new System.Windows.Forms.TextBox();
            this.btnBrowseResults    = new System.Windows.Forms.Button();
            this.lblResourcesFolder  = new System.Windows.Forms.Label();
            this.txtResourcesFolder  = new System.Windows.Forms.TextBox();
            this.btnBrowseResources  = new System.Windows.Forms.Button();

            // ── Database & Simulation tab ─────────────────────────────────
            this.tlpDatabase          = new System.Windows.Forms.TableLayoutPanel();
            this.chkUseSimulators     = new System.Windows.Forms.CheckBox();
            this.chkUseLocalDatabase  = new System.Windows.Forms.CheckBox();
            this.lblConnectionString  = new System.Windows.Forms.Label();
            this.txtConnectionString  = new System.Windows.Forms.TextBox();
            this.chkSaveAuto          = new System.Windows.Forms.CheckBox();

            // ── Bottom buttons ────────────────────────────────────────────
            this.tlpButtons = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave    = new System.Windows.Forms.Button();
            this.btnCancel  = new System.Windows.Forms.Button();

            this.tlpOuter.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabEquipment.SuspendLayout();
            this.tabPaths.SuspendLayout();
            this.tabDatabase.SuspendLayout();
            this.tlpGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEquipment)).BeginInit();
            this.tlpPaths.SuspendLayout();
            this.tlpDatabase.SuspendLayout();
            this.tlpButtons.SuspendLayout();
            this.SuspendLayout();

            // ── tlpOuter ──────────────────────────────────────────────────
            // Single column (100 %).  Three rows: fixed header | fill content | fixed footer.
            this.tlpOuter.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tlpOuter.ColumnCount = 1;
            this.tlpOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOuter.RowCount    = 3;
            this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tlpOuter.Controls.Add(this.lblSettingsPath, 0, 0);
            this.tlpOuter.Controls.Add(this.tabControl,      0, 1);
            this.tlpOuter.Controls.Add(this.tlpButtons,      0, 2);
            this.tlpOuter.Padding  = new System.Windows.Forms.Padding(8);
            this.tlpOuter.Name     = "tlpOuter";
            this.tlpOuter.TabIndex = 0;

            // ── lblSettingsPath ───────────────────────────────────────────
            this.lblSettingsPath.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.lblSettingsPath.BackColor   = System.Drawing.Color.FromArgb(230, 243, 255);
            this.lblSettingsPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSettingsPath.ForeColor   = System.Drawing.Color.FromArgb(0, 70, 130);
            this.lblSettingsPath.Name        = "lblSettingsPath";
            this.lblSettingsPath.Padding     = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSettingsPath.TabIndex    = 0;
            this.lblSettingsPath.Text        = "Editing: (loading...)";
            this.lblSettingsPath.TextAlign   = System.Drawing.ContentAlignment.MiddleLeft;

            // ── tabControl ────────────────────────────────────────────────
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Controls.Add(this.tabGeneral);
            this.tabControl.Controls.Add(this.tabEquipment);
            this.tabControl.Controls.Add(this.tabPaths);
            this.tabControl.Controls.Add(this.tabDatabase);
            this.tabControl.Name          = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.TabIndex      = 1;

            // ── tabGeneral ────────────────────────────────────────────────
            this.tabGeneral.Controls.Add(this.tlpGeneral);
            this.tabGeneral.Name    = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(12);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text    = "  General  ";
            this.tabGeneral.UseVisualStyleBackColor = true;

            // tlpGeneral — 2 cols (30 % | 70 %), 5 rows (20 % each), Dock=Fill
            this.tlpGeneral.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tlpGeneral.ColumnCount = 2;
            this.tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tlpGeneral.RowCount    = 5;
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpGeneral.Controls.Add(this.lblPcName,         0, 0);
            this.tlpGeneral.Controls.Add(this.txtPcName,         1, 0);
            this.tlpGeneral.Controls.Add(this.lblBaseItemNo,     0, 1);
            this.tlpGeneral.Controls.Add(this.txtBaseItemNo,     1, 1);
            this.tlpGeneral.Controls.Add(this.lblRemoteItemNo,   0, 2);
            this.tlpGeneral.Controls.Add(this.txtRemoteItemNo,   1, 2);
            this.tlpGeneral.Controls.Add(this.lblSerialNoLength, 0, 3);
            this.tlpGeneral.Controls.Add(this.txtSerialNoLength, 1, 3);
            this.tlpGeneral.Controls.Add(this.lblPlotUpdate,     0, 4);
            this.tlpGeneral.Controls.Add(this.txtPlotUpdate,     1, 4);
            this.tlpGeneral.Name     = "tlpGeneral";
            this.tlpGeneral.TabIndex = 0;

            // General labels — Dock=Fill, text vertically centred
            this.lblPcName.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.lblPcName.TextAlign     = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPcName.Text          = "PC Name:";
            this.lblBaseItemNo.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblBaseItemNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBaseItemNo.Text      = "Base Item Number:";
            this.lblRemoteItemNo.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblRemoteItemNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRemoteItemNo.Text      = "Remote Item Number:";
            this.lblSerialNoLength.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblSerialNoLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSerialNoLength.Text      = "Serial No Length:";
            this.lblPlotUpdate.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblPlotUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPlotUpdate.Text      = "Plot Update (minutes):";

            // General textboxes — Dock=Fill
            this.txtPcName.Dock         = System.Windows.Forms.DockStyle.Fill;
            this.txtPcName.Name         = "txtPcName";
            this.txtBaseItemNo.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.txtBaseItemNo.Name     = "txtBaseItemNo";
            this.txtRemoteItemNo.Dock   = System.Windows.Forms.DockStyle.Fill;
            this.txtRemoteItemNo.Name   = "txtRemoteItemNo";
            this.txtSerialNoLength.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSerialNoLength.Name = "txtSerialNoLength";
            this.txtPlotUpdate.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.txtPlotUpdate.Name     = "txtPlotUpdate";

            // ── tabEquipment ──────────────────────────────────────────────
            this.tabEquipment.Controls.Add(this.dgvEquipment);
            this.tabEquipment.Name     = "tabEquipment";
            this.tabEquipment.Padding  = new System.Windows.Forms.Padding(12);
            this.tabEquipment.TabIndex = 1;
            this.tabEquipment.Text     = "  Equipment  ";
            this.tabEquipment.UseVisualStyleBackColor = true;

            // colDevice
            this.colDevice.HeaderText = "Device";
            this.colDevice.Name       = "colDevice";
            this.colDevice.ReadOnly   = true;
            this.colDevice.Width      = 230;
            this.colDevice.DefaultCellStyle.BackColor          = System.Drawing.Color.FromArgb(235, 235, 235);
            this.colDevice.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            this.colDevice.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;

            // colResource
            this.colResource.HeaderText   = "VISA Resource / Address";
            this.colResource.Name         = "colResource";
            this.colResource.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;

            // dgvEquipment — Dock=Fill
            this.dgvEquipment.AllowUserToAddRows          = false;
            this.dgvEquipment.AllowUserToDeleteRows       = false;
            this.dgvEquipment.AllowUserToResizeRows       = false;
            this.dgvEquipment.RowHeadersVisible           = false;
            this.dgvEquipment.SelectionMode               = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvEquipment.BackgroundColor             = System.Drawing.SystemColors.Window;
            this.dgvEquipment.BorderStyle                 = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvEquipment.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEquipment.RowTemplate.Height          = 30;
            this.dgvEquipment.Dock                        = System.Windows.Forms.DockStyle.Fill;
            this.dgvEquipment.Name                        = "dgvEquipment";
            this.dgvEquipment.TabIndex                    = 0;
            this.dgvEquipment.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colDevice,
                this.colResource });

            // ── tabPaths ──────────────────────────────────────────────────
            this.tabPaths.Controls.Add(this.tlpPaths);
            this.tabPaths.Name     = "tabPaths";
            this.tabPaths.Padding  = new System.Windows.Forms.Padding(12);
            this.tabPaths.TabIndex = 2;
            this.tabPaths.Text     = "  Paths  ";
            this.tabPaths.UseVisualStyleBackColor = true;

            // tlpPaths — 3 cols (20 % | 65 % | 15 %), 2 rows (50 % each), Dock=Fill
            this.tlpPaths.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tlpPaths.ColumnCount = 3;
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tlpPaths.RowCount    = 2;
            this.tlpPaths.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPaths.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpPaths.Controls.Add(this.lblResultsFolder,   0, 0);
            this.tlpPaths.Controls.Add(this.txtResultsFolder,   1, 0);
            this.tlpPaths.Controls.Add(this.btnBrowseResults,   2, 0);
            this.tlpPaths.Controls.Add(this.lblResourcesFolder, 0, 1);
            this.tlpPaths.Controls.Add(this.txtResourcesFolder, 1, 1);
            this.tlpPaths.Controls.Add(this.btnBrowseResources, 2, 1);
            this.tlpPaths.Name     = "tlpPaths";
            this.tlpPaths.TabIndex = 0;

            // Paths labels — Dock=Fill
            this.lblResultsFolder.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblResultsFolder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblResultsFolder.Text      = "Results Folder:";
            this.lblResourcesFolder.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblResourcesFolder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblResourcesFolder.Text      = "Resources Folder:";

            // Paths textboxes — Dock=Fill
            this.txtResultsFolder.Dock   = System.Windows.Forms.DockStyle.Fill;
            this.txtResultsFolder.Name   = "txtResultsFolder";
            this.txtResourcesFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtResourcesFolder.Name = "txtResourcesFolder";

            // Browse buttons — Dock=Fill
            this.btnBrowseResults.Dock                    = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowseResults.Name                    = "btnBrowseResults";
            this.btnBrowseResults.Text                    = "Browse...";
            this.btnBrowseResults.UseVisualStyleBackColor = true;
            this.btnBrowseResults.Click                  += new System.EventHandler(this.BtnBrowseResults_Click);
            this.btnBrowseResources.Dock                    = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowseResources.Name                    = "btnBrowseResources";
            this.btnBrowseResources.Text                    = "Browse...";
            this.btnBrowseResources.UseVisualStyleBackColor = true;
            this.btnBrowseResources.Click                  += new System.EventHandler(this.BtnBrowseResources_Click);

            // ── tabDatabase ───────────────────────────────────────────────
            this.tabDatabase.Controls.Add(this.tlpDatabase);
            this.tabDatabase.Name     = "tabDatabase";
            this.tabDatabase.Padding  = new System.Windows.Forms.Padding(12);
            this.tabDatabase.TabIndex = 3;
            this.tabDatabase.Text     = "  Database && Simulation  ";
            this.tabDatabase.UseVisualStyleBackColor = true;

            // tlpDatabase — 2 cols (30 % | 70 %), 6 rows (approx), Dock=Fill
            this.tlpDatabase.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tlpDatabase.ColumnCount = 2;
            this.tlpDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tlpDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tlpDatabase.RowCount    = 6;
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpDatabase.Controls.Add(this.chkUseSimulators,    0, 0);
            this.tlpDatabase.Controls.Add(this.chkUseLocalDatabase, 0, 1);
            this.tlpDatabase.Controls.Add(this.lblConnectionString,  0, 2);
            this.tlpDatabase.Controls.Add(this.txtConnectionString,  1, 2);
            this.tlpDatabase.Controls.Add(this.chkSaveAuto,         0, 3);
            this.tlpDatabase.Name     = "tlpDatabase";
            this.tlpDatabase.TabIndex = 0;

            // Checkboxes — span both columns, Dock=Fill, AutoSize=false
            this.chkUseSimulators.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.chkUseSimulators.AutoSize = false;
            this.chkUseSimulators.Name     = "chkUseSimulators";
            this.chkUseSimulators.Text     = "Use Simulators  (no real hardware connected)";
            this.tlpDatabase.SetColumnSpan(this.chkUseSimulators, 2);

            this.chkUseLocalDatabase.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.chkUseLocalDatabase.AutoSize = false;
            this.chkUseLocalDatabase.Name     = "chkUseLocalDatabase";
            this.chkUseLocalDatabase.Text     = "Use Local Database";
            this.tlpDatabase.SetColumnSpan(this.chkUseLocalDatabase, 2);

            this.chkSaveAuto.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.chkSaveAuto.AutoSize = false;
            this.chkSaveAuto.Name     = "chkSaveAuto";
            this.chkSaveAuto.Text     = "Save Results to Database Automatically after each test";
            this.tlpDatabase.SetColumnSpan(this.chkSaveAuto, 2);

            // Connection string label / textbox — Dock=Fill
            this.lblConnectionString.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblConnectionString.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblConnectionString.AutoSize = true;
            this.lblConnectionString.Name      = "lblConnectionString";
            this.lblConnectionString.Text      = "Connection String:";

            this.txtConnectionString.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConnectionString.Name = "txtConnectionString";

            // Simulator tuning labels and textboxes
            this.lblSimPartSpread = new System.Windows.Forms.Label();
            this.lblSimMeasNoise  = new System.Windows.Forms.Label();
            this.txtSimPartSpread = new System.Windows.Forms.TextBox();
            this.txtSimMeasNoise  = new System.Windows.Forms.TextBox();

            this.lblSimPartSpread.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSimPartSpread.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSimPartSpread.Name = "lblSimPartSpread";
            this.lblSimPartSpread.Text = "SIM Part Spread (%):";

            this.txtSimPartSpread.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSimPartSpread.Name = "txtSimPartSpread";

            this.lblSimMeasNoise.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSimMeasNoise.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSimMeasNoise.Name = "lblSimMeasNoise";
            this.lblSimMeasNoise.Text = "SIM Measurement Noise (%):";

            this.txtSimMeasNoise.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSimMeasNoise.Name = "txtSimMeasNoise";

            // Add simulator controls to the database layout after they are instantiated
            this.tlpDatabase.Controls.Add(this.lblSimPartSpread, 0, 4);
            this.tlpDatabase.Controls.Add(this.txtSimPartSpread, 1, 4);
            this.tlpDatabase.Controls.Add(this.lblSimMeasNoise,  0, 5);
            this.tlpDatabase.Controls.Add(this.txtSimMeasNoise,  1, 5);

            // ── tlpButtons ────────────────────────────────────────────────
            // 3 cols (33 % | 34 % | 33 %), 1 row (100 %), Dock=Fill
            this.tlpButtons.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tlpButtons.ColumnCount = 3;
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tlpButtons.Controls.Add(this.btnSave,   0, 0);
            this.tlpButtons.Controls.Add(this.btnCancel, 2, 0);
            this.tlpButtons.Name     = "tlpButtons";
            this.tlpButtons.RowCount = 1;
            this.tlpButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpButtons.TabIndex = 2;

            this.btnSave.Dock                    = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Name                    = "btnSave";
            this.btnSave.TabIndex                = 0;
            this.btnSave.Text                    = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click                  += new System.EventHandler(this.BtnSave_Click);

            this.btnCancel.Dock                    = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Name                    = "btnCancel";
            this.btnCancel.TabIndex                = 1;
            this.btnCancel.Text                    = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click                  += new System.EventHandler(this.BtnCancel_Click);

            // ── SettingsForm ──────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(900, 640);
            this.Controls.Add(this.tlpOuter);
            this.MinimumSize         = new System.Drawing.Size(600, 480);
            this.Name                = "SettingsForm";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text                = "Settings";
            this.Load               += new System.EventHandler(this.SettingsForm_Load);

            this.tlpOuter.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabEquipment.ResumeLayout(false);
            this.tabPaths.ResumeLayout(false);
            this.tabDatabase.ResumeLayout(false);
            this.tlpGeneral.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEquipment)).EndInit();
            this.tlpPaths.ResumeLayout(false);
            this.tlpDatabase.ResumeLayout(false);
            this.tlpButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // ── Field declarations ─────────────────────────────────────────────
        private System.Windows.Forms.TableLayoutPanel            tlpOuter;
        private System.Windows.Forms.Label                       lblSettingsPath;
        private System.Windows.Forms.TabControl                  tabControl;
        private System.Windows.Forms.TabPage                     tabGeneral;
        private System.Windows.Forms.TabPage                     tabEquipment;
        private System.Windows.Forms.TabPage                     tabPaths;
        private System.Windows.Forms.TabPage                     tabDatabase;
        private System.Windows.Forms.TableLayoutPanel            tlpGeneral;
        private System.Windows.Forms.Label                       lblPcName;
        private System.Windows.Forms.TextBox                     txtPcName;
        private System.Windows.Forms.Label                       lblBaseItemNo;
        private System.Windows.Forms.TextBox                     txtBaseItemNo;
        private System.Windows.Forms.Label                       lblRemoteItemNo;
        private System.Windows.Forms.TextBox                     txtRemoteItemNo;
        private System.Windows.Forms.Label                       lblSerialNoLength;
        private System.Windows.Forms.TextBox                     txtSerialNoLength;
        private System.Windows.Forms.Label                       lblPlotUpdate;
        private System.Windows.Forms.TextBox                     txtPlotUpdate;
        private System.Windows.Forms.DataGridView                dgvEquipment;
        private System.Windows.Forms.DataGridViewTextBoxColumn   colDevice;
        private System.Windows.Forms.DataGridViewTextBoxColumn   colResource;
        private System.Windows.Forms.TableLayoutPanel            tlpPaths;
        private System.Windows.Forms.Label                       lblResultsFolder;
        private System.Windows.Forms.TextBox                     txtResultsFolder;
        private System.Windows.Forms.Button                      btnBrowseResults;
        private System.Windows.Forms.Label                       lblResourcesFolder;
        private System.Windows.Forms.TextBox                     txtResourcesFolder;
        private System.Windows.Forms.Button                      btnBrowseResources;
        private System.Windows.Forms.TableLayoutPanel            tlpDatabase;
        private System.Windows.Forms.CheckBox                    chkUseSimulators;
        private System.Windows.Forms.CheckBox                    chkUseLocalDatabase;
        private System.Windows.Forms.Label                       lblConnectionString;
        private System.Windows.Forms.TextBox                     txtConnectionString;
        private System.Windows.Forms.CheckBox           chkSaveAuto;
        private System.Windows.Forms.Label                lblSimPartSpread;
        private System.Windows.Forms.TextBox              txtSimPartSpread;
        private System.Windows.Forms.Label                lblSimMeasNoise;
        private System.Windows.Forms.TextBox              txtSimMeasNoise;
        private System.Windows.Forms.TableLayoutPanel   tlpButtons;
        private System.Windows.Forms.Button             btnSave;
        private System.Windows.Forms.Button             btnCancel;
    }
}
