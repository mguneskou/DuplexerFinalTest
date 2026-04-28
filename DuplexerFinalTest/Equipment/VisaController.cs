using System;
using NationalInstruments.Visa;

namespace DuplexerFinalTest.Equipment
{
    public class VisaController
    {
        private MessageBasedSession _session;
        private ResourceManager _resourceManager;
        private const int MaxBytes = 16777216;

        public bool OpenSession(string resourceName)
        {
            try
            {
                _resourceManager = new ResourceManager();
                _session = (MessageBasedSession)_resourceManager.Open(resourceName);
                _session.TimeoutMilliseconds = 30000;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void CloseSession()
        {
            try
            {
                _session?.Dispose();
                _resourceManager?.Dispose();
            }
            catch { }
            finally
            {
                _session = null;
                _resourceManager = null;
            }
        }

        public bool IsOpen => _session != null;

        public string Query(string command)
        {
            if (_session == null) return string.Empty;
            _session.RawIO.Write(command + "\n");
            return _session.RawIO.ReadString(MaxBytes);
        }

        public void Write(string command)
        {
            _session?.RawIO.Write(command + "\n");
        }

        public string Read()
        {
            if (_session == null) return string.Empty;
            return _session.RawIO.ReadString(MaxBytes);
        }
    }
}
