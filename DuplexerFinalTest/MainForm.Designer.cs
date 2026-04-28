namespace DuplexerFinalTest
{
    partial class MainForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.scMain = new System.Windows.Forms.SplitContainer();
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.lblElapsedTime = new System.Windows.Forms.Label();
            this.chartTemperature = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.prgTestProgress = new System.Windows.Forms.ProgressBar();
            this.lstTestProgress = new System.Windows.Forms.ListView();
            this.label12 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.lblChamberTemperature = new System.Windows.Forms.Label();
            this.lblAverageDUTTemperature = new System.Windows.Forms.Label();
            this.tlpBottom = new System.Windows.Forms.TableLayoutPanel();
            this.tlpEquipment = new System.Windows.Forms.TableLayoutPanel();
            this.pnlOpticalSwitch1x4 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlOpticalSwitch1x13_Base = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pnlOpticalSwitch1x13_Remote = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchBase1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchBase2 = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchBase3 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchRemote1 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchRemote2 = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.pnlElectricalSwitchRemote3 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.pnlSMUMaster = new System.Windows.Forms.Panel();
            this.label9 = new System.Windows.Forms.Label();
            this.pnlSMUSlave = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.pnlClimaticChamber = new System.Windows.Forms.Panel();
            this.label13 = new System.Windows.Forms.Label();
            this.pnlDatabase = new System.Windows.Forms.Panel();
            this.labelDB = new System.Windows.Forms.Label();
            this.btnUpdateEquipmentStatus = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuNewTest = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCalibration = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewLogFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuTestProcedure = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.timerElapsed = new System.Windows.Forms.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            this.tlpMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTemperature)).BeginInit();
            this.tlpBottom.SuspendLayout();
            this.tlpEquipment.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();

            // menuStrip1
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.mnuFile });
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1924, 28);
            this.menuStrip1.TabIndex = 1;

            // mnuFile
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuNewTest, this.mnuCalibration, this.mnuSettings, this.mnuViewLogFiles,
                this.mnuTestProcedure, this.mnuHelp, new System.Windows.Forms.ToolStripSeparator(), this.mnuExit });
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(46, 24);
            this.mnuFile.Text = "File";

            // mnuNewTest
            this.mnuNewTest.Name = "mnuNewTest";
            this.mnuNewTest.Size = new System.Drawing.Size(200, 24);
            this.mnuNewTest.Text = "New Test";
            this.mnuNewTest.Click += new System.EventHandler(this.MnuNewTest_Click);

            // mnuCalibration
            this.mnuCalibration.Name = "mnuCalibration";
            this.mnuCalibration.Size = new System.Drawing.Size(200, 24);
            this.mnuCalibration.Text = "Calibration";
            this.mnuCalibration.Click += new System.EventHandler(this.MnuCalibration_Click);

            // mnuSettings
            this.mnuSettings.Name = "mnuSettings";
            this.mnuSettings.Size = new System.Drawing.Size(200, 24);
            this.mnuSettings.Text = "Settings";
            this.mnuSettings.Click += new System.EventHandler(this.MnuSettings_Click);

            // mnuViewLogFiles
            this.mnuViewLogFiles.Name = "mnuViewLogFiles";
            this.mnuViewLogFiles.Size = new System.Drawing.Size(200, 24);
            this.mnuViewLogFiles.Text = "View Log Files";
            this.mnuViewLogFiles.Click += new System.EventHandler(this.MnuViewLogFiles_Click);

            // mnuTestProcedure
            this.mnuTestProcedure.Name = "mnuTestProcedure";
            this.mnuTestProcedure.Size = new System.Drawing.Size(200, 24);
            this.mnuTestProcedure.Text = "Test Procedure";
            this.mnuTestProcedure.Click += new System.EventHandler(this.MnuTestProcedure_Click);

            // mnuHelp
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(200, 24);
            this.mnuHelp.Text = "Help (Equipment Test)";
            this.mnuHelp.Click += new System.EventHandler(this.MnuHelp_Click);

            // mnuExit
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.Size = new System.Drawing.Size(200, 24);
            this.mnuExit.Text = "Exit";
            this.mnuExit.Click += new System.EventHandler(this.MnuExit_Click);

            // scMain
            this.scMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.scMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMain.Location = new System.Drawing.Point(0, 28);
            this.scMain.Margin = new System.Windows.Forms.Padding(4);
            this.scMain.Name = "scMain";
            this.scMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.scMain.Panel1.Controls.Add(this.tlpMain);
            this.scMain.Panel2.Controls.Add(this.tlpBottom);
            this.scMain.Size = new System.Drawing.Size(1924, 1022);
            this.scMain.SplitterDistance = 648;
            this.scMain.SplitterWidth = 5;
            this.scMain.TabIndex = 0;

            // tlpMain (3 cols 15/15/70, 5 rows 5/5/5/5/80)
            this.tlpMain.ColumnCount = 3;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tlpMain.Controls.Add(this.lblElapsedTime, 0, 0);
            this.tlpMain.Controls.Add(this.prgTestProgress, 0, 1);
            this.tlpMain.Controls.Add(this.label12, 0, 2);
            this.tlpMain.Controls.Add(this.lblChamberTemperature, 1, 2);
            this.tlpMain.Controls.Add(this.label14, 0, 3);
            this.tlpMain.Controls.Add(this.lblAverageDUTTemperature, 1, 3);
            this.tlpMain.Controls.Add(this.lstTestProgress, 0, 4);
            this.tlpMain.Controls.Add(this.chartTemperature, 2, 0);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 5;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tlpMain.Size = new System.Drawing.Size(1922, 646);
            this.tlpMain.TabIndex = 12;

            // lblElapsedTime (spans 2 cols)
            this.lblElapsedTime.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblElapsedTime.AutoSize = true;
            this.tlpMain.SetColumnSpan(this.lblElapsedTime, 2);
            this.lblElapsedTime.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
            this.lblElapsedTime.Location = new System.Drawing.Point(3, 6);
            this.lblElapsedTime.Name = "lblElapsedTime";
            this.lblElapsedTime.Size = new System.Drawing.Size(570, 20);
            this.lblElapsedTime.TabIndex = 0;
            this.lblElapsedTime.Text = "Elapsed time: 00:00:00";
            this.lblElapsedTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // prgTestProgress (spans 2 cols)
            this.tlpMain.SetColumnSpan(this.prgTestProgress, 2);
            this.prgTestProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.prgTestProgress.Location = new System.Drawing.Point(3, 35);
            this.prgTestProgress.Name = "prgTestProgress";
            this.prgTestProgress.Size = new System.Drawing.Size(570, 26);
            this.prgTestProgress.TabIndex = 10;

            // label12 - Chamber Temperature label
            this.label12.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 70);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(282, 20);
            this.label12.TabIndex = 6;
            this.label12.Text = "Chamber Temperature (°C):";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // lblChamberTemperature
            this.lblChamberTemperature.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblChamberTemperature.AutoSize = true;
            this.lblChamberTemperature.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.lblChamberTemperature.ForeColor = System.Drawing.Color.Red;
            this.lblChamberTemperature.Location = new System.Drawing.Point(291, 70);
            this.lblChamberTemperature.Name = "lblChamberTemperature";
            this.lblChamberTemperature.Size = new System.Drawing.Size(282, 20);
            this.lblChamberTemperature.TabIndex = 8;
            this.lblChamberTemperature.Text = "--";
            this.lblChamberTemperature.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // label14 - Average DUT Temperature
            this.label14.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 102);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(282, 20);
            this.label14.TabIndex = 7;
            this.label14.Text = "Average DUT Temperature (°C):";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // lblAverageDUTTemperature
            this.lblAverageDUTTemperature.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblAverageDUTTemperature.AutoSize = true;
            this.lblAverageDUTTemperature.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.lblAverageDUTTemperature.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblAverageDUTTemperature.Location = new System.Drawing.Point(291, 102);
            this.lblAverageDUTTemperature.Name = "lblAverageDUTTemperature";
            this.lblAverageDUTTemperature.Size = new System.Drawing.Size(282, 20);
            this.lblAverageDUTTemperature.TabIndex = 9;
            this.lblAverageDUTTemperature.Text = "--";
            this.lblAverageDUTTemperature.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // lstTestProgress (spans 2 cols)
            this.tlpMain.SetColumnSpan(this.lstTestProgress, 2);
            this.lstTestProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstTestProgress.HideSelection = false;
            this.lstTestProgress.Location = new System.Drawing.Point(3, 131);
            this.lstTestProgress.Name = "lstTestProgress";
            this.lstTestProgress.Size = new System.Drawing.Size(570, 512);
            this.lstTestProgress.TabIndex = 2;
            this.lstTestProgress.UseCompatibleStateImageBehavior = false;
            this.lstTestProgress.View = System.Windows.Forms.View.List;

            // chartTemperature (spans all 5 rows)
            chartArea1.Name = "ChartArea1";
            chartArea1.AxisX.Title = "Time";
            chartArea1.AxisY.Title = "Temperature (°C)";
            this.chartTemperature.ChartAreas.Add(chartArea1);
            this.chartTemperature.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartTemperature.Location = new System.Drawing.Point(579, 3);
            this.chartTemperature.Name = "chartTemperature";
            this.tlpMain.SetRowSpan(this.chartTemperature, 5);
            series1.BorderWidth = 2;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Color = System.Drawing.Color.Red;
            series1.Name = "ChamberTemperature";
            series2.BorderWidth = 2;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Color = System.Drawing.Color.Blue;
            series2.Name = "DUTTemperature";
            this.chartTemperature.Series.Add(series1);
            this.chartTemperature.Series.Add(series2);
            this.chartTemperature.Size = new System.Drawing.Size(1340, 640);
            this.chartTemperature.TabIndex = 1;
            this.chartTemperature.Text = "Temperature";

            // tlpBottom (2 cols 35/65)
            this.tlpBottom.ColumnCount = 2;
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tlpBottom.Controls.Add(this.tlpEquipment, 0, 0);
            this.tlpBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBottom.Location = new System.Drawing.Point(0, 0);
            this.tlpBottom.Name = "tlpBottom";
            this.tlpBottom.RowCount = 1;
            this.tlpBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBottom.Size = new System.Drawing.Size(1922, 367);
            this.tlpBottom.TabIndex = 0;

            // tlpEquipment (4 cols 5/45/5/45, 8 rows)
            this.tlpEquipment.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tlpEquipment.ColumnCount = 4;
            this.tlpEquipment.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpEquipment.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tlpEquipment.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlpEquipment.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tlpEquipment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpEquipment.Name = "tlpEquipment";
            this.tlpEquipment.RowCount = 8;
            for (int r = 0; r < 7; r++)
                this.tlpEquipment.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.tlpEquipment.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tlpEquipment.Size = new System.Drawing.Size(666, 361);
            this.tlpEquipment.TabIndex = 1;

            // Row 0: Optical Switch 1x4 | label  |  Elec Remote1 | label
            this.tlpEquipment.Controls.Add(this.pnlOpticalSwitch1x4, 0, 0);
            this.tlpEquipment.Controls.Add(this.label1, 1, 0);
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchRemote1, 2, 0);
            this.tlpEquipment.Controls.Add(this.label4, 3, 0);

            // Row 1: Optical 1x13 Base | label  |  Elec Remote2 | label
            this.tlpEquipment.Controls.Add(this.pnlOpticalSwitch1x13_Base, 0, 1);
            this.tlpEquipment.Controls.Add(this.label2, 1, 1);
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchRemote2, 2, 1);
            this.tlpEquipment.Controls.Add(this.label10, 3, 1);

            // Row 2: Optical 1x13 Remote | label  |  Elec Remote3 | label
            this.tlpEquipment.Controls.Add(this.pnlOpticalSwitch1x13_Remote, 0, 2);
            this.tlpEquipment.Controls.Add(this.label8, 1, 2);
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchRemote3, 2, 2);
            this.tlpEquipment.Controls.Add(this.label6, 3, 2);

            // Row 3: Elec Base1 | label  |  SMU Master | label
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchBase1, 0, 3);
            this.tlpEquipment.Controls.Add(this.label3, 1, 3);
            this.tlpEquipment.Controls.Add(this.pnlSMUMaster, 2, 3);
            this.tlpEquipment.Controls.Add(this.label9, 3, 3);

            // Row 4: Elec Base2 | label  |  SMU Slave | label
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchBase2, 0, 4);
            this.tlpEquipment.Controls.Add(this.label7, 1, 4);
            this.tlpEquipment.Controls.Add(this.pnlSMUSlave, 2, 4);
            this.tlpEquipment.Controls.Add(this.label11, 3, 4);

            // Row 5: Elec Base3 | label  |  Chamber | label
            this.tlpEquipment.Controls.Add(this.pnlElectricalSwitchBase3, 0, 5);
            this.tlpEquipment.Controls.Add(this.label5, 1, 5);
            this.tlpEquipment.Controls.Add(this.pnlClimaticChamber, 2, 5);
            this.tlpEquipment.Controls.Add(this.label13, 3, 5);

            // Row 6: DB panel | label  |  (empty)
            this.tlpEquipment.Controls.Add(this.pnlDatabase, 0, 6);
            this.tlpEquipment.Controls.Add(this.labelDB, 1, 6);

            // Row 7: Update button
            this.tlpEquipment.Controls.Add(this.btnUpdateEquipmentStatus, 0, 7);
            this.tlpEquipment.SetColumnSpan(this.btnUpdateEquipmentStatus, 4);

            // Equipment panel indicators (shared setup)
            SetupIndicatorPanel(this.pnlOpticalSwitch1x4);
            SetupIndicatorPanel(this.pnlOpticalSwitch1x13_Base);
            SetupIndicatorPanel(this.pnlOpticalSwitch1x13_Remote);
            SetupIndicatorPanel(this.pnlElectricalSwitchBase1);
            SetupIndicatorPanel(this.pnlElectricalSwitchBase2);
            SetupIndicatorPanel(this.pnlElectricalSwitchBase3);
            SetupIndicatorPanel(this.pnlElectricalSwitchRemote1);
            SetupIndicatorPanel(this.pnlElectricalSwitchRemote2);
            SetupIndicatorPanel(this.pnlElectricalSwitchRemote3);
            SetupIndicatorPanel(this.pnlSMUMaster);
            SetupIndicatorPanel(this.pnlSMUSlave);
            SetupIndicatorPanel(this.pnlClimaticChamber);
            SetupIndicatorPanel(this.pnlDatabase);

            // Labels
            SetupLabel(this.label1, "Optical Switch 1x4");
            SetupLabel(this.label2, "Optical Switch 1x13 (Base)");
            SetupLabel(this.label8, "Optical Switch 1x13 (Remote)");
            SetupLabel(this.label3, "Electrical Switch Base #1");
            SetupLabel(this.label7, "Electrical Switch Base #2");
            SetupLabel(this.label5, "Electrical Switch Base #3");
            SetupLabel(this.label4, "Electrical Switch Remote #1");
            SetupLabel(this.label10, "Electrical Switch Remote #2");
            SetupLabel(this.label6, "Electrical Switch Remote #3");
            SetupLabel(this.label9, "SMU Master");
            SetupLabel(this.label11, "SMU Slave");
            SetupLabel(this.label13, "Climatic Chamber");
            SetupLabel(this.labelDB, "Database");

            // btnUpdateEquipmentStatus
            this.btnUpdateEquipmentStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnUpdateEquipmentStatus.Name = "btnUpdateEquipmentStatus";
            this.btnUpdateEquipmentStatus.Size = new System.Drawing.Size(180, 30);
            this.btnUpdateEquipmentStatus.TabIndex = 20;
            this.btnUpdateEquipmentStatus.Text = "Update Equipment Status";
            this.btnUpdateEquipmentStatus.UseVisualStyleBackColor = true;
            this.btnUpdateEquipmentStatus.Click += new System.EventHandler(this.BtnUpdateEquipmentStatus_Click);

            // timerElapsed
            this.timerElapsed.Interval = 1000;
            this.timerElapsed.Tick += new System.EventHandler(this.TimerElapsed_Tick);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1924, 1050);
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Duplexer Final Test";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);

            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            this.scMain.ResumeLayout(false);
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTemperature)).EndInit();
            this.tlpBottom.ResumeLayout(false);
            this.tlpEquipment.ResumeLayout(false);
            this.tlpEquipment.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupIndicatorPanel(System.Windows.Forms.Panel p)
        {
            p.Dock = System.Windows.Forms.DockStyle.Fill;
            p.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            p.Margin = new System.Windows.Forms.Padding(4);
        }

        private void SetupLabel(System.Windows.Forms.Label l, string text)
        {
            l.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            l.AutoSize = true;
            l.Text = text;
            l.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        // Controls
        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.Label lblElapsedTime;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartTemperature;
        private System.Windows.Forms.ProgressBar prgTestProgress;
        private System.Windows.Forms.ListView lstTestProgress;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblChamberTemperature;
        private System.Windows.Forms.Label lblAverageDUTTemperature;
        private System.Windows.Forms.TableLayoutPanel tlpBottom;
        private System.Windows.Forms.TableLayoutPanel tlpEquipment;
        private System.Windows.Forms.Panel pnlOpticalSwitch1x4;
        private System.Windows.Forms.Panel pnlOpticalSwitch1x13_Base;
        private System.Windows.Forms.Panel pnlOpticalSwitch1x13_Remote;
        private System.Windows.Forms.Panel pnlElectricalSwitchBase1;
        private System.Windows.Forms.Panel pnlElectricalSwitchBase2;
        private System.Windows.Forms.Panel pnlElectricalSwitchBase3;
        private System.Windows.Forms.Panel pnlElectricalSwitchRemote1;
        private System.Windows.Forms.Panel pnlElectricalSwitchRemote2;
        private System.Windows.Forms.Panel pnlElectricalSwitchRemote3;
        private System.Windows.Forms.Panel pnlSMUMaster;
        private System.Windows.Forms.Panel pnlSMUSlave;
        private System.Windows.Forms.Panel pnlClimaticChamber;
        private System.Windows.Forms.Panel pnlDatabase;
        private System.Windows.Forms.Label label1, label2, label3, label4, label5;
        private System.Windows.Forms.Label label6, label7, label8, label9, label10;
        private System.Windows.Forms.Label label11, label13;
        private System.Windows.Forms.Label labelDB;
        private System.Windows.Forms.Button btnUpdateEquipmentStatus;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuNewTest;
        private System.Windows.Forms.ToolStripMenuItem mnuCalibration;
        private System.Windows.Forms.ToolStripMenuItem mnuSettings;
        private System.Windows.Forms.ToolStripMenuItem mnuViewLogFiles;
        private System.Windows.Forms.ToolStripMenuItem mnuTestProcedure;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuExit;
        private System.Windows.Forms.Timer timerElapsed;
    }
}
