using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class SensitiveColumn
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public SensitivityType SensitivityType { get; set; }
        public bool IsEncrypted { get; set; }
        public bool HasRowLevelSecurity { get; set; }
        public bool IsNullable { get; set; }
    }
}
