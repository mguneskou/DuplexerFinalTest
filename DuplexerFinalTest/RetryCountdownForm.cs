using System;
using System.Windows.Forms;

namespace DuplexerFinalTest
{
    public enum RetryCountdownResult { ResumeNow, Cancel }

    /// <summary>
    /// Modal dialog shown when equipment communication fails.
    /// Counts down the retry delay and lets the operator resume early or cancel.
    /// Pass TimeSpan.Zero for the final "please fix manually" state (no countdown).
    /// </summary>
    public partial class RetryCountdownForm : Form
    {
        public RetryCountdownResult Result { get; private set; } = RetryCountdownResult.Cancel;

        private TimeSpan _remaining;
        private readonly bool _hasCountdown;
        private System.Windows.Forms.Timer _ticker;

        public RetryCountdownForm(string errorMessage, TimeSpan delay, int attemptNumber)
        {
            InitializeComponent();

            _remaining = delay;
            _hasCountdown = delay > TimeSpan.Zero;

            // Header
            lblAttempt.Text = attemptNumber <= 2
                ? $"Automatic retry {attemptNumber} of 2"
                : "All automatic retries exhausted — manual action required";

            lblError.Text = errorMessage;

            if (_hasCountdown)
            {
                lblCountdownCaption.Text = "Retrying automatically in:";
                lblCountdown.Text = FormatTime(_remaining);

                _ticker = new System.Windows.Forms.Timer { Interval = 1000 };
                _ticker.Tick += Ticker_Tick;
                _ticker.Start();
            }
            else
            {
                lblCountdownCaption.Text = "Please fix the issue, then click Resume Now to continue.";
                lblCountdown.Text = "";
            }
        }

        private void Ticker_Tick(object sender, EventArgs e)
        {
            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                _ticker.Stop();
                Result = RetryCountdownResult.ResumeNow;
                Close();
                return;
            }
            lblCountdown.Text = FormatTime(_remaining);
        }

        private void BtnResumeNow_Click(object sender, EventArgs e)
        {
            _ticker?.Stop();
            Result = RetryCountdownResult.ResumeNow;
            Close();
        }

        private void BtnCancelTest_Click(object sender, EventArgs e)
        {
            _ticker?.Stop();
            Result = RetryCountdownResult.Cancel;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ticker?.Stop();
            _ticker?.Dispose();
            base.OnFormClosed(e);
        }

        private static string FormatTime(TimeSpan ts) =>
            $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
