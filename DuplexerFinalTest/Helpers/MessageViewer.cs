using System;
using System.Windows.Forms;

namespace DuplexerFinalTest.Helpers
{
    public class MessageViewer
    {
        private readonly Form _owner;

        public MessageViewer(Form owner)
        {
            _owner = owner;
        }

        public void ShowMessage(string message, MessageType type = MessageType.Message, string title = null)
        {
            if (_owner == null || _owner.IsDisposed) return;

            MessageBoxIcon icon = type switch
            {
                MessageType.Error => MessageBoxIcon.Error,
                MessageType.Warning => MessageBoxIcon.Warning,
                MessageType.Success => MessageBoxIcon.Information,
                _ => MessageBoxIcon.Information
            };

            string caption = title ?? type.ToString();

            if (_owner.InvokeRequired)
                _owner.Invoke((Action)(() => MessageBox.Show(_owner, message, caption, MessageBoxButtons.OK, icon)));
            else
                MessageBox.Show(_owner, message, caption, MessageBoxButtons.OK, icon);
        }

        public DialogResult ShowQuestion(string message, string title = "Confirm")
        {
            if (_owner != null && _owner.InvokeRequired)
            {
                return (DialogResult)_owner.Invoke((Func<DialogResult>)(() =>
                    MessageBox.Show(_owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)));
            }
            return MessageBox.Show(_owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public void AddNewMessage(string message, MessageType type = MessageType.Message)
        {
            Shared.logger?.Log($"[{type}] {message}");
            ShowMessage(message, type);
        }

        public void AddNewMessageThreadSafe(string message, MessageType type = MessageType.Message)
        {
            Shared.logger?.Log($"[{type}] {message}");
            if (_owner != null && !_owner.IsDisposed)
            {
                if (_owner.InvokeRequired)
                    _owner.BeginInvoke((Action)(() => ShowMessage(message, type)));
                else
                    ShowMessage(message, type);
            }
        }
    }
}
