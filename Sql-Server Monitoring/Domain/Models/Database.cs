using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sql_Server_Monitoring.Domain.Models
{
    /// <summary>
    /// Represents a SQL Server database
    /// </summary>
    public class Database
    {
        /// <summary>
        /// Unique identifier for the database
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the database
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Size of the database in MB
        /// </summary>
        public decimal SizeInMB { get; set; }

        /// <summary>
        /// Status of the database (online, offline, etc)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Creation date of the database
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Last backup date
        /// </summary>
        public DateTime? LastBackupDate { get; set; }

        /// <summary>
        /// Recovery model (Simple, Full, Bulk-logged)
        /// </summary>
        [StringLength(50)]
        public string RecoveryModel { get; set; }

        /// <summary>
        /// Compatibility level of the database
        /// </summary>
        public int? CompatibilityLevel { get; set; }

        /// <summary>
        /// Database collation
        /// </summary>
        [StringLength(128)]
        public string Collation { get; set; }

        /// <summary>
        /// ID of the database instance this database belongs to
        /// </summary>
        public int DatabaseInstanceId { get; set; }

        /// <summary>
        /// Navigation property for the database instance
        /// </summary>
        [ForeignKey("DatabaseInstanceId")]
        public DatabaseInstance DatabaseInstance { get; set; }

        /// <summary>
        /// Date the database was last accessed
        /// </summary>
        public DateTime? LastAccessedDate { get; set; }

        /// <summary>
        /// User who owns the database
        /// </summary>
        [StringLength(128)]
        public string Owner { get; set; }

        /// <summary>
        /// Flag indicating if this database is being actively monitored
        /// </summary>
        public bool IsMonitored { get; set; } = true;

        /// <summary>
        /// Date and time when this database was last analyzed
        /// </summary>
        public DateTime? LastAnalyzedAt { get; set; }

        /// <summary>
        /// Notes or comments about this database
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
