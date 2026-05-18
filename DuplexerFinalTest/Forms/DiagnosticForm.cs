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
        private ComboBox _cmbGoldSeq;
        private Button _btnRunGoldStd;
        private Label _lblGoldRunStatus;
        private ProgressBar _pbGold;
        private TextBox _txtOldResultsFolder;
        private NumericUpDown _nudGoldTol;
        private Button _btnCompareGold;
        private TextBox _txtReportFolder;
        private Button _btnSaveReport;
        private DataGridView _dgvGoldResults;
        private BackgroundWorker _bwGold;
        // last compare metadata — used by report generator
        private List<object[]> _lastCompareResults;
        private string _lastCompareOldFolder;
        private string _lastCompareSeqName;
        private double _lastCompareTol;
        private DateTime _lastCompareTime;

        // ─────────────────────────────────────────────────────────────────────
        public DiagnosticForm()
        {
            BuildUI();
            InitBackgroundWorkers();
            Load += (s, e) => RefreshGoldSequences();
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
            var grpDUT = new GroupBox { Text = "DUT Configuration (used for SMU sweeps)", Dock = DockStyle.Fill, Height = 108 };
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
            var tp  = new TabPage("  Gold Standard  ");
            var tlp = MakeTLP(4, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ── 1. Select sequence & run ────────────────────────────────────
            var grpRun = new GroupBox { Text = "1.  Select Sequence & Run", Dock = DockStyle.Fill, Height = 76 };
            var pnlRun = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(6) };

            var row1 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row1.Controls.Add(MakeLabel("Sequence:", 75));
            _cmbGoldSeq = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310, Margin = new Padding(0, 4, 12, 4) };
            row1.Controls.Add(_cmbGoldSeq);
            var btnRefreshSeq = new Button { Text = "⟳  Refresh Sequences", Width = 170, Height = 32, Margin = new Padding(4) };
            btnRefreshSeq.Click += (s, e) => RefreshGoldSequences();
            row1.Controls.Add(btnRefreshSeq);
            _btnRunGoldStd = new Button { Text = "▶  Run Full Gold Standard Test", Width = 255, Height = 32, Margin = new Padding(4) };
            _btnRunGoldStd.Click += BtnRunGoldStd_Click;
            row1.Controls.Add(_btnRunGoldStd);
            pnlRun.Controls.Add(row1);
            grpRun.Controls.Add(pnlRun);

            // ── Progress + status ────────────────────────────────────────────
            var pnlProg = new Panel { Dock = DockStyle.Fill, Height = 48, Padding = new Padding(10, 4, 10, 4) };
            _pbGold = new ProgressBar { Dock = DockStyle.Top, Height = 22, Minimum = 0, Maximum = 100 };
            _lblGoldRunStatus = new Label { Dock = DockStyle.Bottom, Text = "No run saved yet.", Height = 20, ForeColor = Color.DimGray };
            pnlProg.Controls.Add(_pbGold);
            pnlProg.Controls.Add(_lblGoldRunStatus);

            // ── 2. Compare with previous gold standard ──────────────────────
            var grpCmp = new GroupBox { Text = "2.  Compare with Previous Gold Standard (Old Test Kit)", Dock = DockStyle.Fill, AutoSize = true };
            var pnlCmp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(6) };

            var row2 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row2.Controls.Add(MakeLabel("Previous results folder:", 160));
            _txtOldResultsFolder = new TextBox { Width = 390, Margin = new Padding(0, 4, 8, 4) };
            row2.Controls.Add(_txtOldResultsFolder);
            var btnBrowse = new Button { Text = "Browse...", Width = 85, Height = 32, Margin = new Padding(4) };
            btnBrowse.Click += BtnBrowseOldResults_Click;
            row2.Controls.Add(btnBrowse);
            pnlCmp.Controls.Add(row2);

            var row3 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row3.Controls.Add(MakeLabel("Tolerance ±%:", 100));
            _nudGoldTol = new NumericUpDown { Minimum = 0.1m, Maximum = 50, DecimalPlaces = 1, Value = 5.0m, Width = 80, Margin = new Padding(0, 4, 16, 4) };
            row3.Controls.Add(_nudGoldTol);
            _btnCompareGold = new Button { Text = "Compare", Width = 120, Height = 32, Margin = new Padding(4) };
            _btnCompareGold.Click += BtnCompareGold_Click;
            row3.Controls.Add(_btnCompareGold);
            pnlCmp.Controls.Add(row3);

            var row4 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row4.Controls.Add(MakeLabel("Report folder:", 100));
            _txtReportFolder = new TextBox { Width = 390, Margin = new Padding(0, 4, 8, 4) };
            row4.Controls.Add(_txtReportFolder);
            var btnBrowseReport = new Button { Text = "Browse...", Width = 85, Height = 32, Margin = new Padding(4) };
            btnBrowseReport.Click += BtnBrowseReportFolder_Click;
            row4.Controls.Add(btnBrowseReport);
            _btnSaveReport = new Button { Text = "💾  Save Report", Width = 145, Height = 32, Margin = new Padding(4), Enabled = false };
            _btnSaveReport.Click += BtnSaveReport_Click;
            row4.Controls.Add(_btnSaveReport);
            pnlCmp.Controls.Add(row4);
            grpCmp.Controls.Add(pnlCmp);

            // ── Results grid ─────────────────────────────────────────────────
            _dgvGoldResults = MakeDGV();
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DUT Serial",    Name = "cGSerial", Width = 140 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Test Type",     Name = "cGTest",   Width = 155 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Points Cmprd", Name = "cGPts",    Width = 110 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max Δ%",       Name = "cGDelta",  Width = 90 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Worst Column", Name = "cGWorst",  Width = 155 });
            _dgvGoldResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pass",         Name = "cGPass",   AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            tlp.Controls.Add(grpRun,          0, 0);
            tlp.Controls.Add(pnlProg,         0, 1);
            tlp.Controls.Add(grpCmp,          0, 2);
            tlp.Controls.Add(_dgvGoldResults, 0, 3);
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

        // ── GOLD STANDARD helpers ─────────────────────────────────────────────
        private string GoldStandardsBasePath
        {
            get
            {
                var gs = Shared.sharedGeneralSettings?.GeneralSettings?[0];
                return gs != null ? Path.Combine(gs.RESULTS_FOLDER, "GoldStandards", "Base") : null;
            }
        }

        private string GoldStandardsRemotePath
        {
            get
            {
                var gs = Shared.sharedGeneralSettings?.GeneralSettings?[0];
                return gs != null ? Path.Combine(gs.RESULTS_FOLDER, "GoldStandards", "Remote") : null;
            }
        }

        private void RefreshGoldSequences()
        {
            _cmbGoldSeq.Items.Clear();
            foreach (var seq in Shared.AllAvailableTestSequences)
                _cmbGoldSeq.Items.Add(seq.SequenceName ?? "(unnamed)");
            if (_cmbGoldSeq.Items.Count > 0)
                _cmbGoldSeq.SelectedIndex = 0;
        }

        // ── GOLD STANDARD run ─────────────────────────────────────────────────
        private void BtnRunGoldStd_Click(object sender, EventArgs e)
        {
            if (_bwGold.IsBusy) { LogErr("Gold standard run already in progress."); return; }

            if (_cmbGoldSeq.SelectedIndex < 0 ||
                _cmbGoldSeq.SelectedIndex >= Shared.AllAvailableTestSequences.Count)
            { LogErr("Select a test sequence first."); return; }

            var seq = Shared.AllAvailableTestSequences[_cmbGoldSeq.SelectedIndex];
            if (seq.BaseDUTs.Count == 0 && seq.RemoteDUTs.Count == 0)
            { LogErr($"Sequence '{seq.SequenceName}' has no DUTs."); return; }

            string gsBase   = GoldStandardsBasePath;
            string gsRemote = GoldStandardsRemotePath;
            if (string.IsNullOrEmpty(gsBase))
            { LogErr("Results folder not configured in settings."); return; }

            try
            {
                if (seq.BaseDUTs.Count   > 0) Directory.CreateDirectory(gsBase);
                if (seq.RemoteDUTs.Count > 0) Directory.CreateDirectory(gsRemote);
            }
            catch (Exception ex) { LogErr($"Cannot create gold standard folders: {ex.Message}"); return; }

            _pbGold.Value               = 0;
            _lblGoldRunStatus.Text      = "Running...";
            _lblGoldRunStatus.ForeColor = Color.DimGray;
            _btnRunGoldStd.Enabled      = false;
            _dgvGoldResults.Rows.Clear();

            LogInfo($"Gold standard run started — sequence: {seq.SequenceName}");
            LogInfo($"  Base   → {gsBase}");
            LogInfo($"  Remote → {gsRemote}");

            _bwGold.RunWorkerAsync(
                new Tuple<TestSequenceModel, string, string>(seq, gsBase, gsRemote));
        }

        private void BwGold_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw     = (BackgroundWorker)sender;
            var args   = (Tuple<TestSequenceModel, string, string>)e.Argument;
            var seq    = args.Item1;
            string gsBase   = args.Item2;
            string gsRemote = args.Item3;
            const double temp = 25.0;

            int totalOps = seq.BaseDUTs.Count * 2 + seq.RemoteDUTs.Count * 3;
            if (totalOps == 0) totalOps = 1;
            int done = 0;

            string origBase   = Shared.BaseResultsPath;
            string origRemote = Shared.RemoteResultsPath;
            Shared.BaseResultsPath   = gsBase;
            Shared.RemoteResultsPath = gsRemote;

            try
            {
                for (int i = 0; i < seq.BaseDUTs.Count; i++)
                {
                    var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };

                    bw.ReportProgress(done * 100 / totalOps,
                        $"Base DUT {i + 1}/{seq.BaseDUTs.Count}  [{seq.BaseDUTs[i].SerialNumber}]: Base_Z_IB_IOP ...");
                    IndividualTestRun.RunBase_Z_IB_IOP(seq, tr, bw, i, 1, temp, out bool c1);
                    done++;
                    if (c1) { e.Result = null; return; }

                    bw.ReportProgress(done * 100 / totalOps,
                        $"Base DUT {i + 1}/{seq.BaseDUTs.Count}  [{seq.BaseDUTs[i].SerialNumber}]: Base_Z_IPD ...");
                    IndividualTestRun.RunBase_Z_IPD(seq, tr, bw, i, 1, temp, out bool c2);
                    done++;
                    if (c2) { e.Result = null; return; }
                }

                for (int i = 0; i < seq.RemoteDUTs.Count; i++)
                {
                    var tr = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };

                    bw.ReportProgress(done * 100 / totalOps,
                        $"Remote DUT {i + 1}/{seq.RemoteDUTs.Count}  [{seq.RemoteDUTs[i].SerialNumber}]: Remote_Z_IOP ...");
                    IndividualTestRun.RunRemote_Z_IOP(seq, tr, bw, i, 1, temp, out bool c3);
                    done++;
                    if (c3) { e.Result = null; return; }

                    bw.ReportProgress(done * 100 / totalOps,
                        $"Remote DUT {i + 1}/{seq.RemoteDUTs.Count}  [{seq.RemoteDUTs[i].SerialNumber}]: Remote_Z_IPV ...");
                    IndividualTestRun.RunRemote_Z_IPV(seq, tr, bw, i, 1, temp, out bool c4);
                    done++;
                    if (c4) { e.Result = null; return; }

                    bw.ReportProgress(done * 100 / totalOps,
                        $"Remote DUT {i + 1}/{seq.RemoteDUTs.Count}  [{seq.RemoteDUTs[i].SerialNumber}]: Remote_Z_VPV ...");
                    IndividualTestRun.RunRemote_Z_VPV(seq, tr, bw, i, 1, temp, out bool c5);
                    done++;
                    if (c5) { e.Result = null; return; }
                }
            }
            finally
            {
                Shared.BaseResultsPath   = origBase;
                Shared.RemoteResultsPath = origRemote;
            }

            e.Result = new Tuple<string, string>(gsBase, gsRemote);
        }

        private void BwGold_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            _btnRunGoldStd.Enabled = true;
            _pbGold.Value = 100;

            if (e.Error != null)
            {
                LogErr($"Gold standard run error: {e.Error.Message}");
                _lblGoldRunStatus.Text      = $"Error: {e.Error.Message}";
                _lblGoldRunStatus.ForeColor = Color.OrangeRed;
                return;
            }

            var paths = e.Result as Tuple<string, string>;
            if (paths == null)
            {
                LogInfo("Gold standard run cancelled.");
                _lblGoldRunStatus.Text      = "Cancelled.";
                _lblGoldRunStatus.ForeColor = Color.DimGray;
                return;
            }

            _lblGoldRunStatus.Text      = $"Saved  →  Base: {paths.Item1}   Remote: {paths.Item2}";
            _lblGoldRunStatus.ForeColor = Color.DarkGreen;
            LogOk("Gold standard run complete.");
            LogOk($"  Base results   → {paths.Item1}");
            LogOk($"  Remote results → {paths.Item2}");
        }

        // ── GOLD STANDARD comparison ──────────────────────────────────────────
        private void BtnBrowseOldResults_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog
            {
                Description = "Select the previous gold standard results folder (old test kit)"
            })
            {
                if (!string.IsNullOrEmpty(_txtOldResultsFolder.Text) &&
                    Directory.Exists(_txtOldResultsFolder.Text))
                    fbd.SelectedPath = _txtOldResultsFolder.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                    _txtOldResultsFolder.Text = fbd.SelectedPath;
            }
        }

        private void BtnCompareGold_Click(object sender, EventArgs e)
        {
            string oldFolder = _txtOldResultsFolder.Text.Trim();
            if (!Directory.Exists(oldFolder))
            { LogErr("Select a valid previous results folder first (Browse...)."); return; }

            string gsBase   = GoldStandardsBasePath;
            string gsRemote = GoldStandardsRemotePath;
            if (string.IsNullOrEmpty(gsBase))
            { LogErr("Results folder not configured in settings."); return; }

            bool hasBase   = Directory.Exists(gsBase)   && Directory.GetFiles(gsBase,   "*.csv").Length > 0;
            bool hasRemote = Directory.Exists(gsRemote) && Directory.GetFiles(gsRemote, "*.csv").Length > 0;
            if (!hasBase && !hasRemote)
            { LogErr("No gold standard results found in GoldStandards\\Base or Remote. Run the test first."); return; }

            double tol = (double)_nudGoldTol.Value;
            LogInfo($"Comparing new gold standard results vs: {oldFolder}  tolerance ±{tol:F1}%");

            var results = CompareGoldRuns(oldFolder, gsBase, gsRemote, tol);

            // Store metadata for report generation
            _lastCompareResults    = results;
            _lastCompareOldFolder  = oldFolder;
            _lastCompareSeqName    = _cmbGoldSeq.SelectedItem?.ToString() ?? "";
            _lastCompareTol        = tol;
            _lastCompareTime       = DateTime.Now;

            _dgvGoldResults.Rows.Clear();
            int pass = 0, fail = 0;
            foreach (var row in results)
            {
                int i   = _dgvGoldResults.Rows.Add(row);
                bool ok = row[5].ToString().StartsWith("✓");
                if (ok) pass++;
                else   { fail++; _dgvGoldResults.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose; }
            }
            LogOk($"Compare complete — {pass} pass, {fail} fail  ({results.Count} matched file pairs).");

            _btnSaveReport.Enabled = results.Count > 0;
        }

        private void BtnBrowseReportFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog
            {
                Description = "Select folder to save the Gold Standard comparison report"
            })
            {
                if (!string.IsNullOrEmpty(_txtReportFolder.Text) &&
                    Directory.Exists(_txtReportFolder.Text))
                    fbd.SelectedPath = _txtReportFolder.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                    _txtReportFolder.Text = fbd.SelectedPath;
            }
        }

        private void BtnSaveReport_Click(object sender, EventArgs e)
        {
            if (_lastCompareResults == null || _lastCompareResults.Count == 0)
            { LogErr("No comparison results to save. Run Compare first."); return; }

            string reportDir = _txtReportFolder.Text.Trim();
            if (string.IsNullOrEmpty(reportDir))
            {
                using (var fbd = new FolderBrowserDialog { Description = "Select folder to save the report" })
                {
                    if (fbd.ShowDialog() != DialogResult.OK) return;
                    reportDir = fbd.SelectedPath;
                    _txtReportFolder.Text = reportDir;
                }
            }

            if (!Directory.Exists(reportDir))
            {
                try { Directory.CreateDirectory(reportDir); }
                catch (Exception ex) { LogErr($"Cannot create report folder: {ex.Message}"); return; }
            }

            string stamp    = _lastCompareTime.ToString("yyyyMMdd_HHmmss");
            string baseName = $"GoldStd_Report_{stamp}";

            try
            {
                string htmlPath = Path.Combine(reportDir, baseName + ".html");
                string csvPath  = Path.Combine(reportDir, baseName + ".csv");

                File.WriteAllText(htmlPath, BuildGoldReportHtml(), System.Text.Encoding.UTF8);
                File.WriteAllText(csvPath,  BuildGoldReportCsv(),  System.Text.Encoding.UTF8);

                LogOk($"Report saved → {htmlPath}");
                LogOk($"CSV saved    → {csvPath}");
            }
            catch (Exception ex) { LogErr($"Failed to save report: {ex.Message}"); }
        }

        private string BuildGoldReportHtml()
        {
            string gsBase   = GoldStandardsBasePath   ?? "(not configured)";
            string gsRemote = GoldStandardsRemotePath ?? "(not configured)";

            int total = _lastCompareResults.Count;
            int pass  = 0; int fail = 0;
            double maxAll = 0; double sumMax = 0;
            foreach (var r in _lastCompareResults)
            {
                bool ok = r[5].ToString().StartsWith("✓");
                if (ok) pass++; else fail++;
                if (double.TryParse(r[3].ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    sumMax += d;
                    if (d > maxAll) maxAll = d;
                }
            }
            double avgMax = total > 0 ? sumMax / total : 0;
            bool overall  = fail == 0;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            sb.AppendLine("<title>Gold Standard Comparison Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Arial,sans-serif;font-size:11pt;margin:36px;color:#222}");
            sb.AppendLine("h1{color:#003366;margin-bottom:4px}");
            sb.AppendLine(".meta{font-size:10pt;color:#555;margin-bottom:16px}");
            sb.AppendLine(".info-table{border-collapse:collapse;margin-bottom:18px}");
            sb.AppendLine(".info-table td{padding:3px 14px 3px 0;vertical-align:top}");
            sb.AppendLine(".info-table td:first-child{font-weight:bold;white-space:nowrap;color:#003366}");
            sb.AppendLine(".summary{background:#f0f4fa;border-left:5px solid #003366;padding:12px 18px;margin-bottom:22px;border-radius:3px}");
            sb.AppendLine(".summary b{color:#003366}");
            sb.AppendLine(".overall-pass{color:#155724;font-weight:bold;font-size:13pt}");
            sb.AppendLine(".overall-fail{color:#721c24;font-weight:bold;font-size:13pt}");
            sb.AppendLine("table.results{border-collapse:collapse;width:100%}");
            sb.AppendLine("table.results th{background:#003366;color:#fff;padding:7px 12px;text-align:left;font-size:10pt}");
            sb.AppendLine("table.results td{border:1px solid #ccc;padding:5px 12px;font-size:10pt}");
            sb.AppendLine("table.results tr:nth-child(even){background:#f9f9f9}");
            sb.AppendLine(".pass-cell{background:#d4edda;color:#155724;font-weight:bold;text-align:center}");
            sb.AppendLine(".fail-cell{background:#f8d7da;color:#721c24;font-weight:bold;text-align:center}");
            sb.AppendLine(".footer{margin-top:32px;font-size:9pt;color:#aaa}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h1>Gold Standard Comparison Report</h1>");
            sb.AppendLine($"<div class=\"meta\">Generated: {_lastCompareTime:yyyy-MM-dd  HH:mm:ss}</div>");

            sb.AppendLine("<table class=\"info-table\">");
            sb.AppendLine($"<tr><td>Sequence</td><td>{HtmlEnc(_lastCompareSeqName)}</td></tr>");
            sb.AppendLine($"<tr><td>New results — Base</td><td>{HtmlEnc(gsBase)}</td></tr>");
            sb.AppendLine($"<tr><td>New results — Remote</td><td>{HtmlEnc(gsRemote)}</td></tr>");
            sb.AppendLine($"<tr><td>Reference (old test kit)</td><td>{HtmlEnc(_lastCompareOldFolder)}</td></tr>");
            sb.AppendLine($"<tr><td>Tolerance</td><td>±{_lastCompareTol:F1}%</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<div class=\"summary\">");
            sb.AppendLine($"<b>Pairs compared:</b> {total} &nbsp;|&nbsp; <b>Passed:</b> {pass} &nbsp;|&nbsp; <b>Failed:</b> {fail}<br/>");
            sb.AppendLine($"<b>Overall Max Δ%:</b> {maxAll:F2}% &nbsp;|&nbsp; <b>Average Max Δ%:</b> {avgMax:F2}%<br/><br/>");
            sb.AppendLine(overall
                ? $"<span class=\"overall-pass\">&#10003; OVERALL RESULT: PASS</span>"
                : $"<span class=\"overall-fail\">&#10007; OVERALL RESULT: FAIL  ({fail} pair(s) exceeded ±{_lastCompareTol:F1}%)</span>");
            sb.AppendLine("</div>");

            sb.AppendLine("<table class=\"results\">");
            sb.AppendLine("<tr><th>DUT Serial</th><th>Test Type</th><th>Points Compared</th><th>Max Δ%</th><th>Worst Column</th><th>Result</th></tr>");
            foreach (var r in _lastCompareResults)
            {
                bool ok   = r[5].ToString().StartsWith("✓");
                string cls = ok ? "pass-cell" : "fail-cell";
                sb.AppendLine("<tr>");
                for (int i = 0; i < 5; i++)
                    sb.AppendLine($"<td>{HtmlEnc(r[i]?.ToString())}</td>");
                sb.AppendLine($"<td class=\"{cls}\">{HtmlEnc(r[5]?.ToString())}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine($"<div class=\"footer\">Generated by DuplexerFinalTest v{Shared.SoftwareVersion}</div>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string BuildGoldReportCsv()
        {
            string gsBase   = GoldStandardsBasePath   ?? "";
            string gsRemote = GoldStandardsRemotePath ?? "";

            var sb = new System.Text.StringBuilder();
            // metadata header
            sb.AppendLine($"Gold Standard Comparison Report");
            sb.AppendLine($"Generated,{_lastCompareTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Sequence,{_lastCompareSeqName}");
            sb.AppendLine($"New Base Folder,{gsBase}");
            sb.AppendLine($"New Remote Folder,{gsRemote}");
            sb.AppendLine($"Reference Folder,{_lastCompareOldFolder}");
            sb.AppendLine($"Tolerance ±%,{_lastCompareTol:F1}");
            sb.AppendLine();

            int pass = 0; int fail = 0;
            double maxAll = 0; double sumMax = 0;
            foreach (var r in _lastCompareResults)
            {
                bool ok = r[5].ToString().StartsWith("✓");
                if (ok) pass++; else fail++;
                if (double.TryParse(r[3].ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
                { sumMax += d; if (d > maxAll) maxAll = d; }
            }
            int total   = _lastCompareResults.Count;
            double avg  = total > 0 ? sumMax / total : 0;
            string ovr  = fail == 0 ? "PASS" : "FAIL";
            sb.AppendLine($"Total Pairs,{total},Passed,{pass},Failed,{fail}");
            sb.AppendLine($"Overall Max Delta%,{maxAll:F2},Avg Max Delta%,{avg:F2},Overall Result,{ovr}");
            sb.AppendLine();

            sb.AppendLine("DUT Serial,Test Type,Points Compared,Max Delta%,Worst Column,Result");
            foreach (var r in _lastCompareResults)
                sb.AppendLine($"{CsvEsc(r[0])},{CsvEsc(r[1])},{r[2]},{r[3]},{CsvEsc(r[4])},{CsvEsc(r[5])}");

            return sb.ToString();
        }

        private static string CsvEsc(object val)
        {
            string s = val?.ToString() ?? "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string HtmlEnc(object val)
        {
            if (val == null) return "";
            return val.ToString()
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static (string serial, string testType) ParseGoldFileName(string name)
        {
            string[] markers = { "_Base_Z_IB_IOP_", "_Base_Z_IPD_", "_Remote_Z_IOP_", "_Remote_Z_IPV_", "_Remote_Z_VPV_" };
            foreach (string m in markers)
            {
                int idx = name.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return (name.Substring(0, idx), m.Trim('_'));
            }
            return (name, "Unknown");
        }

        private List<object[]> CompareGoldRuns(
            string oldFolder, string newBaseFolder, string newRemoteFolder, double tolPct)
        {
            var results = new List<object[]>();
            try
            {
                // Collect new result files from GoldStandards\Base and GoldStandards\Remote
                var newFiles = new List<string>();
                if (Directory.Exists(newBaseFolder))   newFiles.AddRange(Directory.GetFiles(newBaseFolder,   "*.csv"));
                if (Directory.Exists(newRemoteFolder)) newFiles.AddRange(Directory.GetFiles(newRemoteFolder, "*.csv"));

                // Index old files by (serial|testType), searching recursively
                var indexOld = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string f in Directory.GetFiles(oldFolder, "*.csv", SearchOption.AllDirectories))
                {
                    var (s, t) = ParseGoldFileName(Path.GetFileNameWithoutExtension(f));
                    string key = s + "|" + t;
                    if (!indexOld.ContainsKey(key)) indexOld[key] = f;
                }

                foreach (string fNew in newFiles)
                {
                    var (serial, testType) = ParseGoldFileName(Path.GetFileNameWithoutExtension(fNew));
                    string key = serial + "|" + testType;
                    if (!indexOld.TryGetValue(key, out string fOld))
                    {
                        LogInfo($"  No match in old results for: {serial} / {testType}");
                        continue;
                    }

                    var (maxDelta, worstCol, count, pass) = CompareCSVSummary(fOld, fNew, tolPct);
                    results.Add(new object[] { serial, testType, count, $"{maxDelta:F2}", worstCol,
                        pass ? "✓ PASS" : "✗ FAIL" });
                }
            }
            catch (Exception ex) { LogErr($"CompareGoldRuns error: {ex.Message}"); }
            return results;
        }

        private (double maxDelta, string worstCol, int count, bool pass) CompareCSVSummary(
            string pathA, string pathB, double tolPct)
        {
            double maxDelta = 0;
            string worstCol = "—";
            int    count    = 0;

            string[] linesA = File.ReadAllLines(pathA);
            string[] linesB = File.ReadAllLines(pathB);
            if (linesA.Length < 2 || linesB.Length < 2) return (0, "—", 0, true);

            string[] headers = linesA[0].Split(',');
            int dataRows = Math.Min(linesA.Length - 1, linesB.Length - 1);

            for (int row = 0; row < dataRows; row++)
            {
                string[] colsA = linesA[row + 1].Split(',');
                string[] colsB = linesB[row + 1].Split(',');
                int cols = Math.Min(colsA.Length, colsB.Length);
                for (int col = 0; col < cols; col++)
                {
                    if (!double.TryParse(colsA[col].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double va)) continue;
                    if (!double.TryParse(colsB[col].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double vb)) continue;
                    if (va == 0 && vb == 0) continue;
                    count++;
                    double delta = va != 0 ? Math.Abs((vb - va) / va) * 100.0 : (vb == 0 ? 0 : 100.0);
                    if (delta > maxDelta)
                    {
                        maxDelta = delta;
                        worstCol = col < headers.Length ? headers[col].Trim() : $"Col{col + 1}";
                    }
                }
            }
            return (maxDelta, worstCol, count, maxDelta <= tolPct);
        }
    }
}
