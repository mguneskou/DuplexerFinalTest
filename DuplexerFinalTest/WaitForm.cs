using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public partial class WaitForm : Form
    {
        private System.Windows.Forms.Timer _spinnerTimer;
        private float _spinnerAngle = 0;
        private Label _lblMessage;
        private Button _btnCancel;
        private Panel _spinnerPanel;
        private BackgroundWorker _bgw;

        public Action<BackgroundWorker, DoWorkEventArgs> DoWork { get; set; }
        public Action<DialogResult> WorkCompleted { get; set; }

        public WaitForm(object sender = null, string message = "Please wait...", bool showCancel = true)
        {
            InitializeWaitForm(message, showCancel);
        }

        private void InitializeWaitForm(string message, bool showCancel)
        {
            // Form style
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(340, 160);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ShowInTaskbar = false;

            // Drop shadow
            // Applied via CreateParams override below

            // Rounded region applied in OnLoad

            // Spinner panel
            _spinnerPanel = new Panel()
            {
                Size = new Size(60, 60),
                Location = new Point(20, 50),
                BackColor = Color.Transparent
            };
            _spinnerPanel.Paint += SpinnerPanel_Paint;
            this.Controls.Add(_spinnerPanel);

            // Message label
            _lblMessage = new Label()
            {
                Text = message,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Location = new Point(90, 55),
                Size = new Size(230, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_lblMessage);

            // Cancel button
            _btnCancel = new Button()
            {
                Text = "Cancel",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Size = new Size(80, 28),
                Location = new Point(240, 118),
                Visible = showCancel
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            _btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(_btnCancel);

            // Spinner timer
            _spinnerTimer = new System.Windows.Forms.Timer() { Interval = 30 };
            _spinnerTimer.Tick += (s, e) =>
            {
                _spinnerAngle = (_spinnerAngle + 8f) % 360f;
                _spinnerPanel.Invalidate();
            };

            this.Load += WaitForm_Load;
            this.FormClosed += WaitForm_FormClosed;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        private void WaitForm_Load(object sender, EventArgs e)
        {
            // Rounded corners
            var path = new GraphicsPath();
            int radius = 12;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            this.Region = new Region(path);

            _spinnerTimer.Start();

            if (DoWork != null)
            {
                _bgw = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                _bgw.DoWork += (s, ev) => DoWork(_bgw, ev);
                _bgw.RunWorkerCompleted += Bgw_RunWorkerCompleted;
                _bgw.RunWorkerAsync();
            }
        }

        private void WaitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _spinnerTimer?.Stop();
            _spinnerTimer?.Dispose();
        }

        private void SpinnerPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(5, 5, 50, 50);
            using (var trackPen = new Pen(Color.FromArgb(60, 60, 60), 5))
                g.DrawEllipse(trackPen, rect);
            using (var arcPen = new Pen(Color.FromArgb(0, 162, 237), 5))
                g.DrawArc(arcPen, rect, _spinnerAngle, 270);
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _btnCancel.Enabled = false;
            _bgw?.CancelAsync();
        }

        private void Bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _spinnerTimer?.Stop();
            var result = e.Cancelled ? DialogResult.Cancel : DialogResult.OK;
            WorkCompleted?.Invoke(result);
            this.DialogResult = result;
            this.Close();
        }

        public void UpdateMessage(string message)
        {
            if (this.InvokeRequired)
                this.Invoke((Action)(() => _lblMessage.Text = message));
            else
                _lblMessage.Text = message;
        }
    }
}
