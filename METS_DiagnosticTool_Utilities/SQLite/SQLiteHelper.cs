using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS_DiagnosticTool_Utilities.SQLite
{
    public class SQLiteHelper
    {
        private const string SQLiteConnectionString = @"Data Source = .\METSDiagnosticTool_DB.db; journal mode = WAL; synchronous = normal; temp_store = memory; mmap_size = 30000000000";

        #region Public Methods
        /// <summary>
        /// Check does Table (that is a Variable Address) exists, if not create it and Insert into PLC Variable Values and Timestamps
        /// </summary>
        /// <param name="plcVariableModel"></param>
        public static void SaveData(PLCVariableDataModel plcVariableModel)
        {
            using (IDbConnection cnn = new SQLiteConnection(SQLiteConnectionString))
            {
                // First check does the Table Exists if not Create it
                // Name of the Table cannot have dots inside as PLC variable Address has, so replace those with underscore
                string _tableName = plcVariableModel.VariableName.ToUpper().Replace('.', '_').Replace("[", string.Empty).Replace("]", string.Empty);
                string _query = string.Concat(string.Concat("CREATE TABLE if not exists ", _tableName, " (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, VariableName TEXT NOT NULL, VariableValue TEXT NOT NULL, UpdateDate TEXT NOT NULL, UpdateTime TEXT NOT NULL)"));
                cnn.Execute(_query);

                // Extra check does the Table Exists
                IEnumerable<PLCVariableDataModel> output = cnn.Query<PLCVariableDataModel>(string.Concat("SELECT 1 FROM sqlite_master WHERE type='table' AND name='", _tableName, "'"), new DynamicParameters());

                if (output.Count() > 0)
                    // Insert into Table that is a Variable Name
                    cnn.Execute(string.Concat("INSERT into ", _tableName, " (VariableName, VariableValue, UpdateDate, UpdateTime) " +
                                                                                               "values (@VariableName, @VariableValue, @UpdateDate, @UpdateTime)"), plcVariableModel);
            }
        }

        /// <summary>
        /// Get last row from a Table (that is a Variable Address)
        /// </summary>
        /// <param name="plcVariableAddress"></param>
        /// <returns></returns>
        public static PLCVariableDataModel GetLastRow(string plcVariableAddress)
        {
            using (IDbConnection cnn = new SQLiteConnection(SQLiteConnectionString))
            {
                // Name of the Table cannot have dots inside as PLC variable Address has, so replace those with underscore
                string _tableName = plcVariableAddress.ToUpper().Replace('.', '_').Replace("[", string.Empty).Replace("]", string.Empty);

                // First check does the table exists, without creating new one
                IEnumerable<PLCVariableDataModel> output = cnn.Query<PLCVariableDataModel>(string.Concat("SELECT 1 FROM sqlite_master WHERE type='table' AND name='", _tableName, "'"), new DynamicParameters());

                if (output.Count() > 0)
                {
                    output = cnn.Query<PLCVariableDataModel>(string.Concat("SELECT * FROM ", _tableName, " ORDER BY Id DESC LIMIT 1"), new DynamicParameters());

                    if (output.Count() > 0)
                        return output.First();
                    else
                        return null;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Delete whole Table if exists(that is a Variable Address)), and creates CSV File of dumped Table under Core Path
        /// </summary>
        /// <param name="corePath">Path to the Core file, needed for saving the CSV</param>
        /// <param name="plcVariableAddress"></param>
        public static void DeleteTable(string corePath, string plcVariableAddress)
        {
            string _tableName = plcVariableAddress.ToUpper().Replace('.', '_').Replace("[", string.Empty).Replace("]", string.Empty);

            // Dump Table to CSV Before Deleting
            Utility.CheckDirCreate(string.Concat(Path.GetDirectoryName(corePath), @"\CSVData\"));

            DumpTableToCSV(string.Concat(Path.GetDirectoryName(corePath), @"\CSVData\", _tableName, ".csv"), _tableName);

            using (IDbConnection cnn = new SQLiteConnection(SQLiteConnectionString))
            {
                // First check does the Table Exists if not Create it
                // Name of the Table cannot have dots inside as PLC variable Address has, so replace those with underscore
                string _query = string.Concat(string.Concat("DROP TABLE if exists ", _tableName));
                cnn.Execute(_query);
            }
        }

        /// <summary>
        /// Create a CSV File from given SQLite Table
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="plcVariableAddress"></param>
        public static void DumpTableToCSV(string fileName, string plcVariableAddress)
        {
            IEnumerable<IDataRecord> source = SourceData(string.Concat("SELECT * FROM ", plcVariableAddress), plcVariableAddress);

            // If File already exist rename it
            if (File.Exists(fileName))
            {
                string dirCSV = Path.GetDirectoryName(fileName);
                string bareCSV = Path.GetFileName(fileName);
                string modifedCSV = bareCSV.Substring(0, bareCSV.Length - 4);
                string completeModifiedCSV = string.Concat(dirCSV, @"\", modifedCSV, "_deleted_", DateTime.Now.ToString("HH_mm_ss_dd_MM_yyyy"), ".csv");

                // Double safe if the file is going to be created in the very same second than the one already exists
                if (!File.Exists(completeModifiedCSV))
                    File.Move(fileName, completeModifiedCSV);
            }

            // Create CSV File
            File.WriteAllLines(fileName, ToCsv(source));
        }
        #endregion

        #region Private Methods
        private static IEnumerable<IDataRecord> SourceData(string sql, string tableName)
        {
            using (IDbConnection cnn = new SQLiteConnection(SQLiteConnectionString))
            {
                cnn.Open();

                IEnumerable<PLCVariableDataModel> output = cnn.Query<PLCVariableDataModel>(string.Concat("SELECT 1 FROM sqlite_master WHERE type='table' AND name='", tableName, "'"), new DynamicParameters());

                if (output.Count() > 0)
                {
                    using (SQLiteCommand q = new SQLiteCommand(sql, (SQLiteConnection)cnn))
                    {
                        using (var reader = q.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //TODO: you may want to add additional conditions here

                                yield return reader;
                            }
                        }
                    }
                }

                cnn.Close();
            }
        }

        private static IEnumerable<string> ToCsv(IEnumerable<IDataRecord> data)
        {
            foreach (IDataRecord record in data)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < record.FieldCount; ++i)
                {
                    string chunk = Convert.ToString(record.GetValue(i));

                    if (i > 0)
                        sb.Append(';');

                    if (chunk.Contains(',') || chunk.Contains(';'))
                        chunk = "\"" + chunk.Replace("\"", "\"\"") + "\"";

                    sb.Append(chunk);
                }

                yield return sb.ToString();
            }
        }
        #endregion
    }
}
