using DuplexerFinalTest.Equipment;
using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public partial class CalibrationForm
    {
        private const int PathsPerSide = 12;
        private const int RemotePathOffset = 12;
        private const string PreferredCalibrationRoot = @"P:\MGunes\DuplexerTestSuite\Calibration";
        private const int SummaryPanelMinWidth = 320;

        private CalibrationWizardState _wizardState;
        private CalibrationWizardPage _currentPage = CalibrationWizardPage.Setup;
        private int _baseOutputIndex;
        private int _baseDriveIndex;
        private int _remoteOutputIndex;
        private int _remoteDriveIndex;
        private string _statusMessage = "Enter the calibration metadata to start the full-system wizard.";

        private TableLayoutPanel _rootLayout;
        private SplitContainer _mainSplit;
        private Label _lblStep;
        private Label _lblTitle;
        private Label _lblStatus;
        private Panel _contentPanel;
        private Label _lblSummaryHeader;
        private ListView _summaryList;
        private Button _btnPrevious;
        private Button _btnNext;
        private Button _btnSave;
        private Button _btnCancel;

        private TextBox _txtOperator;
        private DateTimePicker _dtpCalibrationDate;
        private TextBox _txtPowerMeterId;
        private TextBox _txtStandardBaseId;
        private TextBox _txtStandardRemoteId;
        private TextBox _txtEquipmentSummary;
        private TextBox _txtNotes;
        private Label _lblFolderPreview;

        private void InitializeWizard()
        {
            InitializeWizardState();
            BuildWizardShell();
            UpdateWizardPage();
            Load += CalibrationForm_Load;
            SizeChanged += CalibrationForm_SizeChanged;
            FormClosing += CalibrationForm_FormClosing;
        }

        private void InitializeWizardState()
        {
            _wizardState = new CalibrationWizardState
            {
                OperatorName = Shared.infoModel?.Operator ?? Environment.UserName,
                CalibrationDateTime = DateTime.Now,
                EquipmentSummary = BuildDefaultEquipmentSummary(),
                Notes = string.Empty,
                BaseStandardSlot = 1,
                RemoteStandardSlot = 1
            };

            for (int slot = 1; slot <= PathsPerSide; slot++)
            {
                _wizardState.BaseRecords.Add(new CalibrationSlotRecord(DUTType.Base, slot, slot));
                _wizardState.RemoteRecords.Add(new CalibrationSlotRecord(DUTType.Remote, slot, slot + RemotePathOffset));
            }
        }

        private void BuildWizardShell()
        {
            SuspendLayout();

            Text = "Calibration Wizard";
            MinimumSize = new Size(1200, 800);
            ClientSize = new Size(1320, 860);

            Controls.Clear();

            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18),
                BackColor = Color.WhiteSmoke
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Controls.Add(_rootLayout);

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _lblStep = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 0, 0, 4)
            };
            _lblTitle = new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };
            _lblStatus = new Label
            {
                AutoSize = true,
                ForeColor = Color.DarkSlateGray,
                MaximumSize = new Size(1000, 0)
            };

            headerLayout.Controls.Add(_lblStep, 0, 0);
            headerLayout.Controls.Add(_lblTitle, 0, 1);
            headerLayout.Controls.Add(_lblStatus, 0, 2);
            _rootLayout.Controls.Add(headerLayout, 0, 0);

            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel2,
                BackColor = Color.Gainsboro
            };

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(18)
            };
            _mainSplit.Panel1.Controls.Add(_contentPanel);

            var summaryBox = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Calibration Summary",
                Padding = new Padding(12)
            };
            _lblSummaryHeader = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 92,
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 0, 0, 8),
                TextAlign = ContentAlignment.TopLeft
            };
            _summaryList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                Font = new Font("Consolas", 9F)
            };
            _summaryList.Columns.Add("Path", 52);
            _summaryList.Columns.Add("Type", 68);
            _summaryList.Columns.Add("Slot", 48);
            _summaryList.Columns.Add("Out uA", 78);
            _summaryList.Columns.Add("Drive mA", 82);
            _summaryList.Columns.Add("W/A", 72);

            summaryBox.Controls.Add(_summaryList);
            summaryBox.Controls.Add(_lblSummaryHeader);
            _mainSplit.Panel2.Controls.Add(summaryBox);

            _rootLayout.Controls.Add(_mainSplit, 0, 1);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 0)
            };

            _btnCancel = CreateButton("Close");
            _btnCancel.Click += (sender, args) => Close();

            _btnSave = CreateButton("Save Package");
            _btnSave.Click += (sender, args) => ExecuteUiAction(SaveCalibrationPackage, "Save calibration package");

            _btnNext = CreateButton("Next Step");
            _btnNext.Click += (sender, args) => MoveNext();

            _btnPrevious = CreateButton("Previous");
            _btnPrevious.Click += (sender, args) => MovePrevious();

            footer.Controls.Add(_btnCancel);
            footer.Controls.Add(_btnSave);
            footer.Controls.Add(_btnNext);
            footer.Controls.Add(_btnPrevious);

            _rootLayout.Controls.Add(footer, 0, 2);

            ResumeLayout(true);
        }

        private void UpdateWizardPage()
        {
            if (_contentPanel == null)
                return;

            PersistSetupValues();

            _contentPanel.SuspendLayout();
            _contentPanel.Controls.Clear();

            _lblStep.Text = $"Step {((int)_currentPage) + 1} of {Enum.GetValues(typeof(CalibrationWizardPage)).Length}";
            _lblTitle.Text = GetPageTitle(_currentPage);
            _lblStatus.Text = _statusMessage;

            switch (_currentPage)
            {
                case CalibrationWizardPage.Setup:
                    BuildSetupPage();
                    break;
                case CalibrationWizardPage.BaseStandardPower:
                    BuildStandardPowerPage(true);
                    break;
                case CalibrationWizardPage.BaseOutputCurrent:
                    BuildOutputCurrentPage(true);
                    break;
                case CalibrationWizardPage.BaseDriveCurrent:
                    BuildDriveCurrentPage(true);
                    break;
                case CalibrationWizardPage.RemoteStandardPower:
                    BuildStandardPowerPage(false);
                    break;
                case CalibrationWizardPage.RemoteOutputCurrent:
                    BuildOutputCurrentPage(false);
                    break;
                case CalibrationWizardPage.RemoteDriveCurrent:
                    BuildDriveCurrentPage(false);
                    break;
                case CalibrationWizardPage.Review:
                    BuildReviewPage();
                    break;
            }

            RefreshSummary();
            UpdateNavigationButtons();

            _contentPanel.ResumeLayout(true);
        }

        private void BuildSetupPage()
        {
            EnsureSetupControlsCreated();

            _txtOperator.Text = _wizardState.OperatorName ?? string.Empty;
            _dtpCalibrationDate.Value = _wizardState.CalibrationDateTime;
            _txtPowerMeterId.Text = _wizardState.PowerMeterId ?? string.Empty;
            _txtStandardBaseId.Text = _wizardState.StandardBaseId ?? string.Empty;
            _txtStandardRemoteId.Text = _wizardState.StandardRemoteId ?? string.Empty;
            _txtEquipmentSummary.Text = _wizardState.EquipmentSummary ?? string.Empty;
            _txtNotes.Text = _wizardState.Notes ?? string.Empty;
            UpdateFolderPreview();

            var page = CreatePageLayout();
            AddPageRow(page, CreateBodyLabel(
                "This wizard runs the official full-system calibration for Base paths 1-12 and Remote paths 13-24. It writes a dated calibration package to the calibration share and refreshes the active .cal snapshot used by the software."));

            var metadataBox = CreateGroupBox("Session Metadata");
            var metadataLayout = CreateFieldLayout();
            AddField(metadataLayout, "Operator", _txtOperator);
            AddField(metadataLayout, "Calibration date and time", _dtpCalibrationDate);
            AddField(metadataLayout, "Optical power meter ID", _txtPowerMeterId);
            AddField(metadataLayout, "Standard Base ID", _txtStandardBaseId);
            AddField(metadataLayout, "Standard Remote ID", _txtStandardRemoteId);
            AddField(metadataLayout, "Equipment serial / TPE info", _txtEquipmentSummary, true);
            AddField(metadataLayout, "Notes", _txtNotes, true);
            metadataBox.Controls.Add(metadataLayout);
            AddPageRow(page, metadataBox);

            var outputBox = CreateGroupBox("Output Package");
            var outputLayout = CreatePageLayout();
            AddPageRow(outputLayout, CreateBodyLabel("The package includes the generated .cal file, a CSV of raw and computed values, an HTML report, and a JSON audit snapshot."));
            AddPageRow(outputLayout, _lblFolderPreview);
            outputBox.Controls.Add(outputLayout);
            AddPageRow(page, outputBox);

            _contentPanel.Controls.Add(page);
        }

        private void BuildStandardPowerPage(bool isBase)
        {
            var page = CreatePageLayout();
            string unitName = isBase ? "Base" : "Remote";
            string wavelength = isBase ? "850 nm" : "975 nm";
            double sourceMilliAmp = isBase ? 35.0d : 3.5d;

            AddPageRow(page, CreateBodyLabel(
                $"Place the Standard {unitName} in a convenient jig position, connect the output to the optical power meter, set the power meter wavelength to {wavelength}, and let the wizard bias the selected slot. Enter the power meter reading manually after the bias is applied."));

            decimal slotValue = (decimal)(isBase ? _wizardState.BaseStandardSlot : _wizardState.RemoteStandardSlot);
            decimal powerValue = (decimal)(isBase ? (_wizardState.BaseStandardPowerMw ?? 0.0d) : (_wizardState.RemoteStandardPowerMw ?? 0.0d));

            var nudSlot = new NumericUpDown
            {
                Minimum = 1,
                Maximum = PathsPerSide,
                Value = slotValue,
                Width = 120
            };

            var nudPower = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 3,
                Increment = 0.001m,
                Value = powerValue,
                Width = 120
            };

            var feedbackLabel = CreateBodyLabel(
                $"Target bias: {sourceMilliAmp:F1} mA. Recorded value: {FormatOptional(isBase ? _wizardState.BaseStandardPowerMw : _wizardState.RemoteStandardPowerMw, "F3", "not recorded yet")} mW.");

            var formBox = CreateGroupBox($"Standard {unitName} power");
            var formLayout = CreatePageLayout();
            AddPageRow(formLayout, CreateInlineField($"Selected {unitName} jig slot", nudSlot));
            AddPageRow(formLayout, CreateInlineField("Measured optical power (mW)", nudPower));

            var buttonRow = CreateButtonRow();
            var btnApply = CreateButton("Route And Apply Bias");
            btnApply.Click += (sender, args) => ExecuteUiAction(() =>
            {
                MeasurementPoint point = isBase
                    ? ApplyBaseStandardBias((int)nudSlot.Value)
                    : ApplyRemoteStandardBias((int)nudSlot.Value);
                feedbackLabel.Text =
                    $"Bias active on {unitName} slot {(int)nudSlot.Value} (path {GetPathIndex(isBase ? DUTType.Base : DUTType.Remote, (int)nudSlot.Value)}). Electrical readback: {point.CurrentAmps * 1000.0d:F3} mA, {point.VoltageVolts:F3} V. Enter the power meter reading and click Record.";
            }, $"Apply {unitName} standard bias");

            var btnOff = CreateButton("Switch Outputs Off");
            btnOff.Click += (sender, args) => ExecuteUiAction(() =>
            {
                SafeShutdownOutputs();
                feedbackLabel.Text = "Outputs switched off.";
                SetStatus("Outputs switched off.");
            }, "Switch outputs off");

            var btnRecord = CreateButton("Record Value");
            btnRecord.Click += (sender, args) =>
            {
                double power = (double)nudPower.Value;
                if (power <= 0.0d)
                {
                    MessageBox.Show(this, "Enter the measured optical power before recording this step.", "Missing Value",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (isBase)
                {
                    _wizardState.BaseStandardSlot = (int)nudSlot.Value;
                    _wizardState.BaseStandardPowerMw = power;
                }
                else
                {
                    _wizardState.RemoteStandardSlot = (int)nudSlot.Value;
                    _wizardState.RemoteStandardPowerMw = power;
                }

                feedbackLabel.Text = $"Recorded {power:F3} mW for Standard {unitName} at slot {(int)nudSlot.Value}.";
                SetStatus($"Standard {unitName} optical power recorded: {power:F3} mW.");
                RefreshSummary();
                UpdateNavigationButtons();
            };

            buttonRow.Controls.Add(btnApply);
            buttonRow.Controls.Add(btnOff);
            buttonRow.Controls.Add(btnRecord);
            AddPageRow(formLayout, buttonRow);
            AddPageRow(formLayout, feedbackLabel);

            formBox.Controls.Add(formLayout);
            AddPageRow(page, formBox);

            _contentPanel.Controls.Add(page);
        }

        private void BuildOutputCurrentPage(bool isBase)
        {
            var page = CreatePageLayout();
            string unitName = isBase ? "Base" : "Remote";
            double targetMilliAmp = isBase ? 35.0d : 3.5d;
            List<CalibrationSlotRecord> records = isBase ? _wizardState.BaseRecords : _wizardState.RemoteRecords;
            int currentIndex = isBase ? _baseOutputIndex : _remoteOutputIndex;

            AddPageRow(page, CreateBodyLabel(
                $"Connect the Standard {unitName} output to the active path and let the wizard run the existing measurement path automatically. The recorded calibration current comes from the sweep point closest to {targetMilliAmp:F1} mA and feeds the W/A calculation for the generated .cal file."));

            if (currentIndex >= records.Count)
            {
                AddPageRow(page, CreateCompletionBox(
                    $"All {unitName.ToLowerInvariant()} output-current measurements are recorded.",
                    records,
                    isBase,
                    false));
                _contentPanel.Controls.Add(page);
                return;
            }

            CalibrationSlotRecord record = records[currentIndex];
            var stepBox = CreateGroupBox($"{unitName} output current: path {record.PathIndex}");
            var stepLayout = CreatePageLayout();

            AddPageRow(stepLayout, CreateBodyLabel(
                $"Prepare Standard {unitName} for slot {record.Slot}. When the path is ready, click Measure Current. The wizard records the electrical current automatically and then advances to the next path."));

            string recordedText = record.OutputCurrentMicroAmp.HasValue
                ? $"Last recorded current: {record.OutputCurrentMicroAmp.Value:F3} uA."
                : "No current has been recorded for this path yet.";
            var feedbackLabel = CreateBodyLabel(recordedText);

            var buttonRow = CreateButtonRow();
            var btnMeasure = CreateButton($"Measure Path {record.PathIndex}");
            btnMeasure.Click += (sender, args) => ExecuteUiAction(() =>
            {
                double outputCurrent = isBase
                    ? MeasureBaseOutputCurrentMicroAmps(record.Slot)
                    : MeasureRemoteOutputCurrentMicroAmps(record.Slot);

                record.OutputCurrentMicroAmp = outputCurrent;
                if (isBase)
                    _baseOutputIndex++;
                else
                    _remoteOutputIndex++;

                SetStatus($"{unitName} output current captured for path {record.PathIndex}: {outputCurrent:F3} uA.");
                UpdateWizardPage();
            }, $"Measure {unitName} output current");
            buttonRow.Controls.Add(btnMeasure);

            AddPageRow(stepLayout, buttonRow);
            AddPageRow(stepLayout, feedbackLabel);
            stepBox.Controls.Add(stepLayout);
            AddPageRow(page, stepBox);

            _contentPanel.Controls.Add(page);
        }

        private void BuildDriveCurrentPage(bool isBase)
        {
            var page = CreatePageLayout();
            string unitName = isBase ? "Base" : "Remote";
            double targetPower = isBase ? 10.0d : 1.0d;
            double tolerance = isBase ? 0.1d : 0.01d;
            double defaultCurrent = isBase ? 20.0d : 2.0d;
            List<CalibrationSlotRecord> records = isBase ? _wizardState.BaseRecords : _wizardState.RemoteRecords;
            int currentIndex = isBase ? _baseDriveIndex : _remoteDriveIndex;

            AddPageRow(page, CreateBodyLabel(
                $"Connect the optical power meter to the active {unitName.ToLowerInvariant()} output. Use Apply Current to bias the path, adjust the setpoint until the power meter reads {targetPower:F1} mW +/- {tolerance:F2}, then record the final current and observed optical power."));

            if (currentIndex >= records.Count)
            {
                AddPageRow(page, CreateCompletionBox(
                    $"All {unitName.ToLowerInvariant()} drive-current measurements are recorded.",
                    records,
                    isBase,
                    true));
                _contentPanel.Controls.Add(page);
                return;
            }

            CalibrationSlotRecord record = records[currentIndex];
            decimal setpoint = (decimal)(record.DriveCurrentMilliAmp ?? defaultCurrent);
            decimal observedPower = (decimal)(record.ObservedPowerMilliWatt ?? targetPower);

            var stepBox = CreateGroupBox($"{unitName} drive current: path {record.PathIndex}");
            var stepLayout = CreatePageLayout();
            AddPageRow(stepLayout, CreateBodyLabel(
                $"Prepare {unitName} slot {record.Slot}. Apply a current, watch the power meter, and iterate until the observed output reaches the target. Record the final source current after the power meter is stable."));

            var nudCurrent = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 2,
                Increment = isBase ? 0.10m : 0.01m,
                Value = setpoint,
                Width = 120
            };
            var nudPower = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 3,
                Increment = 0.001m,
                Value = observedPower,
                Width = 120
            };
            var feedbackLabel = CreateBodyLabel(
                record.DriveCurrentMilliAmp.HasValue
                    ? $"Last recorded current: {record.DriveCurrentMilliAmp.Value:F2} mA at {record.ObservedPowerMilliWatt.GetValueOrDefault(targetPower):F3} mW."
                    : "No drive-current value has been recorded for this path yet.");

            AddPageRow(stepLayout, CreateInlineField("Current setpoint (mA)", nudCurrent));
            AddPageRow(stepLayout, CreateInlineField("Observed optical power (mW)", nudPower));

            var buttonRow = CreateButtonRow();
            var btnApply = CreateButton("Apply Current");
            btnApply.Click += (sender, args) => ExecuteUiAction(() =>
            {
                MeasurementPoint point = isBase
                    ? ApplyBaseDriveCurrent(record.Slot, (double)nudCurrent.Value)
                    : ApplyRemoteDriveCurrent(record.Slot, (double)nudCurrent.Value);
                feedbackLabel.Text =
                    $"Path {record.PathIndex} is biased at {(double)nudCurrent.Value:F2} mA. Electrical readback: {point.CurrentAmps * 1000.0d:F3} mA, {point.VoltageVolts:F3} V. Adjust the setpoint until the power meter reaches the target, then record the value.";
            }, $"Apply {unitName} drive current");

            var btnOff = CreateButton("Switch Outputs Off");
            btnOff.Click += (sender, args) => ExecuteUiAction(() =>
            {
                SafeShutdownOutputs();
                feedbackLabel.Text = "Outputs switched off.";
                SetStatus("Outputs switched off.");
            }, "Switch outputs off");

            var btnRecord = CreateButton("Record And Next Path");
            btnRecord.Click += (sender, args) =>
            {
                double current = (double)nudCurrent.Value;
                double observed = (double)nudPower.Value;
                if (current <= 0.0d)
                {
                    MessageBox.Show(this, "Enter the final current before recording this path.", "Missing Value",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                record.DriveCurrentMilliAmp = current;
                record.ObservedPowerMilliWatt = observed;
                SafeShutdownOutputs();

                if (isBase)
                    _baseDriveIndex++;
                else
                    _remoteDriveIndex++;

                SetStatus($"{unitName} drive current recorded for path {record.PathIndex}: {current:F2} mA at {observed:F3} mW.");
                UpdateWizardPage();
            };

            buttonRow.Controls.Add(btnApply);
            buttonRow.Controls.Add(btnOff);
            buttonRow.Controls.Add(btnRecord);
            AddPageRow(stepLayout, buttonRow);
            AddPageRow(stepLayout, feedbackLabel);

            stepBox.Controls.Add(stepLayout);
            AddPageRow(page, stepBox);

            _contentPanel.Controls.Add(page);
        }

        private void BuildReviewPage()
        {
            var page = CreatePageLayout();
            AddPageRow(page, CreateBodyLabel(
                "Review the final package contents below. Save Package writes the .cal file, raw/computed CSV, HTML report, and JSON audit file into the dated calibration folder, then reloads the latest calibration snapshot for runtime use."));

            var packageBox = CreateGroupBox("Package Overview");
            var packageLayout = CreatePageLayout();
            AddPageRow(packageLayout, CreateBodyLabel(
                $"Output folder: {Path.Combine(PreferredCalibrationRoot, _wizardState.CalibrationDateTime.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture))}"));
            AddPageRow(packageLayout, CreateBodyLabel(
                $"Metadata: Operator={_wizardState.OperatorName}, Meter={_wizardState.PowerMeterId}, Standard Base={_wizardState.StandardBaseId}, Standard Remote={_wizardState.StandardRemoteId}"));
            AddPageRow(packageLayout, CreateBodyLabel(
                $"Recorded values: Base output {CountRecorded(_wizardState.BaseRecords, false)}/{PathsPerSide}, Base drive {CountRecorded(_wizardState.BaseRecords, true)}/{PathsPerSide}, Remote output {CountRecorded(_wizardState.RemoteRecords, false)}/{PathsPerSide}, Remote drive {CountRecorded(_wizardState.RemoteRecords, true)}/{PathsPerSide}."));
            packageBox.Controls.Add(packageLayout);
            AddPageRow(page, packageBox);

            var previewBox = CreateGroupBox(".cal Preview");
            var txtPreview = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
                Width = GetContentWidth() - 24,
                Height = 430,
                Text = BuildCalibrationFileContents()
            };
            previewBox.Controls.Add(txtPreview);
            AddPageRow(page, previewBox);

            _contentPanel.Controls.Add(page);
        }

        private void MoveNext()
        {
            PersistSetupValues();

            if (!ValidateCurrentPage())
                return;

            if (_currentPage < CalibrationWizardPage.Review)
            {
                _currentPage++;
                UpdateWizardPage();
            }
        }

        private void MovePrevious()
        {
            if (_currentPage > CalibrationWizardPage.Setup)
            {
                _currentPage--;
                UpdateWizardPage();
            }
        }

        private bool ValidateCurrentPage()
        {
            switch (_currentPage)
            {
                case CalibrationWizardPage.Setup:
                    if (!HasRequiredMetadata())
                    {
                        MessageBox.Show(this,
                            "Enter the operator, power meter ID, Standard Base ID, and Standard Remote ID before continuing.",
                            "Missing Metadata",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;

                case CalibrationWizardPage.BaseStandardPower:
                    return RequireRecordedValue(_wizardState.BaseStandardPowerMw, "Record the Standard Base optical power before continuing.");

                case CalibrationWizardPage.BaseOutputCurrent:
                    return RequireStageComplete(_wizardState.BaseRecords.All(record => record.OutputCurrentMicroAmp.HasValue),
                        "Measure all Base output-current paths before continuing.");

                case CalibrationWizardPage.BaseDriveCurrent:
                    return RequireStageComplete(_wizardState.BaseRecords.All(record => record.DriveCurrentMilliAmp.HasValue),
                        "Record all Base drive-current paths before continuing.");

                case CalibrationWizardPage.RemoteStandardPower:
                    return RequireRecordedValue(_wizardState.RemoteStandardPowerMw, "Record the Standard Remote optical power before continuing.");

                case CalibrationWizardPage.RemoteOutputCurrent:
                    return RequireStageComplete(_wizardState.RemoteRecords.All(record => record.OutputCurrentMicroAmp.HasValue),
                        "Measure all Remote output-current paths before continuing.");

                case CalibrationWizardPage.RemoteDriveCurrent:
                    return RequireStageComplete(_wizardState.RemoteRecords.All(record => record.DriveCurrentMilliAmp.HasValue),
                        "Record all Remote drive-current paths before continuing.");

                default:
                    return true;
            }
        }

        private void UpdateNavigationButtons()
        {
            if (_btnPrevious == null)
                return;

            _btnPrevious.Enabled = _currentPage != CalibrationWizardPage.Setup;
            _btnNext.Visible = _currentPage != CalibrationWizardPage.Review;
            _btnSave.Visible = _currentPage == CalibrationWizardPage.Review;

            if (_currentPage == CalibrationWizardPage.Review)
                _btnSave.Enabled = HasRequiredMetadata() && IsCalibrationComplete();
            else
                _btnNext.Enabled = IsCurrentPageReady();
        }

        private bool IsCurrentPageReady()
        {
            switch (_currentPage)
            {
                case CalibrationWizardPage.Setup:
                    return HasRequiredMetadata();
                case CalibrationWizardPage.BaseStandardPower:
                    return _wizardState.BaseStandardPowerMw.HasValue;
                case CalibrationWizardPage.BaseOutputCurrent:
                    return _wizardState.BaseRecords.All(record => record.OutputCurrentMicroAmp.HasValue);
                case CalibrationWizardPage.BaseDriveCurrent:
                    return _wizardState.BaseRecords.All(record => record.DriveCurrentMilliAmp.HasValue);
                case CalibrationWizardPage.RemoteStandardPower:
                    return _wizardState.RemoteStandardPowerMw.HasValue;
                case CalibrationWizardPage.RemoteOutputCurrent:
                    return _wizardState.RemoteRecords.All(record => record.OutputCurrentMicroAmp.HasValue);
                case CalibrationWizardPage.RemoteDriveCurrent:
                    return _wizardState.RemoteRecords.All(record => record.DriveCurrentMilliAmp.HasValue);
                case CalibrationWizardPage.Review:
                    return IsCalibrationComplete();
                default:
                    return false;
            }
        }

        private void RefreshSummary()
        {
            if (_summaryList == null)
                return;

            _summaryList.BeginUpdate();
            _summaryList.Items.Clear();

            foreach (CalibrationSlotRecord record in GetAllRecords().OrderBy(row => row.PathIndex))
            {
                double? standardPower = GetStandardPower(record.DutType);
                double? waValue = record.GetWattPerAmp(standardPower);

                var item = new ListViewItem(record.PathIndex.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(record.DutType == DUTType.Base ? "Base" : "Remote");
                item.SubItems.Add(record.Slot.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(FormatOptional(record.OutputCurrentMicroAmp, "F2", "-"));
                item.SubItems.Add(FormatOptional(record.DriveCurrentMilliAmp, "F2", "-"));
                item.SubItems.Add(FormatOptional(waValue, "F1", "-"));
                item.ForeColor = record.IsComplete ? Color.Black : Color.DimGray;
                _summaryList.Items.Add(item);
            }

            _summaryList.EndUpdate();

            _lblSummaryHeader.Text =
                $"Base standard power: {FormatOptional(_wizardState.BaseStandardPowerMw, "F3", "not recorded")} mW{Environment.NewLine}" +
                $"Remote standard power: {FormatOptional(_wizardState.RemoteStandardPowerMw, "F3", "not recorded")} mW{Environment.NewLine}" +
                $"Base output {CountRecorded(_wizardState.BaseRecords, false)}/{PathsPerSide}, Base drive {CountRecorded(_wizardState.BaseRecords, true)}/{PathsPerSide}, Remote output {CountRecorded(_wizardState.RemoteRecords, false)}/{PathsPerSide}, Remote drive {CountRecorded(_wizardState.RemoteRecords, true)}/{PathsPerSide}";
        }

        private int CountRecorded(IEnumerable<CalibrationSlotRecord> records, bool drive)
        {
            return drive
                ? records.Count(record => record.DriveCurrentMilliAmp.HasValue)
                : records.Count(record => record.OutputCurrentMicroAmp.HasValue);
        }

        private void EnsureSetupControlsCreated()
        {
            if (_txtOperator != null)
                return;

            _txtOperator = new TextBox { Width = 320 };
            _txtPowerMeterId = new TextBox { Width = 320 };
            _txtStandardBaseId = new TextBox { Width = 320 };
            _txtStandardRemoteId = new TextBox { Width = 320 };
            _txtEquipmentSummary = new TextBox
            {
                Width = 460,
                Height = 110,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _txtNotes = new TextBox
            {
                Width = 460,
                Height = 110,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _dtpCalibrationDate = new DateTimePicker
            {
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                ShowUpDown = true
            };
            _lblFolderPreview = CreateBodyLabel(string.Empty);

            _txtOperator.TextChanged += SetupMetadataChanged;
            _txtPowerMeterId.TextChanged += SetupMetadataChanged;
            _txtStandardBaseId.TextChanged += SetupMetadataChanged;
            _txtStandardRemoteId.TextChanged += SetupMetadataChanged;
            _txtEquipmentSummary.TextChanged += SetupMetadataChanged;
            _txtNotes.TextChanged += SetupMetadataChanged;
            _dtpCalibrationDate.ValueChanged += SetupMetadataChanged;
        }

        private void SetupMetadataChanged(object sender, EventArgs e)
        {
            PersistSetupValues();
            UpdateFolderPreview();
            RefreshSummary();
            UpdateNavigationButtons();
        }

        private void PersistSetupValues()
        {
            if (_wizardState == null || _txtOperator == null)
                return;

            _wizardState.OperatorName = (_txtOperator.Text ?? string.Empty).Trim();
            _wizardState.CalibrationDateTime = _dtpCalibrationDate.Value;
            _wizardState.PowerMeterId = (_txtPowerMeterId.Text ?? string.Empty).Trim();
            _wizardState.StandardBaseId = (_txtStandardBaseId.Text ?? string.Empty).Trim();
            _wizardState.StandardRemoteId = (_txtStandardRemoteId.Text ?? string.Empty).Trim();
            _wizardState.EquipmentSummary = _txtEquipmentSummary.Text ?? string.Empty;
            _wizardState.Notes = _txtNotes.Text ?? string.Empty;
        }

        private void UpdateFolderPreview()
        {
            if (_lblFolderPreview == null || _wizardState == null)
                return;

            string folder = Path.Combine(PreferredCalibrationRoot,
                _wizardState.CalibrationDateTime.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture));
            _lblFolderPreview.Text = $"Package folder: {folder}";
        }

        private string GetPageTitle(CalibrationWizardPage page)
        {
            switch (page)
            {
                case CalibrationWizardPage.Setup:
                    return "Session Setup";
                case CalibrationWizardPage.BaseStandardPower:
                    return "Base Standard Power";
                case CalibrationWizardPage.BaseOutputCurrent:
                    return "Base Output Current";
                case CalibrationWizardPage.BaseDriveCurrent:
                    return "Base Drive Current";
                case CalibrationWizardPage.RemoteStandardPower:
                    return "Remote Standard Power";
                case CalibrationWizardPage.RemoteOutputCurrent:
                    return "Remote Output Current";
                case CalibrationWizardPage.RemoteDriveCurrent:
                    return "Remote Drive Current";
                case CalibrationWizardPage.Review:
                    return "Review And Save";
                default:
                    return "Calibration Wizard";
            }
        }

        private string BuildDefaultEquipmentSummary()
        {
            GeneralSetting settings = Shared.sharedGeneralSettings?.GeneralSettings?.FirstOrDefault();
            if (settings == null)
                return string.Empty;

            var lines = new List<string>
            {
                $"PC: {settings.PC_NAME}",
                $"SMU Master: {settings.SMU_MASTER_RESOURCE}",
                $"SMU Slave: {settings.SMU_SLAVE_RESOURCE}",
                $"Base optical 1x13: {settings.OPTICAL_SWITCH1x13_1_RESOURCE}",
                $"Remote optical 1x13: {settings.OPTICAL_SWITCH1x13_2_RESOURCE}",
                $"Optical 1x4: {settings.OPTICAL_SWITCH1x4_1_RESOURCE}",
                $"Base electrical banks: {settings.ELECTRICAL_SWITCH1_RESOURCE}, {settings.ELECTRICAL_SWITCH2_RESOURCE}, {settings.ELECTRICAL_SWITCH3_RESOURCE}",
                $"Remote electrical banks: {settings.ELECTRICAL_SWITCH4_RESOURCE}, {settings.ELECTRICAL_SWITCH5_RESOURCE}, {settings.ELECTRICAL_SWITCH6_RESOURCE}"
            };
            return string.Join(Environment.NewLine, lines);
        }

        private void ExecuteUiAction(Action action, string activity)
        {
            UseWaitCursor = true;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                SafeShutdownOutputs();
                Shared.logger?.LogError(activity, ex);
                SetStatus($"{activity} failed: {ex.Message}");
                MessageBox.Show(this, ex.Message, "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void SaveCalibrationPackage()
        {
            PersistSetupValues();

            if (!HasRequiredMetadata())
                throw new InvalidOperationException("Calibration metadata is incomplete.");

            if (!IsCalibrationComplete())
                throw new InvalidOperationException("The calibration wizard is not complete yet.");

            string folderPath = Path.Combine(PreferredCalibrationRoot,
                _wizardState.CalibrationDateTime.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folderPath);

            string timeStamp = _wizardState.CalibrationDateTime.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string calPath = Path.Combine(folderPath, timeStamp + ".cal");
            string csvPath = Path.Combine(folderPath, timeStamp + "_CalibrationData.csv");
            string htmlPath = Path.Combine(folderPath, timeStamp + "_CalibrationReport.html");
            string jsonPath = Path.Combine(folderPath, timeStamp + "_CalibrationAudit.json");

            File.WriteAllText(calPath, BuildCalibrationFileContents(), Encoding.ASCII);
            File.WriteAllText(csvPath, BuildCalibrationCsv(), Encoding.ASCII);
            File.WriteAllText(htmlPath, BuildCalibrationHtml(), Encoding.UTF8);
            File.WriteAllText(jsonPath, BuildCalibrationAuditJson(folderPath, calPath, csvPath, htmlPath), Encoding.UTF8);

            Shared.infoModel.Operator = _wizardState.OperatorName;
            Shared.calibrationModel = Shared.LoadLatestCalibrationModel(null, PreferredCalibrationRoot);
            SetStatus($"Calibration package saved to {folderPath}.");
            Shared.logger?.Log($"Calibration package saved: {folderPath}", MessageType.Success);

            MessageBox.Show(this,
                $"Calibration package saved successfully.{Environment.NewLine}{folderPath}",
                "Calibration Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private string BuildCalibrationFileContents()
        {
            var sb = new StringBuilder();
            DateTime stamp = _wizardState.CalibrationDateTime;

            sb.AppendLine("! Calibration File, Automatically created from calibration spreadsheet");
            sb.AppendLine("!");
            sb.AppendLine("[Calibration status]");
            sb.AppendLine($"last_cal_dateTime = \"{stamp:dd/MM/yyyy} at {stamp:HH:mm}\"");
            sb.AppendLine();

            sb.AppendLine("! Calibration of optical power to electrical power via ISphere");
            sb.AppendLine("! this is for BASE 850nm");
            sb.AppendLine("! Watts /Amp");
            sb.AppendLine("[Z-IB-IOP]");
            AppendPathValues(sb, TestSequences.Base_Z_IB_IOP);
            sb.AppendLine();

            sb.AppendLine("! Remote drive current lookup point in A equivalent to 1mW");
            sb.AppendLine("[Z-IPD]");
            AppendPathValues(sb, TestSequences.Base_Z_IPD);
            sb.AppendLine();

            sb.AppendLine("! Calibration of optical power to electrical power via ISphere");
            sb.AppendLine("! this is for Remote 975nm");
            sb.AppendLine("! Watts /Amp");
            sb.AppendLine("[Z-IOP]");
            AppendPathValues(sb, TestSequences.Remote_Z_IOP);
            sb.AppendLine();

            sb.AppendLine("! Base drive current lookup point in A equivalent to 10mW");
            sb.AppendLine("[Z-VPV]");
            AppendPathValues(sb, TestSequences.Remote_Z_VPV);

            return sb.ToString();
        }

        private void AppendPathValues(StringBuilder sb, TestSequences sequence)
        {
            for (int pathIndex = 1; pathIndex <= PathsPerSide * 2; pathIndex++)
            {
                string valueText;
                switch (sequence)
                {
                    case TestSequences.Base_Z_IB_IOP:
                        CalibrationSlotRecord baseOptical = _wizardState.BaseRecords.FirstOrDefault(record => record.PathIndex == pathIndex);
                        double? baseWa = baseOptical?.GetWattPerAmp(_wizardState.BaseStandardPowerMw);
                        valueText = baseWa.HasValue ? Math.Round(baseWa.Value, 1).ToString("0.0", CultureInfo.InvariantCulture) : "1";
                        break;

                    case TestSequences.Base_Z_IPD:
                        CalibrationSlotRecord baseDrive = _wizardState.BaseRecords.FirstOrDefault(record => record.PathIndex == pathIndex);
                        valueText = baseDrive?.DriveCurrentMilliAmp.HasValue == true
                            ? (baseDrive.DriveCurrentMilliAmp.Value / 1000.0d).ToString("0.00E+0", CultureInfo.InvariantCulture)
                            : "1";
                        break;

                    case TestSequences.Remote_Z_IOP:
                        CalibrationSlotRecord remoteOptical = _wizardState.RemoteRecords.FirstOrDefault(record => record.PathIndex == pathIndex);
                        double? remoteWa = remoteOptical?.GetWattPerAmp(_wizardState.RemoteStandardPowerMw);
                        valueText = remoteWa.HasValue ? Math.Round(remoteWa.Value, 1).ToString("0.0", CultureInfo.InvariantCulture) : "1";
                        break;

                    case TestSequences.Remote_Z_VPV:
                        CalibrationSlotRecord remoteDrive = _wizardState.RemoteRecords.FirstOrDefault(record => record.PathIndex == pathIndex);
                        valueText = remoteDrive?.DriveCurrentMilliAmp.HasValue == true
                            ? (remoteDrive.DriveCurrentMilliAmp.Value / 1000.0d).ToString("0.00E+0", CultureInfo.InvariantCulture)
                            : "1";
                        break;

                    default:
                        valueText = "1";
                        break;
                }

                sb.AppendLine($"path{pathIndex}\t= {valueText}");
            }
        }

        private string BuildCalibrationCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Path,DUT Type,Internal Slot,Standard Power (mW),Output Current (uA),Drive Current (mA),Drive Current (A),Observed Optical Power (mW),W/A");

            foreach (CalibrationSlotRecord record in GetAllRecords().OrderBy(row => row.PathIndex))
            {
                double? standardPower = GetStandardPower(record.DutType);
                double? waValue = record.GetWattPerAmp(standardPower);
                sb.AppendLine(string.Join(",",
                    record.PathIndex.ToString(CultureInfo.InvariantCulture),
                    record.DutType == DUTType.Base ? "Base" : "Remote",
                    record.Slot.ToString(CultureInfo.InvariantCulture),
                    CsvValue(standardPower),
                    CsvValue(record.OutputCurrentMicroAmp),
                    CsvValue(record.DriveCurrentMilliAmp),
                    CsvValue(record.DriveCurrentMilliAmp.HasValue ? record.DriveCurrentMilliAmp.Value / 1000.0d : (double?)null),
                    CsvValue(record.ObservedPowerMilliWatt),
                    CsvValue(waValue)));
            }

            return sb.ToString();
        }

        private string BuildCalibrationHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8' />");
            sb.AppendLine("<title>Calibration Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; margin: 24px; color: #1e293b; background: #f8fafc; }");
            sb.AppendLine("h1, h2 { color: #0f172a; }");
            sb.AppendLine(".card { background: #ffffff; border: 1px solid #dbe4ee; border-radius: 10px; padding: 18px; margin-bottom: 18px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 12px; }");
            sb.AppendLine("th, td { border: 1px solid #dbe4ee; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background: #e2e8f0; }");
            sb.AppendLine(".muted { color: #64748b; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h1>Calibration Report</h1>");

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>Metadata</h2>");
            sb.AppendLine($"<p><strong>Operator:</strong> {Encode(_wizardState.OperatorName)}<br />");
            sb.AppendLine($"<strong>Calibration date:</strong> {Encode(_wizardState.CalibrationDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}<br />");
            sb.AppendLine($"<strong>Power meter ID:</strong> {Encode(_wizardState.PowerMeterId)}<br />");
            sb.AppendLine($"<strong>Standard Base ID:</strong> {Encode(_wizardState.StandardBaseId)}<br />");
            sb.AppendLine($"<strong>Standard Remote ID:</strong> {Encode(_wizardState.StandardRemoteId)}</p>");
            sb.AppendLine($"<p class='muted'><strong>Equipment / TPE info:</strong><br />{Encode(_wizardState.EquipmentSummary).Replace(Environment.NewLine, "<br />")}</p>");
            if (!string.IsNullOrWhiteSpace(_wizardState.Notes))
                sb.AppendLine($"<p class='muted'><strong>Notes:</strong><br />{Encode(_wizardState.Notes).Replace(Environment.NewLine, "<br />")}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>Reference Powers</h2>");
            sb.AppendLine($"<p><strong>Base standard power:</strong> {FormatOptional(_wizardState.BaseStandardPowerMw, "F3", "n/a")} mW<br />");
            sb.AppendLine($"<strong>Remote standard power:</strong> {FormatOptional(_wizardState.RemoteStandardPowerMw, "F3", "n/a")} mW</p>");
            sb.AppendLine("</div>");

            AppendHtmlTable(sb, "Base Paths 1-12", _wizardState.BaseRecords, _wizardState.BaseStandardPowerMw);
            AppendHtmlTable(sb, "Remote Paths 13-24", _wizardState.RemoteRecords, _wizardState.RemoteStandardPowerMw);

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private void AppendHtmlTable(StringBuilder sb, string title, IEnumerable<CalibrationSlotRecord> records, double? standardPower)
        {
            sb.AppendLine("<div class='card'>");
            sb.AppendLine($"<h2>{Encode(title)}</h2>");
            sb.AppendLine("<table><thead><tr><th>Path</th><th>Slot</th><th>Output Current (uA)</th><th>Drive Current (mA)</th><th>Drive Current (A)</th><th>Observed Optical Power (mW)</th><th>W/A</th></tr></thead><tbody>");
            foreach (CalibrationSlotRecord record in records.OrderBy(row => row.PathIndex))
            {
                double? waValue = record.GetWattPerAmp(standardPower);
                double? driveCurrentAmps = record.DriveCurrentMilliAmp.HasValue ? record.DriveCurrentMilliAmp.Value / 1000.0d : (double?)null;
                sb.AppendLine("<tr>" +
                    $"<td>{record.PathIndex}</td>" +
                    $"<td>{record.Slot}</td>" +
                    $"<td>{Encode(FormatOptional(record.OutputCurrentMicroAmp, "F3", "-"))}</td>" +
                    $"<td>{Encode(FormatOptional(record.DriveCurrentMilliAmp, "F2", "-"))}</td>" +
                    $"<td>{Encode(FormatOptional(driveCurrentAmps, "0.00E+0", "-"))}</td>" +
                    $"<td>{Encode(FormatOptional(record.ObservedPowerMilliWatt, "F3", "-"))}</td>" +
                    $"<td>{Encode(FormatOptional(waValue, "F1", "-"))}</td>" +
                    "</tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        private string BuildCalibrationAuditJson(string folderPath, string calPath, string csvPath, string htmlPath)
        {
            var payload = new
            {
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                CalibrationDateTime = _wizardState.CalibrationDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                OutputFolder = folderPath,
                Files = new
                {
                    CalibrationFile = calPath,
                    Csv = csvPath,
                    Html = htmlPath
                },
                Metadata = new
                {
                    Operator = _wizardState.OperatorName,
                    PowerMeterId = _wizardState.PowerMeterId,
                    StandardBaseId = _wizardState.StandardBaseId,
                    StandardRemoteId = _wizardState.StandardRemoteId,
                    EquipmentSummary = _wizardState.EquipmentSummary,
                    Notes = _wizardState.Notes
                },
                ReferencePowers = new
                {
                    BaseMilliWatt = _wizardState.BaseStandardPowerMw,
                    RemoteMilliWatt = _wizardState.RemoteStandardPowerMw
                },
                Records = GetAllRecords()
                    .OrderBy(record => record.PathIndex)
                    .Select(record => new
                    {
                        Path = record.PathIndex,
                        Type = record.DutType == DUTType.Base ? "Base" : "Remote",
                        InternalSlot = record.Slot,
                        OutputCurrentMicroAmp = record.OutputCurrentMicroAmp,
                        DriveCurrentMilliAmp = record.DriveCurrentMilliAmp,
                        DriveCurrentAmp = record.DriveCurrentMilliAmp.HasValue ? record.DriveCurrentMilliAmp.Value / 1000.0d : (double?)null,
                        ObservedOpticalPowerMilliWatt = record.ObservedPowerMilliWatt,
                        WattPerAmp = record.GetWattPerAmp(GetStandardPower(record.DutType))
                    })
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        private MeasurementPoint ApplyBaseStandardBias(int slot)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureBaseOutputHardware();
            SafeShutdownOutputs();
            RouteBaseOutputPath(slot);
            return ApplyCurrentSource(Shared.SMU_master, Shared.Base_Z_IB_IOP.SMU1.Channels[0].ChannelNumber, 0.035d, 4.0d, 0.045d, 20.0d);
        }

        private MeasurementPoint ApplyRemoteStandardBias(int slot)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureRemoteOutputHardware();
            SafeShutdownOutputs();
            RouteRemoteOutputPath(slot);
            return ApplyCurrentSource(Shared.SMU_master, Shared.Remote_Z_IOP.SMU1.Channels[0].ChannelNumber, 0.0035d, 4.0d, 0.01d, 20.0d);
        }

        private double MeasureBaseOutputCurrentMicroAmps(int slot)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureBaseOutputHardware();
            SafeShutdownOutputs();
            RouteBaseOutputPath(slot);

            SMUSettingsModel driveSettings = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU1.Channels[0]);
            SMUSettingsModel readSettings = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU1.Channels[1]);
            SMUSettingsModel auxSettings = BuildSMUSettings(Shared.Base_Z_IB_IOP.SMU2.Channels[0]);

            Shared.SMU_master.Reset();
            Shared.SMU_slave.Reset();

            if (!Shared.SMU_master.SetSweepChannel(driveSettings))
                throw new InvalidOperationException("Could not configure the Base drive channel.");
            if (!Shared.SMU_master.SetReadingChannel(readSettings))
                throw new InvalidOperationException("Could not configure the Base reading channel.");
            if (!Shared.SMU_slave.SetReadingChannel(auxSettings))
                throw new InvalidOperationException("Could not configure the Base auxiliary reading channel.");

            if (!Shared.SMU_master.InitiateReading(new List<int> { driveSettings.Channel, readSettings.Channel }, readSettings))
                throw new InvalidOperationException("Could not start the Base output-current sweep.");
            if (!Shared.SMU_slave.InitiateReading(new List<int> { auxSettings.Channel }, auxSettings))
                throw new InvalidOperationException("Could not start the Base auxiliary readback.");

            List<MeasurementPoint> drivePoints = ReadMeasurements(Shared.SMU_master, driveSettings.Channel, driveSettings.SweepRange.Steps);
            List<MeasurementPoint> readPoints = ReadMeasurements(Shared.SMU_master, readSettings.Channel, driveSettings.SweepRange.Steps);
            ReadMeasurements(Shared.SMU_slave, auxSettings.Channel, driveSettings.SweepRange.Steps);

            SafeShutdownOutputs();
            return Math.Abs(FindClosestCurrent(drivePoints, readPoints, 0.035d)) * 1000000.0d;
        }

        private double MeasureRemoteOutputCurrentMicroAmps(int slot)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureRemoteOutputHardware();
            SafeShutdownOutputs();
            RouteRemoteOutputPath(slot);

            SMUSettingsModel driveSettings = BuildSMUSettings(Shared.Remote_Z_IOP.SMU1.Channels[0]);
            SMUSettingsModel readSettings = BuildSMUSettings(Shared.Remote_Z_IOP.SMU2.Channels[0]);

            Shared.SMU_master.Reset();
            Shared.SMU_slave.Reset();

            if (!Shared.SMU_master.SetSweepChannel(driveSettings))
                throw new InvalidOperationException("Could not configure the Remote drive channel.");
            if (!Shared.SMU_slave.SetSweepChannel(readSettings))
                throw new InvalidOperationException("Could not configure the Remote reading channel.");

            if (!Shared.SMU_master.InitiateReading(new List<int> { driveSettings.Channel }, driveSettings))
                throw new InvalidOperationException("Could not start the Remote source sweep.");
            if (!Shared.SMU_slave.InitiateReading(new List<int> { readSettings.Channel }, readSettings))
                throw new InvalidOperationException("Could not start the Remote readback sweep.");

            List<MeasurementPoint> drivePoints = ReadMeasurements(Shared.SMU_master, driveSettings.Channel, driveSettings.SweepRange.Steps);
            List<MeasurementPoint> readPoints = ReadMeasurements(Shared.SMU_slave, readSettings.Channel, driveSettings.SweepRange.Steps);

            SafeShutdownOutputs();
            return Math.Abs(FindClosestCurrent(drivePoints, readPoints, 0.0035d)) * 1000000.0d;
        }

        private MeasurementPoint ApplyBaseDriveCurrent(int slot, double currentMilliAmp)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureBaseDriveHardware();
            SafeShutdownOutputs();
            RouteBaseDrivePath(slot);
            return ApplyCurrentSource(Shared.SMU_master, Shared.Base_Z_IPD.SMU1.Channels[0].ChannelNumber,
                currentMilliAmp / 1000.0d, 4.0d, 0.045d, 20.0d);
        }

        private MeasurementPoint ApplyRemoteDriveCurrent(int slot, double currentMilliAmp)
        {
            EnsureCalibrationResourcesLoaded();
            EnsureRemoteDriveHardware();
            SafeShutdownOutputs();
            RouteRemoteDrivePath(slot);
            return ApplyCurrentSource(Shared.SMU_master, Shared.Remote_Z_VPV.SMU1.Channels[0].ChannelNumber,
                currentMilliAmp / 1000.0d, 4.0d, 0.01d, 20.0d);
        }

        private MeasurementPoint ApplyCurrentSource(ISMU smu, int channel, double currentAmp, double complianceVolt, double sourceRange, double measureRange)
        {
            var settings = new SMUSettingsModel
            {
                Channel = channel,
                MeasureMode = SMUMeasureMode.VOLT,
                SourceMode = SMUMeasureMode.CURR,
                SweepRange = new SweepRangeModel
                {
                    Start = currentAmp,
                    Stop = currentAmp,
                    Steps = 1
                },
                Compliance = complianceVolt,
                IsSourceRangeAuto = false,
                SourceRange = sourceRange,
                IsMeasureRangeAuto = false,
                MeasureRange = measureRange
            };

            smu.Reset();
            if (!smu.SetSweepChannel(settings))
                throw new InvalidOperationException("Could not configure the current source.");
            if (!smu.InitiateReading(new List<int> { channel }, settings))
                throw new InvalidOperationException("Could not start the current source.");

            return ReadSingleMeasurement(smu, channel);
        }

        private MeasurementPoint ReadSingleMeasurement(ISMU smu, int channel)
        {
            List<MeasurementPoint> measurements = ReadMeasurements(smu, channel, 1);
            if (measurements.Count == 0)
                throw new InvalidOperationException("No measurement data was returned by the SMU.");

            return measurements[0];
        }

        private List<MeasurementPoint> ReadMeasurements(ISMU smu, int channel, int readSize)
        {
            var results = new List<MeasurementPoint>();
            double[,] data = new double[Math.Max(1, readSize), 3];
            bool fromStart = true;

            while (true)
            {
                if (!smu.ReadData(channel, fromStart, Math.Max(1, readSize), ref data, out int actrow))
                    throw new InvalidOperationException($"SMU read failed on channel {channel}.");

                fromStart = false;
                if (actrow == 0)
                    break;

                for (int index = 0; index < actrow; index++)
                {
                    results.Add(new MeasurementPoint(data[index, 0], data[index, 1], data[index, 2]));
                }
            }

            return results;
        }

        private double FindClosestCurrent(IReadOnlyList<MeasurementPoint> searchPoints, IReadOnlyList<MeasurementPoint> readPoints, double targetCurrentAmp)
        {
            int count = Math.Min(searchPoints.Count, readPoints.Count);
            if (count == 0)
                throw new InvalidOperationException("No sweep data was returned for the calibration measurement.");

            double bestDiff = double.MaxValue;
            double bestCurrent = double.NaN;
            for (int index = 0; index < count; index++)
            {
                double diff = Math.Abs(searchPoints[index].CurrentAmps - targetCurrentAmp);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestCurrent = readPoints[index].CurrentAmps;
                }
            }

            return bestCurrent;
        }

        private void RouteBaseOutputPath(int slot)
        {
            if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                throw new InvalidOperationException("Optical switch 1x4 could not close channel 1.");
            Shared.ElectricalSwitchBase2.Reset();

            if (!Shared.OpticalSwitch1x13_Base.CloseChannel(slot))
                throw new InvalidOperationException($"Base optical switch 1x13 could not close channel {slot}.");

            if (!Shared.ElectricalSwitchBase1.CloseChannels(new List<int>
                {
                    Shared.Base_Z_IB_IOP.ElectricalSwitch1.Positions[slot - 1].FromChannel
                }))
            {
                throw new InvalidOperationException("Base electrical switch #1 could not route the selected path.");
            }

            if (!Shared.ElectricalSwitchBase3.CloseChannels(new List<int>
                {
                    Shared.Base_Z_IB_IOP.ElectricalSwitch2.Positions[0].FromChannel
                }))
            {
                throw new InvalidOperationException("Base electrical switch #3 could not route the selected path.");
            }
        }

        private void RouteRemoteOutputPath(int slot)
        {
            if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                throw new InvalidOperationException("Optical switch 1x4 could not close channel 1.");
            Shared.ElectricalSwitchRemote2.Reset();

            if (!Shared.OpticalSwitch1x13_Remote.CloseChannel(slot))
                throw new InvalidOperationException($"Remote optical switch 1x13 could not close channel {slot}.");

            if (!Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>
                {
                    Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[slot - 1].FromChannel,
                    Shared.Remote_Z_IOP.ElectricalSwitch1.Positions[slot - 1].ToChannel
                }))
            {
                throw new InvalidOperationException("Remote electrical switch #1 could not route the selected path.");
            }

            if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>
                {
                    Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].FromChannel,
                    Shared.Remote_Z_IOP.ElectricalSwitch2.Positions[0].ToChannel
                }))
            {
                throw new InvalidOperationException("Remote electrical switch #3 could not route the selected path.");
            }
        }

        private void RouteBaseDrivePath(int slot)
        {
            if (!Shared.OpticalSwitch1x4.CloseChannel(3))
                throw new InvalidOperationException("Optical switch 1x4 could not close channel 3.");
            Shared.ElectricalSwitchBase2.Reset();

            if (!Shared.OpticalSwitch1x13_Base.CloseChannel(slot))
                throw new InvalidOperationException($"Base optical switch 1x13 could not close channel {slot}.");

            if (!Shared.ElectricalSwitchBase1.CloseChannels(new List<int>
                {
                    Shared.Base_Z_IPD.ElectricalSwitch1.Positions[0].FromChannel,
                    Shared.Base_Z_IPD.ElectricalSwitch1.Positions[0].ToChannel
                }))
            {
                throw new InvalidOperationException("Base electrical switch #1 could not route the Z_IPD path.");
            }

            if (!Shared.ElectricalSwitchBase3.CloseChannels(new List<int>
                {
                    Shared.Base_Z_IPD.ElectricalSwitch2.Positions[slot - 1].FromChannel,
                    Shared.Base_Z_IPD.ElectricalSwitch2.Positions[slot - 1].ToChannel
                }))
            {
                throw new InvalidOperationException("Base electrical switch #3 could not route the Z_IPD path.");
            }
        }

        private void RouteRemoteDrivePath(int slot)
        {
            if (!Shared.OpticalSwitch1x4.CloseChannel(1))
                throw new InvalidOperationException("Optical switch 1x4 could not close channel 1.");
            Shared.ElectricalSwitchRemote2.Reset();

            if (!Shared.OpticalSwitch1x13_Remote.CloseChannel(slot))
                throw new InvalidOperationException($"Remote optical switch 1x13 could not close channel {slot}.");

            if (!Shared.ElectricalSwitchRemote1.CloseChannels(new List<int>
                {
                    Shared.Remote_Z_VPV.ElectricalSwitch1.Positions[0].FromChannel,
                    Shared.Remote_Z_VPV.ElectricalSwitch1.Positions[0].ToChannel
                }))
            {
                throw new InvalidOperationException("Remote electrical switch #1 could not route the Z_VPV path.");
            }

            if (!Shared.ElectricalSwitchRemote3.CloseChannels(new List<int>
                {
                    Shared.Remote_Z_VPV.ElectricalSwitch2.Positions[slot - 1].FromChannel,
                    Shared.Remote_Z_VPV.ElectricalSwitch2.Positions[slot - 1].ToChannel
                }))
            {
                throw new InvalidOperationException("Remote electrical switch #3 could not route the Z_VPV path.");
            }
        }

        private void EnsureCalibrationResourcesLoaded()
        {
            if (Shared.Base_Z_IB_IOP != null && Shared.Base_Z_IPD != null && Shared.Remote_Z_IOP != null && Shared.Remote_Z_VPV != null)
                return;

            string resourcesFolder = Shared.sharedGeneralSettings?.GeneralSettings?.FirstOrDefault()?.RESOURCES_FOLDER;
            if (string.IsNullOrWhiteSpace(resourcesFolder))
                throw new InvalidOperationException("The resources folder is not configured.");

            string testFlowDirectory = Path.Combine(resourcesFolder, "TestFlows");
            if (!Directory.Exists(testFlowDirectory))
                throw new DirectoryNotFoundException($"Test flow folder was not found: {testFlowDirectory}");

            Shared.Base_Z_IB_IOP = Shared.Base_Z_IB_IOP ?? Shared.ParseTestFlow(Path.Combine(testFlowDirectory, "Base_Z_IB_IOP.json"));
            Shared.Base_Z_IPD = Shared.Base_Z_IPD ?? Shared.ParseTestFlow(Path.Combine(testFlowDirectory, "Base_Z_IPD.json"));
            Shared.Remote_Z_IOP = Shared.Remote_Z_IOP ?? Shared.ParseTestFlow(Path.Combine(testFlowDirectory, "Remote_Z_IOP.json"));
            Shared.Remote_Z_VPV = Shared.Remote_Z_VPV ?? Shared.ParseTestFlow(Path.Combine(testFlowDirectory, "Remote_Z_VPV.json"));
        }

        private void EnsureBaseOutputHardware()
        {
            EnsureConnected("Optical switch 1x4", Shared.OpticalSwitch1x4?.IsConnected == true);
            EnsureConnected("Base optical switch 1x13", Shared.OpticalSwitch1x13_Base?.IsConnected == true);
            EnsureConnected("Base electrical switch #1", Shared.ElectricalSwitchBase1?.IsConnected == true);
            EnsureConnected("Base electrical switch #3", Shared.ElectricalSwitchBase3?.IsConnected == true);
            EnsureConnected("Master SMU", Shared.SMU_master?.IsConnected == true);
            EnsureConnected("Slave SMU", Shared.SMU_slave?.IsConnected == true);
        }

        private void EnsureRemoteOutputHardware()
        {
            EnsureConnected("Optical switch 1x4", Shared.OpticalSwitch1x4?.IsConnected == true);
            EnsureConnected("Remote optical switch 1x13", Shared.OpticalSwitch1x13_Remote?.IsConnected == true);
            EnsureConnected("Remote electrical switch #1", Shared.ElectricalSwitchRemote1?.IsConnected == true);
            EnsureConnected("Remote electrical switch #3", Shared.ElectricalSwitchRemote3?.IsConnected == true);
            EnsureConnected("Master SMU", Shared.SMU_master?.IsConnected == true);
            EnsureConnected("Slave SMU", Shared.SMU_slave?.IsConnected == true);
        }

        private void EnsureBaseDriveHardware()
        {
            EnsureConnected("Optical switch 1x4", Shared.OpticalSwitch1x4?.IsConnected == true);
            EnsureConnected("Base optical switch 1x13", Shared.OpticalSwitch1x13_Base?.IsConnected == true);
            EnsureConnected("Base electrical switch #1", Shared.ElectricalSwitchBase1?.IsConnected == true);
            EnsureConnected("Base electrical switch #3", Shared.ElectricalSwitchBase3?.IsConnected == true);
            EnsureConnected("Master SMU", Shared.SMU_master?.IsConnected == true);
        }

        private void EnsureRemoteDriveHardware()
        {
            EnsureConnected("Optical switch 1x4", Shared.OpticalSwitch1x4?.IsConnected == true);
            EnsureConnected("Remote optical switch 1x13", Shared.OpticalSwitch1x13_Remote?.IsConnected == true);
            EnsureConnected("Remote electrical switch #1", Shared.ElectricalSwitchRemote1?.IsConnected == true);
            EnsureConnected("Remote electrical switch #3", Shared.ElectricalSwitchRemote3?.IsConnected == true);
            EnsureConnected("Master SMU", Shared.SMU_master?.IsConnected == true);
        }

        private void EnsureConnected(string equipmentName, bool isConnected)
        {
            if (!isConnected)
                throw new InvalidOperationException($"{equipmentName} is not connected. Connect the equipment before running calibration steps.");
        }

        private void SafeShutdownOutputs()
        {
            try { Shared.SMU_master?.CloseAllChannels(); } catch { }
            try { Shared.SMU_slave?.CloseAllChannels(); } catch { }
        }

        private void CalibrationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SafeShutdownOutputs();
        }

        private void CalibrationForm_Load(object sender, EventArgs e)
        {
            ApplySafeSplitterDistance();
        }

        private void CalibrationForm_SizeChanged(object sender, EventArgs e)
        {
            ApplySafeSplitterDistance();
        }

        private void ApplySafeSplitterDistance()
        {
            if (_mainSplit == null || _mainSplit.IsDisposed)
                return;

            if (_mainSplit.Width <= 0)
                return;

            if (_mainSplit.Panel2MinSize != SummaryPanelMinWidth)
                _mainSplit.Panel2MinSize = SummaryPanelMinWidth;

            int minPanel1 = Math.Max(0, _mainSplit.Panel1MinSize);
            int maxPanel1 = _mainSplit.Width - _mainSplit.Panel2MinSize;
            if (maxPanel1 < minPanel1)
                return;

            int preferredDistance = Math.Min(900, maxPanel1);
            int safeDistance = Math.Max(minPanel1, preferredDistance);
            if (_mainSplit.SplitterDistance != safeDistance)
                _mainSplit.SplitterDistance = safeDistance;
        }

        private bool HasRequiredMetadata()
        {
            return !string.IsNullOrWhiteSpace(_wizardState.OperatorName)
                && !string.IsNullOrWhiteSpace(_wizardState.PowerMeterId)
                && !string.IsNullOrWhiteSpace(_wizardState.StandardBaseId)
                && !string.IsNullOrWhiteSpace(_wizardState.StandardRemoteId);
        }

        private bool IsCalibrationComplete()
        {
            return _wizardState.BaseStandardPowerMw.HasValue
                && _wizardState.RemoteStandardPowerMw.HasValue
                && _wizardState.BaseRecords.All(record => record.IsComplete)
                && _wizardState.RemoteRecords.All(record => record.IsComplete);
        }

        private bool RequireRecordedValue(double? value, string message)
        {
            if (value.HasValue)
                return true;

            MessageBox.Show(this, message, "Step Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool RequireStageComplete(bool complete, string message)
        {
            if (complete)
                return true;

            MessageBox.Show(this, message, "Step Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private IEnumerable<CalibrationSlotRecord> GetAllRecords()
        {
            return _wizardState.BaseRecords.Concat(_wizardState.RemoteRecords);
        }

        private double? GetStandardPower(DUTType type)
        {
            return type == DUTType.Base ? _wizardState.BaseStandardPowerMw : _wizardState.RemoteStandardPowerMw;
        }

        private int GetPathIndex(DUTType type, int slot)
        {
            return type == DUTType.Base ? slot : slot + RemotePathOffset;
        }

        private SMUSettingsModel BuildSMUSettings(SMUChannelModel channel)
        {
            return new SMUSettingsModel
            {
                Channel = channel.ChannelNumber,
                MeasureMode = (SMUMeasureMode)Enum.Parse(typeof(SMUMeasureMode), channel.MeasureModel.MeasureMode),
                SourceMode = (SMUMeasureMode)Enum.Parse(typeof(SMUMeasureMode), channel.MeasureModel.SourceMode),
                SweepRange = new SweepRangeModel
                {
                    Start = channel.MeasureModel.Start,
                    Stop = channel.MeasureModel.Stop,
                    Steps = channel.MeasureModel.SweepNumPoints
                },
                Compliance = channel.MeasureModel.Compliance,
                IsSourceRangeAuto = channel.MeasureModel.IsSourceRangeAuto,
                SourceRange = channel.MeasureModel.SourceRange,
                IsMeasureRangeAuto = channel.MeasureModel.IsMeasureRangeAuto,
                MeasureRange = channel.MeasureModel.MeasureRange
            };
        }

        private string CsvValue(double? value)
        {
            return value.HasValue ? value.Value.ToString("G", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Encode(string text)
        {
            return WebUtility.HtmlEncode(text ?? string.Empty);
        }

        private string FormatOptional(double? value, string format, string emptyText)
        {
            return value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : emptyText;
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            if (_lblStatus != null)
                _lblStatus.Text = message;
        }

        private int GetContentWidth()
        {
            if (_contentPanel == null || _contentPanel.ClientSize.Width <= 0)
                return 860;

            return Math.Max(780, _contentPanel.ClientSize.Width - 40);
        }

        private static void ApplyFixedWidth(Control control, int width)
        {
            control.Width = width;
            control.MinimumSize = new Size(width, control.MinimumSize.Height);
            control.MaximumSize = new Size(width, 0);
        }

        private TableLayoutPanel CreatePageLayout()
        {
            int width = GetContentWidth();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 0,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ApplyFixedWidth(layout, width);
            return layout;
        }

        private void AddPageRow(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Margin = new Padding(0, 0, 0, 12);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            layout.Controls.Add(control, 0, row);
        }

        private GroupBox CreateGroupBox(string text)
        {
            int width = GetContentWidth();
            var groupBox = new GroupBox
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(12)
            };
            ApplyFixedWidth(groupBox, width);
            return groupBox;
        }

        private TableLayoutPanel CreateFieldLayout()
        {
            int width = GetContentWidth() - 36;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 0,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ApplyFixedWidth(layout, width);
            return layout;
        }

        private void AddField(TableLayoutPanel layout, string labelText, Control control, bool tall = false)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(tall ? SizeType.Absolute : SizeType.AutoSize, tall ? 122F : 0F));
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                MaximumSize = new Size(228, 0),
                Margin = new Padding(0, 8, 12, 0)
            };
            label.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            control.Margin = new Padding(0, 4, 0, 0);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private Control CreateInlineField(string labelText, Control control)
        {
            int width = GetContentWidth() - 48;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ApplyFixedWidth(layout, width);
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                MaximumSize = new Size(228, 0),
                Margin = new Padding(0, 8, 12, 0)
            };
            label.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            layout.Controls.Add(label, 0, 0);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            control.Margin = new Padding(0, 4, 0, 0);
            layout.Controls.Add(control, 1, 0);
            return layout;
        }

        private Label CreateBodyLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                MaximumSize = new Size(GetContentWidth() - 20, 0),
                Text = text,
                ForeColor = Color.FromArgb(45, 55, 72)
            };
        }

        private FlowLayoutPanel CreateButtonRow()
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
        }

        private Button CreateButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(130, 36),
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private Control CreateCompletionBox(string message, List<CalibrationSlotRecord> records, bool isBase, bool isDrive)
        {
            string unitName = isBase ? "Base" : "Remote";
            var box = CreateGroupBox($"{unitName} stage complete");
            var layout = CreatePageLayout();
            AddPageRow(layout, CreateBodyLabel(message));

            var nudRetakeSlot = new NumericUpDown
            {
                Minimum = 1,
                Maximum = PathsPerSide,
                Value = 1,
                Width = 120
            };

            AddPageRow(layout, CreateInlineField($"Re-run {unitName.ToLowerInvariant()} slot", nudRetakeSlot));

            var buttonRow = CreateButtonRow();
            var btnRetake = CreateButton(isDrive ? "Re-run Drive Step" : "Re-run Output Step");
            btnRetake.Click += (sender, args) =>
            {
                int slot = (int)nudRetakeSlot.Value;
                if (isBase)
                {
                    if (isDrive)
                        _baseDriveIndex = slot - 1;
                    else
                        _baseOutputIndex = slot - 1;
                }
                else
                {
                    if (isDrive)
                        _remoteDriveIndex = slot - 1;
                    else
                        _remoteOutputIndex = slot - 1;
                }

                SetStatus($"Re-running {unitName.ToLowerInvariant()} path {GetPathIndex(isBase ? DUTType.Base : DUTType.Remote, slot)}.");
                UpdateWizardPage();
            };

            buttonRow.Controls.Add(btnRetake);
            AddPageRow(layout, buttonRow);

            box.Controls.Add(layout);
            return box;
        }

        private sealed class CalibrationWizardState
        {
            public CalibrationWizardState()
            {
                BaseRecords = new List<CalibrationSlotRecord>();
                RemoteRecords = new List<CalibrationSlotRecord>();
            }

            public string OperatorName { get; set; }
            public DateTime CalibrationDateTime { get; set; }
            public string PowerMeterId { get; set; }
            public string StandardBaseId { get; set; }
            public string StandardRemoteId { get; set; }
            public string EquipmentSummary { get; set; }
            public string Notes { get; set; }
            public int BaseStandardSlot { get; set; }
            public int RemoteStandardSlot { get; set; }
            public double? BaseStandardPowerMw { get; set; }
            public double? RemoteStandardPowerMw { get; set; }
            public List<CalibrationSlotRecord> BaseRecords { get; }
            public List<CalibrationSlotRecord> RemoteRecords { get; }
        }

        private sealed class CalibrationSlotRecord
        {
            public CalibrationSlotRecord(DUTType dutType, int slot, int pathIndex)
            {
                DutType = dutType;
                Slot = slot;
                PathIndex = pathIndex;
            }

            public DUTType DutType { get; }
            public int Slot { get; }
            public int PathIndex { get; }
            public double? OutputCurrentMicroAmp { get; set; }
            public double? DriveCurrentMilliAmp { get; set; }
            public double? ObservedPowerMilliWatt { get; set; }

            public bool IsComplete => OutputCurrentMicroAmp.HasValue && DriveCurrentMilliAmp.HasValue;

            public double? GetWattPerAmp(double? standardPowerMilliWatt)
            {
                if (!standardPowerMilliWatt.HasValue || !OutputCurrentMicroAmp.HasValue || OutputCurrentMicroAmp.Value <= 0.0d)
                    return null;

                return standardPowerMilliWatt.Value * 1000.0d / OutputCurrentMicroAmp.Value;
            }
        }

        private readonly struct MeasurementPoint
        {
            public MeasurementPoint(double voltageVolts, double currentAmps, double timeSeconds)
            {
                VoltageVolts = voltageVolts;
                CurrentAmps = currentAmps;
                TimeSeconds = timeSeconds;
            }

            public double VoltageVolts { get; }
            public double CurrentAmps { get; }
            public double TimeSeconds { get; }
        }

        private enum CalibrationWizardPage
        {
            Setup = 0,
            BaseStandardPower = 1,
            BaseOutputCurrent = 2,
            BaseDriveCurrent = 3,
            RemoteStandardPower = 4,
            RemoteOutputCurrent = 5,
            RemoteDriveCurrent = 6,
            Review = 7
        }
    }
}