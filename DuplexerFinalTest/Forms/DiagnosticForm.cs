using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using DuplexerFinalTest.Tests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public class DiagnosticForm : Form
    {
        // ── Shared log ────────────────────────────────────────────────────────
        private RichTextBox _rtbLog;
        private Button _btnClearLog;

        // ── Tab control ───────────────────────────────────────────────────────
        private TabControl _tabMain;

        // ── Tab 1: Chamber ────────────────────────────────────────────────────
        private Label _lblChamberMeasured;
        private Label _lblChamberSetpoint;
        private NumericUpDown _nudTargetTemp;
        private NumericUpDown _nudTempTolerance;
        private Button _btnSetAndWait;
        private BackgroundWorker _bwChamber;

        // ── Tab 2: DUT Temperatures ───────────────────────────────────────────
        private RadioButton _rbThermistor;
        private RadioButton _rbThermocouple;
        private ComboBox _cmbDUTSwitch;
        private NumericUpDown _nudDUTChannel;
        private Label _lblSingleDUTResult;
        private Button _btnReadAllBase;
        private Button _btnReadAllRemote;
        private DataGridView _dgvDUTTemps;
        private BackgroundWorker _bwDUTTemp;

        // ── Tab 3: Electrical Switches ────────────────────────────────────────
        private ComboBox _cmbElecSwitch;
        private TextBox _txtElecChannels;
        private RadioButton _rbCloseExclusive;
        private RadioButton _rbCloseAdditive;
        private RadioButton _rbElecOpenAll;

        // ── Tab 4: Optical Switches ───────────────────────────────────────────
        private RadioButton _rbOpt1x4;
        private RadioButton _rbOpt1x13Base;
        private RadioButton _rbOpt1x13Remote;
        private NumericUpDown _nudOptChannel;

        // ── Tab 5: SMU Sweeps ─────────────────────────────────────────────────
        private TextBox _txtBaseDUTSerial;
        private NumericUpDown _nudBaseSlot;
        private NumericUpDown _nudBaseThermCh;
        private TextBox _txtRemoteDUTSerial;
        private NumericUpDown _nudRemoteSlot;
        private NumericUpDown _nudRemoteThermCh;
        private CheckBox _chkBase_Z_IB_IOP;
        private CheckBox _chkBase_Z_IPD;
        private CheckBox _chkRemote_Z_IOP;
        private CheckBox _chkRemote_Z_IPV;
        private CheckBox _chkRemote_Z_VPV;
        private NumericUpDown _nudSweepTemp;
        private Button _btnRunSweeps;
        private ProgressBar _pbSweep;
        private DataGridView _dgvSweepResults;
        private BackgroundWorker _bwSweeps;

        // ── Tab 6: Gold Standard ──────────────────────────────────────────────
        private Label _lblBaselineFile;
        private NumericUpDown _nudGoldTol;
        private Button _btnRunCompare;
        private ProgressBar _pbGold;
        private DataGridView _dgvGoldResults;
        private string _baselineFilePath;
        private BackgroundWorker _bwGold;

        // ─────────────────────────────────────────────────────────────────────
        public DiagnosticForm()
        {
            BuildUI();
            InitBackgroundWorkers();
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUI()
        {
            Text = "Equipment Diagnostic";
            Size = new Size(1050, 820);
            MinimumSize = new Size(840, 640);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.0f);

            // Log panel (bottom)
            var pnlLog = new Panel { Dock = DockStyle.Bottom, Height = 185, Padding = new Padding(4) };
            _btnClearLog = new Button { Text = "Clear Log", Dock = DockStyle.Bottom, Height = 26 };
            _btnClearLog.Click += (s, e) => _rtbLog.Clear();
            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 8.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            pnlLog.Controls.Add(_rtbLog);
            pnlLog.Controls.Add(_btnClearLog);
            Controls.Add(pnlLog);

            // Tab control (fills remaining space above log)
            _tabMain = new TabControl { Dock = DockStyle.Fill };
            _tabMain.TabPages.Add(BuildChamberTab());
            _tabMain.TabPages.Add(BuildDUTTempTab());
            _tabMain.TabPages.Add(BuildElecSwitchTab());
            _tabMain.TabPages.Add(BuildOptSwitchTab());
            _tabMain.TabPages.Add(BuildSMUSweepTab());
            _tabMain.TabPages.Add(BuildGoldStandardTab());
            Controls.Add(_tabMain);
        }

        // ── Tab 1: Chamber ────────────────────────────────────────────────────
        private TabPage BuildChamberTab()
        {
            var tp = new TabPage("  Chamber  ");
            var tlp = MakeTLP(2, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Status group
            var grpStatus = new GroupBox { Text = "Current Status", Dock = DockStyle.Fill };
            var pnlStatus = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = true };
            _lblChamberMeasured = new Label { Text = "Measured: — °C", AutoSize = true, Width = 200, Margin = new Padding(4, 8, 4, 4) };
            _lblChamberSetpoint = new Label { Text = "Setpoint: — °C", AutoSize = true, Width = 200, Margin = new Padding(4, 8, 4, 4) };
            var btnRead = new Button { Text = "Read Temperature", Width = 160, Height = 32, Margin = new Padding(4) };
            var btnStandby = new Button { Text = "Set Mode: Standby", Width = 160, Height = 32, Margin = new Padding(4) };
            btnRead.Click += BtnReadChamberTemp_Click;
            btnStandby.Click += BtnChamberStandby_Click;
            pnlStatus.Controls.AddRange(new Control[] { _lblChamberMeasured, _lblChamberSetpoint, btnRead, btnStandby });
            grpStatus.Controls.Add(pnlStatus);
            grpStatus.Height = 90;

            // Set temperature group
            var grpSet = new GroupBox { Text = "Set Temperature & Wait for Stable", Dock = DockStyle.Fill };
            var pnlSet = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = false };
            pnlSet.Controls.Add(MakeLabel("Target °C:", 80));
            _nudTargetTemp = new NumericUpDown { Minimum = -80, Maximum = 180, DecimalPlaces = 1, Value = 25, Width = 85, Margin = new Padding(0, 4, 12, 4) };
            pnlSet.Controls.Add(_nudTargetTemp);
            pnlSet.Controls.Add(MakeLabel("Tolerance ±°C:", 105));
            _nudTempTolerance = new NumericUpDown { Minimum = 0.1m, Maximum = 20, DecimalPlaces = 1, Increment = 0.5m, Value = 2.0m, Width = 80, Margin = new Padding(0, 4, 12, 4) };
            pnlSet.Controls.Add(_nudTempTolerance);
            _btnSetAndWait = new Button { Text = "Set & Wait for Stable", Width = 190, Height = 32, Margin = new Padding(4) };
            _btnSetAndWait.Click += BtnSetAndWait_Click;
            pnlSet.Controls.Add(_btnSetAndWait);
            grpSet.Controls.Add(pnlSet);
            grpSet.Height = 80;

            tlp.Controls.Add(grpStatus, 0, 0);
            tlp.Controls.Add(grpSet, 0, 1);
            tp.Controls.Add(tlp);
            return tp;
        }

        // ── Tab 2: DUT Temperatures ───────────────────────────────────────────
        private TabPage BuildDUTTempTab()
        {
            var tp = new TabPage("  DUT Temps  ");
            var tlp = MakeTLP(3, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Sensor type
            var grpSensor = new GroupBox { Text = "Sensor Type", Dock = DockStyle.Fill, Height = 65 };
            var pnlSensor = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            _rbThermistor = new RadioButton { Text = "Thermistor (5 kΩ)", AutoSize = true, Checked = true, Margin = new Padding(4) };
            _rbThermocouple = new RadioButton { Text = "Thermocouple (K-type)", AutoSize = true, Margin = new Padding(4) };
            pnlSensor.Controls.AddRange(new Control[] { _rbThermistor, _rbThermocouple });
            grpSensor.Controls.Add(pnlSensor);

            // Single channel
            var grpSingle = new GroupBox { Text = "Single Channel Read", Dock = DockStyle.Fill, Height = 72 };
            var pnlSingle = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = false };
            pnlSingle.Controls.Add(MakeLabel("Switch:", 55));
            _cmbDUTSwitch = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175, Margin = new Padding(0, 4, 12, 4) };
            _cmbDUTSwitch.Items.AddRange(new object[] { "Base 1", "Base 2", "Base 3", "Remote 1", "Remote 2", "Remote 3" });
            _cmbDUTSwitch.SelectedIndex = 2; // Default: Base 3 (thermistor switch)
            pnlSingle.Controls.Add(_cmbDUTSwitch);
            pnlSingle.Controls.Add(MakeLabel("Channel:", 62));
            _nudDUTChannel = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Width = 70, Margin = new Padding(0, 4, 12, 4) };
            pnlSingle.Controls.Add(_nudDUTChannel);
            var btnReadSingle = new Button { Text = "Read", Width = 75, Height = 32, Margin = new Padding(4) };
            btnReadSingle.Click += BtnReadSingleDUT_Click;
            pnlSingle.Controls.Add(btnReadSingle);
            _lblSingleDUTResult = new Label { Text = "— °C", AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Margin = new Padding(10, 8, 4, 4) };
            pnlSingle.Controls.Add(_lblSingleDUTResult);
            grpSingle.Controls.Add(pnlSingle);

            // All DUT slots from current sequence
            var grpAll = new GroupBox { Text = "All DUT Slots  (uses current test sequence loaded from StartForm)", Dock = DockStyle.Fill };
            var tlpAll = MakeTLP(2, 1);
            tlpAll.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            tlpAll.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlAllBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            _btnReadAllBase = new Button { Text = "Read All Base Slots", Width = 185, Height = 32, Margin = new Padding(4) };
            _btnReadAllRemote = new Button { Text = "Read All Remote Slots", Width = 185, Height = 32, Margin = new Padding(4) };
            _btnReadAllBase.Click += BtnReadAllBase_Click;
            _btnReadAllRemote.Click += BtnReadAllRemote_Click;
            pnlAllBtns.Controls.AddRange(new Control[] { _btnReadAllBase, _btnReadAllRemote });

            _dgvDUTTemps = MakeDGV();
            _dgvDUTTemps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", Name = "cDUTType", Width = 70 });
            _dgvDUTTemps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Serial No", Name = "cSerial", Width = 130 });
            _dgvDUTTemps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Slot", Name = "cSlot", Width = 50 });
            _dgvDUTTemps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Therm Ch", Name = "cThermCh", Width = 80 });
            _dgvDUTTemps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Temperature (°C)", Name = "cTemp", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            tlpAll.Controls.Add(pnlAllBtns, 0, 0);
            tlpAll.Controls.Add(_dgvDUTTemps, 0, 1);
            grpAll.Controls.Add(tlpAll);

            tlp.Controls.Add(grpSensor, 0, 0);
            tlp.Controls.Add(grpSingle, 0, 1);
            tlp.Controls.Add(grpAll, 0, 2);
            tp.Controls.Add(tlp);
            return tp;
        }

        // ── Tab 3: Electrical Switches ────────────────────────────────────────
        private TabPage BuildElecSwitchTab()
        {
            var tp = new TabPage("  Elec Switches  ");
            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 230, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12) };

            var row1 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 4, 0, 4) };
            row1.Controls.Add(MakeLabel("Switch:", 58));
            _cmbElecSwitch = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(0, 2, 16, 2) };
            _cmbElecSwitch.Items.AddRange(new object[] { "Base 1 (ElecSwBase1)", "Base 2 (ElecSwBase2)", "Base 3 (ElecSwBase3)", "Remote 1 (ElecSwRemote1)", "Remote 2 (ElecSwRemote2)", "Remote 3 (ElecSwRemote3)" });
            _cmbElecSwitch.SelectedIndex = 0;
            row1.Controls.Add(_cmbElecSwitch);
            row1.Controls.Add(MakeLabel("Channels (comma-separated):", 200));
            _txtElecChannels = new TextBox { Text = "1", Width = 200, Margin = new Padding(0, 2, 0, 2) };
            row1.Controls.Add(_txtElecChannels);
            pnl.Controls.Add(row1);

            var row2 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 4) };
            _rbCloseExclusive = new RadioButton { Text = "Close – exclusive (open all others first)", AutoSize = true, Checked = true, Margin = new Padding(4) };
            _rbCloseAdditive = new RadioButton { Text = "Close – additive (keep others)", AutoSize = true, Margin = new Padding(4) };
            _rbElecOpenAll = new RadioButton { Text = "Open All (Reset)", AutoSize = true, Margin = new Padding(4) };
            row2.Controls.AddRange(new Control[] { _rbCloseExclusive, _rbCloseAdditive, _rbElecOpenAll });
            pnl.Controls.Add(row2);

            var row3 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 4) };
            var btnExecute = new Button { Text = "Execute", Width = 120, Height = 32, Margin = new Padding(4) };
            btnExecute.Click += BtnElecExecute_Click;
            row3.Controls.Add(btnExecute);
            pnl.Controls.Add(row3);
            tp.Controls.Add(pnl);
            return tp;
        }

        // ── Tab 4: Optical Switches ───────────────────────────────────────────
        private TabPage BuildOptSwitchTab()
        {
            var tp = new TabPage("  Opt Switches  ");
            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 170, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12) };

            var row1 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 4, 0, 4) };
            _rbOpt1x4 = new RadioButton { Text = "1×4 Optical Switch", AutoSize = true, Checked = true, Margin = new Padding(4) };
            _rbOpt1x13Base = new RadioButton { Text = "1×13 Base", AutoSize = true, Margin = new Padding(4) };
            _rbOpt1x13Remote = new RadioButton { Text = "1×13 Remote", AutoSize = true, Margin = new Padding(4) };
            row1.Controls.AddRange(new Control[] { _rbOpt1x4, _rbOpt1x13Base, _rbOpt1x13Remote });
            pnl.Controls.Add(row1);

            var row2 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 4) };
            row2.Controls.Add(MakeLabel("Channel:", 65));
            _nudOptChannel = new NumericUpDown { Minimum = 1, Maximum = 13, Value = 1, Width = 75, Margin = new Padding(0, 4, 16, 4) };
            row2.Controls.Add(_nudOptChannel);
            var btnRoute = new Button { Text = "Route to Channel", Width = 155, Height = 32, Margin = new Padding(4) };
            var btnOptReset = new Button { Text = "Reset (All Open)", Width = 155, Height = 32, Margin = new Padding(4) };
            btnRoute.Click += BtnOptRoute_Click;
            btnOptReset.Click += BtnOptReset_Click;
            row2.Controls.AddRange(new Control[] { btnRoute, btnOptReset });
            pnl.Controls.Add(row2);

            tp.Controls.Add(pnl);
            return tp;
        }

        // ── Tab 5: SMU Sweeps ─────────────────────────────────────────────────
        private TabPage BuildSMUSweepTab()
        {
            var tp = new TabPage("  SMU Sweeps  ");
            var tlp = MakeTLP(4, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // DUT configuration
            var grpDUT = new GroupBox { Text = "DUT Configuration (used for SMU sweeps and Gold Standard)", Dock = DockStyle.Fill, Height = 108 };
            var tlpDUT = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 2, Padding = new Padding(6, 4, 6, 4) };
            for (int i = 0; i < 7; i++) tlpDUT.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpDUT.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlpDUT.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Base DUT row
            tlpDUT.Controls.Add(MakeLabelFill("Base Serial:"), 0, 0);
            _txtBaseDUTSerial = new TextBox { Text = "DIAG_BASE", Dock = DockStyle.Fill, Margin = new Padding(2) };
            tlpDUT.Controls.Add(_txtBaseDUTSerial, 1, 0);
            tlpDUT.Controls.Add(MakeLabelFill("Slot:"), 2, 0);
            _nudBaseSlot = new NumericUpDown { Minimum = 1, Maximum = 6, Value = 1, Width = 55 };
            tlpDUT.Controls.Add(_nudBaseSlot, 3, 0);
            tlpDUT.Controls.Add(MakeLabelFill("Therm Ch:"), 4, 0);
            _nudBaseThermCh = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Width = 65 };
            tlpDUT.Controls.Add(_nudBaseThermCh, 5, 0);

            // Remote DUT row
            tlpDUT.Controls.Add(MakeLabelFill("Remote Serial:"), 0, 1);
            _txtRemoteDUTSerial = new TextBox { Text = "DIAG_REMOTE", Dock = DockStyle.Fill, Margin = new Padding(2) };
            tlpDUT.Controls.Add(_txtRemoteDUTSerial, 1, 1);
            tlpDUT.Controls.Add(MakeLabelFill("Slot:"), 2, 1);
            _nudRemoteSlot = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 1, Width = 55 };
            tlpDUT.Controls.Add(_nudRemoteSlot, 3, 1);
            tlpDUT.Controls.Add(MakeLabelFill("Therm Ch:"), 4, 1);
            _nudRemoteThermCh = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Width = 65 };
            tlpDUT.Controls.Add(_nudRemoteThermCh, 5, 1);

            grpDUT.Controls.Add(tlpDUT);

            // Test selection
            var grpTests = new GroupBox { Text = "Test Types", Dock = DockStyle.Fill, Height = 82 };
            var pnlTests = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = true };
            _chkBase_Z_IB_IOP = new CheckBox { Text = "Base_Z_IB_IOP", Checked = true, AutoSize = true, Margin = new Padding(4) };
            _chkBase_Z_IPD    = new CheckBox { Text = "Base_Z_IPD",    Checked = true, AutoSize = true, Margin = new Padding(4) };
            _chkRemote_Z_IOP  = new CheckBox { Text = "Remote_Z_IOP",  Checked = true, AutoSize = true, Margin = new Padding(4) };
            _chkRemote_Z_IPV  = new CheckBox { Text = "Remote_Z_IPV",  Checked = true, AutoSize = true, Margin = new Padding(4) };
            _chkRemote_Z_VPV  = new CheckBox { Text = "Remote_Z_VPV",  Checked = true, AutoSize = true, Margin = new Padding(4) };
            pnlTests.Controls.AddRange(new Control[] { _chkBase_Z_IB_IOP, _chkBase_Z_IPD, _chkRemote_Z_IOP, _chkRemote_Z_IPV, _chkRemote_Z_VPV });
            pnlTests.Controls.Add(MakeLabel("  At Temp (°C):", 110));
            _nudSweepTemp = new NumericUpDown { Minimum = -80, Maximum = 180, DecimalPlaces = 1, Value = 25, Width = 82, Margin = new Padding(0, 4, 8, 4) };
            pnlTests.Controls.Add(_nudSweepTemp);
            _btnRunSweeps = new Button { Text = "▶  Run Selected Sweeps", Width = 210, Height = 32, Margin = new Padding(8, 4, 4, 4) };
            _btnRunSweeps.Click += BtnRunSweeps_Click;
            pnlTests.Controls.Add(_btnRunSweeps);
            grpTests.Controls.Add(pnlTests);

            _pbSweep = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };

            _dgvSweepResults = MakeDGV();
            _dgvSweepResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Test Type", Name = "cTestType", Width = 170 });
            _dgvSweepResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DUT Serial", Name = "cDUTSerial", Width = 140 });
            _dgvSweepResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sweep Points", Name = "cPts", Width = 110 });
            _dgvSweepResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Name = "cStatus", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            tlp.Controls.Add(grpDUT, 0, 0);
            tlp.Controls.Add(grpTests, 0, 1);
            tlp.Controls.Add(_pbSweep, 0, 2);
            tlp.Controls.Add(_dgvSweepResults, 0, 3);
            tp.Controls.Add(tlp);
            return tp;
        }

        // ── Tab 6: Gold Standard ──────────────────────────────────────────────
        private TabPage BuildGoldStandardTab()
        {
            var tp = new TabPage("  Gold Standard  ");
            var tlp = MakeTLP(3, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var grpBaseline = new GroupBox { Text = "Baseline CSV — reference sweep data from a known-good unit", Dock = DockStyle.Fill, Height = 120 };
            var pnlBaseline = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(6) };

            var row1 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var btnLoad = new Button { Text = "Load Baseline CSV...", Width = 185, Height = 32, Margin = new Padding(4) };
            btnLoad.Click += BtnLoadBaseline_Click;
            _lblBaselineFile = new Label { Text = "No file loaded", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(8, 10, 4, 4) };
            row1.Controls.AddRange(new Control[] { btnLoad, _lblBaselineFile });
            pnlBaseline.Controls.Add(row1);

            var row2 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row2.Controls.Add(MakeLabel("Tolerance band ±%:", 140));
            _nudGoldTol = new NumericUpDown { Minimum = 0.1m, Maximum = 50, DecimalPlaces = 1, Value = 5.0m, Width = 80, Margin = new Padding(0, 4, 12, 4) };
            row2.Controls.Add(_nudGoldTol);
            _btnRunCompare = new Button { Text = "▶  Run Sweep & Compare", Width = 220, Height = 32, Margin = new Padding(4) };
            _btnRunCompare.Click += BtnRunCompare_Click;
            row2.Controls.AddRange(new Control[] { _nudGoldTol, _btnRunCompare });
            pnlBaseline.Controls.Add(row2);
            grpBaseline.Controls.Add(pnlBaseline);

            _pbGold = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };

            _dgvGoldResults = MakeDGV();
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Data Row", Name = "cGRow", Width = 75 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Column", Name = "cGCol", Width = 180 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Baseline", Name = "cGBase", Width = 110 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Measured", Name = "cGMeas", Width = 110 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Δ%", Name = "cGDelta", Width = 70 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pass", Name = "cGPass", Width = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            tlp.Controls.Add(grpBaseline, 0, 0);
            tlp.Controls.Add(_pbGold, 0, 1);
            tlp.Controls.Add(_dgvGoldResults, 0, 2);
            tp.Controls.Add(tlp);
            return tp;
        }

        // ── BackgroundWorker init ─────────────────────────────────────────────
        private void InitBackgroundWorkers()
        {
            _bwChamber = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _bwChamber.DoWork             += BwChamber_DoWork;
            _bwChamber.ProgressChanged    += BwGeneric_ProgressChanged;
            _bwChamber.RunWorkerCompleted += BwChamber_Completed;

            _bwDUTTemp = new BackgroundWorker { WorkerReportsProgress = true };
            _bwDUTTemp.DoWork             += BwDUTTemp_DoWork;
            _bwDUTTemp.RunWorkerCompleted += BwDUTTemp_Completed;

            _bwSweeps = new BackgroundWorker { WorkerReportsProgress = true };
            _bwSweeps.DoWork             += BwSweeps_DoWork;
            _bwSweeps.ProgressChanged    += BwGeneric_ProgressChanged;
            _bwSweeps.RunWorkerCompleted += BwSweeps_Completed;

            _bwGold = new BackgroundWorker { WorkerReportsProgress = true };
            _bwGold.DoWork             += BwGold_DoWork;
            _bwGold.ProgressChanged    += BwGeneric_ProgressChanged;
            _bwGold.RunWorkerCompleted += BwGold_Completed;
        }

        private void BwGeneric_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string msg)
                Log(msg, Color.Cyan);
        }

        // ── Log helpers ───────────────────────────────────────────────────────
        private void Log(string message, Color? color = null)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}]  {message}";
            if (_rtbLog.InvokeRequired)
                _rtbLog.BeginInvoke((MethodInvoker)(() => AppendLog(line, color)));
            else
                AppendLog(line, color);
        }

        private void AppendLog(string line, Color? color)
        {
            _rtbLog.SelectionStart  = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor  = color ?? Color.Lime;
            _rtbLog.AppendText(line + "\n");
            _rtbLog.ScrollToCaret();
        }

        private void LogOk(string msg)   => Log("[OK] "  + msg, Color.Lime);
        private void LogErr(string msg)  => Log("[ERR] " + msg, Color.OrangeRed);
        private void LogInfo(string msg) => Log(msg,             Color.Cyan);

        // ── Helper factories ──────────────────────────────────────────────────
        private static Label MakeLabel(string text, int width) =>
            new Label { Text = text, AutoSize = true, Width = width, Margin = new Padding(4, 8, 0, 4) };

        private static Label MakeLabelFill(string text) =>
            new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

        private static TableLayoutPanel MakeTLP(int rows, int cols)
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = rows,
                ColumnCount = cols
            };
        }

        private static DataGridView MakeDGV()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            };
        }

        // ── DUT model helpers ─────────────────────────────────────────────────
        private IElectricalSwitch ElecSwitchByIndex(int idx)
        {
            switch (idx)
            {
                case 0: return Shared.ElectricalSwitchBase1;
                case 1: return Shared.ElectricalSwitchBase2;
                case 2: return Shared.ElectricalSwitchBase3;
                case 3: return Shared.ElectricalSwitchRemote1;
                case 4: return Shared.ElectricalSwitchRemote2;
                default: return Shared.ElectricalSwitchRemote3;
            }
        }

        private IOpticalSwitch SelectedOptSwitch()
        {
            if (_rbOpt1x4.Checked)       return Shared.OpticalSwitch1x4;
            if (_rbOpt1x13Base.Checked)  return Shared.OpticalSwitch1x13_Base;
            return Shared.OpticalSwitch1x13_Remote;
        }

        private string SelectedOptSwitchName =>
            _rbOpt1x4.Checked ? "1×4" : _rbOpt1x13Base.Checked ? "1×13 Base" : "1×13 Remote";

        private TemperatureMeasureMode SelectedTempMode =>
            _rbThermocouple.Checked ? TemperatureMeasureMode.ThermoCouple : TemperatureMeasureMode.Thermistor;

        private TestSequenceModel BuildDiagSequence()
        {
            return new TestSequenceModel
            {
                SequenceName = "Diagnostic",
                BaseDUTs = new List<DUTModel>
                {
                    new DUTModel
                    {
                        SerialNumber      = _txtBaseDUTSerial.Text.Trim(),
                        DUTType           = DUTType.Base,
                        Slot              = (int)_nudBaseSlot.Value,
                        ThermistorChannel = (int)_nudBaseThermCh.Value,
                        ReadyToTest       = true
                    }
                },
                RemoteDUTs = new List<DUTModel>
                {
                    new DUTModel
                    {
                        SerialNumber      = _txtRemoteDUTSerial.Text.Trim(),
                        DUTType           = DUTType.Remote,
                        Slot              = (int)_nudRemoteSlot.Value,
                        ThermistorChannel = (int)_nudRemoteThermCh.Value,
                        ReadyToTest       = true
                    }
                }
            };
        }

        // ── CHAMBER event handlers ────────────────────────────────────────────
        private void BtnReadChamberTemp_Click(object sender, EventArgs e)
        {
            if (Shared.ClimaticChamber == null || !Shared.ClimaticChamber.IsConnected)
            { LogErr("Chamber not connected."); return; }
            try
            {
                var ct = Shared.ClimaticChamber.GetTemperature();
                _lblChamberMeasured.Text = $"Measured: {ct?.MeasuredTemperature:F1} °C";
                _lblChamberSetpoint.Text = $"Setpoint: {ct?.SetPointTemperature:F1} °C";
                LogOk($"Chamber: measured={ct?.MeasuredTemperature:F1}°C  setpoint={ct?.SetPointTemperature:F1}°C");
            }
            catch (Exception ex) { LogErr($"Read chamber temp: {ex.Message}"); }
        }

        private void BtnChamberStandby_Click(object sender, EventArgs e)
        {
            if (Shared.ClimaticChamber == null || !Shared.ClimaticChamber.IsConnected)
            { LogErr("Chamber not connected."); return; }
            try
            {
                bool ok = Shared.ClimaticChamber.SetMode(ChamberModes.STANDBY);
                if (ok) LogOk("Chamber mode set to STANDBY.");
                else    LogErr("Chamber SetMode(STANDBY) returned false.");
            }
            catch (Exception ex) { LogErr($"Chamber standby: {ex.Message}"); }
        }

        private void BtnSetAndWait_Click(object sender, EventArgs e)
        {
            if (_bwChamber.IsBusy) { LogErr("Chamber operation already in progress."); return; }
            if (Shared.ClimaticChamber == null || !Shared.ClimaticChamber.IsConnected)
            { LogErr("Chamber not connected."); return; }

            _btnSetAndWait.Enabled = false;
            LogInfo($"Chamber: commanding {_nudTargetTemp.Value:F1}°C ±{_nudTempTolerance.Value:F1}°C ...");
            _bwChamber.RunWorkerAsync(new Tuple<double, double>((double)_nudTargetTemp.Value, (double)_nudTempTolerance.Value));
        }

        private void BwChamber_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker)sender;
            var args = (Tuple<double, double>)e.Argument;
            double target = args.Item1;
            double tol    = args.Item2;

            // Read current temp as the start temperature for ramp
            double startTemp = 25.0;
            try
            {
                var ct = Shared.ClimaticChamber.GetTemperature();
                startTemp = ct?.MeasuredTemperature ?? 25.0;
            }
            catch { }

            Shared.ClimaticChamber.RunRemoteProgram(startTemp, target);

            int consecutive = 0;
            const int required = 5;
            long deadline = System.Diagnostics.Stopwatch.GetTimestamp()
                + 120L * 60 * System.Diagnostics.Stopwatch.Frequency;

            while (consecutive < required)
            {
                if (bw.CancellationPending) { e.Cancel = true; return; }
                System.Threading.Thread.Sleep(500);

                double meas = double.NaN;
                try { meas = Shared.ClimaticChamber.GetTemperature()?.MeasuredTemperature ?? double.NaN; }
                catch { }

                bw.ReportProgress(0, $"Chamber stabilising: {meas:F1}°C  target={target:F1}°C  stable={consecutive}/{required}");

                if (System.Diagnostics.Stopwatch.GetTimestamp() > deadline)
                {
                    e.Result = false;
                    return;
                }

                if (!double.IsNaN(meas) && Math.Abs(meas - target) <= tol)
                    consecutive++;
                else
                    consecutive = 0;
            }
            e.Result = true;
        }

        private void BwChamber_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            _btnSetAndWait.Enabled = true;
            if (e.Cancelled)    { LogInfo("Chamber set-and-wait cancelled."); return; }
            if (e.Error != null){ LogErr($"Chamber worker error: {e.Error.Message}"); return; }
            bool ok = (bool)(e.Result ?? false);
            if (ok) LogOk($"Chamber stable at {_nudTargetTemp.Value:F1}°C.");
            else    LogErr("Chamber did not stabilise within 120 minutes.");
        }

        // ── DUT TEMPERATURE event handlers ────────────────────────────────────
        private void BtnReadSingleDUT_Click(object sender, EventArgs e)
        {
            var sw = ElecSwitchByIndex(_cmbDUTSwitch.SelectedIndex);
            if (sw == null || !sw.IsConnected) { LogErr($"Switch '{_cmbDUTSwitch.Text}' not connected."); return; }
            try
            {
                int ch = (int)_nudDUTChannel.Value;
                double t = sw.MeasureTemperature(SelectedTempMode, ch);
                _lblSingleDUTResult.Text = $"{t:F2} °C";
                _lblSingleDUTResult.ForeColor = double.IsNaN(t) ? Color.OrangeRed : Color.DarkGreen;
                LogInfo($"[{_cmbDUTSwitch.Text} ch{ch} {SelectedTempMode}]  →  {t:F2} °C");
            }
            catch (Exception ex) { LogErr($"Read DUT temp: {ex.Message}"); }
        }

        private void BtnReadAllBase_Click(object sender, EventArgs e)
        {
            if (_bwDUTTemp.IsBusy) { LogErr("DUT temp read already in progress."); return; }
            if (Shared.ElectricalSwitchBase3 == null || !Shared.ElectricalSwitchBase3.IsConnected)
            { LogErr("ElectricalSwitchBase3 not connected."); return; }
            SetDUTTempButtons(false);
            _bwDUTTemp.RunWorkerAsync("Base");
        }

        private void BtnReadAllRemote_Click(object sender, EventArgs e)
        {
            if (_bwDUTTemp.IsBusy) { LogErr("DUT temp read already in progress."); return; }
            if (Shared.ElectricalSwitchRemote3 == null || !Shared.ElectricalSwitchRemote3.IsConnected)
            { LogErr("ElectricalSwitchRemote3 not connected."); return; }
            SetDUTTempButtons(false);
            _bwDUTTemp.RunWorkerAsync("Remote");
        }

        private void SetDUTTempButtons(bool enabled)
        {
            _btnReadAllBase.Enabled   = enabled;
            _btnReadAllRemote.Enabled = enabled;
        }

        private void BwDUTTemp_DoWork(object sender, DoWorkEventArgs e)
        {
            string dutType = (string)e.Argument;
            var rows = new List<object[]>();

            List<DUTModel> duts = dutType == "Base"
                ? Shared.infoModel?.Test?.BaseDUTs
                : Shared.infoModel?.Test?.RemoteDUTs;

            if (duts == null || duts.Count == 0)
            {
                Log($"No {dutType} DUTs in current test sequence. Load a sequence from the Start screen first.", Color.Yellow);
                e.Result = rows;
                return;
            }

            foreach (var dut in duts)
            {
                double t = double.NaN;
                try { t = dut.ReadThermistor; }
                catch (Exception ex) { Log($"  {dut.SerialNumber} slot {dut.Slot}: {ex.Message}", Color.OrangeRed); }
                rows.Add(new object[] { dutType, dut.SerialNumber, dut.Slot, dut.ThermistorChannel, double.IsNaN(t) ? "—" : $"{t:F2}" });
            }
            e.Result = rows;
        }

        private void BwDUTTemp_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            SetDUTTempButtons(true);
            if (e.Error != null) { LogErr(e.Error.Message); return; }
            var rows = (List<object[]>)e.Result;
            _dgvDUTTemps.Rows.Clear();
            foreach (var row in rows)
            {
                int i = _dgvDUTTemps.Rows.Add(row);
                string tempStr = row[4].ToString();
                _dgvDUTTemps.Rows[i].DefaultCellStyle.ForeColor = tempStr == "—" ? Color.DimGray : Color.DarkGreen;
                LogInfo($"  {row[0]} serial={row[1]}  slot={row[2]}  therm_ch={row[3]}  →  {tempStr} °C");
            }
            LogOk($"DUT temperature scan complete — {rows.Count} DUTs read.");
        }

        // ── ELECTRICAL SWITCH event handlers ──────────────────────────────────
        private void BtnElecExecute_Click(object sender, EventArgs e)
        {
            var sw = ElecSwitchByIndex(_cmbElecSwitch.SelectedIndex);
            if (sw == null || !sw.IsConnected)
            { LogErr($"'{_cmbElecSwitch.Text}' not connected."); return; }

            try
            {
                if (_rbElecOpenAll.Checked)
                {
                    bool ok = sw.Reset();
                    Log($"Elec {_cmbElecSwitch.Text}: OPEN ALL  →  {(ok ? "OK" : "FAILED")}",
                        ok ? Color.Lime : Color.OrangeRed);
                }
                else
                {
                    var channels = _txtElecChannels.Text
                        .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out int v) ? v : -1)
                        .Where(v => v > 0)
                        .ToList();

                    if (channels.Count == 0) { LogErr("No valid channel numbers entered."); return; }

                    bool openFirst = _rbCloseExclusive.Checked;
                    bool ok = sw.CloseChannels(channels, openFirst);
                    string action = openFirst ? "CLOSE-EXCL" : "CLOSE-ADD";
                    string chStr = string.Join(",", channels);
                    Log($"Elec {_cmbElecSwitch.Text}: {action} ch=[{chStr}]  →  {(ok ? "OK" : "FAILED")}",
                        ok ? Color.Lime : Color.OrangeRed);
                }
            }
            catch (Exception ex) { LogErr($"Elec switch error: {ex.Message}"); }
        }

        // ── OPTICAL SWITCH event handlers ─────────────────────────────────────
        private void BtnOptRoute_Click(object sender, EventArgs e)
        {
            var sw = SelectedOptSwitch();
            if (sw == null || !sw.IsConnected) { LogErr($"Optical switch {SelectedOptSwitchName} not connected."); return; }
            int ch = (int)_nudOptChannel.Value;
            bool ok = sw.CloseChannel(ch);
            Log($"Opt {SelectedOptSwitchName}: route to ch {ch}  →  {(ok ? "OK" : "FAILED")}",
                ok ? Color.Lime : Color.OrangeRed);
        }

        private void BtnOptReset_Click(object sender, EventArgs e)
        {
            var sw = SelectedOptSwitch();
            if (sw == null || !sw.IsConnected) { LogErr($"Optical switch {SelectedOptSwitchName} not connected."); return; }
            bool ok = sw.Reset();
            Log($"Opt {SelectedOptSwitchName}: Reset  →  {(ok ? "OK" : "FAILED")}",
                ok ? Color.Lime : Color.OrangeRed);
        }

        // ── SMU SWEEP event handlers ──────────────────────────────────────────
        private void BtnRunSweeps_Click(object sender, EventArgs e)
        {
            if (_bwSweeps.IsBusy) { LogErr("Sweep already running."); return; }

            var selected = new List<string>();
            if (_chkBase_Z_IB_IOP.Checked) selected.Add("Base_Z_IB_IOP");
            if (_chkBase_Z_IPD.Checked)    selected.Add("Base_Z_IPD");
            if (_chkRemote_Z_IOP.Checked)  selected.Add("Remote_Z_IOP");
            if (_chkRemote_Z_IPV.Checked)  selected.Add("Remote_Z_IPV");
            if (_chkRemote_Z_VPV.Checked)  selected.Add("Remote_Z_VPV");
            if (selected.Count == 0) { LogErr("No test types selected."); return; }

            _dgvSweepResults.Rows.Clear();
            _pbSweep.Value   = 0;
            _pbSweep.Maximum = selected.Count;
            _btnRunSweeps.Enabled = false;

            var seq  = BuildDiagSequence();
            var args = new Tuple<TestSequenceModel, List<string>, double>(seq, selected, (double)_nudSweepTemp.Value);
            _bwSweeps.RunWorkerAsync(args);
        }

        private void BwSweeps_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw   = (BackgroundWorker)sender;
            var args = (Tuple<TestSequenceModel, List<string>, double>)e.Argument;
            var seq  = args.Item1;
            var tests = args.Item2;
            double temp = args.Item3;

            var results = new List<object[]>();
            var testResults = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };

            for (int ti = 0; ti < tests.Count; ti++)
            {
                string testName = tests[ti];
                bw.ReportProgress(0, $"▶ Starting {testName} @ {temp:F1}°C ...");

                bool ok = false;
                bool cancelled = false;
                int  pts = 0;

                try
                {
                    switch (testName)
                    {
                        case "Base_Z_IB_IOP":
                            ok  = IndividualTestRun.RunBase_Z_IB_IOP(seq, testResults, bw, ti, 1, temp, out cancelled);
                            pts = testResults.Base_Z_IB_IOP_Results?.CH1_Current?.Count ?? 0;
                            break;
                        case "Base_Z_IPD":
                            ok  = IndividualTestRun.RunBase_Z_IPD(seq, testResults, bw, ti, 1, temp, out cancelled);
                            pts = testResults.Base_Z_IPD_Results?.CH3_Current?.Count ?? 0;
                            break;
                        case "Remote_Z_IOP":
                            ok  = IndividualTestRun.RunRemote_Z_IOP(seq, testResults, bw, ti, 1, temp, out cancelled);
                            pts = testResults.Remote_Z_IOP_Results?.CH1_Current?.Count ?? 0;
                            break;
                        case "Remote_Z_IPV":
                            ok  = IndividualTestRun.RunRemote_Z_IPV(seq, testResults, bw, ti, 1, temp, out cancelled);
                            pts = testResults.Remote_Z_IPV_Results?.CH3_Current?.Count ?? 0;
                            break;
                        case "Remote_Z_VPV":
                            ok  = IndividualTestRun.RunRemote_Z_VPV(seq, testResults, bw, ti, 1, temp, out cancelled);
                            pts = testResults.Remote_Z_VPV_Results?.CH3_Current?.Count ?? 0;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log($"  {testName}: EXCEPTION — {ex.Message}", Color.OrangeRed);
                }

                string status = cancelled ? "CANCELLED" : (ok ? "OK" : "FAILED");
                results.Add(new object[] { testName, seq.BaseDUTs[0].SerialNumber, pts, status });
                bw.ReportProgress(ti + 1, $"  {testName} complete — {pts} sweep points — {status}");

                if (cancelled) break;
            }

            e.Result = results;
        }

        private void BwSweeps_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            _btnRunSweeps.Enabled = true;
            _pbSweep.Value = _pbSweep.Maximum;

            if (e.Error != null) { LogErr(e.Error.Message); return; }

            var results = (List<object[]>)e.Result;
            _dgvSweepResults.Rows.Clear();
            foreach (var row in results)
            {
                int i = _dgvSweepResults.Rows.Add(row);
                string status = row[3].ToString();
                if (status == "OK")
                    _dgvSweepResults.Rows[i].DefaultCellStyle.ForeColor = Color.DarkGreen;
                else if (status == "FAILED")
                    _dgvSweepResults.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
            }
            LogOk($"SMU sweep run complete — {results.Count} test type(s) executed.");
        }

        // ── GOLD STANDARD event handlers ──────────────────────────────────────
        private void BtnLoadBaseline_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "CSV files|*.csv|All files|*.*", Title = "Load Baseline CSV" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _baselineFilePath   = ofd.FileName;
                    _lblBaselineFile.Text = Path.GetFileName(_baselineFilePath);
                    _lblBaselineFile.ForeColor = Color.DarkGreen;
                    LogInfo($"Baseline loaded: {_baselineFilePath}");
                }
            }
        }

        private void BtnRunCompare_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_baselineFilePath) || !File.Exists(_baselineFilePath))
            { LogErr("Load a baseline CSV file first."); return; }
            if (_bwGold.IsBusy) { LogErr("Comparison already running."); return; }

            _dgvGoldResults.Rows.Clear();
            _pbGold.Value = 0;
            _btnRunCompare.Enabled = false;

            var seq  = BuildDiagSequence();
            double tol = (double)_nudGoldTol.Value;
            _bwGold.RunWorkerAsync(new Tuple<TestSequenceModel, string, double>(seq, _baselineFilePath, tol));
        }

        private void BwGold_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw   = (BackgroundWorker)sender;
            var args = (Tuple<TestSequenceModel, string, double>)e.Argument;
            var seq  = args.Item1;
            string baselinePath = args.Item2;
            double tolPct = args.Item3;

            // Run Base_Z_IB_IOP sweep to get measured data
            bw.ReportProgress(0, "Gold standard: running Base_Z_IB_IOP sweep...");
            var testResults = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };
            bool cancelled = false;
            bool ok = IndividualTestRun.RunBase_Z_IB_IOP(seq, testResults, bw, 0, 1, 25.0, out cancelled);

            if (!ok || cancelled)
            {
                bw.ReportProgress(0, "Gold standard: sweep failed or cancelled — aborting comparison.");
                e.Result = new List<object[]>();
                return;
            }

            // Build measured CSV in memory (same format as TestResultSaver.WriteBase_Z_IB_IOP)
            var sb = new StringBuilder();
            sb.AppendLine("CH1 Voltage(V),CH1 Current(A),CH1 Time,CH2 Voltage(V),CH2 Current(A),CH2 Time,CH4 Voltage(V),CH4 Current(A),CH4 Time,CH4 Power(W)");
            var r = testResults.Base_Z_IB_IOP_Results;
            int count = r?.CH1_Current?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(
                    $"{SafeD(r.CH1_Voltage, i)},{SafeD(r.CH1_Current, i)},{SafeS(r.CH1_Time, i)}," +
                    $"{SafeD(r.CH2_Voltage, i)},{SafeD(r.CH2_Current, i)},{SafeS(r.CH2_Time, i)}," +
                    $"{SafeD(r.CH4_Voltage, i)},{SafeD(r.CH4_Current, i)},{SafeS(r.CH4_Time, i)},{SafeD(r.CH4_Power, i)}");
            }

            string tempPath = Path.Combine(Path.GetTempPath(), "DuplexerDiag_measured.csv");
            File.WriteAllText(tempPath, sb.ToString());
            bw.ReportProgress(50, $"Gold standard: {count} sweep points captured — comparing to baseline...");

            e.Result = CompareCSVFiles(baselinePath, tempPath, tolPct);
        }

        private List<object[]> CompareCSVFiles(string baselinePath, string measuredPath, double tolPct)
        {
            var rows = new List<object[]>();
            try
            {
                string[] bLines = File.ReadAllLines(baselinePath);
                string[] mLines = File.ReadAllLines(measuredPath);
                if (bLines.Length < 2 || mLines.Length < 2) return rows;

                string[] headers = bLines[0].Split(',');
                int dataRows = Math.Min(bLines.Length - 1, mLines.Length - 1);

                for (int row = 0; row < dataRows; row++)
                {
                    string[] bCols = bLines[row + 1].Split(',');
                    string[] mCols = mLines[row + 1].Split(',');
                    int colCount = Math.Min(bCols.Length, mCols.Length);

                    for (int col = 0; col < colCount; col++)
                    {
                        if (!double.TryParse(bCols[col].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double bv)) continue;
                        if (!double.TryParse(mCols[col].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double mv)) continue;
                        if (bv == 0 && mv == 0) continue;

                        double delta = bv != 0 ? Math.Abs((mv - bv) / bv) * 100.0 : (mv == 0 ? 0 : 100.0);
                        bool pass    = delta <= tolPct;
                        string colName = col < headers.Length ? headers[col].Trim() : $"Col{col + 1}";
                        rows.Add(new object[] {
                            row + 1,
                            colName,
                            bv.ToString("G6", CultureInfo.InvariantCulture),
                            mv.ToString("G6", CultureInfo.InvariantCulture),
                            $"{delta:F2}",
                            pass ? "✓ PASS" : "✗ FAIL"
                        });
                    }
                }
            }
            catch (Exception ex) { Log($"CompareCSVFiles error: {ex.Message}", Color.OrangeRed); }
            return rows;
        }

        private void BwGold_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            _btnRunCompare.Enabled = true;
            _pbGold.Value = 100;
            if (e.Error != null) { LogErr(e.Error.Message); return; }

            var rows = (List<object[]>)e.Result;
            int passCount = 0, failCount = 0;
            _dgvGoldResults.Rows.Clear();
            foreach (var row in rows)
            {
                int i   = _dgvGoldResults.Rows.Add(row);
                bool ok = row[5].ToString().StartsWith("✓");
                if (ok) passCount++;
                else  { failCount++; _dgvGoldResults.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose; }
            }
            LogOk($"Gold standard compare: {passCount} pass, {failCount} fail  ({rows.Count} numeric data points).");
        }

        // ── Small CSV helpers ─────────────────────────────────────────────────
        private static string SafeD(List<double> list, int i) =>
            (list != null && i < list.Count) ? list[i].ToString("G9", CultureInfo.InvariantCulture) : "0";

        private static string SafeS(List<string> list, int i) =>
            (list != null && i < list.Count) ? list[i] : "";
    }
}
