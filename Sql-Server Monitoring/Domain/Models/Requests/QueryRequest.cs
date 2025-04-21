using System.ComponentModel.DataAnnotations;
using Sql_Server_Monitoring.Domain.Models.ValidationAttributes;

namespace Sql_Server_Monitoring.Domain.Models.Requests
{
    public class QueryRequest
    {
        [ConnectionStringRequired]
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; }

        [Required(ErrorMessage = "Database name is required")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Query text is required")]
        public string QueryText { get; set; }

        [Range(1, 3600, ErrorMessage = "Timeout must be between 1 and 3600 seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        [Range(1, 10000, ErrorMessage = "Max rows must be between 1 and 10000")]
        public int MaxRows { get; set; } = 1000;
    }

    public class StoredProcedureExecuteRequest
    {
        [ConnectionStringRequired]
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; }

        [Required(ErrorMessage = "Database name is required")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Schema name is required")]
        public string SchemaName { get; set; }

        [Required(ErrorMessage = "Procedure name is required")]
        public string ProcedureName { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class StoredProcedureCreateRequest
    {
        [ConnectionStringRequired]
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; }

        [Required(ErrorMessage = "Database name is required")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Schema name is required")]
        public string SchemaName { get; set; }

        [Required(ErrorMessage = "Procedure name is required")]
        public string ProcedureName { get; set; }

        [Required(ErrorMessage = "Definition is required")]
        public string Definition { get; set; }
    }

    public class DatabaseBackupRequest
    {
        [ConnectionStringRequired]
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; }

        [Required(ErrorMessage = "Database name is required")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Backup type is required")]
        public BackupType BackupType { get; set; }

        [Required(ErrorMessage = "Backup path is required")]
        public string BackupPath { get; set; }
    }

    public class DatabaseRestoreRequest
    {
        [ConnectionStringRequired]
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; }

        [Required(ErrorMessage = "Database name is required")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Backup file path is required")]
        public string BackupFilePath { get; set; }

        public string NewDatabaseName { get; set; }
    }
} 