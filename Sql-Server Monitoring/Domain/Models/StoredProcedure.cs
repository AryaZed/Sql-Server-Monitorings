namespace Sql_Server_Monitoring.Domain.Models
{
    public class StoredProcedure
    {
        public string SchemaName { get; set; }
        public string Name { get; set; }
        public string FullName => $"{SchemaName}.{Name}";
        public string Definition { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsEncrypted { get; set; }
        public int ParameterCount { get; set; }
        public bool HasDynamicSql { get; set; }
        public List<StoredProcedureParameter> Parameters { get; set; } = new List<StoredProcedureParameter>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public int ExecutionCount { get; set; }
        public int AverageDurationMs { get; set; }
        public int LastDurationMs { get; set; }
        public DateTime? LastExecutionTime { get; set; }
    }

    public class StoredProcedureParameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool HasDefault { get; set; }
        public bool IsOutput { get; set; }
        public int MaxLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
    }
} 