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
                Shared.messageViewer?.AddNewMessage($"Start form load: {ex.Message}", MessageType.Error);
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
                foreach (var pnl in tlpDUT.Controls.OfType<Panel>().ToList())
                {
                    var dut = DUTs.Find(d => string.Equals(d.Tag, pnl.Tag?.ToString()));
                    if (dut != null)
                        pnl.BackgroundImage = dut.ReadyToTest ? Shared.PassImage : Shared.FailImage;
                }
                btnStart.Enabled = DUTs.Count > 0 && DUTs.All(d => d.ReadyToTest);
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"Pretest button: {ex.Message}", MessageType.Error);
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

                DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Shared.messageViewer?.AddNewMessage($"Start button: {ex.Message}", MessageType.Error);
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
                var txt = sender as TextBox;
                var tagParts = txt.Tag.ToString().Split('_');
                var dutType = tagParts[0] == "Base" ? DUTType.Base : DUTType.Remote;
                var slot = int.Parse(tagParts[1]);

                txt.BackColor = Color.White;

                if (!string.IsNullOrEmpty(txt.Text))
                {
                    // Check for duplicate serial number
                    if (tlpDUT.Controls.OfType<TextBox>().Any(t => t != txt && t.Text.Equals(txt.Text)))
                    {
                        txt.BackColor = Color.Pink;
                        return;
                    }

                    // Remove existing DUT with same slot+type if any
                    var existing = DUTs.Find(d => d.DUTType == dutType && d.Slot == slot);
                    if (existing != null)
                        DUTs.Remove(existing);

                    DUTs.Add(new DUTModel()
                    {
                        DUTType = dutType,
                        ReadyToTest = false,
                        SerialNumber = txt.Text,
                        Slot = slot,
                        Tag = txt.Tag.ToString(),
                        ItemNumber = dutType == DUTType.Base
                            ? Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER
                            : Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER,
                        ThermistorChannel = Shared.GetThermistorChannel(dutType, slot)
                    });

                    // Enable pretest when all textboxes have unique values
                    var allTxts = tlpDUT.Controls.OfType<TextBox>().ToList();
                    if (allTxts.Count > 0 && allTxts.All(t => !string.IsNullOrEmpty(t.Text) && t.BackColor == Color.White))
                        btnPretest.Enabled = true;
                    else
                        btnPretest.Enabled = false;
                }
                else
                {
                    var existing = DUTs.Find(d => d.DUTType == dutType && d.Slot == slot);
                    if (existing != null)
                        DUTs.Remove(existing);
                    btnPretest.Enabled = false;
                    btnStart.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Serial number text changed: {ex.Message}");
            }
        }
    }
}
