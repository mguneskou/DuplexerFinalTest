using DuplexerFinalTest.Helpers;
using DuplexerFinalTest.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace DuplexerFinalTest.Helpers
{
    public class ProductionDatabase
    {
        private SqlConnection _connection;
        private string _connectionString;

        public void ConnectToServer()
        {
            var s = Shared.sharedGeneralSettings?.GeneralSettings[0];
            if (s == null) throw new InvalidOperationException("Settings not loaded.");

            bool useLocal = s.USE_LOCAL_DATABASE?.Trim().ToLower() == "true";
            if (useLocal)
                ConnectToLocalServer();
            else
                ConnectToProductionServer();
        }

        private void ConnectToLocalServer()
        {
            var s = Shared.sharedGeneralSettings.GeneralSettings[0];
            _connectionString = !string.IsNullOrWhiteSpace(s.LOCAL_DATABASE_CONNECTION_STRING)
                ? s.LOCAL_DATABASE_CONNECTION_STRING
                : "Server=.\\SQLEXPRESS;Database=JDS_Production;Integrated Security=SSPI;TrustServerCertificate=True;";
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        private void ConnectToProductionServer()
        {
            // Read from registry
            string regPath = @"SOFTWARE\WOW6432Node\JDS Uniphase\MultiTier\SQL Torquay";
            string regValue = null;
            using (var key = Registry.LocalMachine.OpenSubKey(regPath))
            {
                regValue = key?.GetValue("")?.ToString();
            }

            if (string.IsNullOrEmpty(regValue))
                throw new Exception("Production database connection string not found in registry.");

            // Strip Provider=...;
            regValue = Regex.Replace(regValue, @"Provider=[^;]+;", "", RegexOptions.IgnoreCase).Trim();
            // Append password
            if (!regValue.EndsWith(";")) regValue += ";";
            regValue += "pwd=wideband;TrustServerCertificate=True;";
            _connectionString = regValue;
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        public bool IsExistingRecord(string deviceCode, out int finalIncrement)
        {
            finalIncrement = 0;
            try
            {
                EnsureConnected();
                string sql = "SELECT MAX(FinalIncrement) FROM MeasMain WHERE DeviceCode = @dc";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@dc", deviceCode);
                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        finalIncrement = Convert.ToInt32(result);
                        return true;
                    }
                    return false;
                }
            }
            catch { return false; }
        }

        public void UpdateExistingRecord(string deviceCode, int finalIncrement)
        {
            try
            {
                EnsureConnected();
                string sql = "UPDATE MeasMain SET FinalIncrement = @fi WHERE DeviceCode = @dc";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@fi", finalIncrement);
                    cmd.Parameters.AddWithValue("@dc", deviceCode);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public List<FinalTestSpecModel> GetFinalTestSpecs(DUTType dutType)
        {
            var list = new List<FinalTestSpecModel>();
            try
            {
                EnsureConnected();
                string itemNo = dutType == DUTType.Base
                    ? Shared.sharedGeneralSettings.GeneralSettings[0].BASE_ITEM_NUMBER
                    : Shared.sharedGeneralSettings.GeneralSettings[0].REMOTE_ITEM_NUMBER;
                string sql = @"SELECT ts.TestID, ts.TestCaption, ts.TestUnit, ts.LimitMin, ts.LimitMax 
                               FROM FinalTestSpecs ts 
                               INNER JOIN ItemSpec i ON i.TestID = ts.TestID 
                               WHERE i.ItemNo = @itemNo ORDER BY ts.TestID";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@itemNo", itemNo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new FinalTestSpecModel()
                            {
                                TestID = reader.GetInt32(0),
                                TestCaption = reader.GetString(1),
                                TestUnit = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                LimitMin = reader.IsDBNull(3) ? 0f : reader.GetFloat(3),
                                LimitMax = reader.IsDBNull(4) ? 0f : reader.GetFloat(4)
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public List<DataTable> GetTestDataTables(string serialNo, string itemNo, List<int> testIDs, out int specRevision)
        {
            specRevision = 0;
            var tables = new List<DataTable>();
            try
            {
                EnsureConnected();
                foreach (var testId in testIDs)
                {
                    string sql = "SELECT * FROM MeasManualTest WHERE SerialNo = @sn AND ItemNo = @ino AND TestID = @tid";
                    using (var cmd = new SqlCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@sn", serialNo);
                        cmd.Parameters.AddWithValue("@ino", itemNo);
                        cmd.Parameters.AddWithValue("@tid", testId);
                        var dt = new DataTable();
                        using (var adapter = new SqlDataAdapter(cmd))
                            adapter.Fill(dt);
                        tables.Add(dt);
                    }
                }
            }
            catch { }
            return tables;
        }

        public bool InsertIntoMeasMain(MeasMainModel model)
        {
            try
            {
                EnsureConnected();
                string sql = @"INSERT INTO MeasMain 
                    (DeviceCode, SerialNo, Operator, TestDate, TestTime, TestRig, SoftwareRev, ItemNo, ItemNoRev, Passed, TestType)
                    VALUES (@dc, @sn, @op, @td, @tt, @tr, @sr, @ino, @inr, @pass, @tt2)";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@dc", model.DeviceCode);
                    cmd.Parameters.AddWithValue("@sn", model.SerialNo);
                    cmd.Parameters.AddWithValue("@op", model.Operator ?? "");
                    cmd.Parameters.AddWithValue("@td", model.TestDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@tt", model.TestTime ?? DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@tr", model.TestRig ?? Environment.MachineName);
                    cmd.Parameters.AddWithValue("@sr", model.SoftwareRev ?? Shared.SoftwareVersion);
                    cmd.Parameters.AddWithValue("@ino", model.ItemNo ?? "");
                    cmd.Parameters.AddWithValue("@inr", model.ItemNoRev);
                    cmd.Parameters.AddWithValue("@pass", model.Passed ? 1 : 0);
                    cmd.Parameters.AddWithValue("@tt2", (int)model.TestType);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch { return false; }
        }

        public bool InsertIntoMeasManualTest(MeasManualTestModel model)
        {
            try
            {
                EnsureConnected();
                string sql = @"INSERT INTO MeasManualTest (DeviceCode, TestID, TestData, Passed) 
                               VALUES (@dc, @tid, @td, @pass)";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@dc", model.DeviceCode);
                    cmd.Parameters.AddWithValue("@tid", model.TestID);
                    cmd.Parameters.AddWithValue("@td", model.TestData);
                    cmd.Parameters.AddWithValue("@pass", model.Passed ? 1 : 0);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch { return false; }
        }

        private void EnsureConnected()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                ConnectToServer();
        }

        public void Close()
        {
            try { _connection?.Close(); }
            catch { }
        }
    }
}
