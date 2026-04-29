using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DuplexerFinalTest.Helpers
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _fileLock = new object();
        private ListView _listView;

        public string LogFilePath => _logFilePath;
        public string LogDirectory => Path.GetDirectoryName(_logFilePath);

        public Logger(string logFolder)
        {
            try { Directory.CreateDirectory(logFolder); } catch { }
            string fileName = $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            _logFilePath = Path.Combine(logFolder, fileName);
            WriteToFile($"=== DuplexerFinalTest Session Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }

        /// <summary>Attach a ListView for on-screen event display. Call once the handle is created.</summary>
        public void AttachListView(ListView lv)
        {
            _listView = lv;
            if (_listView == null) return;
            void Setup()
            {
                _listView.View = View.Details;
                _listView.FullRowSelect = true;
                _listView.GridLines = true;
                _listView.Font = new Font("Consolas", 8.5f);
                _listView.Columns.Clear();
                _listView.Columns.Add("Time", 75);
                _listView.Columns.Add("Type", 80);
                _listView.Columns.Add("Message", -2);
            }
            if (_listView.IsHandleCreated && _listView.InvokeRequired)
                _listView.Invoke((Action)Setup);
            else
                Setup();
        }

        public void Log(string message, MessageType type = MessageType.Message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            WriteToFile($"[{timestamp}] [{type,-8}] {message}");
            AddToListView(timestamp, type.ToString(), message, type);
        }

        public void LogError(string message, Exception ex = null)
        {
            string fullMsg = ex != null ? $"{message}: {ex.Message}" : message;
            Log(fullMsg, MessageType.Error);
        }

        private void WriteToFile(string line)
        {
            lock (_fileLock)
            {
                try { File.AppendAllText(_logFilePath, line + Environment.NewLine); }
                catch { }
            }
        }

        private void AddToListView(string time, string type, string message, MessageType msgType)
        {
            if (_listView == null || !_listView.IsHandleCreated) return;
            void AddItem()
            {
                var item = new ListViewItem(time);
                item.SubItems.Add(type);
                item.SubItems.Add(message);
                item.ForeColor = msgType switch
                {
                    MessageType.Error   => Color.Red,
                    MessageType.Warning => Color.DarkOrange,
                    MessageType.Success => Color.DarkGreen,
                    _                   => Color.Black
                };
                _listView.Items.Add(item);
                _listView.Items[_listView.Items.Count - 1].EnsureVisible();
            }
            if (_listView.InvokeRequired)
                _listView.BeginInvoke((Action)AddItem);
            else
                AddItem();
        }
    }
}

