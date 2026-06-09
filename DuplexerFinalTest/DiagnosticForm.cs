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

        // ── Tab 7: Gauge R&R ────────────────────────────────────────────────
        private ComboBox _cmbGrrSequence;
        private Label _lblGrrSequenceSummary;
        private NumericUpDown _nudGrrBaseCount;
        private NumericUpDown _nudGrrRemoteCount;
        private TableLayoutPanel _tlpGrrDuts;
        private Label _lblGrrDutSummary;
        private NumericUpDown _nudGrrOperators;
        private FlowLayoutPanel _pnlGrrOperators;
        private NumericUpDown _nudGrrReplicates;
        private TextBox _txtGrrOutputFolder;
        private CheckBox _chkGrrIncludeBase;
        private ComboBox _cmbGrrBaseTest;
        private ComboBox _cmbGrrBaseColumn;
        private NumericUpDown _nudGrrBaseTolerance;
        private CheckBox _chkGrrIncludeRemote;
        private ComboBox _cmbGrrRemoteTest;
        private ComboBox _cmbGrrRemoteColumn;
        private NumericUpDown _nudGrrRemoteTolerance;
        private Label _lblGrrStepTitle;
        private Label _lblGrrPrompt;
        private Label _lblGrrRunStatus;
        private Panel _pnlGrrSetup;
        private Panel _pnlGrrReview;
        private Panel _pnlGrrRun;
        private Panel _pnlGrrResults;
        private RichTextBox _rtbGrrReview;
        private Button _btnGrrRunCurrentStep;
        private Button _btnGrrPrevious;
        private Button _btnGrrStart;
        private Button _btnGrrNext;
        private ProgressBar _pbGrr;
        private Label _lblGrrResultsStatus;
        private DataGridView _dgvGrrSchedule;
        private DataGridView _dgvGrrSummary;
        private BackgroundWorker _bwGrr;

        private enum GrrWizardPage
        {
            Setup = 0,
            Review = 1,
            Run = 2,
            Results = 3
        }

        private GrrWizardPage _grrPage = GrrWizardPage.Setup;
        private GaugeRrStudyDefinition _grrStudy;
        private List<GaugeRrGroupResult> _grrResults = new List<GaugeRrGroupResult>();
        private readonly List<DUTModel> _grrDuts = new List<DUTModel>();
        private int _grrCurrentStepIndex = -1;
        private string _grrReportHtmlPath;
        private string _grrReportCsvPath;

        private class GaugeRrStudyDefinition
        {
            public string StudyName { get; set; }
            public DateTime CreatedAt { get; set; }
            public string SequenceName { get; set; }
            public TestSequenceModel Sequence { get; set; }
            public int Replicates { get; set; }
            public List<string> Operators { get; set; } = new List<string>();
            public List<GaugeRrMeasurePlan> Plans { get; set; } = new List<GaugeRrMeasurePlan>();
            public List<GaugeRrRunStep> Steps { get; set; } = new List<GaugeRrRunStep>();
            public List<GaugeRrObservation> Observations { get; set; } = new List<GaugeRrObservation>();
            public string OutputFolder { get; set; }
            public string DataFolderRoot { get; set; }
        }

        private class GaugeRrMeasurePlan
        {
            public string GroupName { get; set; }
            public DUTType DutType { get; set; }
            public TestSequences TestType { get; set; }
            public string ColumnName { get; set; }
            public double Tolerance { get; set; }
            public List<DUTModel> Parts { get; set; } = new List<DUTModel>();
        }

        private class GaugeRrRunStep
        {
            public int Index { get; set; }
            public int OperatorIndex { get; set; }
            public string OperatorName { get; set; }
            public int ReplicateNo { get; set; }
            public string FolderPath { get; set; }
            public string Status { get; set; } = "Pending";
            public string Notes { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public bool Completed { get; set; }
        }

        private class GaugeRrObservation
        {
            public string GroupName { get; set; }
            public TestSequences TestType { get; set; }
            public string ColumnName { get; set; }
            public string OperatorName { get; set; }
            public int ReplicateNo { get; set; }
            public string PartSerial { get; set; }
            public double Value { get; set; }
            public string CsvPath { get; set; }
        }

        private class GaugeRrStepResult
        {
            public GaugeRrRunStep Step { get; set; }
            public List<GaugeRrObservation> Observations { get; set; } = new List<GaugeRrObservation>();
            public string Summary { get; set; }
        }

        private class GaugeRrGroupResult
        {
            public GaugeRrMeasurePlan Plan { get; set; }
            public List<GaugeRrObservation> Observations { get; set; } = new List<GaugeRrObservation>();
            public GaugeRrMethodResult Anova { get; set; }
            public GaugeRrMethodResult Range { get; set; }
            public List<GaugeRrControlChart> ControlCharts { get; set; } = new List<GaugeRrControlChart>();
        }

        private class GaugeRrMethodResult
        {
            public string MethodName { get; set; }
            public double EVVariance { get; set; }
            public double AVVariance { get; set; }
            public double InteractionVariance { get; set; }
            public double ReproducibilityVariance { get; set; }
            public double PartVariance { get; set; }
            public double GrrVariance { get; set; }
            public double TotalVariance { get; set; }
            public double EVPctStudyVar { get; set; }
            public double AVPctStudyVar { get; set; }
            public double ReproPctStudyVar { get; set; }
            public double PartPctStudyVar { get; set; }
            public double GrrPctStudyVar { get; set; }
            public double EVPctTolerance { get; set; }
            public double AVPctTolerance { get; set; }
            public double ReproPctTolerance { get; set; }
            public double PartPctTolerance { get; set; }
            public double GrrPctTolerance { get; set; }
            public double Ndc { get; set; }
            public string Verdict { get; set; }
            public string Notes { get; set; }
            public List<GaugeRrComponentRow> Components { get; set; } = new List<GaugeRrComponentRow>();
        }

        private class GaugeRrComponentRow
        {
            public string Name { get; set; }
            public double Variance { get; set; }
            public double StandardDeviation { get; set; }
            public double StudyVariation { get; set; }
            public double PercentStudyVariation { get; set; }
            public double PercentTolerance { get; set; }
        }

        private class GaugeRrControlChart
        {
            public string OperatorName { get; set; }
            public double XBarCenter { get; set; }
            public double XBarUcl { get; set; }
            public double XBarLcl { get; set; }
            public double RCenter { get; set; }
            public double RUcl { get; set; }
            public double RLcl { get; set; }
            public List<GaugeRrControlPoint> Points { get; set; } = new List<GaugeRrControlPoint>();
        }

        private class GaugeRrControlPoint
        {
            public string PartSerial { get; set; }
            public double MeanValue { get; set; }
            public double RangeValue { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        public DiagnosticForm()
        {
            BuildUI();
            InitBackgroundWorkers();
            Load += (s, e) =>
            {
                RefreshGoldSequences();
                RefreshGrrSequences();
                UpdateGrrOperatorInputs();
                UpdateGrrWizardUi();
            };
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

            // Tab control (fills remaining space above log)
            _tabMain = new TabControl { Dock = DockStyle.Fill };
            _tabMain.TabPages.Add(BuildChamberTab());
            _tabMain.TabPages.Add(BuildDUTTempTab());
            _tabMain.TabPages.Add(BuildElecSwitchTab());
            _tabMain.TabPages.Add(BuildOptSwitchTab());
            _tabMain.TabPages.Add(BuildSMUSweepTab());
            _tabMain.TabPages.Add(BuildGoldStandardTab());
            _tabMain.TabPages.Add(BuildGaugeRrTab());
            Controls.Add(_tabMain);
            Controls.Add(pnlLog);
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

        // ── Tab 7: Gauge R&R ────────────────────────────────────────────────
        private TabPage BuildGaugeRrTab()
        {
            var tp = new TabPage("  Gauge R&&R  ");
            var tlp = MakeTLP(3, 1);
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var pnlHeader = new Panel { Dock = DockStyle.Fill, Height = 74, Padding = new Padding(8, 6, 8, 6) };
            _lblGrrStepTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                Text = "Step 1 of 4 — Study Setup"
            };
            _lblGrrPrompt = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Text = "Configure the sequence definition, scan the DUT barcodes for this study, then use Next / Previous to walk the operators through the Gauge R&R run."
            };
            pnlHeader.Controls.Add(_lblGrrPrompt);
            pnlHeader.Controls.Add(_lblGrrStepTitle);

            var pnlHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };

            _pnlGrrSetup = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _pnlGrrReview = new Panel { Dock = DockStyle.Fill, Visible = false };
            _pnlGrrRun = new Panel { Dock = DockStyle.Fill, Visible = false };
            _pnlGrrResults = new Panel { Dock = DockStyle.Fill, Visible = false };

            var setupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                AutoSize = false
            };
            setupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53f));
            setupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47f));
            setupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var leftSetupLayout = MakeTLP(6, 1);
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftSetupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var grpStudy = new GroupBox { Text = "1. Sequence Definition", Dock = DockStyle.Fill, Height = 110 };
            var pnlStudy = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = true };
            pnlStudy.Controls.Add(MakeLabel("Sequence:", 70));
            _cmbGrrSequence = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, Margin = new Padding(0, 4, 16, 4) };
            _cmbGrrSequence.SelectedIndexChanged += (s, e) => RefreshGrrSequenceSelection();
            pnlStudy.Controls.Add(_cmbGrrSequence);
            var btnRefreshSeq = new Button { Text = "⟳  Refresh Sequences", Width = 160, Height = 32, Margin = new Padding(4) };
            btnRefreshSeq.Click += (s, e) => RefreshGrrSequences();
            pnlStudy.Controls.Add(btnRefreshSeq);
            pnlStudy.Controls.Add(MakeLabel("Operators:", 70));
            _nudGrrOperators = new NumericUpDown { Minimum = 2, Maximum = 8, Value = 3, Width = 60, Margin = new Padding(0, 4, 16, 4) };
            _nudGrrOperators.ValueChanged += (s, e) => UpdateGrrOperatorInputs();
            pnlStudy.Controls.Add(_nudGrrOperators);
            pnlStudy.Controls.Add(MakeLabel("Replicates:", 75));
            _nudGrrReplicates = new NumericUpDown { Minimum = 2, Maximum = 6, Value = 2, Width = 60, Margin = new Padding(0, 4, 16, 4) };
            pnlStudy.Controls.Add(_nudGrrReplicates);
            _lblGrrSequenceSummary = new Label
            {
                AutoSize = false,
                Width = 520,
                Height = 52,
                Margin = new Padding(8, 8, 4, 4),
                ForeColor = Color.DimGray,
                Text = "Select a sequence definition. DUT serial numbers are scanned separately below for this Gauge R&R study."
            };
            pnlStudy.Controls.Add(_lblGrrSequenceSummary);
            grpStudy.Controls.Add(pnlStudy);

            var grpDuts = new GroupBox { Text = "2. Study Parts / Barcode Scan", Dock = DockStyle.Fill };
            var dutLayout = MakeTLP(3, 1);
            dutLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dutLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dutLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlDutConfig = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = true };
            pnlDutConfig.Controls.Add(MakeLabel("Base parts:", 78));
            _nudGrrBaseCount = new NumericUpDown { Minimum = 0, Maximum = 12, Value = 1, Width = 60, Margin = new Padding(0, 4, 16, 4) };
            _nudGrrBaseCount.ValueChanged += GrrDutCount_ValueChanged;
            pnlDutConfig.Controls.Add(_nudGrrBaseCount);
            pnlDutConfig.Controls.Add(MakeLabel("Remote parts:", 92));
            _nudGrrRemoteCount = new NumericUpDown { Minimum = 0, Maximum = 12, Value = 1, Width = 60, Margin = new Padding(0, 4, 16, 4) };
            _nudGrrRemoteCount.ValueChanged += GrrDutCount_ValueChanged;
            pnlDutConfig.Controls.Add(_nudGrrRemoteCount);
            _lblGrrDutSummary = new Label
            {
                AutoSize = false,
                Width = 460,
                Height = 24,
                Margin = new Padding(8, 8, 4, 4),
                ForeColor = Color.DimGray,
                Text = "Scan or type the DUT barcodes for this Gauge R&R study."
            };
            pnlDutConfig.Controls.Add(_lblGrrDutSummary);

            var lblDutNotes = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 10, 6),
                ForeColor = Color.DimGray,
                Text = "Scan or type each DUT barcode into the table below. Slot numbers are assigned by the table row. Duplicate serial numbers are highlighted in pink. These DUTs belong only to the Gauge R&R study and are not read from the sequence file."
            };

            var pnlDutTableHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(6, 0, 6, 6) };
            _tlpGrrDuts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pnlDutTableHost.Controls.Add(_tlpGrrDuts);
            pnlDutTableHost.Resize += (s, e) => ResizeGrrDutTable();

            dutLayout.Controls.Add(pnlDutConfig, 0, 0);
            dutLayout.Controls.Add(lblDutNotes, 0, 1);
            dutLayout.Controls.Add(pnlDutTableHost, 0, 2);
            grpDuts.Controls.Add(dutLayout);

            var grpOperators = new GroupBox { Text = "3. Operators", Dock = DockStyle.Fill, Height = 78 };
            _pnlGrrOperators = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), AutoScroll = true, WrapContents = true };
            grpOperators.Controls.Add(_pnlGrrOperators);

            var grpMeasure = new GroupBox { Text = "4. Measurement Plans", Dock = DockStyle.Fill, Height = 138 };
            var pnlMeasure = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), FlowDirection = FlowDirection.TopDown, WrapContents = false };

            var rowBase = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            _chkGrrIncludeBase = new CheckBox { Text = "Include Base DUTs", AutoSize = true, Checked = true, Margin = new Padding(4, 8, 12, 4) };
            _chkGrrIncludeBase.CheckedChanged += (s, e) => UpdateGrrMeasureOptions();
            rowBase.Controls.Add(_chkGrrIncludeBase);
            rowBase.Controls.Add(MakeLabel("Test:", 42));
            _cmbGrrBaseTest = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Margin = new Padding(0, 4, 12, 4) };
            _cmbGrrBaseTest.SelectedIndexChanged += (s, e) => PopulateGrrColumnCombo(_cmbGrrBaseColumn, SelectedGrrTestType(_cmbGrrBaseTest));
            rowBase.Controls.Add(_cmbGrrBaseTest);
            rowBase.Controls.Add(MakeLabel("Column:", 55));
            _cmbGrrBaseColumn = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(0, 4, 12, 4) };
            rowBase.Controls.Add(_cmbGrrBaseColumn);
            rowBase.Controls.Add(MakeLabel("Tolerance:", 62));
            _nudGrrBaseTolerance = new NumericUpDown { Minimum = 0.0001m, Maximum = 1000000m, DecimalPlaces = 4, Value = 1.0000m, Width = 96, Margin = new Padding(0, 4, 4, 4) };
            rowBase.Controls.Add(_nudGrrBaseTolerance);
            pnlMeasure.Controls.Add(rowBase);

            var rowRemote = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            _chkGrrIncludeRemote = new CheckBox { Text = "Include Remote DUTs", AutoSize = true, Checked = true, Margin = new Padding(4, 8, 4, 4) };
            _chkGrrIncludeRemote.CheckedChanged += (s, e) => UpdateGrrMeasureOptions();
            rowRemote.Controls.Add(_chkGrrIncludeRemote);
            rowRemote.Controls.Add(MakeLabel("Test:", 42));
            _cmbGrrRemoteTest = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Margin = new Padding(0, 4, 12, 4) };
            _cmbGrrRemoteTest.SelectedIndexChanged += (s, e) => PopulateGrrColumnCombo(_cmbGrrRemoteColumn, SelectedGrrTestType(_cmbGrrRemoteTest));
            rowRemote.Controls.Add(_cmbGrrRemoteTest);
            rowRemote.Controls.Add(MakeLabel("Column:", 55));
            _cmbGrrRemoteColumn = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(0, 4, 12, 4) };
            rowRemote.Controls.Add(_cmbGrrRemoteColumn);
            rowRemote.Controls.Add(MakeLabel("Tolerance:", 62));
            _nudGrrRemoteTolerance = new NumericUpDown { Minimum = 0.0001m, Maximum = 1000000m, DecimalPlaces = 4, Value = 1.0000m, Width = 96, Margin = new Padding(0, 4, 4, 4) };
            rowRemote.Controls.Add(_nudGrrRemoteTolerance);
            pnlMeasure.Controls.Add(rowRemote);
            grpMeasure.Controls.Add(pnlMeasure);

            var grpOutput = new GroupBox { Text = "5. Output", Dock = DockStyle.Fill, Height = 74 };
            var pnlOutput = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = false };
            pnlOutput.Controls.Add(MakeLabel("Study/report folder:", 120));
            _txtGrrOutputFolder = new TextBox { Width = 440, Margin = new Padding(0, 4, 8, 4) };
            pnlOutput.Controls.Add(_txtGrrOutputFolder);
            var btnBrowseOutput = new Button { Text = "Browse...", Width = 85, Height = 32, Margin = new Padding(4) };
            btnBrowseOutput.Click += BtnGrrBrowseOutput_Click;
            pnlOutput.Controls.Add(btnBrowseOutput);
            grpOutput.Controls.Add(pnlOutput);

            var grpActions = new GroupBox { Text = "6. Actions", Dock = DockStyle.Fill, Height = 78 };
            var pnlActions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), WrapContents = false };
            _btnGrrStart = new Button { Text = "▶  Start Gauge R&&R Study", Width = 210, Height = 34, Margin = new Padding(4) };
            _btnGrrStart.Click += BtnGrrStart_Click;
            pnlActions.Controls.Add(_btnGrrStart);
            pnlActions.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(10, 12, 4, 4),
                Text = "Build the schedule and continue to the review step."
            });
            grpActions.Controls.Add(pnlActions);

            var grpSetupNotes = new GroupBox { Text = "7. Study Notes", Dock = DockStyle.Fill };
            var lblSetupNotes = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Text = "This wizard runs the configured test type for every scanned DUT in this Gauge R&R setup, once per operator per replicate. " +
                       "The selected sequence provides the test definition and equipment flow; the scanned barcodes define the actual study parts."
            };
            grpSetupNotes.Controls.Add(lblSetupNotes);

            leftSetupLayout.Controls.Add(grpStudy, 0, 0);
            leftSetupLayout.Controls.Add(grpOperators, 0, 1);
            leftSetupLayout.Controls.Add(grpMeasure, 0, 2);
            leftSetupLayout.Controls.Add(grpOutput, 0, 3);
            leftSetupLayout.Controls.Add(grpActions, 0, 4);
            leftSetupLayout.Controls.Add(grpSetupNotes, 0, 5);

            setupLayout.Controls.Add(leftSetupLayout, 0, 0);
            setupLayout.Controls.Add(grpDuts, 1, 0);
            _pnlGrrSetup.Controls.Add(setupLayout);

            GenerateGrrDutTable((int)_nudGrrBaseCount.Value, (int)_nudGrrRemoteCount.Value);

            var reviewLayout = MakeTLP(1, 1);
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _rtbGrrReview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9.0f) };
            reviewLayout.Controls.Add(_rtbGrrReview, 0, 0);
            _pnlGrrReview.Controls.Add(reviewLayout);

            var runLayout = MakeTLP(3, 1);
            runLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            runLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            runLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            var grpRunPrompt = new GroupBox { Text = "Current Run Step", Dock = DockStyle.Fill, Height = 140 };
            var pnlRunPrompt = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            _lblGrrRunStatus = new Label { AutoSize = true, Width = 860, Text = "No step has been started yet.", ForeColor = Color.DimGray };
            _btnGrrRunCurrentStep = new Button { Text = "▶  Run Current Step", Width = 180, Height = 34, Margin = new Padding(4, 8, 4, 4) };
            _btnGrrRunCurrentStep.Click += BtnGrrRunCurrentStep_Click;
            _pbGrr = new ProgressBar { Width = 860, Height = 22, Minimum = 0, Maximum = 100, Margin = new Padding(4, 8, 4, 4) };
            pnlRunPrompt.Controls.Add(_lblGrrRunStatus);
            pnlRunPrompt.Controls.Add(_btnGrrRunCurrentStep);
            pnlRunPrompt.Controls.Add(_pbGrr);
            grpRunPrompt.Controls.Add(pnlRunPrompt);
            _dgvGrrSchedule = MakeDGV();
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Operator", Name = "cGrrOper", Width = 120 });
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Replicate", Name = "cGrrRep", Width = 90 });
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Name = "cGrrStatus", Width = 140 });
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Started", Name = "cGrrStarted", Width = 140 });
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Completed", Name = "cGrrDone", Width = 140 });
            _dgvGrrSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Notes", Name = "cGrrNotes", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            runLayout.Controls.Add(grpRunPrompt, 0, 0);
            runLayout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Use Previous / Next to move between operator-replicate steps. Each step will prompt the technician what to do next before the automated run starts.",
                Height = 44,
                ForeColor = Color.DimGray,
                Padding = new Padding(4, 0, 0, 4)
            }, 0, 1);
            // Reduce the grid height slightly so the descriptive label above has more vertical room
            _dgvGrrSchedule.Dock = DockStyle.Top;
            _dgvGrrSchedule.Height = 300; // slightly smaller than full-fill
            runLayout.Controls.Add(_dgvGrrSchedule, 0, 2);
            _pnlGrrRun.Controls.Add(runLayout);

            var resultsLayout = MakeTLP(2, 1);
            resultsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _lblGrrResultsStatus = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 140,
                ForeColor = Color.DimGray,
                Padding = new Padding(8, 8, 8, 8),
                TextAlign = ContentAlignment.TopLeft,
                Text = "When all study steps are complete, the wizard will generate a full Gauge R&R report with ANOVA and range-method sections, variance components, %tolerance, ndc, and control charts."
            };
            _dgvGrrSummary = MakeDGV();
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Group", Name = "cGrrGroup", Width = 100 });
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Method", Name = "cGrrMethod", Width = 110 });
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "%GRR", Name = "cGrrPct", Width = 85 });
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "%Tol", Name = "cGrrTol", Width = 85 });
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ndc", Name = "cGrrNdc", Width = 70 });
            _dgvGrrSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Result", Name = "cGrrResult", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            resultsLayout.Controls.Add(_lblGrrResultsStatus, 0, 0);
            resultsLayout.Controls.Add(_dgvGrrSummary, 0, 1);
            _pnlGrrResults.Controls.Add(resultsLayout);

            pnlHost.Controls.Add(_pnlGrrResults);
            pnlHost.Controls.Add(_pnlGrrRun);
            pnlHost.Controls.Add(_pnlGrrReview);
            pnlHost.Controls.Add(_pnlGrrSetup);

            var pnlNav = new Panel { Dock = DockStyle.Fill, Height = 46, Padding = new Padding(8, 4, 8, 4) };
            _btnGrrPrevious = new Button { Text = "◀  Previous", Width = 120, Height = 32, Dock = DockStyle.Left };
            _btnGrrPrevious.Click += BtnGrrPrevious_Click;
            var pnlNavActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            _btnGrrNext = new Button { Text = "Next  ▶", Width = 120, Height = 32, Margin = new Padding(6, 0, 0, 0) };
            _btnGrrNext.Click += BtnGrrNext_Click;
            pnlNavActions.Controls.Add(_btnGrrNext);
            pnlNav.Controls.Add(_btnGrrPrevious);
            pnlNav.Controls.Add(pnlNavActions);

            tlp.Controls.Add(pnlHeader, 0, 0);
            tlp.Controls.Add(pnlHost, 0, 1);
            tlp.Controls.Add(pnlNav, 0, 2);
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

            _bwGrr = new BackgroundWorker { WorkerReportsProgress = true };
            _bwGrr.DoWork             += BwGrr_DoWork;
            _bwGrr.ProgressChanged    += BwGeneric_ProgressChanged;
            _bwGrr.RunWorkerCompleted += BwGrr_Completed;
        }

        private void BwGeneric_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (sender == _bwGrr && _pbGrr != null)
                _pbGrr.Value = Math.Max(_pbGrr.Minimum, Math.Min(_pbGrr.Maximum, e.ProgressPercentage));

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

        private void RefreshGrrSequences()
        {
            if (_cmbGrrSequence == null) return;

            _cmbGrrSequence.Items.Clear();
            foreach (var seq in Shared.AllAvailableTestSequences)
                _cmbGrrSequence.Items.Add(seq.SequenceName ?? "(unnamed)");

            if (_cmbGrrSequence.Items.Count > 0)
                _cmbGrrSequence.SelectedIndex = 0;

            RefreshGrrSequenceSelection();
        }

        private void RefreshGrrSequenceSelection()
        {
            if (_cmbGrrSequence == null || _lblGrrSequenceSummary == null) return;

            TestSequenceModel seq = SelectedGrrSequence();
            if (seq == null)
            {
                _lblGrrSequenceSummary.Text = "Select a sequence definition. DUT serial numbers are scanned separately below for this Gauge R&R study.";
                return;
            }

            _lblGrrSequenceSummary.Text = BuildGrrSequenceSummary(seq);
            UpdateGrrMeasureOptions();
        }

        private void UpdateGrrOperatorInputs()
        {
            if (_pnlGrrOperators == null || _nudGrrOperators == null) return;

            List<string> existingNames = _pnlGrrOperators.Controls.OfType<TextBox>()
                .Select(tb => tb.Text)
                .ToList();
            int target = (int)_nudGrrOperators.Value;
            _pnlGrrOperators.SuspendLayout();
            _pnlGrrOperators.Controls.Clear();
            for (int i = 0; i < target; i++)
            {
                _pnlGrrOperators.Controls.Add(new Label
                {
                    Text = $"Operator {i + 1}:",
                    AutoSize = true,
                    Margin = new Padding(4, 10, 0, 4)
                });
                _pnlGrrOperators.Controls.Add(new TextBox
                {
                    Width = 150,
                    Margin = new Padding(6, 6, 14, 4),
                    Text = i < existingNames.Count && !string.IsNullOrWhiteSpace(existingNames[i])
                        ? existingNames[i]
                        : $"Operator {i + 1}"
                });
            }
            _pnlGrrOperators.ResumeLayout();
        }

        private void GrrDutCount_ValueChanged(object sender, EventArgs e)
        {
            GenerateGrrDutTable((int)_nudGrrBaseCount.Value, (int)_nudGrrRemoteCount.Value);
        }

        private void GenerateGrrDutTable(int baseCount, int remoteCount)
        {
            if (_tlpGrrDuts == null)
                return;

            Dictionary<int, string> baseSerials = GetGrrParts(DUTType.Base)
                .ToDictionary(part => part.Slot, part => part.SerialNumber);
            Dictionary<int, string> remoteSerials = GetGrrParts(DUTType.Remote)
                .ToDictionary(part => part.Slot, part => part.SerialNumber);

            _tlpGrrDuts.SuspendLayout();
            _tlpGrrDuts.Controls.Clear();
            _tlpGrrDuts.ColumnStyles.Clear();
            _tlpGrrDuts.RowStyles.Clear();
            _tlpGrrDuts.ColumnCount = 2;
            _tlpGrrDuts.RowCount = 2 + baseCount + remoteCount;
            _tlpGrrDuts.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            _tlpGrrDuts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (int row = 0; row < _tlpGrrDuts.RowCount; row++)
                _tlpGrrDuts.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            int rowIndex = 0;

            var lblBaseTitle = new Label
            {
                Text = "Base DUTs",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _tlpGrrDuts.Controls.Add(lblBaseTitle, 0, rowIndex);
            _tlpGrrDuts.SetColumnSpan(lblBaseTitle, 2);
            rowIndex++;

            for (int index = 0; index < baseCount; index++)
            {
                _tlpGrrDuts.Controls.Add(new Label
                {
                    Text = $"Base #{index + 1}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Margin = new Padding(4, 0, 4, 0)
                }, 0, rowIndex);

                var txtBase = new TextBox
                {
                    Tag = $"Base_{index + 1}",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4, 3, 8, 3),
                    Text = baseSerials.TryGetValue(index + 1, out string serial) ? serial : string.Empty,
                    TabIndex = index
                };
                txtBase.TextChanged += GrrDutTextChanged;
                _tlpGrrDuts.Controls.Add(txtBase, 1, rowIndex);
                rowIndex++;
            }

            var lblRemoteTitle = new Label
            {
                Text = "Remote DUTs",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };
            _tlpGrrDuts.Controls.Add(lblRemoteTitle, 0, rowIndex);
            _tlpGrrDuts.SetColumnSpan(lblRemoteTitle, 2);
            rowIndex++;

            for (int index = 0; index < remoteCount; index++)
            {
                _tlpGrrDuts.Controls.Add(new Label
                {
                    Text = $"Remote #{index + 1}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Margin = new Padding(4, 0, 4, 0)
                }, 0, rowIndex);

                var txtRemote = new TextBox
                {
                    Tag = $"Remote_{index + 1}",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4, 3, 8, 3),
                    Text = remoteSerials.TryGetValue(index + 1, out string serial) ? serial : string.Empty,
                    TabIndex = baseCount + index
                };
                txtRemote.TextChanged += GrrDutTextChanged;
                _tlpGrrDuts.Controls.Add(txtRemote, 1, rowIndex);
                rowIndex++;
            }

            _tlpGrrDuts.ResumeLayout();
            ResizeGrrDutTable();
            UpdateGrrDutEntries();
        }

        private void ResizeGrrDutTable()
        {
            if (_tlpGrrDuts?.Parent == null)
                return;

            _tlpGrrDuts.Width = Math.Max(240, _tlpGrrDuts.Parent.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
        }

        private void GrrDutTextChanged(object sender, EventArgs e)
        {
            UpdateGrrDutEntries();
        }

        private void UpdateGrrDutEntries()
        {
            if (_tlpGrrDuts == null)
                return;

            List<TextBox> serialBoxes = _tlpGrrDuts.Controls.OfType<TextBox>().ToList();
            HashSet<string> duplicateSerials = serialBoxes
                .Select(textBox => (textBox.Text ?? string.Empty).Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .GroupBy(text => text, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _grrDuts.Clear();
            foreach (TextBox textBox in serialBoxes)
            {
                string serialNumber = (textBox.Text ?? string.Empty).Trim();
                textBox.BackColor = Color.White;

                if (string.IsNullOrWhiteSpace(serialNumber))
                    continue;

                if (duplicateSerials.Contains(serialNumber))
                {
                    textBox.BackColor = Color.Pink;
                    continue;
                }

                string[] tagParts = (textBox.Tag?.ToString() ?? string.Empty).Split('_');
                if (tagParts.Length != 2 || !int.TryParse(tagParts[1], out int slot))
                    continue;

                DUTType dutType = string.Equals(tagParts[0], "Base", StringComparison.OrdinalIgnoreCase)
                    ? DUTType.Base
                    : DUTType.Remote;

                _grrDuts.Add(new DUTModel
                {
                    DUTType = dutType,
                    ReadyToTest = true,
                    SerialNumber = serialNumber,
                    Slot = slot,
                    Tag = textBox.Tag.ToString(),
                    ItemNumber = dutType == DUTType.Base
                        ? Shared.sharedGeneralSettings?.GeneralSettings?[0]?.BASE_ITEM_NUMBER
                        : Shared.sharedGeneralSettings?.GeneralSettings?[0]?.REMOTE_ITEM_NUMBER,
                    ThermistorChannel = Shared.GetThermistorChannel(dutType, slot)
                });
            }

            UpdateGrrDutSummary(duplicateSerials.Count);
            UpdateGrrMeasureOptions();
        }

        private void UpdateGrrDutSummary(int duplicateCount)
        {
            if (_lblGrrDutSummary == null)
                return;

            int baseConfigured = GetGrrConfiguredPartCount(DUTType.Base);
            int remoteConfigured = GetGrrConfiguredPartCount(DUTType.Remote);
            int baseScanned = _grrDuts.Count(dut => dut.DUTType == DUTType.Base);
            int remoteScanned = _grrDuts.Count(dut => dut.DUTType == DUTType.Remote);
            string duplicateText = duplicateCount > 0 ? $"  Duplicate serials: {duplicateCount}" : string.Empty;

            _lblGrrDutSummary.Text =
                $"Scanned Base {baseScanned}/{baseConfigured}    Scanned Remote {remoteScanned}/{remoteConfigured}    Total study parts {baseScanned + remoteScanned}{duplicateText}";
        }

        private int GetGrrConfiguredPartCount(DUTType dutType)
        {
            if (dutType == DUTType.Base)
                return _nudGrrBaseCount != null ? (int)_nudGrrBaseCount.Value : 0;

            return _nudGrrRemoteCount != null ? (int)_nudGrrRemoteCount.Value : 0;
        }

        private List<DUTModel> GetGrrParts(DUTType dutType)
        {
            return _grrDuts
                .Where(dut => dut.DUTType == dutType)
                .OrderBy(dut => dut.Slot)
                .Select(dut => dut.Clone())
                .ToList();
        }

        private string BuildGrrSequenceSummary(TestSequenceModel seq)
        {
            if (seq == null)
                return "Select a sequence definition. DUT serial numbers are scanned separately below for this Gauge R&R study.";

            string revisionText = string.IsNullOrWhiteSpace(seq.Revision) ? string.Empty : $"Rev {seq.Revision} — ";
            string chamberText = seq.CallsChamberProgram
                ? $"chamber program #{seq.ChamberProgram?.ProgramNumber ?? 0}"
                : $"manual chamber run with {seq.ChamberManualRun?.ChamberRunSteps?.Count ?? 0} step(s)";
            List<string> tests = ExtractGrrSequenceTests(seq);
            string testsText = tests.Count > 0 ? string.Join(", ", tests) : "no explicit test list";
            return $"{revisionText}{chamberText}. Declared tests: {testsText}. DUT serial numbers are scanned below for this Gauge R&R study.";
        }

        private List<string> ExtractGrrSequenceTests(TestSequenceModel seq)
        {
            var tests = new List<string>();

            if (seq?.ChamberProgram?.TestsForEachStep != null)
                tests.AddRange(seq.ChamberProgram.TestsForEachStep.Select(step => step.Tests));
            if (seq?.ChamberManualRun?.ChamberRunSteps != null)
                tests.AddRange(seq.ChamberManualRun.ChamberRunSteps.Select(step => step.Tests));

            return tests
                .SelectMany(text => (text ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(text => text.Trim())
                .Where(text => text.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void UpdateGrrMeasureOptions()
        {
            bool hasBase = _grrDuts.Any(dut => dut.DUTType == DUTType.Base);
            bool hasRemote = _grrDuts.Any(dut => dut.DUTType == DUTType.Remote);

            if (_chkGrrIncludeBase != null)
            {
                bool wasEnabled = _chkGrrIncludeBase.Enabled;
                _chkGrrIncludeBase.Enabled = hasBase;
                if (!hasBase) _chkGrrIncludeBase.Checked = false;
                else if (!wasEnabled && !_chkGrrIncludeBase.Checked) _chkGrrIncludeBase.Checked = true;
            }
            if (_chkGrrIncludeRemote != null)
            {
                bool wasEnabled = _chkGrrIncludeRemote.Enabled;
                _chkGrrIncludeRemote.Enabled = hasRemote;
                if (!hasRemote) _chkGrrIncludeRemote.Checked = false;
                else if (!wasEnabled && !_chkGrrIncludeRemote.Checked) _chkGrrIncludeRemote.Checked = true;
            }

            PopulateGrrTestCombo(_cmbGrrBaseTest, true);
            PopulateGrrTestCombo(_cmbGrrRemoteTest, false);

            PopulateGrrColumnCombo(_cmbGrrBaseColumn, SelectedGrrTestType(_cmbGrrBaseTest));
            PopulateGrrColumnCombo(_cmbGrrRemoteColumn, SelectedGrrTestType(_cmbGrrRemoteTest));

            SetGrrPlanControlsEnabled(_chkGrrIncludeBase?.Checked == true, _cmbGrrBaseTest, _cmbGrrBaseColumn, _nudGrrBaseTolerance);
            SetGrrPlanControlsEnabled(_chkGrrIncludeRemote?.Checked == true, _cmbGrrRemoteTest, _cmbGrrRemoteColumn, _nudGrrRemoteTolerance);
        }

        private void SetGrrPlanControlsEnabled(bool enabled, params Control[] controls)
        {
            foreach (Control control in controls)
                if (control != null)
                    control.Enabled = enabled;
        }

        private void PopulateGrrTestCombo(ComboBox combo, bool isBase)
        {
            if (combo == null) return;

            string current = combo.SelectedItem?.ToString();
            combo.Items.Clear();
            foreach (TestSequences test in GetGrrAvailableTests(isBase))
                combo.Items.Add(test.ToString());

            if (!string.IsNullOrEmpty(current) && combo.Items.Contains(current))
                combo.SelectedItem = current;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private static IEnumerable<TestSequences> GetGrrAvailableTests(bool isBase)
        {
            if (isBase)
            {
                yield return TestSequences.Base_Z_IB_IOP;
                yield return TestSequences.Base_Z_IPD;
                yield break;
            }

            yield return TestSequences.Remote_Z_IOP;
            yield return TestSequences.Remote_Z_IPV;
            yield return TestSequences.Remote_Z_VPV;
        }

        private static TestSequences? SelectedGrrTestType(ComboBox combo)
        {
            if (combo == null || combo.SelectedItem == null) return null;
            if (Enum.TryParse(combo.SelectedItem.ToString(), out TestSequences test))
                return test;
            return null;
        }

        private void PopulateGrrColumnCombo(ComboBox combo, TestSequences? test)
        {
            if (combo == null) return;

            string current = combo.SelectedItem?.ToString();
            combo.Items.Clear();
            foreach (string column in GetGrrColumns(test))
                combo.Items.Add(column);

            if (!string.IsNullOrEmpty(current) && combo.Items.Contains(current))
                combo.SelectedItem = current;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private static IEnumerable<string> GetGrrColumns(TestSequences? test)
        {
            switch (test)
            {
                case TestSequences.Base_Z_IB_IOP:
                    return new[] { "CH1 Voltage(V)", "CH1 Current(A)", "CH2 Voltage(V)", "CH2 Current(A)", "CH4 Voltage(V)", "CH4 Current(A)", "CH4 Power(W)" };
                case TestSequences.Base_Z_IPD:
                    return new[] { "CH3 Voltage(V)", "CH3 Current(A)", "CH2 Voltage(V)", "CH2 Current(A)" };
                case TestSequences.Remote_Z_IOP:
                    return new[] { "CH1 Voltage(V)", "CH1 Current(A)", "CH4 Voltage(V)", "CH4 Current(A)", "CH4 Power(W)" };
                case TestSequences.Remote_Z_IPV:
                    return new[] { "CH3 Voltage(V)", "CH3 Current(A)", "CH2 Voltage(V)", "CH2 Current(A)" };
                case TestSequences.Remote_Z_VPV:
                    return new[] { "CH3 Voltage(V)", "CH3 Current(A)", "CH2 Voltage(V)", "CH2 Current(A)", "CH5 Current(A)", "Power (VxA)" };
                default:
                    return Array.Empty<string>();
            }
        }

        private TestSequenceModel SelectedGrrSequence()
        {
            if (_cmbGrrSequence == null || _cmbGrrSequence.SelectedIndex < 0 || _cmbGrrSequence.SelectedIndex >= Shared.AllAvailableTestSequences.Count)
                return null;

            return Shared.AllAvailableTestSequences[_cmbGrrSequence.SelectedIndex];
        }

        private void UpdateGrrWizardUi()
        {
            if (_pnlGrrSetup == null) return;

            bool busy = _bwGrr != null && _bwGrr.IsBusy;

            _pnlGrrSetup.Visible = _grrPage == GrrWizardPage.Setup;
            _pnlGrrReview.Visible = _grrPage == GrrWizardPage.Review;
            _pnlGrrRun.Visible = _grrPage == GrrWizardPage.Run;
            _pnlGrrResults.Visible = _grrPage == GrrWizardPage.Results;

            if (_btnGrrStart != null)
            {
                _btnGrrStart.Visible = _grrPage == GrrWizardPage.Setup;
                _btnGrrStart.Enabled = !busy;
            }
            if (_btnGrrNext != null)
                _btnGrrNext.Visible = _grrPage != GrrWizardPage.Setup;

            switch (_grrPage)
            {
                case GrrWizardPage.Setup:
                    _lblGrrStepTitle.Text = "Step 1 of 4 — Study Setup";
                    _lblGrrPrompt.Text = "Choose the sequence definition, scan the DUT barcodes for this Gauge R&R study, then select operators, replicates, measurement columns, tolerances, and the output folder.";
                    _btnGrrPrevious.Enabled = false;
                    break;

                case GrrWizardPage.Review:
                    _lblGrrStepTitle.Text = "Step 2 of 4 — Review Instructions";
                    _lblGrrPrompt.Text = "Review the study plan and operator instructions. When the station is ready, click Next to begin the first prompted operator step.";
                    _btnGrrPrevious.Enabled = !busy;
                    _btnGrrNext.Enabled = !busy && _grrStudy != null;
                    _btnGrrNext.Text = "Next  ▶";
                    if (_grrStudy != null)
                        _rtbGrrReview.Text = BuildGrrReviewText(_grrStudy);
                    break;

                case GrrWizardPage.Run:
                    int totalSteps = _grrStudy?.Steps.Count ?? 0;
                    int currentStepNo = _grrCurrentStepIndex >= 0 ? _grrCurrentStepIndex + 1 : 0;
                    _lblGrrStepTitle.Text = totalSteps > 0
                        ? $"Step 3 of 4 — Execute Study ({currentStepNo}/{totalSteps})"
                        : "Step 3 of 4 — Execute Study";
                    _lblGrrPrompt.Text = "The study rotates operator order across replicates to counterbalance time and order effects. Before every prompted step, unload any DUTs remaining in the jig, clean and reseat them, then reconnect them in the same configured slots before running the step.";
                    _btnGrrPrevious.Enabled = !busy;
                    _btnGrrNext.Enabled = !busy && CanAdvanceGrrRunStep();
                    _btnGrrNext.Text = CanShowGrrResults() ? "View Report  ▶" : "Next  ▶";
                    UpdateGrrRunStepUi();
                    break;

                default:
                    _lblGrrStepTitle.Text = "Step 4 of 4 — Report";
                    _lblGrrPrompt.Text = "The report has been generated. Use Previous to review step prompts or Reset to configure a new Gauge R&R study.";
                    _btnGrrPrevious.Enabled = !busy;
                    _btnGrrNext.Enabled = !busy;
                    _btnGrrNext.Text = "Reset  ↺";
                    UpdateGrrResultsUi();
                    break;
            }
        }

        private void BtnGrrStart_Click(object sender, EventArgs e)
        {
            if (_grrPage != GrrWizardPage.Setup)
                return;

            BtnGrrNext_Click(sender, e);
        }

        private void BtnGrrBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog { Description = "Select the output folder for Gauge R&R reports and raw study data" })
            {
                if (!string.IsNullOrEmpty(_txtGrrOutputFolder.Text) && Directory.Exists(_txtGrrOutputFolder.Text))
                    fbd.SelectedPath = _txtGrrOutputFolder.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                    _txtGrrOutputFolder.Text = fbd.SelectedPath;
            }
        }

        private void BtnGrrPrevious_Click(object sender, EventArgs e)
        {
            if (_bwGrr != null && _bwGrr.IsBusy) return;

            switch (_grrPage)
            {
                case GrrWizardPage.Review:
                    _grrPage = GrrWizardPage.Setup;
                    break;

                case GrrWizardPage.Run:
                    if (_grrCurrentStepIndex > 0)
                    {
                        _grrCurrentStepIndex--;
                        UpdateGrrRunStepUi();
                    }
                    else
                    {
                        _grrPage = GrrWizardPage.Review;
                    }
                    break;

                case GrrWizardPage.Results:
                    _grrPage = GrrWizardPage.Run;
                    break;
            }

            UpdateGrrWizardUi();
        }

        private void BtnGrrNext_Click(object sender, EventArgs e)
        {
            if (_bwGrr != null && _bwGrr.IsBusy) return;

            switch (_grrPage)
            {
                case GrrWizardPage.Setup:
                    if (!TryBuildGrrStudy(out GaugeRrStudyDefinition study)) return;
                    _grrStudy = study;
                    _grrResults.Clear();
                    _grrCurrentStepIndex = 0;
                    _grrReportHtmlPath = null;
                    _grrReportCsvPath = null;
                    _pbGrr.Value = 0;
                    _lblGrrRunStatus.ForeColor = Color.DimGray;
                    _lblGrrRunStatus.Text = "Study schedule created. Review the counterbalanced operator instructions before running the first step.";
                    _lblGrrResultsStatus.Text = "No report generated yet.";
                    _dgvGrrSummary.Rows.Clear();
                    RebuildGrrScheduleGrid();
                    _grrPage = GrrWizardPage.Review;
                    break;

                case GrrWizardPage.Review:
                    if (_grrStudy == null)
                    {
                        LogErr("Create a valid Gauge R&R study first.");
                        return;
                    }
                    _grrPage = GrrWizardPage.Run;
                    break;

                case GrrWizardPage.Run:
                    if (_grrStudy == null)
                    {
                        LogErr("No Gauge R&R study is loaded.");
                        return;
                    }

                    GaugeRrRunStep currentStep = CurrentGrrStep();
                    if (currentStep == null)
                    {
                        LogErr("No active Gauge R&R run step is available.");
                        return;
                    }

                    if (!currentStep.Completed)
                    {
                        LogErr("Run the current operator step before moving to the next prompt.");
                        return;
                    }

                    if (_grrCurrentStepIndex < _grrStudy.Steps.Count - 1)
                    {
                        _grrCurrentStepIndex++;
                        UpdateGrrRunStepUi();
                    }
                    else if (_grrResults.Count > 0)
                    {
                        _grrPage = GrrWizardPage.Results;
                    }
                    break;

                default:
                    ResetGrrWorkflow();
                    break;
            }

            UpdateGrrWizardUi();
        }

        private void BtnGrrRunCurrentStep_Click(object sender, EventArgs e)
        {
            if (_bwGrr.IsBusy)
            {
                LogErr("A Gauge R&R step is already running.");
                return;
            }
            if (_grrStudy == null)
            {
                LogErr("Create and review a Gauge R&R study first.");
                return;
            }

            GaugeRrRunStep step = CurrentGrrStep();
            if (step == null)
            {
                LogErr("No Gauge R&R run step is selected.");
                return;
            }
            if (step.Completed)
            {
                LogInfo("This step is already complete. Use Next or Previous to navigate the study.");
                return;
            }

            step.Status = "Running";
            step.Notes = BuildGrrPlanSummary(_grrStudy);
            step.StartedAt = DateTime.Now;
            step.CompletedAt = null;
            _pbGrr.Value = 0;
            LogInfo($"Gauge R&R step {step.Index + 1}/{_grrStudy.Steps.Count} started — operator: {step.OperatorName}, replicate: {step.ReplicateNo}");
            RebuildGrrScheduleGrid();
            UpdateGrrRunStepUi();

            _bwGrr.RunWorkerAsync(new Tuple<GaugeRrStudyDefinition, GaugeRrRunStep>(_grrStudy, step));
        }

        private void BwGrr_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker)sender;
            var args = (Tuple<GaugeRrStudyDefinition, GaugeRrRunStep>)e.Argument;
            e.Result = ExecuteGrrStep(args.Item1, args.Item2, bw);
        }

        private void BwGrr_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_grrStudy == null)
            {
                _btnGrrRunCurrentStep.Enabled = true;
                return;
            }

            GaugeRrRunStep step = CurrentGrrStep();
            if (e.Error != null)
            {
                if (step != null)
                {
                    step.Status = "Failed";
                    step.Completed = false;
                    step.Notes = e.Error.Message;
                }
                _pbGrr.Value = 0;
                _lblGrrRunStatus.ForeColor = Color.OrangeRed;
                _lblGrrRunStatus.Text = $"Step failed: {e.Error.Message}";
                LogErr($"Gauge R&R step failed: {e.Error.Message}");
                RebuildGrrScheduleGrid();
                UpdateGrrWizardUi();
                return;
            }

            var result = e.Result as GaugeRrStepResult;
            if (result == null)
            {
                LogErr("Gauge R&R step returned no data.");
                UpdateGrrWizardUi();
                return;
            }

            step = result.Step;
            _grrStudy.Observations.RemoveAll(o =>
                string.Equals(o.OperatorName, step.OperatorName, StringComparison.OrdinalIgnoreCase) &&
                o.ReplicateNo == step.ReplicateNo);
            _grrStudy.Observations.AddRange(result.Observations);

            step.Status = "Complete";
            step.Completed = true;
            step.CompletedAt = DateTime.Now;
            step.Notes = result.Summary;
            _pbGrr.Value = 100;
            LogOk($"Gauge R&R step complete — {result.Summary}");
            RebuildGrrScheduleGrid();

            bool allStepsComplete = _grrStudy.Steps.Count > 0 && _grrStudy.Steps.All(s => s.Completed);
            if (allStepsComplete)
            {
                FinalizeGrrStudy();
                return;
            }

            _lblGrrRunStatus.ForeColor = Color.DarkGreen;
            _lblGrrRunStatus.Text = BuildGrrCompletedStepStatus(step);
            UpdateGrrWizardUi();
        }

        private bool TryBuildGrrStudy(out GaugeRrStudyDefinition study)
        {
            study = null;

            TestSequenceModel seq = SelectedGrrSequence();
            if (seq == null)
            {
                LogErr("Select a test sequence for the Gauge R&R study.");
                return false;
            }

            string outputFolder = (_txtGrrOutputFolder.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                LogErr("Select the Gauge R&R output folder first.");
                return false;
            }

            try
            {
                Directory.CreateDirectory(outputFolder);
            }
            catch (Exception ex)
            {
                LogErr($"Cannot create or access the selected output folder: {ex.Message}");
                return false;
            }

            List<string> operators = GetGrrOperatorNames();
            if (operators.Count < 2)
            {
                LogErr("Gauge R&R requires at least two operators.");
                return false;
            }
            if (operators.Any(string.IsNullOrWhiteSpace))
            {
                LogErr("Every operator name must be filled in before the study can start.");
                return false;
            }
            if (operators.Distinct(StringComparer.OrdinalIgnoreCase).Count() != operators.Count)
            {
                LogErr("Operator names must be unique.");
                return false;
            }

            var plans = new List<GaugeRrMeasurePlan>();
            TestSequenceModel runtimeSequence = seq.Clone();
            runtimeSequence.BaseDUTs.Clear();
            runtimeSequence.BaseDUTs.AddRange(GetGrrParts(DUTType.Base));
            runtimeSequence.RemoteDUTs.Clear();
            runtimeSequence.RemoteDUTs.AddRange(GetGrrParts(DUTType.Remote));

            if (_chkGrrIncludeBase.Checked)
            {
                GaugeRrMeasurePlan basePlan = BuildGrrPlan(
                    "Base",
                    DUTType.Base,
                    SelectedGrrTestType(_cmbGrrBaseTest),
                    _cmbGrrBaseColumn,
                    _nudGrrBaseTolerance,
                    GetGrrParts(DUTType.Base),
                    GetGrrConfiguredPartCount(DUTType.Base));
                if (basePlan == null) return false;
                plans.Add(basePlan);
            }
            if (_chkGrrIncludeRemote.Checked)
            {
                GaugeRrMeasurePlan remotePlan = BuildGrrPlan(
                    "Remote",
                    DUTType.Remote,
                    SelectedGrrTestType(_cmbGrrRemoteTest),
                    _cmbGrrRemoteColumn,
                    _nudGrrRemoteTolerance,
                    GetGrrParts(DUTType.Remote),
                    GetGrrConfiguredPartCount(DUTType.Remote));
                if (remotePlan == null) return false;
                plans.Add(remotePlan);
            }

            if (plans.Count == 0)
            {
                LogErr("Select at least one measurement plan (Base and/or Remote) for the Gauge R&R study.");
                return false;
            }

            string studyStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string studyName = SanitizeFileName(seq.SequenceName ?? "Sequence") + "_" + studyStamp;
            string dataFolderRoot = Path.Combine(outputFolder, "GaugeRR_" + studyName + "_Data");

            try
            {
                Directory.CreateDirectory(dataFolderRoot);
            }
            catch (Exception ex)
            {
                LogErr($"Cannot create the Gauge R&R raw data folder: {ex.Message}");
                return false;
            }

            study = new GaugeRrStudyDefinition
            {
                StudyName = studyName,
                CreatedAt = DateTime.Now,
                SequenceName = seq.SequenceName ?? "(unnamed)",
                Sequence = runtimeSequence,
                Replicates = (int)_nudGrrReplicates.Value,
                Operators = operators,
                Plans = plans,
                OutputFolder = outputFolder,
                DataFolderRoot = dataFolderRoot
            };

            int stepIndex = 0;
            for (int replicate = 1; replicate <= study.Replicates; replicate++)
            {
                foreach (int operatorIndex in GetCounterbalancedGrrOperatorOrder(study.Operators.Count, replicate))
                {
                    string operatorFolder = $"{operatorIndex + 1:00}_{SanitizeFileName(study.Operators[operatorIndex])}";
                    study.Steps.Add(new GaugeRrRunStep
                    {
                        Index = stepIndex++,
                        OperatorIndex = operatorIndex,
                        OperatorName = study.Operators[operatorIndex],
                        ReplicateNo = replicate,
                        FolderPath = Path.Combine(study.DataFolderRoot, operatorFolder, $"Replicate_{replicate:00}"),
                        Status = "Pending",
                        Notes = BuildGrrPlanSummary(study)
                    });
                }
            }

            return true;
        }

        private GaugeRrMeasurePlan BuildGrrPlan(
            string groupName,
            DUTType dutType,
            TestSequences? testType,
            ComboBox columnCombo,
            NumericUpDown toleranceControl,
            List<DUTModel> parts,
            int expectedPartCount)
        {
            if (expectedPartCount <= 0)
            {
                LogErr($"Add at least one {groupName} DUT to the Gauge R&R study before enabling that measurement plan.");
                return null;
            }
            if (parts == null || parts.Count == 0)
            {
                LogErr($"Scan at least one unique {groupName} DUT barcode before the Gauge R&R study can start.");
                return null;
            }
            if (parts.Count < expectedPartCount)
            {
                LogErr($"Scan unique serial numbers for all configured {groupName} DUTs before the Gauge R&R study can start. Expected {expectedPartCount}, captured {parts.Count}.");
                return null;
            }
            if (testType == null)
            {
                LogErr($"Select the {groupName} test type for the Gauge R&R study.");
                return null;
            }

            string columnName = columnCombo?.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
            {
                LogErr($"Select the {groupName} measurement column for the Gauge R&R study.");
                return null;
            }

            return new GaugeRrMeasurePlan
            {
                GroupName = groupName,
                DutType = dutType,
                TestType = testType.Value,
                ColumnName = columnName,
                Tolerance = (double)toleranceControl.Value,
                Parts = parts.Select(part => part.Clone()).ToList()
            };
        }

        private List<string> GetGrrOperatorNames()
        {
            if (_pnlGrrOperators == null)
                return new List<string>();

            return _pnlGrrOperators.Controls.OfType<TextBox>()
                .Select(tb => (tb.Text ?? string.Empty).Trim())
                .Where(name => name.Length > 0)
                .ToList();
        }

        private GaugeRrRunStep CurrentGrrStep()
        {
            if (_grrStudy == null || _grrCurrentStepIndex < 0 || _grrCurrentStepIndex >= _grrStudy.Steps.Count)
                return null;

            return _grrStudy.Steps[_grrCurrentStepIndex];
        }

        private bool CanAdvanceGrrRunStep()
        {
            if (_grrStudy == null || _grrStudy.Steps.Count == 0)
                return false;

            GaugeRrRunStep step = CurrentGrrStep();
            if (step == null)
                return false;

            if (_grrResults.Count > 0 && _grrCurrentStepIndex >= _grrStudy.Steps.Count - 1)
                return true;

            return step.Completed;
        }

        private bool CanShowGrrResults()
        {
            return _grrResults.Count > 0 && _grrStudy != null && _grrCurrentStepIndex == _grrStudy.Steps.Count - 1;
        }

        private void ResetGrrWorkflow()
        {
            _grrStudy = null;
            _grrResults.Clear();
            _grrCurrentStepIndex = -1;
            _grrReportHtmlPath = null;
            _grrReportCsvPath = null;
            _pbGrr.Value = 0;
            _grrPage = GrrWizardPage.Setup;

            _rtbGrrReview.Clear();
            _dgvGrrSchedule.Rows.Clear();
            _dgvGrrSummary.Rows.Clear();
            _lblGrrRunStatus.ForeColor = Color.DimGray;
            _lblGrrRunStatus.Text = "No step has been started yet.";
            _lblGrrResultsStatus.Text = "When all study steps are complete, the wizard will generate a full Gauge R&R report with ANOVA and range-method sections, variance components, %tolerance, ndc, and control charts.";
            UpdateGrrWizardUi();
        }

        private void UpdateGrrRunStepUi()
        {
            if (_lblGrrRunStatus == null)
                return;

            GaugeRrRunStep step = CurrentGrrStep();
            if (_grrStudy == null || step == null)
            {
                _lblGrrRunStatus.ForeColor = Color.DimGray;
                _lblGrrRunStatus.Text = "No Gauge R&R study schedule is loaded yet.";
                _btnGrrRunCurrentStep.Enabled = false;
                RebuildGrrScheduleGrid();
                return;
            }

            if (_bwGrr.IsBusy && string.Equals(step.Status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                _lblGrrRunStatus.ForeColor = Color.DimGray;
                _lblGrrRunStatus.Text = $"Running step {step.Index + 1}/{_grrStudy.Steps.Count} — operator '{step.OperatorName}', replicate {step.ReplicateNo}.";
            }
            else if (string.Equals(step.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                _lblGrrRunStatus.ForeColor = Color.OrangeRed;
                _lblGrrRunStatus.Text = $"Step {step.Index + 1}/{_grrStudy.Steps.Count} failed for operator '{step.OperatorName}', replicate {step.ReplicateNo}. Fix the issue, then click Run Current Step to retry. Details: {step.Notes}";
            }
            else if (step.Completed)
            {
                _lblGrrRunStatus.ForeColor = Color.DarkGreen;
                _lblGrrRunStatus.Text = BuildGrrCompletedStepStatus(step);
            }
            else
            {
                _lblGrrRunStatus.ForeColor = Color.DimGray;
                _lblGrrRunStatus.Text = BuildGrrStepInstruction(step);
            }

            _btnGrrRunCurrentStep.Enabled = !_bwGrr.IsBusy && !step.Completed;
            RebuildGrrScheduleGrid();
        }

        private string BuildGrrStepInstruction(GaugeRrRunStep step)
        {
            string plans = string.Join("   |   ", _grrStudy.Plans.Select(plan =>
                $"{plan.GroupName}: {plan.TestType} → mean({plan.ColumnName})"));
            string setupInstruction = step.Index == 0
                ? "Clean and connect all scanned Gauge R&R DUTs in the configured slots before running this step."
                : "Unload any DUTs remaining in the jig, clean and reseat the DUT/jig interfaces, then reconnect all scanned Gauge R&R DUTs in the configured slots before running this step.";
            return $"Step {step.Index + 1}/{_grrStudy.Steps.Count}. Operator '{step.OperatorName}' must take control of the station for replicate {step.ReplicateNo} of {_grrStudy.Replicates}. This study rotates operator order across replicates to counterbalance time and order effects. {setupInstruction} Use sequence definition '{_grrStudy.SequenceName}', then click Run Current Step. Planned captures: {plans}.";
        }

        private string BuildGrrCompletedStepStatus(GaugeRrRunStep step)
        {
            GaugeRrRunStep nextStep = GetNextGrrStep(step);
            if (nextStep == null)
                return $"Final study step complete for operator '{step.OperatorName}', replicate {step.ReplicateNo}. The Gauge R&R report is ready.";

            const string setupReminder = "Before running it, unload any DUTs remaining in the jig, clean and reseat the interfaces, then reconnect all scanned DUTs in the same configured slots.";
            if (nextStep.ReplicateNo == step.ReplicateNo)
                return $"Step {step.Index + 1}/{_grrStudy.Steps.Count} complete for operator '{step.OperatorName}', replicate {step.ReplicateNo}. Click Next to hand the station to operator '{nextStep.OperatorName}' for the same replicate. {setupReminder}";

            return $"Step {step.Index + 1}/{_grrStudy.Steps.Count} complete for operator '{step.OperatorName}', replicate {step.ReplicateNo}. Click Next to begin replicate {nextStep.ReplicateNo} with operator '{nextStep.OperatorName}'. {setupReminder}";
        }

        private GaugeRrRunStep GetNextGrrStep(GaugeRrRunStep step)
        {
            if (_grrStudy == null || step == null)
                return null;

            int nextIndex = step.Index + 1;
            return nextIndex >= 0 && nextIndex < _grrStudy.Steps.Count
                ? _grrStudy.Steps[nextIndex]
                : null;
        }

        private IEnumerable<int> GetCounterbalancedGrrOperatorOrder(int operatorCount, int replicateNo)
        {
            if (operatorCount <= 0)
                yield break;

            int startIndex = (replicateNo - 1) % operatorCount;
            for (int orderPosition = 0; orderPosition < operatorCount; orderPosition++)
                yield return (startIndex + orderPosition) % operatorCount;
        }

        private string BuildGrrPlanSummary(GaugeRrStudyDefinition study)
        {
            return string.Join(" | ", study.Plans.Select(plan =>
                $"{plan.GroupName}: {plan.TestType} / mean({plan.ColumnName}) / Tol {plan.Tolerance:F4}"));
        }

        private void RebuildGrrScheduleGrid()
        {
            if (_dgvGrrSchedule == null)
                return;

            _dgvGrrSchedule.Rows.Clear();
            if (_grrStudy == null)
                return;

            foreach (GaugeRrRunStep step in _grrStudy.Steps)
            {
                int rowIndex = _dgvGrrSchedule.Rows.Add(
                    step.OperatorName,
                    step.ReplicateNo,
                    step.Status,
                    FormatGrrTimestamp(step.StartedAt),
                    FormatGrrTimestamp(step.CompletedAt),
                    step.Notes ?? string.Empty);

                DataGridViewRow row = _dgvGrrSchedule.Rows[rowIndex];
                if (_grrCurrentStepIndex == step.Index)
                    row.DefaultCellStyle.BackColor = step.Completed ? Color.Honeydew : Color.LightCyan;
                else if (string.Equals(step.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                else if (string.Equals(step.Status, "Running", StringComparison.OrdinalIgnoreCase))
                    row.DefaultCellStyle.BackColor = Color.LemonChiffon;
                else if (step.Completed)
                    row.DefaultCellStyle.BackColor = Color.Honeydew;
            }
        }

        private static string FormatGrrTimestamp(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
        }

        private GaugeRrStepResult ExecuteGrrStep(GaugeRrStudyDefinition study, GaugeRrRunStep step, BackgroundWorker bw)
        {
            const double temperature = 25.0;

            ResetGrrStepFolder(step.FolderPath);
            string baseFolder = Path.Combine(step.FolderPath, "Base");
            string remoteFolder = Path.Combine(step.FolderPath, "Remote");
            string originalBase = Shared.BaseResultsPath;
            string originalRemote = Shared.RemoteResultsPath;
            var result = new GaugeRrStepResult { Step = step };

            try
            {
                if (study.Plans.Any(plan => plan.DutType == DUTType.Base)) Directory.CreateDirectory(baseFolder);
                if (study.Plans.Any(plan => plan.DutType == DUTType.Remote)) Directory.CreateDirectory(remoteFolder);
                Shared.BaseResultsPath = baseFolder;
                Shared.RemoteResultsPath = remoteFolder;

                TestSequenceModel seq = study.Sequence.Clone();
                int totalPlans = Math.Max(study.Plans.Count, 1);
                for (int i = 0; i < study.Plans.Count; i++)
                {
                    GaugeRrMeasurePlan plan = study.Plans[i];
                    bw.ReportProgress(i * 100 / totalPlans,
                        $"Gauge R&R — operator {step.OperatorName}, replicate {step.ReplicateNo}: running {plan.GroupName} / {plan.TestType} ...");
                    RunGrrPlannedTest(seq, plan.TestType, bw, step.ReplicateNo, temperature);

                    string folder = plan.DutType == DUTType.Base ? baseFolder : remoteFolder;
                    result.Observations.AddRange(ReadGrrObservations(plan, folder, step.OperatorName, step.ReplicateNo));
                    bw.ReportProgress((i + 1) * 100 / totalPlans,
                        $"Gauge R&R — operator {step.OperatorName}, replicate {step.ReplicateNo}: captured {plan.GroupName} results.");
                }
            }
            finally
            {
                Shared.BaseResultsPath = originalBase;
                Shared.RemoteResultsPath = originalRemote;
            }

            result.Summary = $"{result.Observations.Count} scalar observation(s) captured across {study.Plans.Count} measurement plan(s).";
            return result;
        }

        private static void ResetGrrStepFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
            Directory.CreateDirectory(folderPath);
        }

        private void RunGrrPlannedTest(
            TestSequenceModel sequence,
            TestSequences testType,
            BackgroundWorker bw,
            int replicateNo,
            double temperature)
        {
            bool cancelled;
            bool ok;
            var testResults = new TestResultModel { OverallPassFail = OverallPassFail.PASS, SaveIntoProductionDB = false };

            switch (testType)
            {
                case TestSequences.Base_Z_IB_IOP:
                    ok = IndividualTestRun.RunBase_Z_IB_IOP(sequence, testResults, bw, 0, replicateNo, temperature, out cancelled);
                    break;
                case TestSequences.Base_Z_IPD:
                    ok = IndividualTestRun.RunBase_Z_IPD(sequence, testResults, bw, 0, replicateNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_IOP:
                    ok = IndividualTestRun.RunRemote_Z_IOP(sequence, testResults, bw, 0, replicateNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_IPV:
                    ok = IndividualTestRun.RunRemote_Z_IPV(sequence, testResults, bw, 0, replicateNo, temperature, out cancelled);
                    break;
                case TestSequences.Remote_Z_VPV:
                    ok = IndividualTestRun.RunRemote_Z_VPV(sequence, testResults, bw, 0, replicateNo, temperature, out cancelled);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Gauge R&R test type: {testType}");
            }

            if (cancelled)
                throw new OperationCanceledException($"Gauge R&R test {testType} was cancelled.");
            if (!ok)
                throw new InvalidOperationException($"Gauge R&R test {testType} failed.");
        }

        private List<GaugeRrObservation> ReadGrrObservations(
            GaugeRrMeasurePlan plan,
            string folder,
            string operatorName,
            int replicateNo)
        {
            var matchingFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string filePath in Directory.GetFiles(folder, "*.csv", SearchOption.TopDirectoryOnly))
            {
                var (serial, testType) = ParseGoldFileName(Path.GetFileNameWithoutExtension(filePath));
                if (!string.Equals(testType, plan.TestType.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;

                matchingFiles[serial] = filePath;
            }

            var observations = new List<GaugeRrObservation>();
            foreach (DUTModel part in plan.Parts)
            {
                if (!matchingFiles.TryGetValue(part.SerialNumber, out string csvPath))
                    throw new InvalidOperationException($"Missing CSV file for {plan.GroupName} DUT '{part.SerialNumber}' in step folder '{folder}'.");

                observations.Add(new GaugeRrObservation
                {
                    GroupName = plan.GroupName,
                    TestType = plan.TestType,
                    ColumnName = plan.ColumnName,
                    OperatorName = operatorName,
                    ReplicateNo = replicateNo,
                    PartSerial = part.SerialNumber,
                    Value = ReadGrrMeanFromCsv(csvPath, plan.ColumnName),
                    CsvPath = csvPath
                });
            }

            return observations;
        }

        private static double ReadGrrMeanFromCsv(string csvPath, string columnName)
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
                throw new InvalidOperationException($"CSV file has no measurement rows: {csvPath}");

            string[] headers = lines[0].Split(',');
            int columnIndex = Array.FindIndex(headers,
                h => string.Equals(h.Trim(), columnName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (columnIndex < 0)
                throw new InvalidOperationException($"Column '{columnName}' was not found in {csvPath}");

            double sum = 0;
            int count = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = lines[i].Split(',');
                if (columnIndex >= cols.Length) continue;
                if (double.TryParse(cols[columnIndex].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                {
                    sum += value;
                    count++;
                }
            }

            if (count == 0)
                throw new InvalidOperationException($"Column '{columnName}' in {csvPath} does not contain numeric data rows.");

            return sum / count;
        }

        private void FinalizeGrrStudy()
        {
            try
            {
                _grrResults = AnalyzeGrrStudy(_grrStudy);
                // Log detailed GRR analysis results for debugging discrimination (ndc) issues
                try
                {
                    if (_grrResults != null && _grrResults.Count > 0)
                    {
                        foreach (var group in _grrResults)
                        {
                            var an = group.Anova;
                            var rg = group.Range;
                        
                        }
                    }
                }
                catch { }
                WriteGrrReports();
                LogOk($"Gauge R&R report saved → {_grrReportHtmlPath}");
                LogOk($"Gauge R&R CSV saved → {_grrReportCsvPath}");
                _grrPage = GrrWizardPage.Results;
                UpdateGrrWizardUi();
            }
            catch (Exception ex)
            {
                _lblGrrRunStatus.ForeColor = Color.OrangeRed;
                _lblGrrRunStatus.Text = $"Gauge R&R finalization failed: {ex.Message}";
                LogErr($"Gauge R&R finalization failed: {ex.Message}");
            }
        }

        private List<GaugeRrGroupResult> AnalyzeGrrStudy(GaugeRrStudyDefinition study)
        {
            var results = new List<GaugeRrGroupResult>();
            foreach (GaugeRrMeasurePlan plan in study.Plans)
            {
                List<GaugeRrObservation> observations = study.Observations
                    .Where(observation => string.Equals(observation.GroupName, plan.GroupName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(observation => observation.PartSerial)
                    .ThenBy(observation => observation.OperatorName)
                    .ThenBy(observation => observation.ReplicateNo)
                    .ToList();

                double[,,] matrix = BuildGrrMatrix(study, plan, observations);
                results.Add(new GaugeRrGroupResult
                {
                    Plan = plan,
                    Observations = observations,
                    Anova = AnalyzeGrrAnova(study, plan, matrix),
                    Range = AnalyzeGrrRange(study, plan, matrix),
                    ControlCharts = BuildGrrControlCharts(study, plan, matrix)
                });
            }

            return results;
        }

        private double[,,] BuildGrrMatrix(
            GaugeRrStudyDefinition study,
            GaugeRrMeasurePlan plan,
            List<GaugeRrObservation> observations)
        {
            int partCount = plan.Parts.Count;
            int operatorCount = study.Operators.Count;
            int replicateCount = study.Replicates;

            var matrix = new double[partCount, operatorCount, replicateCount];
            var seen = new bool[partCount, operatorCount, replicateCount];
            var partIndex = plan.Parts
                .Select((part, index) => new { part.SerialNumber, index })
                .ToDictionary(x => x.SerialNumber, x => x.index, StringComparer.OrdinalIgnoreCase);
            var operatorIndex = study.Operators
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

            // Debug: log expected parts/operators and actual observation counts
                

            foreach (GaugeRrObservation observation in observations)
            {
                if (!partIndex.TryGetValue(observation.PartSerial, out int pIndex))
                    throw new InvalidOperationException($"Observation contains an unknown part serial: {observation.PartSerial}");
                if (!operatorIndex.TryGetValue(observation.OperatorName, out int oIndex))
                    throw new InvalidOperationException($"Observation contains an unknown operator: {observation.OperatorName}");

                int rIndex = observation.ReplicateNo - 1;
                if (rIndex < 0 || rIndex >= replicateCount)
                    throw new InvalidOperationException($"Observation contains an invalid replicate index: {observation.ReplicateNo}");
                if (seen[pIndex, oIndex, rIndex])
                    throw new InvalidOperationException($"Duplicate observation detected for part '{observation.PartSerial}', operator '{observation.OperatorName}', replicate {observation.ReplicateNo}.");

                matrix[pIndex, oIndex, rIndex] = observation.Value;
                seen[pIndex, oIndex, rIndex] = true;
            }

            for (int p = 0; p < partCount; p++)
            {
                for (int o = 0; o < operatorCount; o++)
                {
                    for (int r = 0; r < replicateCount; r++)
                    {
                        if (!seen[p, o, r])
                        {
                            throw new InvalidOperationException(
                                $"Missing observation for group '{plan.GroupName}', part '{plan.Parts[p].SerialNumber}', operator '{study.Operators[o]}', replicate {r + 1}.");
                        }
                    }
                }
            }

            return matrix;
        }

        private GaugeRrMethodResult AnalyzeGrrAnova(
            GaugeRrStudyDefinition study,
            GaugeRrMeasurePlan plan,
            double[,,] matrix)
        {
            int partCount = matrix.GetLength(0);
            int operatorCount = matrix.GetLength(1);
            int replicateCount = matrix.GetLength(2);

            var partMeans = new double[partCount];
            var operatorMeans = new double[operatorCount];
            var cellMeans = new double[partCount, operatorCount];

            double grandTotal = 0;
            for (int p = 0; p < partCount; p++)
            {
                for (int o = 0; o < operatorCount; o++)
                {
                    double cellSum = 0;
                    for (int r = 0; r < replicateCount; r++)
                    {
                        double value = matrix[p, o, r];
                        grandTotal += value;
                        partMeans[p] += value;
                        operatorMeans[o] += value;
                        cellSum += value;
                    }
                    cellMeans[p, o] = cellSum / replicateCount;
                }
            }

            double grandMean = grandTotal / (partCount * operatorCount * replicateCount);
            for (int p = 0; p < partCount; p++)
                partMeans[p] /= operatorCount * replicateCount;
            for (int o = 0; o < operatorCount; o++)
                operatorMeans[o] /= partCount * replicateCount;

            double ssPart = 0;
            double ssOperator = 0;
            double ssInteraction = 0;
            double ssRepeat = 0;

            for (int p = 0; p < partCount; p++)
                ssPart += Math.Pow(partMeans[p] - grandMean, 2.0);
            ssPart *= operatorCount * replicateCount;

            for (int o = 0; o < operatorCount; o++)
                ssOperator += Math.Pow(operatorMeans[o] - grandMean, 2.0);
            ssOperator *= partCount * replicateCount;

            for (int p = 0; p < partCount; p++)
            {
                for (int o = 0; o < operatorCount; o++)
                {
                    ssInteraction += Math.Pow(cellMeans[p, o] - partMeans[p] - operatorMeans[o] + grandMean, 2.0);
                    for (int r = 0; r < replicateCount; r++)
                        ssRepeat += Math.Pow(matrix[p, o, r] - cellMeans[p, o], 2.0);
                }
            }
            ssInteraction *= replicateCount;

            int dfPart = partCount - 1;
            int dfOperator = operatorCount - 1;
            int dfInteraction = (partCount - 1) * (operatorCount - 1);
            int dfRepeat = partCount * operatorCount * (replicateCount - 1);

            double msPart = dfPart > 0 ? ssPart / dfPart : 0;
            double msOperator = dfOperator > 0 ? ssOperator / dfOperator : 0;
            double msInteraction = dfInteraction > 0 ? ssInteraction / dfInteraction : 0;
            double msRepeat = dfRepeat > 0 ? ssRepeat / dfRepeat : 0;

                // Debug logging for ANOVA internals
                

            double evVariance = Math.Max(msRepeat, 0.0);
            double interactionVariance = dfInteraction > 0 ? Math.Max((msInteraction - msRepeat) / replicateCount, 0.0) : 0.0;
            double avVariance = Math.Max((msOperator - (dfInteraction > 0 ? msInteraction : msRepeat)) / (partCount * replicateCount), 0.0);
            double partVariance = Math.Max((msPart - (dfInteraction > 0 ? msInteraction : msRepeat)) / (operatorCount * replicateCount), 0.0);

            string notes = interactionVariance > 0
                ? "Operator-by-part interaction contributes to the reproducibility estimate."
                : "Operator-by-part interaction was truncated to zero after variance decomposition.";

            return BuildGrrMethodResult("ANOVA", plan.Tolerance, evVariance, avVariance, interactionVariance, partVariance, notes);
        }

        private GaugeRrMethodResult AnalyzeGrrRange(
            GaugeRrStudyDefinition study,
            GaugeRrMeasurePlan plan,
            double[,,] matrix)
        {
            int partCount = matrix.GetLength(0);
            int operatorCount = matrix.GetLength(1);
            int replicateCount = matrix.GetLength(2);

            var operatorMeans = new double[operatorCount];
            var partMeans = new double[partCount];
            var cellRanges = new List<double>();

            for (int o = 0; o < operatorCount; o++)
            {
                double operatorSum = 0;
                for (int p = 0; p < partCount; p++)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    for (int r = 0; r < replicateCount; r++)
                    {
                        double value = matrix[p, o, r];
                        operatorSum += value;
                        partMeans[p] += value;
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }
                    cellRanges.Add(max - min);
                }
                operatorMeans[o] = operatorSum / (partCount * replicateCount);
            }

            for (int p = 0; p < partCount; p++)
                partMeans[p] /= operatorCount * replicateCount;

            double avgRange = cellRanges.Average();
            double evSigma = avgRange / GetD2Constant(replicateCount);
            double rangeOperatorMeans = operatorMeans.Max() - operatorMeans.Min();
            double rangePartMeans = partMeans.Max() - partMeans.Min();
            double avSigma = operatorCount > 1
                ? Math.Sqrt(Math.Max(Math.Pow(rangeOperatorMeans / GetD2Constant(operatorCount), 2.0) - (evSigma * evSigma) / (partCount * replicateCount), 0.0))
                : 0.0;
            double partSigma = partCount > 1
                ? Math.Sqrt(Math.Max(Math.Pow(rangePartMeans / GetD2Constant(partCount), 2.0) - (evSigma * evSigma) / (operatorCount * replicateCount), 0.0))
                : 0.0;

            return BuildGrrMethodResult(
                "Range",
                plan.Tolerance,
                evSigma * evSigma,
                avSigma * avSigma,
                0.0,
                partSigma * partSigma,
                "Average-and-range estimate. Operator-by-part interaction is not separated in this method.");
        }

        private GaugeRrMethodResult BuildGrrMethodResult(
            string methodName,
            double tolerance,
            double evVariance,
            double avVariance,
            double interactionVariance,
            double partVariance,
            string notes)
        {
            var result = new GaugeRrMethodResult
            {
                MethodName = methodName,
                EVVariance = Math.Max(evVariance, 0.0),
                AVVariance = Math.Max(avVariance, 0.0),
                InteractionVariance = Math.Max(interactionVariance, 0.0),
                PartVariance = Math.Max(partVariance, 0.0),
                Notes = notes
            };

            result.ReproducibilityVariance = result.AVVariance + result.InteractionVariance;
            result.GrrVariance = result.EVVariance + result.ReproducibilityVariance;
            result.TotalVariance = result.GrrVariance + result.PartVariance;

            result.EVPctStudyVar = CalcGrrPctStudy(result.EVVariance, result.TotalVariance);
            result.AVPctStudyVar = CalcGrrPctStudy(result.AVVariance, result.TotalVariance);
            result.ReproPctStudyVar = CalcGrrPctStudy(result.ReproducibilityVariance, result.TotalVariance);
            result.PartPctStudyVar = CalcGrrPctStudy(result.PartVariance, result.TotalVariance);
            result.GrrPctStudyVar = CalcGrrPctStudy(result.GrrVariance, result.TotalVariance);

            result.EVPctTolerance = CalcGrrPctTolerance(result.EVVariance, tolerance);
            result.AVPctTolerance = CalcGrrPctTolerance(result.AVVariance, tolerance);
            result.ReproPctTolerance = CalcGrrPctTolerance(result.ReproducibilityVariance, tolerance);
            result.PartPctTolerance = CalcGrrPctTolerance(result.PartVariance, tolerance);
            result.GrrPctTolerance = CalcGrrPctTolerance(result.GrrVariance, tolerance);
            result.Ndc = CalcGrrNdc(result.PartVariance, result.GrrVariance);
            result.Verdict = EvaluateGrrVerdict(result, tolerance);

            result.Components.Add(BuildGrrComponentRow("Equipment Variation (EV)", result.EVVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Appraiser Variation (AV)", result.AVVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Operator × Part", result.InteractionVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Reproducibility", result.ReproducibilityVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Part-to-Part", result.PartVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Total Gage R&R", result.GrrVariance, result.TotalVariance, tolerance));
            result.Components.Add(BuildGrrComponentRow("Total Variation", result.TotalVariance, result.TotalVariance, tolerance));

            return result;
        }

        private static GaugeRrComponentRow BuildGrrComponentRow(string name, double variance, double totalVariance, double tolerance)
        {
            double stdDev = Math.Sqrt(Math.Max(variance, 0.0));
            double studyVar = 6.0 * stdDev;
            double totalStudyVar = 6.0 * Math.Sqrt(Math.Max(totalVariance, 0.0));
            return new GaugeRrComponentRow
            {
                Name = name,
                Variance = Math.Max(variance, 0.0),
                StandardDeviation = stdDev,
                StudyVariation = studyVar,
                PercentStudyVariation = totalStudyVar > 0 ? studyVar / totalStudyVar * 100.0 : 0.0,
                PercentTolerance = tolerance > 0 ? studyVar / tolerance * 100.0 : 0.0
            };
        }

        private static double CalcGrrPctStudy(double variance, double totalVariance)
        {
            if (totalVariance <= 0) return 0;
            return Math.Sqrt(Math.Max(variance, 0.0)) / Math.Sqrt(totalVariance) * 100.0;
        }

        private static double CalcGrrPctTolerance(double variance, double tolerance)
        {
            if (tolerance <= 0) return 0;
            return (6.0 * Math.Sqrt(Math.Max(variance, 0.0)) / tolerance) * 100.0;
        }

        private static double CalcGrrNdc(double partVariance, double grrVariance)
        {
            if (partVariance <= 0) return 1;
            if (grrVariance <= 0) return 999;
            return Math.Max(1.0, Math.Floor(1.41 * Math.Sqrt(partVariance / grrVariance)));
        }

        private static string EvaluateGrrVerdict(GaugeRrMethodResult result, double tolerance)
        {
            bool tolEnabled = tolerance > 0;
            bool studyGood = result.GrrPctStudyVar <= 10.0;
            bool studyMarginal = result.GrrPctStudyVar <= 30.0;
            bool tolGood = !tolEnabled || result.GrrPctTolerance <= 10.0;
            bool tolMarginal = !tolEnabled || result.GrrPctTolerance <= 30.0;

            if (studyGood && tolGood && result.Ndc >= 5)
                return "Acceptable";
            if (studyMarginal && tolMarginal && result.Ndc >= 4)
                return "Marginal — review discrimination and tolerance usage";
            if (result.Ndc < 5)
                return "Not acceptable — ndc is too low";
            return "Not acceptable";
        }

        private List<GaugeRrControlChart> BuildGrrControlCharts(
            GaugeRrStudyDefinition study,
            GaugeRrMeasurePlan plan,
            double[,,] matrix)
        {
            int partCount = matrix.GetLength(0);
            int operatorCount = matrix.GetLength(1);
            int replicateCount = matrix.GetLength(2);
            double a2 = GetA2Constant(replicateCount);
            double d3 = GetD3Constant(replicateCount);
            double d4 = GetD4Constant(replicateCount);
            var charts = new List<GaugeRrControlChart>();

            for (int o = 0; o < operatorCount; o++)
            {
                var chart = new GaugeRrControlChart { OperatorName = study.Operators[o] };
                for (int p = 0; p < partCount; p++)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    double sum = 0;
                    for (int r = 0; r < replicateCount; r++)
                    {
                        double value = matrix[p, o, r];
                        sum += value;
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }

                    chart.Points.Add(new GaugeRrControlPoint
                    {
                        PartSerial = plan.Parts[p].SerialNumber,
                        MeanValue = sum / replicateCount,
                        RangeValue = max - min
                    });
                }

                chart.XBarCenter = chart.Points.Average(point => point.MeanValue);
                chart.RCenter = chart.Points.Average(point => point.RangeValue);
                chart.XBarUcl = chart.XBarCenter + a2 * chart.RCenter;
                chart.XBarLcl = chart.XBarCenter - a2 * chart.RCenter;
                chart.RUcl = d4 * chart.RCenter;
                chart.RLcl = d3 * chart.RCenter;
                charts.Add(chart);
            }

            return charts;
        }

        private void WriteGrrReports()
        {
            string fileStem = "GaugeRR_Report_" + _grrStudy.StudyName;
            _grrReportHtmlPath = Path.Combine(_grrStudy.OutputFolder, fileStem + ".html");
            _grrReportCsvPath = Path.Combine(_grrStudy.OutputFolder, fileStem + ".csv");

            File.WriteAllText(_grrReportHtmlPath, BuildGrrReportHtml(), Encoding.UTF8);
            File.WriteAllText(_grrReportCsvPath, BuildGrrReportCsv(), Encoding.UTF8);
        }

        private void UpdateGrrResultsUi()
        {
            if (_dgvGrrSummary == null)
                return;

            _dgvGrrSummary.Rows.Clear();
            foreach (GaugeRrGroupResult group in _grrResults)
            {
                AddGrrSummaryRow(group.Plan.GroupName, group.Anova);
                AddGrrSummaryRow(group.Plan.GroupName, group.Range);
            }

            if (_grrResults.Count == 0)
            {
                _lblGrrResultsStatus.Text = "No report generated yet.";
                return;
            }

            _lblGrrResultsStatus.Text =
                $"Gauge R&R report generated.\nHTML: {_grrReportHtmlPath}\nCSV: {_grrReportCsvPath}\nRaw step data: {_grrStudy.DataFolderRoot}";
        }

        private void AddGrrSummaryRow(string groupName, GaugeRrMethodResult method)
        {
            int rowIndex = _dgvGrrSummary.Rows.Add(
                groupName,
                method.MethodName,
                method.GrrPctStudyVar.ToString("F2", CultureInfo.InvariantCulture),
                method.GrrPctTolerance.ToString("F2", CultureInfo.InvariantCulture),
                method.Ndc.ToString("F0", CultureInfo.InvariantCulture),
                method.Verdict);

            if (method.Verdict.StartsWith("Acceptable", StringComparison.OrdinalIgnoreCase))
                _dgvGrrSummary.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Honeydew;
            else if (method.Verdict.StartsWith("Marginal", StringComparison.OrdinalIgnoreCase))
                _dgvGrrSummary.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LemonChiffon;
            else
                _dgvGrrSummary.Rows[rowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
        }

        private string BuildGrrReviewText(GaugeRrStudyDefinition study)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Gauge R&R Study Review");
            sb.AppendLine(new string('=', 72));
            sb.AppendLine($"Study name   : {study.StudyName}");
            sb.AppendLine($"Created      : {study.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Sequence     : {study.SequenceName}");
            sb.AppendLine($"Scanned DUTs : {study.Sequence.BaseDUTs.Count} Base, {study.Sequence.RemoteDUTs.Count} Remote");
            sb.AppendLine($"Operators    : {string.Join(", ", study.Operators)}");
            sb.AppendLine($"Replicates   : {study.Replicates}");
            sb.AppendLine($"Report folder: {study.OutputFolder}");
            sb.AppendLine($"Raw data root: {study.DataFolderRoot}");
            sb.AppendLine();
            sb.AppendLine("Measurement plans");
            sb.AppendLine(new string('-', 72));
            foreach (GaugeRrMeasurePlan plan in study.Plans)
            {
                sb.AppendLine($"{plan.GroupName}  ->  Test: {plan.TestType}");
                sb.AppendLine($"             Column mean: {plan.ColumnName}");
                sb.AppendLine($"             Tolerance  : {plan.Tolerance:F4} (same units as the selected column)");
                sb.AppendLine($"             Parts      : {plan.Parts.Count}  [{string.Join(", ", plan.Parts.Select(part => part.SerialNumber))}]");
            }
            sb.AppendLine();
            sb.AppendLine("Study flow");
            sb.AppendLine(new string('-', 72));
            sb.AppendLine("- The study rotates the operator order on each replicate to counterbalance time and order effects instead of leaving one operator permanently first or last.");
            sb.AppendLine("- Before every prompted step, unload any DUTs remaining in the jig, clean and reseat the DUT/jig interfaces as required, then reconnect all scanned DUTs in the same configured slots.");
            if (study.Operators.Count > 0 && study.Replicates % study.Operators.Count != 0)
                sb.AppendLine($"- This run uses {study.Replicates} replicate(s) across {study.Operators.Count} operator(s). The rotation still spreads order effects, but a fully even first-position balance occurs when the replicate count is a multiple of the operator count.");
            sb.AppendLine();
            sb.AppendLine("Execution order");
            sb.AppendLine(new string('-', 72));
            foreach (GaugeRrRunStep step in study.Steps)
            {
                sb.AppendLine($"{step.Index + 1}. Operator '{step.OperatorName}' — replicate {step.ReplicateNo}");
                sb.AppendLine($"   Prompt: {BuildGrrStepInstruction(step)}");
            }
            sb.AppendLine();
            sb.AppendLine("Report contents");
            sb.AppendLine(new string('-', 72));
            sb.AppendLine("- ANOVA Gauge R&R section");
            sb.AppendLine("- Range-method Gauge R&R section");
            sb.AppendLine("- Variance component tables");
            sb.AppendLine("- %Study Variation, %Tolerance, ndc");
            sb.AppendLine("- X-bar and R control charts for each operator");
            sb.AppendLine("- Raw measurement traceability table with CSV file paths");

            if (study.Plans.Count > 1)
            {
                sb.AppendLine();
                sb.AppendLine("Note");
                sb.AppendLine(new string('-', 72));
                sb.AppendLine("Base and Remote are executed in one workflow, but each group is analyzed in its own Gauge R&R section because the selected test types and response columns may differ.");
            }

            return sb.ToString();
        }

        private string BuildGrrReportHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            sb.AppendLine("<title>Gauge R&amp;R Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;font-size:11pt;margin:30px;color:#1f2733}");
            sb.AppendLine("h1,h2,h3{color:#123a69;margin-bottom:8px}");
            sb.AppendLine("h1{margin-top:0}");
            sb.AppendLine("table{border-collapse:collapse;width:100%}");
            sb.AppendLine("th{background:#123a69;color:#fff;padding:7px 10px;text-align:left;font-size:10pt}");
            sb.AppendLine("td{border:1px solid #d0d7de;padding:6px 10px;vertical-align:top;font-size:10pt}");
            sb.AppendLine("tr:nth-child(even){background:#f8fafc}");
            sb.AppendLine(".meta td:first-child,.mini td:first-child{font-weight:600;color:#123a69;width:180px}");
            sb.AppendLine(".summary-pass{background:#d4edda;color:#155724;font-weight:700}");
            sb.AppendLine(".summary-marginal{background:#fff3cd;color:#856404;font-weight:700}");
            sb.AppendLine(".summary-fail{background:#f8d7da;color:#721c24;font-weight:700}");
            sb.AppendLine(".card{border:1px solid #d0d7de;border-radius:6px;padding:14px;margin-bottom:16px;background:#fff}");
            sb.AppendLine(".method-grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}");
            sb.AppendLine(".chart-grid{display:grid;grid-template-columns:1fr 1fr;gap:14px;margin-bottom:18px}");
            sb.AppendLine(".muted{color:#61758a}");
            sb.AppendLine(".section-note{background:#eef5ff;border-left:4px solid #2b6cb0;padding:10px 12px;margin:12px 0}");
            sb.AppendLine(".footer{margin-top:26px;font-size:9pt;color:#75808c}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h1>Gauge R&amp;R Report</h1>");
            sb.AppendLine($"<div class=\"muted\">Generated: {HtmlEnc(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
            sb.AppendLine("<table class=\"meta\" style=\"margin-top:14px;margin-bottom:18px\">");
            sb.AppendLine($"<tr><td>Study name</td><td>{HtmlEnc(_grrStudy.StudyName)}</td></tr>");
            sb.AppendLine($"<tr><td>Sequence</td><td>{HtmlEnc(_grrStudy.SequenceName)}</td></tr>");
            sb.AppendLine($"<tr><td>Operators</td><td>{HtmlEnc(string.Join(", ", _grrStudy.Operators))}</td></tr>");
            sb.AppendLine($"<tr><td>Replicates</td><td>{_grrStudy.Replicates}</td></tr>");
            sb.AppendLine($"<tr><td>Report folder</td><td>{HtmlEnc(_grrStudy.OutputFolder)}</td></tr>");
            sb.AppendLine($"<tr><td>Raw data root</td><td>{HtmlEnc(_grrStudy.DataFolderRoot)}</td></tr>");
            sb.AppendLine("</table>");

            if (_grrStudy.Plans.Count > 1)
            {
                sb.AppendLine("<div class=\"section-note\">Base and Remote DUTs were run in one wizard workflow. The report presents separate Gauge R&amp;R sections for each group because the configured test types and scalar responses can differ.</div>");
            }

            sb.AppendLine("<div class=\"section-note\">The execution order rotates operator positions on each replicate to counterbalance time and order effects. Before every step, unload any DUTs remaining in the jig, clean and reseat the interfaces, then reconnect the scanned DUTs in the configured slots.</div>");

            sb.AppendLine("<h2>Execution Order</h2>");
            sb.AppendLine("<table><tr><th>#</th><th>Operator</th><th>Replicate</th><th>Step Folder</th><th>Status</th></tr>");
            foreach (GaugeRrRunStep step in _grrStudy.Steps)
            {
                sb.AppendLine($"<tr><td>{step.Index + 1}</td><td>{HtmlEnc(step.OperatorName)}</td><td>{step.ReplicateNo}</td><td>{HtmlEnc(step.FolderPath)}</td><td>{HtmlEnc(step.Status)}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Executive Summary</h2>");
            sb.AppendLine("<table><tr><th>Group</th><th>Method</th><th>%GRR</th><th>%Repeatability</th><th>%Reproducibility</th><th>%Part-to-Part</th><th>%Tolerance</th><th>ndc</th><th>Verdict</th></tr>");
            foreach (GaugeRrGroupResult group in _grrResults)
            {
                AppendGrrSummaryHtmlRow(sb, group.Plan.GroupName, group.Anova);
                AppendGrrSummaryHtmlRow(sb, group.Plan.GroupName, group.Range);
            }
            sb.AppendLine("</table>");

            foreach (GaugeRrGroupResult group in _grrResults)
            {
                sb.AppendLine($"<h2>{HtmlEnc(group.Plan.GroupName)} Group</h2>");
                sb.AppendLine("<table class=\"mini\" style=\"margin-bottom:16px\">");
                sb.AppendLine($"<tr><td>Test type</td><td>{HtmlEnc(group.Plan.TestType.ToString())}</td></tr>");
                sb.AppendLine($"<tr><td>Scalar response</td><td>Mean of column {HtmlEnc(group.Plan.ColumnName)}</td></tr>");
                sb.AppendLine($"<tr><td>Tolerance</td><td>{group.Plan.Tolerance:F4}</td></tr>");
                sb.AppendLine($"<tr><td>Parts</td><td>{group.Plan.Parts.Count}: {HtmlEnc(string.Join(", ", group.Plan.Parts.Select(part => part.SerialNumber)))}</td></tr>");
                sb.AppendLine("</table>");

                sb.AppendLine("<div class=\"method-grid\">");
                sb.AppendLine(BuildGrrMethodCardHtml(group.Anova));
                sb.AppendLine(BuildGrrMethodCardHtml(group.Range));
                sb.AppendLine("</div>");

                sb.AppendLine($"<h3>{HtmlEnc(group.Anova.MethodName)} Variance Components</h3>");
                sb.AppendLine(BuildGrrComponentTableHtml(group.Anova.Components));
                sb.AppendLine($"<h3>{HtmlEnc(group.Range.MethodName)} Variance Components</h3>");
                sb.AppendLine(BuildGrrComponentTableHtml(group.Range.Components));

                sb.AppendLine("<h3>Control Charts</h3>");
                foreach (GaugeRrControlChart chart in group.ControlCharts)
                {
                    sb.AppendLine($"<div class=\"card\"><h4 style=\"margin-top:0;color:#123a69\">Operator: {HtmlEnc(chart.OperatorName)}</h4>");
                    sb.AppendLine("<div class=\"chart-grid\">");
                    sb.AppendLine(BuildGrrChartPanelHtml(
                        "X-bar chart",
                        BuildGrrControlChartSvg(
                            chart.Points.Select(point => point.PartSerial).ToList(),
                            chart.Points.Select(point => point.MeanValue).ToList(),
                            chart.XBarCenter,
                            chart.XBarLcl,
                            chart.XBarUcl,
                            "#2b6cb0")));
                    sb.AppendLine(BuildGrrChartPanelHtml(
                        "R chart",
                        BuildGrrControlChartSvg(
                            chart.Points.Select(point => point.PartSerial).ToList(),
                            chart.Points.Select(point => point.RangeValue).ToList(),
                            chart.RCenter,
                            chart.RLcl,
                            chart.RUcl,
                            "#b45309")));
                    sb.AppendLine("</div></div>");
                }
            }

            sb.AppendLine("<h2>Raw Measurement Traceability</h2>");
            sb.AppendLine("<table><tr><th>Group</th><th>Test Type</th><th>Column</th><th>Part Serial</th><th>Operator</th><th>Replicate</th><th>Value</th><th>CSV Path</th></tr>");
            foreach (GaugeRrObservation observation in _grrStudy.Observations
                .OrderBy(obs => obs.GroupName)
                .ThenBy(obs => obs.PartSerial)
                .ThenBy(obs => obs.OperatorName)
                .ThenBy(obs => obs.ReplicateNo))
            {
                sb.AppendLine($"<tr><td>{HtmlEnc(observation.GroupName)}</td><td>{HtmlEnc(observation.TestType.ToString())}</td><td>{HtmlEnc(observation.ColumnName)}</td><td>{HtmlEnc(observation.PartSerial)}</td><td>{HtmlEnc(observation.OperatorName)}</td><td>{observation.ReplicateNo}</td><td>{observation.Value.ToString("F6", CultureInfo.InvariantCulture)}</td><td>{HtmlEnc(observation.CsvPath)}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine($"<div class=\"footer\">Generated by DuplexerFinalTest v{HtmlEnc(Shared.SoftwareVersion)}</div>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private void AppendGrrSummaryHtmlRow(StringBuilder sb, string groupName, GaugeRrMethodResult method)
        {
            string cssClass = method.Verdict.StartsWith("Acceptable", StringComparison.OrdinalIgnoreCase)
                ? "summary-pass"
                : method.Verdict.StartsWith("Marginal", StringComparison.OrdinalIgnoreCase)
                    ? "summary-marginal"
                    : "summary-fail";
            sb.AppendLine($"<tr><td>{HtmlEnc(groupName)}</td><td>{HtmlEnc(method.MethodName)}</td><td>{method.GrrPctStudyVar:F2}</td><td>{method.EVPctStudyVar:F2}</td><td>{method.ReproPctStudyVar:F2}</td><td>{method.PartPctStudyVar:F2}</td><td>{method.GrrPctTolerance:F2}</td><td>{method.Ndc:F0}</td><td class=\"{cssClass}\">{HtmlEnc(method.Verdict)}</td></tr>");
        }

        private string BuildGrrMethodCardHtml(GaugeRrMethodResult method)
        {
            string verdictClass = method.Verdict.StartsWith("Acceptable", StringComparison.OrdinalIgnoreCase)
                ? "summary-pass"
                : method.Verdict.StartsWith("Marginal", StringComparison.OrdinalIgnoreCase)
                    ? "summary-marginal"
                    : "summary-fail";

            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"card\">");
            sb.AppendLine($"<h3 style=\"margin-top:0\">{HtmlEnc(method.MethodName)}</h3>");
            sb.AppendLine("<table class=\"mini\">");
            sb.AppendLine($"<tr><td>Total Gage R&amp;R (%Study Var)</td><td>{method.GrrPctStudyVar:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>Repeatability (%Study Var)</td><td>{method.EVPctStudyVar:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>Reproducibility (%Study Var)</td><td>{method.ReproPctStudyVar:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>Part-to-Part (%Study Var)</td><td>{method.PartPctStudyVar:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>Total Gage R&amp;R (%Tolerance)</td><td>{method.GrrPctTolerance:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>ndc</td><td>{method.Ndc:F0}</td></tr>");
            sb.AppendLine($"<tr><td>Notes</td><td>{HtmlEnc(method.Notes)}</td></tr>");
            sb.AppendLine($"<tr><td>Verdict</td><td class=\"{verdictClass}\">{HtmlEnc(method.Verdict)}</td></tr>");
            sb.AppendLine("</table></div>");
            return sb.ToString();
        }

        private string BuildGrrComponentTableHtml(List<GaugeRrComponentRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table><tr><th>Component</th><th>Variance</th><th>Std Dev</th><th>Study Var (6σ)</th><th>%Study Var</th><th>%Tolerance</th></tr>");
            foreach (GaugeRrComponentRow row in rows)
            {
                sb.AppendLine($"<tr><td>{HtmlEnc(row.Name)}</td><td>{row.Variance:F6}</td><td>{row.StandardDeviation:F6}</td><td>{row.StudyVariation:F6}</td><td>{row.PercentStudyVariation:F2}</td><td>{row.PercentTolerance:F2}</td></tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private string BuildGrrChartPanelHtml(string title, string svg)
        {
            return $"<div><div style=\"font-weight:600;margin-bottom:6px;color:#123a69\">{HtmlEnc(title)}</div>{svg}</div>";
        }

        private string BuildGrrControlChartSvg(
            List<string> labels,
            List<double> values,
            double centerLine,
            double lowerControlLimit,
            double upperControlLimit,
            string strokeColor)
        {
            if (values == null || values.Count == 0)
                return "<svg width=\"520\" height=\"220\"></svg>";

            const double width = 520;
            const double height = 220;
            const double left = 48;
            const double right = 12;
            const double top = 18;
            const double bottom = 42;

            double minY = Math.Min(values.Min(), Math.Min(centerLine, Math.Min(lowerControlLimit, upperControlLimit)));
            double maxY = Math.Max(values.Max(), Math.Max(centerLine, Math.Max(lowerControlLimit, upperControlLimit)));
            if (Math.Abs(maxY - minY) < 1e-12)
            {
                maxY += 1;
                minY -= 1;
            }

            Func<int, double> xAt = index =>
                labels.Count == 1
                    ? (left + (width - right)) / 2.0
                    : left + ((width - left - right) * index / (labels.Count - 1.0));
            Func<double, double> yAt = value =>
                top + ((maxY - value) / (maxY - minY)) * (height - top - bottom);

            string points = string.Join(" ", values.Select((value, index) =>
                xAt(index).ToString("F1", CultureInfo.InvariantCulture) + "," + yAt(value).ToString("F1", CultureInfo.InvariantCulture)));

            var sb = new StringBuilder();
            sb.AppendLine($"<svg viewBox=\"0 0 {width.ToString(CultureInfo.InvariantCulture)} {height.ToString(CultureInfo.InvariantCulture)}\" width=\"100%\" height=\"220\" xmlns=\"http://www.w3.org/2000/svg\">");
            sb.AppendLine("<rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"#ffffff\" stroke=\"#d0d7de\"/>");
            sb.AppendLine($"<line x1=\"{left:F1}\" y1=\"{yAt(centerLine):F1}\" x2=\"{width - right:F1}\" y2=\"{yAt(centerLine):F1}\" stroke=\"#5b728a\" stroke-dasharray=\"5,4\"/>");
            sb.AppendLine($"<line x1=\"{left:F1}\" y1=\"{yAt(upperControlLimit):F1}\" x2=\"{width - right:F1}\" y2=\"{yAt(upperControlLimit):F1}\" stroke=\"#c53030\" stroke-dasharray=\"4,3\"/>");
            sb.AppendLine($"<line x1=\"{left:F1}\" y1=\"{yAt(lowerControlLimit):F1}\" x2=\"{width - right:F1}\" y2=\"{yAt(lowerControlLimit):F1}\" stroke=\"#c53030\" stroke-dasharray=\"4,3\"/>");
            sb.AppendLine($"<polyline fill=\"none\" stroke=\"{strokeColor}\" stroke-width=\"2\" points=\"{points}\"/>");

            for (int i = 0; i < values.Count; i++)
            {
                double x = xAt(i);
                double y = yAt(values[i]);
                sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"3.2\" fill=\"{strokeColor}\"/>");
                string label = labels[i].Length > 10 ? labels[i].Substring(0, 10) : labels[i];
                sb.AppendLine($"<text x=\"{x:F1}\" y=\"{height - 12:F1}\" font-size=\"9\" text-anchor=\"middle\" fill=\"#425466\">{HtmlEnc(label)}</text>");
            }

            sb.AppendLine($"<text x=\"8\" y=\"{yAt(upperControlLimit) - 4:F1}\" font-size=\"9\" fill=\"#c53030\">UCL {upperControlLimit:F4}</text>");
            sb.AppendLine($"<text x=\"8\" y=\"{yAt(centerLine) - 4:F1}\" font-size=\"9\" fill=\"#5b728a\">CL {centerLine:F4}</text>");
            sb.AppendLine($"<text x=\"8\" y=\"{yAt(lowerControlLimit) - 4:F1}\" font-size=\"9\" fill=\"#c53030\">LCL {lowerControlLimit:F4}</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private string BuildGrrReportCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Gauge R&R Report");
            sb.AppendLine($"Generated,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Study Name,{CsvEsc(_grrStudy.StudyName)}");
            sb.AppendLine($"Sequence,{CsvEsc(_grrStudy.SequenceName)}");
            sb.AppendLine($"Operators,{CsvEsc(string.Join("; ", _grrStudy.Operators))}");
            sb.AppendLine($"Replicates,{_grrStudy.Replicates}");
            sb.AppendLine($"Report Folder,{CsvEsc(_grrStudy.OutputFolder)}");
            sb.AppendLine($"Raw Data Root,{CsvEsc(_grrStudy.DataFolderRoot)}");
            sb.AppendLine();

            sb.AppendLine("Summary");
            sb.AppendLine("Group,Method,%GRR,%Repeatability,%Reproducibility,%Part-to-Part,%Tolerance,ndc,Verdict,Notes");
            foreach (GaugeRrGroupResult group in _grrResults)
            {
                AppendGrrSummaryCsvLine(sb, group.Plan.GroupName, group.Anova);
                AppendGrrSummaryCsvLine(sb, group.Plan.GroupName, group.Range);
            }
            sb.AppendLine();

            sb.AppendLine("Variance Components");
            sb.AppendLine("Group,Method,Component,Variance,StdDev,StudyVariation,%StudyVariation,%Tolerance");
            foreach (GaugeRrGroupResult group in _grrResults)
            {
                foreach (GaugeRrComponentRow row in group.Anova.Components)
                    sb.AppendLine($"{CsvEsc(group.Plan.GroupName)},{CsvEsc(group.Anova.MethodName)},{CsvEsc(row.Name)},{row.Variance:F6},{row.StandardDeviation:F6},{row.StudyVariation:F6},{row.PercentStudyVariation:F2},{row.PercentTolerance:F2}");
                foreach (GaugeRrComponentRow row in group.Range.Components)
                    sb.AppendLine($"{CsvEsc(group.Plan.GroupName)},{CsvEsc(group.Range.MethodName)},{CsvEsc(row.Name)},{row.Variance:F6},{row.StandardDeviation:F6},{row.StudyVariation:F6},{row.PercentStudyVariation:F2},{row.PercentTolerance:F2}");
            }
            sb.AppendLine();

            sb.AppendLine("Raw Measurements");
            sb.AppendLine("Group,TestType,Column,PartSerial,Operator,Replicate,Value,CsvPath");
            foreach (GaugeRrObservation observation in _grrStudy.Observations
                .OrderBy(obs => obs.GroupName)
                .ThenBy(obs => obs.PartSerial)
                .ThenBy(obs => obs.OperatorName)
                .ThenBy(obs => obs.ReplicateNo))
            {
                sb.AppendLine($"{CsvEsc(observation.GroupName)},{CsvEsc(observation.TestType.ToString())},{CsvEsc(observation.ColumnName)},{CsvEsc(observation.PartSerial)},{CsvEsc(observation.OperatorName)},{observation.ReplicateNo},{observation.Value.ToString("F6", CultureInfo.InvariantCulture)},{CsvEsc(observation.CsvPath)}");
            }

            return sb.ToString();
        }

        private void AppendGrrSummaryCsvLine(StringBuilder sb, string groupName, GaugeRrMethodResult method)
        {
            sb.AppendLine(
                $"{CsvEsc(groupName)},{CsvEsc(method.MethodName)},{method.GrrPctStudyVar:F2},{method.EVPctStudyVar:F2},{method.ReproPctStudyVar:F2},{method.PartPctStudyVar:F2},{method.GrrPctTolerance:F2},{method.Ndc:F0},{CsvEsc(method.Verdict)},{CsvEsc(method.Notes)}");
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Study";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            return sb.ToString().Trim();
        }

        private static double GetD2Constant(int n)
        {
            switch (n)
            {
                case 2: return 1.128;
                case 3: return 1.693;
                case 4: return 2.059;
                case 5: return 2.326;
                case 6: return 2.534;
                case 7: return 2.704;
                case 8: return 2.847;
                case 9: return 2.970;
                case 10: return 3.078;
                case 11: return 3.173;
                case 12: return 3.258;
                case 13: return 3.336;
                case 14: return 3.407;
                case 15: return 3.472;
                case 16: return 3.532;
                case 17: return 3.588;
                case 18: return 3.640;
                case 19: return 3.689;
                case 20: return 3.735;
                default: return n <= 2 ? 1.128 : 3.735 + ((n - 20) * 0.04);
            }
        }

        private static double GetA2Constant(int n)
        {
            switch (n)
            {
                case 2: return 1.880;
                case 3: return 1.023;
                case 4: return 0.729;
                case 5: return 0.577;
                case 6: return 0.483;
                case 7: return 0.419;
                case 8: return 0.373;
                case 9: return 0.337;
                case 10: return 0.308;
                default: return 0.0;
            }
        }

        private static double GetD3Constant(int n)
        {
            switch (n)
            {
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    return 0.0;
                case 7: return 0.076;
                case 8: return 0.136;
                case 9: return 0.184;
                case 10: return 0.223;
                default: return 0.0;
            }
        }

        private static double GetD4Constant(int n)
        {
            switch (n)
            {
                case 2: return 3.267;
                case 3: return 2.574;
                case 4: return 2.282;
                case 5: return 2.114;
                case 6: return 2.004;
                case 7: return 1.924;
                case 8: return 1.864;
                case 9: return 1.816;
                case 10: return 1.777;
                default: return 1.777;
            }
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
