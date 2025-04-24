using System;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class IdentityColumnInfo
    {
        public string DatabaseName { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public long CurrentValue { get; set; }
        public long LastValue { get; set; }
        public long Increment { get; set; }
        public long SeedValue { get; set; }
        public float PercentUsed { get; set; }
        public DateTime LastChecked { get; set; }
        
        // Calculated property to determine how soon the identity might exhaust
        public bool IsNearingExhaustion => PercentUsed > 80;
    }
} 