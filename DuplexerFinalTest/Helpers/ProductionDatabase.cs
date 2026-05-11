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

        /// <summary>
        /// Returns all DeviceCode values in MeasMain that match M{serialNo}A% ordered by DeviceCode.
        /// e.g. for serialNo "12345678": ["M12345678A", "M12345678A1", "M12345678A2"]
        /// </summary>
        public string[] GetExistingDeviceCodes(string serialNo)
        {
            try
            {
                EnsureConnected();
                string sql = "SELECT DeviceCode FROM MeasMain WHERE DeviceCode LIKE @pattern ORDER BY DeviceCode";
                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@pattern", $"M{serialNo}A%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<string>();
                        while (reader.Read()) list.Add(reader.GetString(0));
                        return list.ToArray();
                    }
                }
            }
            catch { return new string[0]; }
        }

        /// <summary>
        /// Saves a test result following the reference device-code-shift pattern:
        ///   - 1st test:  saves as M{sn}A
        ///   - 2nd test:  renames M{sn}A → M{sn}A1 in both tables, saves new as M{sn}A
        ///   - 3rd test:  renames M{sn}A1 → M{sn}A2, M{sn}A → M{sn}A1, saves new as M{sn}A
        ///   (latest result always has DeviceCode = M{sn}A)
        /// All renames and inserts are wrapped in one SQL transaction.
        /// </summary>
        public void SaveTestResultsWithHistory(MeasMainModel measMain, List<MeasManualTestModel> manualTests)
        {
            try
            {
                EnsureConnected();
                string baseCode = $"M{measMain.SerialNo}A";
                string[] existing = GetExistingDeviceCodes(measMain.SerialNo);
                Shared.logger?.Log($"DB save: SerialNo={measMain.SerialNo} | existing={existing.Length} records | DeviceCode={baseCode}");

                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Shift existing device codes up by 1 to free up baseCode (e.g. MA)
                        if (existing.Length == 1 && existing[0] == baseCode)
                        {
                            // Only "MA" exists → rename to "MA1"
                            RenameDeviceCode(baseCode, $"{baseCode}1", transaction);
                            Shared.logger?.Log($"DB: renamed {baseCode} → {baseCode}1");
                        }
                        else if (existing.Length > 1)
                        {
                            // e.g. ["MA", "MA1", "MA2"] → lastSuffix=2
                            // Rename from highest down: MA2→MA3, MA1→MA2, MA→MA1
                            int lastSuffix = int.Parse(existing[existing.Length - 1].Substring(baseCode.Length));
                            for (int counter = lastSuffix; counter >= 0; counter--)
                            {
                                string from = counter == 0 ? baseCode : $"{baseCode}{counter}";
                                string to = $"{baseCode}{counter + 1}";
                                RenameDeviceCode(from, to, transaction);
                                Shared.logger?.Log($"DB: renamed {from} → {to}");
                            }
                        }

                        // INSERT MeasMain with the base device code (always MA)
                        string sqlMain = @"INSERT INTO MeasMain
                            (DeviceCode,SerialNo,Operator,TestDate,TestTime,TestRig,SoftwareRev,ItemNo,ItemNoRev,Passed,TestType)
                            VALUES (@dc,@sn,@op,@td,@tt,@tr,@sr,@ino,@inr,@pass,@tt2)";
                        using (var cmd = new SqlCommand(sqlMain, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@dc", baseCode);
                            cmd.Parameters.AddWithValue("@sn", measMain.SerialNo);
                            cmd.Parameters.AddWithValue("@op", measMain.Operator ?? "");
                            cmd.Parameters.AddWithValue("@td", measMain.TestDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@tt", measMain.TestTime ?? DateTime.Now.ToString("HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@tr", measMain.TestRig ?? Environment.MachineName);
                            cmd.Parameters.AddWithValue("@sr", measMain.SoftwareRev ?? "");
                            cmd.Parameters.AddWithValue("@ino", measMain.ItemNo ?? "");
                            cmd.Parameters.AddWithValue("@inr", measMain.ItemNoRev);
                            cmd.Parameters.AddWithValue("@pass", measMain.Passed ? 1 : 0);
                            cmd.Parameters.AddWithValue("@tt2", measMain.TestType.ToString());
                            cmd.ExecuteNonQuery();
                        }

                        // INSERT MeasManualTest rows
                        string sqlManual = @"INSERT INTO MeasManualTest (DeviceCode,TestID,TestData,Passed)
                            VALUES (@dc,@tid,@td,@pass)";
                        foreach (var mt in manualTests)
                        {
                            using (var cmd = new SqlCommand(sqlManual, _connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@dc", baseCode);
                                cmd.Parameters.AddWithValue("@tid", mt.TestID);
                                cmd.Parameters.AddWithValue("@td",
                                    (double.IsNaN(mt.TestData) || double.IsInfinity(mt.TestData))
                                        ? (object)DBNull.Value
                                        : mt.TestData);
                                cmd.Parameters.AddWithValue("@pass", mt.Passed ? 1 : 0);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        Shared.logger?.Log($"DB save complete: {baseCode} | {manualTests.Count} MeasManualTest rows", MessageType.Success);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Shared.logger?.LogError($"DB save rolled back for {baseCode}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.logger?.LogError($"SaveTestResultsWithHistory({measMain?.SerialNo})", ex);
            }
        }

        private void RenameDeviceCode(string from, string to, SqlTransaction transaction)
        {
            foreach (string table in new[] { "MeasMain", "MeasManualTest" })
            {
                using (var cmd = new SqlCommand($"UPDATE {table} SET DeviceCode=@to WHERE DeviceCode=@from", _connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    cmd.ExecuteNonQuery();
                }
            }
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
                Shared.logger?.Log($"GetFinalTestSpecs: dutType={dutType} itemNo={itemNo}");

                // Step 1: get the latest revision for this item number
                int revisionNo = 0;
                string sqlRev = "SELECT TOP 1 RevisionNo FROM specmain WHERE Itemno = @itemNo AND RevisionDate <= GETDATE() ORDER BY RevisionNo DESC";
                using (var cmd = new SqlCommand(sqlRev, _connection))
                {
                    cmd.Parameters.AddWithValue("@itemNo", itemNo);
                    var scalar = cmd.ExecuteScalar();
                    if (scalar != null && scalar != DBNull.Value)
                        revisionNo = Convert.ToInt32(scalar);
                    else
                        Shared.logger?.Log($"GetFinalTestSpecs: no specmain row found for itemNo='{itemNo}' — check BASE_ITEM_NUMBER/REMOTE_ITEM_NUMBER in SettingsGeneral.json", MessageType.Warning);
                }
                Shared.logger?.Log($"GetFinalTestSpecs: itemNo={itemNo} revisionNo={revisionNo}");

                // Step 2: get all test specs joined from specmanuallimit + specmanualtest
                string sqlSpecs = @"SELECT t.TestID, t.TestCaption, t.TestUnit, l.LimitMin, l.LimitMax
                                    FROM specmanuallimit l
                                    INNER JOIN specmanualtest t ON l.testid = t.testid
                                    WHERE l.itemnoid = (SELECT ItemNoId FROM specmain WHERE itemno = @itemNo AND RevisionNo = @rev)
                                    ORDER BY t.TestID";
                using (var cmd = new SqlCommand(sqlSpecs, _connection))
                {
                    cmd.Parameters.AddWithValue("@itemNo", itemNo);
                    cmd.Parameters.AddWithValue("@rev", revisionNo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new FinalTestSpecModel()
                            {
                                TestID = reader.GetInt32(0),
                                TestCaption = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                TestUnit = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                LimitMin = reader.IsDBNull(3) ? 0.0 : reader.GetDouble(3),
                                LimitMax = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4)
                            });
                        }
                    }
                }
                Shared.logger?.Log($"GetFinalTestSpecs: loaded {list.Count} specs for {dutType}");
            }
            catch (Exception ex)
            {
                Shared.logger?.Log($"GetFinalTestSpecs FAILED for {dutType}: {ex.Message}", MessageType.Error);
            }
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
                    cmd.Parameters.AddWithValue("@tt2", model.TestType.ToString());
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
