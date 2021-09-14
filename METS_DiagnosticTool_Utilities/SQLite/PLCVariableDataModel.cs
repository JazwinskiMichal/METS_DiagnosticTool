namespace METS_DiagnosticTool_Utilities.SQLite
{
    public class PLCVariableDataModel
    {
        public int Id { get; set; }

        public string VariableName { get; set; }

        public string VariableValue { get; set; }

        public string UpdateDate { get; set; }

        public string UpdateTime { get; set; }
    }
}
