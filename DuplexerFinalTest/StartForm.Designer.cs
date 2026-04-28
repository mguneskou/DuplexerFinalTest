namespace DuplexerFinalTest
{
    partial class StartForm
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
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.tlpDUT = new System.Windows.Forms.TableLayoutPanel();
            this.lstTestSequences = new System.Windows.Forms.ListBox();
            this.lblOperator = new System.Windows.Forms.Label();
            this.txtOperator = new System.Windows.Forms.TextBox();
            this.lblTestDate = new System.Windows.Forms.Label();
            this.dtpTestDate = new System.Windows.Forms.DateTimePicker();
            this.lblBaseCount = new System.Windows.Forms.Label();
            this.nudBaseCount = new System.Windows.Forms.NumericUpDown();
            this.lblRemoteCount = new System.Windows.Forms.Label();
            this.nudRemoteCount = new System.Windows.Forms.NumericUpDown();
            this.btnPretest = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblTestSequences = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRemoteCount)).BeginInit();
            this.tlpMain.SuspendLayout();
            this.SuspendLayout();

            // tlpMain
            this.tlpMain.ColumnCount = 4;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tlpMain.RowCount = 8;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.TabIndex = 0;
            this.tlpMain.Controls.Add(this.lblOperator, 0, 0);
            this.tlpMain.Controls.Add(this.txtOperator, 1, 0);
            this.tlpMain.Controls.Add(this.lblTestDate, 0, 1);
            this.tlpMain.Controls.Add(this.dtpTestDate, 1, 1);
            this.tlpMain.Controls.Add(this.lblBaseCount, 0, 2);
            this.tlpMain.Controls.Add(this.nudBaseCount, 1, 2);
            this.tlpMain.Controls.Add(this.lblRemoteCount, 0, 3);
            this.tlpMain.Controls.Add(this.nudRemoteCount, 1, 3);
            this.tlpMain.Controls.Add(this.lblTestSequences, 2, 0);
            this.tlpMain.Controls.Add(this.lstTestSequences, 2, 1);
            this.tlpMain.SetRowSpan(this.lstTestSequences, 4);
            this.tlpMain.SetColumnSpan(this.lstTestSequences, 2);
            this.tlpMain.Controls.Add(this.tlpDUT, 0, 5);
            this.tlpMain.SetColumnSpan(this.tlpDUT, 4);
            this.tlpMain.Controls.Add(this.btnPretest, 0, 6);
            this.tlpMain.Controls.Add(this.btnStart, 1, 6);
            this.tlpMain.Controls.Add(this.btnCancel, 3, 6);

            // lblOperator
            this.lblOperator.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblOperator.AutoSize = true;
            this.lblOperator.Name = "lblOperator";
            this.lblOperator.Text = "Operator:";
            this.lblOperator.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // txtOperator
            this.txtOperator.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtOperator.Name = "txtOperator";
            this.txtOperator.TabIndex = 1;

            // lblTestDate
            this.lblTestDate.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTestDate.AutoSize = true;
            this.lblTestDate.Name = "lblTestDate";
            this.lblTestDate.Text = "Test Date:";
            this.lblTestDate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // dtpTestDate
            this.dtpTestDate.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dtpTestDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTestDate.Name = "dtpTestDate";
            this.dtpTestDate.TabIndex = 2;

            // lblBaseCount
            this.lblBaseCount.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblBaseCount.AutoSize = true;
            this.lblBaseCount.Name = "lblBaseCount";
            this.lblBaseCount.Text = "# Base Units:";
            this.lblBaseCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // nudBaseCount
            this.nudBaseCount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudBaseCount.Minimum = 0;
            this.nudBaseCount.Maximum = 12;
            this.nudBaseCount.Value = 1;
            this.nudBaseCount.Name = "nudBaseCount";
            this.nudBaseCount.TabIndex = 3;
            this.nudBaseCount.ValueChanged += new System.EventHandler(this.NudCount_ValueChanged);

            // lblRemoteCount
            this.lblRemoteCount.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblRemoteCount.AutoSize = true;
            this.lblRemoteCount.Name = "lblRemoteCount";
            this.lblRemoteCount.Text = "# Remote Units:";
            this.lblRemoteCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // nudRemoteCount
            this.nudRemoteCount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudRemoteCount.Minimum = 0;
            this.nudRemoteCount.Maximum = 12;
            this.nudRemoteCount.Value = 1;
            this.nudRemoteCount.Name = "nudRemoteCount";
            this.nudRemoteCount.TabIndex = 4;
            this.nudRemoteCount.ValueChanged += new System.EventHandler(this.NudCount_ValueChanged);

            // lblTestSequences
            this.lblTestSequences.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.lblTestSequences.AutoSize = true;
            this.lblTestSequences.Name = "lblTestSequences";
            this.lblTestSequences.Text = "Test Sequence:";

            // lstTestSequences
            this.lstTestSequences.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstTestSequences.Name = "lstTestSequences";
            this.lstTestSequences.TabIndex = 5;

            // tlpDUT
            this.tlpDUT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDUT.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tlpDUT.Name = "tlpDUT";
            this.tlpDUT.TabIndex = 6;

            // btnPretest
            this.btnPretest.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnPretest.Enabled = false;
            this.btnPretest.Name = "btnPretest";
            this.btnPretest.Size = new System.Drawing.Size(100, 30);
            this.btnPretest.TabIndex = 10;
            this.btnPretest.Text = "Pretest";
            this.btnPretest.UseVisualStyleBackColor = true;
            this.btnPretest.Click += new System.EventHandler(this.BtnPretest_Click);

            // btnStart
            this.btnStart.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnStart.Enabled = false;
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(100, 30);
            this.btnStart.TabIndex = 11;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);

            // btnCancel
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // StartForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 650);
            this.Controls.Add(this.tlpMain);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "StartForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Test Setup";
            this.Load += new System.EventHandler(this.StartForm_Load);

            ((System.ComponentModel.ISupportInitialize)(this.nudBaseCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRemoteCount)).EndInit();
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.TableLayoutPanel tlpDUT;
        private System.Windows.Forms.ListBox lstTestSequences;
        private System.Windows.Forms.Label lblOperator;
        private System.Windows.Forms.TextBox txtOperator;
        private System.Windows.Forms.Label lblTestDate;
        private System.Windows.Forms.DateTimePicker dtpTestDate;
        private System.Windows.Forms.Label lblBaseCount;
        private System.Windows.Forms.NumericUpDown nudBaseCount;
        private System.Windows.Forms.Label lblRemoteCount;
        private System.Windows.Forms.NumericUpDown nudRemoteCount;
        private System.Windows.Forms.Button btnPretest;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblTestSequences;
    }
}
