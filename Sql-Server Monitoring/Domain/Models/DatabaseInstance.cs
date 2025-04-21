using System.ComponentModel.DataAnnotations;

namespace Sql_Server_Monitoring.Domain.Models
{
    /// <summary>
    /// Represents a SQL Server database instance
    /// </summary>
    public class DatabaseInstance
    {
        /// <summary>
        /// Unique identifier for the database instance
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the database instance
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Server name or IP address
        /// </summary>
        [Required]
        [StringLength(255)]
        public string ServerName { get; set; }

        /// <summary>
        /// Port number for the SQL Server instance
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Authentication type (Windows or SQL Server)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AuthenticationType { get; set; }

        /// <summary>
        /// Username for SQL Server authentication
        /// </summary>
        [StringLength(100)]
        public string Username { get; set; }

        /// <summary>
        /// Password for SQL Server authentication (should be encrypted in storage)
        /// </summary>
        [StringLength(255)]
        public string Password { get; set; }

        /// <summary>
        /// Connection string for the database instance
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Flag indicating if this instance is being actively monitored
        /// </summary>
        public bool IsMonitored { get; set; } = true;

        /// <summary>
        /// Date and time when this instance was added to monitoring
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when this instance was last updated
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// Notes or comments about this database instance
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
