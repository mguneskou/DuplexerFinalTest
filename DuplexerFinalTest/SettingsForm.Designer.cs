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

            // ── lblSettingsPath ───────────────────────────────────────────
            this.lblSettingsPath.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblSettingsPath.BackColor = System.Drawing.Color.FromArgb(230, 243, 255);
            this.lblSettingsPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSettingsPath.ForeColor = System.Drawing.Color.FromArgb(0, 70, 130);
            this.lblSettingsPath.Location = new System.Drawing.Point(12, 12);
            this.lblSettingsPath.Name = "lblSettingsPath";
            this.lblSettingsPath.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.lblSettingsPath.Size = new System.Drawing.Size(876, 28);
            this.lblSettingsPath.TabIndex = 0;
            this.lblSettingsPath.Text = "Editing: (loading...)";

            // ── tabControl ────────────────────────────────────────────────
            this.tabControl.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.tabControl.Controls.Add(this.tabGeneral);
            this.tabControl.Controls.Add(this.tabEquipment);
            this.tabControl.Controls.Add(this.tabPaths);
            this.tabControl.Controls.Add(this.tabDatabase);
            this.tabControl.Location = new System.Drawing.Point(12, 48);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(876, 534);
            this.tabControl.TabIndex = 1;

            // ── tabGeneral ────────────────────────────────────────────────
            this.tabGeneral.Controls.Add(this.tlpGeneral);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(12);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "  General  ";
            this.tabGeneral.UseVisualStyleBackColor = true;

            // tlpGeneral
            this.tlpGeneral.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.tlpGeneral.AutoSize = true;
            this.tlpGeneral.ColumnCount = 2;
            this.tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
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
            this.tlpGeneral.Location = new System.Drawing.Point(0, 0);
            this.tlpGeneral.Name = "tlpGeneral";
            this.tlpGeneral.RowCount = 5;
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpGeneral.TabIndex = 0;

            // General labels
            this.lblPcName.Anchor         = System.Windows.Forms.AnchorStyles.Left;
            this.lblPcName.AutoSize       = true;
            this.lblPcName.Text           = "PC Name:";
            this.lblBaseItemNo.Anchor     = System.Windows.Forms.AnchorStyles.Left;
            this.lblBaseItemNo.AutoSize   = true;
            this.lblBaseItemNo.Text       = "Base Item Number:";
            this.lblRemoteItemNo.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.lblRemoteItemNo.AutoSize = true;
            this.lblRemoteItemNo.Text     = "Remote Item Number:";
            this.lblSerialNoLength.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.lblSerialNoLength.AutoSize = true;
            this.lblSerialNoLength.Text     = "Serial No Length:";
            this.lblPlotUpdate.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.lblPlotUpdate.AutoSize = true;
            this.lblPlotUpdate.Text     = "Plot Update (minutes):";

            // General textboxes
            this.txtPcName.Anchor         = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtPcName.Name           = "txtPcName";
            this.txtBaseItemNo.Anchor     = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtBaseItemNo.Name       = "txtBaseItemNo";
            this.txtRemoteItemNo.Anchor   = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtRemoteItemNo.Name     = "txtRemoteItemNo";
            this.txtSerialNoLength.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtSerialNoLength.Name   = "txtSerialNoLength";
            this.txtPlotUpdate.Anchor     = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtPlotUpdate.Name       = "txtPlotUpdate";

            // ── tabEquipment ──────────────────────────────────────────────
            this.tabEquipment.Controls.Add(this.dgvEquipment);
            this.tabEquipment.Name = "tabEquipment";
            this.tabEquipment.Padding = new System.Windows.Forms.Padding(12);
            this.tabEquipment.TabIndex = 1;
            this.tabEquipment.Text = "  Equipment  ";
            this.tabEquipment.UseVisualStyleBackColor = true;

            // colDevice
            this.colDevice.HeaderText = "Device";
            this.colDevice.Name = "colDevice";
            this.colDevice.ReadOnly = true;
            this.colDevice.Width = 230;
            this.colDevice.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            this.colDevice.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            this.colDevice.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;

            // colResource
            this.colResource.HeaderText = "VISA Resource / Address";
            this.colResource.Name = "colResource";
            this.colResource.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;

            // dgvEquipment
            this.dgvEquipment.AllowUserToAddRows    = false;
            this.dgvEquipment.AllowUserToDeleteRows = false;
            this.dgvEquipment.AllowUserToResizeRows = false;
            this.dgvEquipment.RowHeadersVisible     = false;
            this.dgvEquipment.SelectionMode         = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvEquipment.BackgroundColor        = System.Drawing.SystemColors.Window;
            this.dgvEquipment.BorderStyle            = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvEquipment.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEquipment.RowTemplate.Height    = 30;
            this.dgvEquipment.Dock                  = System.Windows.Forms.DockStyle.Fill;
            this.dgvEquipment.Name                  = "dgvEquipment";
            this.dgvEquipment.TabIndex              = 0;
            this.dgvEquipment.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colDevice,
                this.colResource });

            // ── tabPaths ──────────────────────────────────────────────────
            this.tabPaths.Controls.Add(this.tlpPaths);
            this.tabPaths.Name = "tabPaths";
            this.tabPaths.Padding = new System.Windows.Forms.Padding(12);
            this.tabPaths.TabIndex = 2;
            this.tabPaths.Text = "  Paths  ";
            this.tabPaths.UseVisualStyleBackColor = true;

            // tlpPaths
            this.tlpPaths.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.tlpPaths.AutoSize = true;
            this.tlpPaths.ColumnCount = 3;
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpPaths.Controls.Add(this.lblResultsFolder,   0, 0);
            this.tlpPaths.Controls.Add(this.txtResultsFolder,   1, 0);
            this.tlpPaths.Controls.Add(this.btnBrowseResults,   2, 0);
            this.tlpPaths.Controls.Add(this.lblResourcesFolder, 0, 1);
            this.tlpPaths.Controls.Add(this.txtResourcesFolder, 1, 1);
            this.tlpPaths.Controls.Add(this.btnBrowseResources, 2, 1);
            this.tlpPaths.Location = new System.Drawing.Point(0, 0);
            this.tlpPaths.Name = "tlpPaths";
            this.tlpPaths.RowCount = 2;
            this.tlpPaths.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tlpPaths.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tlpPaths.TabIndex = 0;

            // Paths labels
            this.lblResultsFolder.Anchor    = System.Windows.Forms.AnchorStyles.Left;
            this.lblResultsFolder.AutoSize  = true;
            this.lblResultsFolder.Text      = "Results Folder:";
            this.lblResourcesFolder.Anchor  = System.Windows.Forms.AnchorStyles.Left;
            this.lblResourcesFolder.AutoSize = true;
            this.lblResourcesFolder.Text    = "Resources Folder:";

            // Paths textboxes
            this.txtResultsFolder.Anchor   = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtResultsFolder.Name     = "txtResultsFolder";
            this.txtResourcesFolder.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtResourcesFolder.Name   = "txtResourcesFolder";

            // Browse buttons
            this.btnBrowseResults.Anchor           = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnBrowseResults.Name             = "btnBrowseResults";
            this.btnBrowseResults.Text             = "Browse...";
            this.btnBrowseResults.UseVisualStyleBackColor = true;
            this.btnBrowseResults.Click           += new System.EventHandler(this.BtnBrowseResults_Click);
            this.btnBrowseResources.Anchor         = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnBrowseResources.Name           = "btnBrowseResources";
            this.btnBrowseResources.Text           = "Browse...";
            this.btnBrowseResources.UseVisualStyleBackColor = true;
            this.btnBrowseResources.Click         += new System.EventHandler(this.BtnBrowseResources_Click);

            // ── tabDatabase ───────────────────────────────────────────────
            this.tabDatabase.Controls.Add(this.tlpDatabase);
            this.tabDatabase.Name = "tabDatabase";
            this.tabDatabase.Padding = new System.Windows.Forms.Padding(12);
            this.tabDatabase.TabIndex = 3;
            this.tabDatabase.Text = "  Database && Simulation  ";
            this.tabDatabase.UseVisualStyleBackColor = true;

            // tlpDatabase
            this.tlpDatabase.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.tlpDatabase.AutoSize = true;
            this.tlpDatabase.ColumnCount = 2;
            this.tlpDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDatabase.Controls.Add(this.chkUseSimulators,    0, 0);
            this.tlpDatabase.Controls.Add(this.chkUseLocalDatabase, 0, 1);
            this.tlpDatabase.Controls.Add(this.lblConnectionString,  0, 2);
            this.tlpDatabase.Controls.Add(this.txtConnectionString,  1, 2);
            this.tlpDatabase.Controls.Add(this.chkSaveAuto,         0, 3);
            this.tlpDatabase.Location = new System.Drawing.Point(0, 0);
            this.tlpDatabase.Name = "tlpDatabase";
            this.tlpDatabase.RowCount = 4;
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlpDatabase.TabIndex = 0;

            // Checkboxes — each spans both columns
            this.chkUseSimulators.Anchor    = System.Windows.Forms.AnchorStyles.Left;
            this.chkUseSimulators.AutoSize  = true;
            this.chkUseSimulators.Name      = "chkUseSimulators";
            this.chkUseSimulators.Text      = "Use Simulators  (no real hardware connected)";
            this.tlpDatabase.SetColumnSpan(this.chkUseSimulators, 2);

            this.chkUseLocalDatabase.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.chkUseLocalDatabase.AutoSize = true;
            this.chkUseLocalDatabase.Name     = "chkUseLocalDatabase";
            this.chkUseLocalDatabase.Text     = "Use Local Database";
            this.tlpDatabase.SetColumnSpan(this.chkUseLocalDatabase, 2);

            this.chkSaveAuto.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.chkSaveAuto.AutoSize = true;
            this.chkSaveAuto.Name     = "chkSaveAuto";
            this.chkSaveAuto.Text     = "Save Results to Database Automatically after each test";
            this.tlpDatabase.SetColumnSpan(this.chkSaveAuto, 2);

            // Connection string row
            this.lblConnectionString.Anchor   = System.Windows.Forms.AnchorStyles.Left;
            this.lblConnectionString.AutoSize = true;
            this.lblConnectionString.Name     = "lblConnectionString";
            this.lblConnectionString.Text     = "Connection String:";

            this.txtConnectionString.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtConnectionString.Name   = "txtConnectionString";

            // ── tlpButtons ────────────────────────────────────────────────
            this.tlpButtons.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.tlpButtons.ColumnCount = 3;
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.tlpButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tlpButtons.Controls.Add(this.btnSave,   0, 0);
            this.tlpButtons.Controls.Add(this.btnCancel, 2, 0);
            this.tlpButtons.Location = new System.Drawing.Point(12, 590);
            this.tlpButtons.Name = "tlpButtons";
            this.tlpButtons.RowCount = 1;
            this.tlpButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpButtons.Size = new System.Drawing.Size(876, 40);
            this.tlpButtons.TabIndex = 2;

            this.btnSave.Anchor             = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Name               = "btnSave";
            this.btnSave.Size               = new System.Drawing.Size(100, 30);
            this.btnSave.TabIndex           = 0;
            this.btnSave.Text               = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click             += new System.EventHandler(this.BtnSave_Click);

            this.btnCancel.Anchor           = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Name             = "btnCancel";
            this.btnCancel.Size             = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex         = 1;
            this.btnCancel.Text             = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click           += new System.EventHandler(this.BtnCancel_Click);

            // ── SettingsForm ──────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(900, 640);
            this.Controls.Add(this.lblSettingsPath);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.tlpButtons);
            this.MinimumSize         = new System.Drawing.Size(820, 680);
            this.Name                = "SettingsForm";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text                = "Settings";
            this.Load               += new System.EventHandler(this.SettingsForm_Load);

            this.tabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabEquipment.ResumeLayout(false);
            this.tabPaths.ResumeLayout(false);
            this.tabDatabase.ResumeLayout(false);
            this.tlpGeneral.ResumeLayout(false);
            this.tlpGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEquipment)).EndInit();
            this.tlpPaths.ResumeLayout(false);
            this.tlpPaths.PerformLayout();
            this.tlpDatabase.ResumeLayout(false);
            this.tlpDatabase.PerformLayout();
            this.tlpButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // ── Field declarations ─────────────────────────────────────────────
        private System.Windows.Forms.Label              lblSettingsPath;
        private System.Windows.Forms.TabControl         tabControl;
        private System.Windows.Forms.TabPage            tabGeneral;
        private System.Windows.Forms.TabPage            tabEquipment;
        private System.Windows.Forms.TabPage            tabPaths;
        private System.Windows.Forms.TabPage            tabDatabase;
        private System.Windows.Forms.TableLayoutPanel   tlpGeneral;
        private System.Windows.Forms.Label              lblPcName;
        private System.Windows.Forms.TextBox            txtPcName;
        private System.Windows.Forms.Label              lblBaseItemNo;
        private System.Windows.Forms.TextBox            txtBaseItemNo;
        private System.Windows.Forms.Label              lblRemoteItemNo;
        private System.Windows.Forms.TextBox            txtRemoteItemNo;
        private System.Windows.Forms.Label              lblSerialNoLength;
        private System.Windows.Forms.TextBox            txtSerialNoLength;
        private System.Windows.Forms.Label              lblPlotUpdate;
        private System.Windows.Forms.TextBox            txtPlotUpdate;
        private System.Windows.Forms.DataGridView       dgvEquipment;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDevice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResource;
        private System.Windows.Forms.TableLayoutPanel   tlpPaths;
        private System.Windows.Forms.Label              lblResultsFolder;
        private System.Windows.Forms.TextBox            txtResultsFolder;
        private System.Windows.Forms.Button             btnBrowseResults;
        private System.Windows.Forms.Label              lblResourcesFolder;
        private System.Windows.Forms.TextBox            txtResourcesFolder;
        private System.Windows.Forms.Button             btnBrowseResources;
        private System.Windows.Forms.TableLayoutPanel   tlpDatabase;
        private System.Windows.Forms.CheckBox           chkUseSimulators;
        private System.Windows.Forms.CheckBox           chkUseLocalDatabase;
        private System.Windows.Forms.Label              lblConnectionString;
        private System.Windows.Forms.TextBox            txtConnectionString;
        private System.Windows.Forms.CheckBox           chkSaveAuto;
        private System.Windows.Forms.TableLayoutPanel   tlpButtons;
        private System.Windows.Forms.Button             btnSave;
        private System.Windows.Forms.Button             btnCancel;
    }
}
