namespace Sql_Server_Monitoring.Domain.Models
{
    /// <summary>
    /// Represents the type of database backup
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Full backup of the database
        /// </summary>
        Full = 0,

        /// <summary>
        /// Differential backup, containing changes since the last full backup
        /// </summary>
        Differential = 1,

        /// <summary>
        /// Transaction log backup, containing all transaction logs since the last backup
        /// </summary>
        TransactionLog = 2
    }
} 