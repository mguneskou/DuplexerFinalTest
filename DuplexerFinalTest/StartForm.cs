using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public partial class StartForm : Form
    {
        private List<DUTModel> DUTs = new List<DUTModel>();
        private bool _serialsCompleteAndUnique = false;
        private int _previousDuplicateEntryCount = 0;

        public StartForm()
        {
            InitializeComponent();
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, tlpDUT, new object[] { true });
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, tlpMain, new object[] { true });
        }

        private void StartForm_Load(object sender, EventArgs e)
        {
            try
            {
                DUTs.Clear();
                Shared.currentRunMetrics = new RunMetricsModel();
                _serialsCompleteAndUnique = false;
                _previousDuplicateEntryCount = 0;
                Shared.infoModel.TestDate = string.Empty;
                Shared.infoModel.TestTime = string.Empty;
                Shared.infoModel.Operator = string.Empty;
                Shared.infoModel.Test?.BaseDUTs?.Clear();
                Shared.infoModel.Test?.RemoteDUTs?.Clear();
                GenerateDUTTable(Convert.ToInt32(nudBaseCount.Value), Convert.ToInt32(nudRemoteCount.Value));
                lstTestSequences.DataSource = null;
                lstTestSequences.DataSource = Shared.AllAvailableTestSequences;
                lstTestSequences.DisplayMember = "SequenceName";
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Start form load: {ex.Message}", MessageType.Error);
            }
        }

        private void NudCount_ValueChanged(object sender, EventArgs e)
        {
            var nud = sender as NumericUpDown;
            if (nud.Value < 0)
            {
                nud.Value = 0;
                return;
            }
            GenerateDUTTable(Convert.ToInt32(nudBaseCount.Value), Convert.ToInt32(nudRemoteCount.Value));
        }

        private void BtnPretest_Click(object sender, EventArgs e)
        {
            try
            {
                Shared.pretest.Run(DUTs);
                Shared.currentRunMetrics?.RecordPretestAttempt(DUTs.Count(d => !d.ReadyToTest));
                UpdateDutIndicators();
                btnStart.Enabled = DUTs.Count > 0 && DUTs.All(d => d.ReadyToTest);
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Pretest button: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtOperator.Text))
                {
                    MessageBox.Show("Please enter operator name!");
                    txtOperator.Focus();
                    return;
                }
                if (lstTestSequences.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a test sequence!");
                    return;
                }
                Shared.infoModel.Operator = txtOperator.Text;
                Shared.infoModel.TestDate = dtpTestDate.Value.ToString("yyyy-MM-dd");
                Shared.infoModel.TestTime = dtpTestDate.Value.ToString("HH:mm:ss");
                Shared.infoModel.Test = Shared.AllAvailableTestSequences
                    .Find(a => a.SequenceName.Equals(lstTestSequences.Text))?.Clone();
                Shared.infoModel.NumberOfBaseUnits = Convert.ToInt32(nudBaseCount.Value);
                Shared.infoModel.NumberOfRemoteUnits = Convert.ToInt32(nudRemoteCount.Value);

                Shared.infoModel.Test.BaseDUTs.Clear();
                Shared.infoModel.Test.BaseDUTs.AddRange(DUTs.Where(d => d.DUTType == DUTType.Base));
                Shared.infoModel.Test.RemoteDUTs.Clear();
                Shared.infoModel.Test.RemoteDUTs.AddRange(DUTs.Where(d => d.DUTType == DUTType.Remote));
                Shared.TopLevelResultFileName = null;
                if (Shared.currentRunMetrics != null)
                    Shared.currentRunMetrics.TestStartRequestedAt = DateTime.Now;

                DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"Start button: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void GenerateDUTTable(int baseCount, int remoteCount)
        {
            try
            {
                btnPretest.Enabled = false;
                btnStart.Enabled = false;
                DUTs.Clear();
                _serialsCompleteAndUnique = false;
                _previousDuplicateEntryCount = 0;
                if (Shared.currentRunMetrics != null)
                    Shared.currentRunMetrics.SerialEntryCompletedAt = null;
                tlpDUT.Controls.Clear();
                tlpDUT.ColumnStyles.Clear();
                tlpDUT.RowStyles.Clear();
                tlpDUT.RowCount = Math.Max(baseCount, remoteCount) + 1;
                tlpDUT.ColumnCount = 6;
                tlpDUT.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
                float colW = 100f / 6f;
                float rowH = 100f / tlpDUT.RowCount;
                for (int i = 0; i < 6; i++)
                    tlpDUT.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colW));
                for (int i = 0; i < tlpDUT.RowCount; i++)
                    tlpDUT.RowStyles.Add(new RowStyle(SizeType.Percent, rowH));

                // Title labels
                var lblBaseTitle = new Label()
                {
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Text = "Base DUTs",
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tlpDUT.Controls.Add(lblBaseTitle, 0, 0);
                tlpDUT.SetColumnSpan(lblBaseTitle, 3);

                var lblRemoteTitle = new Label()
                {
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Text = "Remote DUTs",
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tlpDUT.Controls.Add(lblRemoteTitle, 3, 0);
                tlpDUT.SetColumnSpan(lblRemoteTitle, 3);

                // Base DUT controls
                for (int i = 0; i < baseCount; i++)
                {
                    string tag = $"Base_{i + 1}";
                    var pnl = new Panel()
                    {
                        Name = $"pnlBase{i + 1}",
                        Tag = tag,
                        Size = new Size(25, 25),
                        Anchor = AnchorStyles.Right,
                        BackgroundImage = Shared.FailImage,
                        BackgroundImageLayout = ImageLayout.Zoom
                    };
                    tlpDUT.Controls.Add(pnl, 0, i + 1);

                    var lbl = new Label()
                    {
                        Name = $"lblBase{i + 1}",
                        Text = $"Base #{i + 1}",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    tlpDUT.Controls.Add(lbl, 1, i + 1);

                    var txt = new TextBox()
                    {
                        Name = $"txtBase{i + 1}",
                        Tag = tag,
                        TabStop = true,
                        TabIndex = i + 5,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right
                    };
                    txt.Leave += Txt_Leave;
                    txt.TextChanged += Txt_TextChanged;
                    tlpDUT.Controls.Add(txt, 2, i + 1);
                }

                // Remote DUT controls
                for (int i = 0; i < remoteCount; i++)
                {
                    string tag = $"Remote_{i + 1}";
                    var pnl = new Panel()
                    {
                        Name = $"pnlRemote{i + 1}",
                        Tag = tag,
                        Size = new Size(25, 25),
                        Anchor = AnchorStyles.Right,
                        BackgroundImage = Shared.FailImage,
                        BackgroundImageLayout = ImageLayout.Zoom
                    };
                    tlpDUT.Controls.Add(pnl, 3, i + 1);

                    var lbl = new Label()
                    {
                        Name = $"lblRemote{i + 1}",
                        Text = $"Remote #{i + 1}",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    tlpDUT.Controls.Add(lbl, 4, i + 1);

                    var txt = new TextBox()
                    {
                        Name = $"txtRemote{i + 1}",
                        Tag = tag,
                        TabStop = true,
                        TabIndex = baseCount + i + 5,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right
                    };
                    txt.Leave += Txt_Leave;
                    txt.TextChanged += Txt_TextChanged;
                    tlpDUT.Controls.Add(txt, 5, i + 1);
                }

                btnPretest.TabIndex = baseCount + remoteCount + 5;
                btnStart.TabIndex   = baseCount + remoteCount + 6;
                btnCancel.TabIndex  = baseCount + remoteCount + 7;
            }
            catch (Exception ex)
            {
                throw new Exception($"Generate DUT table: {ex.Message}");
            }
        }

        private void Txt_Leave(object sender, EventArgs e)
        {
            // No strict format check in new project — accept any non-empty string
        }

        private void Txt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateSerialEntryState();
                RebuildDutEntries();
                UpdateDutIndicators();
                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Serial number text changed: {ex.Message}");
            }
        }

        private void UpdateSerialEntryState()
        {
            var allTxts = tlpDUT.Controls.OfType<TextBox>().ToList();
            var serialCounts = allTxts
                .Select(t => (t.Text ?? string.Empty).Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .GroupBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            int duplicateEntryCount = 0;
            foreach (var textBox in allTxts)
            {
                string serialNumber = (textBox.Text ?? string.Empty).Trim();
                bool isDuplicate = !string.IsNullOrWhiteSpace(serialNumber)
                    && serialCounts.TryGetValue(serialNumber, out int count)
                    && count > 1;

                if (isDuplicate)
                    duplicateEntryCount++;

                textBox.BackColor = isDuplicate ? Color.Pink : Color.White;
            }

            if (duplicateEntryCount < _previousDuplicateEntryCount)
                Shared.currentRunMetrics?.RecordDuplicateCorrection();

            _previousDuplicateEntryCount = duplicateEntryCount;

            bool allCompleteAndUnique = allTxts.Count > 0
                && allTxts.All(t => !string.IsNullOrWhiteSpace((t.Text ?? string.Empty).Trim()) && t.BackColor == Color.White);

            btnPretest.Enabled = allCompleteAndUnique;
            if (!allCompleteAndUnique)
                btnStart.Enabled = false;

            if (Shared.currentRunMetrics != null)
            {
                if (allCompleteAndUnique && !_serialsCompleteAndUnique)
                    Shared.currentRunMetrics.SerialEntryCompletedAt = DateTime.Now;
                else if (!allCompleteAndUnique)
                    Shared.currentRunMetrics.SerialEntryCompletedAt = null;
            }

            _serialsCompleteAndUnique = allCompleteAndUnique;
        }

        private void RebuildDutEntries()
        {
            DUTs.Clear();

            foreach (var textBox in tlpDUT.Controls.OfType<TextBox>())
            {
                string serialNumber = (textBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(serialNumber) || textBox.BackColor == Color.Pink || textBox.Tag == null)
                    continue;

                var tagParts = textBox.Tag.ToString().Split('_');
                var dutType = tagParts[0] == "Base" ? DUTType.Base : DUTType.Remote;
                var slot = int.Parse(tagParts[1]);

                DUTs.Add(new DUTModel()
                {
                    DUTType = dutType,
                    ReadyToTest = false,
                    SerialNumber = serialNumber,
                    Slot = slot,
                    Tag = textBox.Tag.ToString(),
                    ItemNumber = dutType == DUTType.Base
                        ? Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER
                        : Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER,
                    ThermistorChannel = Shared.GetThermistorChannel(dutType, slot)
                });
            }
        }

        private void UpdateDutIndicators()
        {
            foreach (var pnl in tlpDUT.Controls.OfType<Panel>().ToList())
            {
                var dut = DUTs.Find(d => string.Equals(d.Tag, pnl.Tag?.ToString(), StringComparison.OrdinalIgnoreCase));
                pnl.BackgroundImage = dut != null && dut.ReadyToTest ? Shared.PassImage : Shared.FailImage;
            }
        }
    }
}
