using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS_DiagnosticTool_Utilities.SQLite
{
    public class SQLiteHelper
    {
        /// <summary>
        /// Check does Table (that is a Variable Address) exists, if not create it and Insert into PLC Variable Values and Timestamps
        /// </summary>
        /// <param name="plcVariableModel"></param>
        public static void SaveData(PLCVariableDataModel plcVariableModel)
        {
            try
            {
                using (IDbConnection cnn = new SQLiteConnection(@"Data Source = .\METSDiagnosticTool_DB.db"))
                {
                    // First check does the Table Exists if not Create it
                    // Name of the Table cannot have dots inside as PLC variable Address has, so replace those with underscore
                    string _tableName = plcVariableModel.VariableName.ToUpper().Replace('.', '_');
                    string _query = string.Concat(string.Concat("CREATE TABLE if not exists ", _tableName, " (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, VariableName TEXT NOT NULL, VariableValue TEXT NOT NULL, UpdateDate TEXT NOT NULL, UpdateTime TEXT NOT NULL)"));
                    cnn.Execute(_query);

                    // Insert into Table that is a Variable Name
                    cnn.Execute(string.Concat("INSERT into ", _tableName, " (VariableName, VariableValue, UpdateDate, UpdateTime) " +
                                                                                               "values (@VariableName, @VariableValue, @UpdateDate, @UpdateTime)"), plcVariableModel);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Exception when trying to save to SQLite ", ex.ToString()), Logger.logEvents.Blank);
            }
        }

        public static PLCVariableDataModel GetLastRow(string plcVariableAddress)
        {
            using (IDbConnection cnn = new SQLiteConnection(@"Data Source = .\METSDiagnosticTool_DB.db"))
            {
                // First check does the Table Exists if not Create it
                // Name of the Table cannot have dots inside as PLC variable Address has, so replace those with underscore
                string _tableName = plcVariableAddress.ToUpper().Replace('.', '_');
                string _query = string.Concat(string.Concat("CREATE TABLE if not exists ", _tableName, " (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, VariableName TEXT NOT NULL, VariableValue TEXT NOT NULL, UpdateDate TEXT NOT NULL, UpdateTime TEXT NOT NULL)"));
                cnn.Execute(_query);

                IEnumerable<PLCVariableDataModel> output = cnn.Query<PLCVariableDataModel>(string.Concat("SELECT * FROM ", _tableName, " ORDER BY Id DESC LIMIT 1"), new DynamicParameters());
                
                if (output.Count() > 0)
                    return output.First();
                else
                    return null;
            }
        }
    }
}
