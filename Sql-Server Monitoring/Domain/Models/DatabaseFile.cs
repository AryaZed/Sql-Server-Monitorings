using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Sql_Server_Monitoring.Domain.Models
{
    /// <summary>
    /// Represents a physical file that stores database data or log
    /// </summary>
    public class DatabaseFile
    {
        /// <summary>
        /// Unique identifier for the database file
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the file
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Logical name of the file
        /// </summary>
        [Required]
        [StringLength(128)]
        public string LogicalName { get; set; }

        /// <summary>
        /// Physical file path
        /// </summary>
        [Required]
        [StringLength(260)]
        public string PhysicalPath { get; set; }

        /// <summary>
        /// Type of file (Data, Log, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string FileType { get; set; }

        /// <summary>
        /// Size of the file in MB
        /// </summary>
        public double SizeInMB { get; set; }

        /// <summary>
        /// Maximum size of the file in MB (null if unlimited)
        /// </summary>
        public double MaxSizeInMB { get; set; }

        /// <summary>
        /// Growth increment in MB or percentage
        /// </summary>
        public double GrowthValue { get; set; }

        /// <summary>
        /// Whether file is set to auto-grow
        /// </summary>
        public bool IsAutoGrowthEnabled { get; set; }

        /// <summary>
        /// ID of the database this file belongs to
        /// </summary>
        public int DatabaseId { get; set; }

        /// <summary>
        /// Navigation property for the database
        /// </summary>
        [ForeignKey("DatabaseId")]
        public Database Database { get; set; }

        /// <summary>
        /// Date and time when file was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when file was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Used space in MB
        /// </summary>
        public double UsedSpaceInMB { get; set; }

        /// <summary>
        /// Free space in MB
        /// </summary>
        public double FreeSpaceInMB => SizeInMB - UsedSpaceInMB;

        /// <summary>
        /// Free space percentage
        /// </summary>
        public double PercentUsed => Math.Round((UsedSpaceInMB / SizeInMB) * 100, 2);

        /// <summary>
        /// Free space percentage
        /// </summary>
        public double PercentFree => Math.Round(100 - PercentUsed, 2);

        /// <summary>
        /// Whether the file is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Growth type of the file
        /// </summary>
        public FileGrowthType GrowthType { get; set; }
    }
}
