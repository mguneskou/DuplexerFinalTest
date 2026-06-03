using DuplexerFinalTest.Helpers;
using System;
using System.Collections.Generic;

namespace DuplexerFinalTest.Models
{
    public class CalibrationModel
    {
        public DateTime EffectiveTimestamp { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public Dictionary<string, double> Z_IB_IOP { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> Z_IPD { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> Z_IOP { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> Z_VPV { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public double GetValue(TestSequences testSequence, int slot)
        {
            if (slot <= 0)
                return double.NaN;

            Dictionary<string, double> section = GetSection(testSequence);
            if (section == null)
                return double.NaN;

            foreach (int pathIndex in GetCandidatePathIndexes(testSequence, slot))
            {
                if (section.TryGetValue($"path{pathIndex}", out double value))
                    return value;
            }

            return double.NaN;
        }

        private Dictionary<string, double> GetSection(TestSequences testSequence)
        {
            switch (testSequence)
            {
                case TestSequences.Base_Z_IB_IOP:
                    return Z_IB_IOP;
                case TestSequences.Base_Z_IPD:
                    return Z_IPD;
                case TestSequences.Remote_Z_IOP:
                    return Z_IOP;
                case TestSequences.Remote_Z_IPV:
                case TestSequences.Remote_Z_VPV:
                    return Z_VPV;
                default:
                    return null;
            }
        }

        private IEnumerable<int> GetCandidatePathIndexes(TestSequences testSequence, int slot)
        {
            bool isRemote = testSequence == TestSequences.Remote_Z_IOP
                || testSequence == TestSequences.Remote_Z_IPV
                || testSequence == TestSequences.Remote_Z_VPV;

            if (isRemote)
                yield return slot + 12;

            yield return slot;
        }
    }
}
