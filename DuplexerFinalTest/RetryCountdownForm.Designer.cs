namespace DuplexerFinalTest
{
    partial class RetryCountdownForm
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
            this.pnlHeader        = new System.Windows.Forms.Panel();
            this.lblTitle         = new System.Windows.Forms.Label();
            this.lblAttempt       = new System.Windows.Forms.Label();
            this.lblErrorCaption  = new System.Windows.Forms.Label();
            this.lblError         = new System.Windows.Forms.Label();
            this.lblCountdownCaption = new System.Windows.Forms.Label();
            this.lblCountdown     = new System.Windows.Forms.Label();
            this.pnlButtons       = new System.Windows.Forms.Panel();
            this.btnResumeNow     = new System.Windows.Forms.Button();
            this.btnCancelTest    = new System.Windows.Forms.Button();

            this.pnlHeader.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            // ── pnlHeader ─────────────────────────────────────────────────
            this.pnlHeader.BackColor = System.Drawing.Color.Firebrick;
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 64;
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.TabIndex = 0;

            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
            this.lblTitle.Text = "\u26a0  Equipment Communication Failure";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.TabIndex = 0;

            // ── lblAttempt ────────────────────────────────────────────────
            this.lblAttempt.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblAttempt.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Italic);
            this.lblAttempt.ForeColor = System.Drawing.Color.DimGray;
            this.lblAttempt.Location = new System.Drawing.Point(16, 76);
            this.lblAttempt.Name = "lblAttempt";
            this.lblAttempt.Size = new System.Drawing.Size(518, 22);
            this.lblAttempt.TabIndex = 1;
            this.lblAttempt.Text = "";
            this.lblAttempt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── lblErrorCaption ───────────────────────────────────────────
            this.lblErrorCaption.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left;
            this.lblErrorCaption.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblErrorCaption.Location = new System.Drawing.Point(16, 106);
            this.lblErrorCaption.Name = "lblErrorCaption";
            this.lblErrorCaption.AutoSize = true;
            this.lblErrorCaption.TabIndex = 2;
            this.lblErrorCaption.Text = "Error detail:";

            // ── lblError ──────────────────────────────────────────────────
            this.lblError.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblError.BackColor = System.Drawing.Color.FromArgb(255, 240, 240);
            this.lblError.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 9.5f);
            this.lblError.ForeColor = System.Drawing.Color.DarkRed;
            this.lblError.Location = new System.Drawing.Point(16, 130);
            this.lblError.Name = "lblError";
            this.lblError.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.lblError.Size = new System.Drawing.Size(518, 52);
            this.lblError.TabIndex = 3;
            this.lblError.Text = "";

            // ── lblCountdownCaption ───────────────────────────────────────
            this.lblCountdownCaption.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblCountdownCaption.Font = new System.Drawing.Font("Segoe UI", 10f);
            this.lblCountdownCaption.Location = new System.Drawing.Point(16, 196);
            this.lblCountdownCaption.Name = "lblCountdownCaption";
            this.lblCountdownCaption.Size = new System.Drawing.Size(518, 26);
            this.lblCountdownCaption.TabIndex = 4;
            this.lblCountdownCaption.Text = "";
            this.lblCountdownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── lblCountdown ──────────────────────────────────────────────
            this.lblCountdown.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblCountdown.Font = new System.Drawing.Font("Segoe UI", 32f, System.Drawing.FontStyle.Bold);
            this.lblCountdown.ForeColor = System.Drawing.Color.Firebrick;
            this.lblCountdown.Location = new System.Drawing.Point(16, 228);
            this.lblCountdown.Name = "lblCountdown";
            this.lblCountdown.Size = new System.Drawing.Size(518, 60);
            this.lblCountdown.TabIndex = 5;
            this.lblCountdown.Text = "";
            this.lblCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ── pnlButtons ────────────────────────────────────────────────
            this.pnlButtons.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.pnlButtons.Controls.Add(this.btnResumeNow);
            this.pnlButtons.Controls.Add(this.btnCancelTest);
            this.pnlButtons.Location = new System.Drawing.Point(16, 304);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(518, 48);
            this.pnlButtons.TabIndex = 6;

            // btnResumeNow
            this.btnResumeNow.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnResumeNow.BackColor = System.Drawing.Color.ForestGreen;
            this.btnResumeNow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResumeNow.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.btnResumeNow.ForeColor = System.Drawing.Color.White;
            this.btnResumeNow.Location = new System.Drawing.Point(0, 6);
            this.btnResumeNow.Name = "btnResumeNow";
            this.btnResumeNow.Size = new System.Drawing.Size(200, 36);
            this.btnResumeNow.TabIndex = 0;
            this.btnResumeNow.Text = "Resume Now";
            this.btnResumeNow.UseVisualStyleBackColor = false;
            this.btnResumeNow.Click += new System.EventHandler(this.BtnResumeNow_Click);

            // btnCancelTest
            this.btnCancelTest.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnCancelTest.BackColor = System.Drawing.Color.Firebrick;
            this.btnCancelTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelTest.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.btnCancelTest.ForeColor = System.Drawing.Color.White;
            this.btnCancelTest.Location = new System.Drawing.Point(318, 6);
            this.btnCancelTest.Name = "btnCancelTest";
            this.btnCancelTest.Size = new System.Drawing.Size(200, 36);
            this.btnCancelTest.TabIndex = 1;
            this.btnCancelTest.Text = "Cancel Test";
            this.btnCancelTest.UseVisualStyleBackColor = false;
            this.btnCancelTest.Click += new System.EventHandler(this.BtnCancelTest_Click);

            // ── RetryCountdownForm ────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 366);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.lblAttempt);
            this.Controls.Add(this.lblErrorCaption);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.lblCountdownCaption);
            this.Controls.Add(this.lblCountdown);
            this.Controls.Add(this.pnlButtons);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RetryCountdownForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Equipment Communication Failure";
            this.TopMost = true;

            this.pnlHeader.ResumeLayout(false);
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Panel   pnlHeader;
        private System.Windows.Forms.Label   lblTitle;
        private System.Windows.Forms.Label   lblAttempt;
        private System.Windows.Forms.Label   lblErrorCaption;
        private System.Windows.Forms.Label   lblError;
        private System.Windows.Forms.Label   lblCountdownCaption;
        private System.Windows.Forms.Label   lblCountdown;
        private System.Windows.Forms.Panel   pnlButtons;
        private System.Windows.Forms.Button  btnResumeNow;
        private System.Windows.Forms.Button  btnCancelTest;
    }
}
