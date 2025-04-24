using System;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class DbccCheckHistory
    {
        public string DatabaseName { get; set; }
        public DateTime LastGoodCheckDate { get; set; }
        public string CheckType { get; set; } // CHECKDB, CHECKALLOC, CHECKTABLE, etc.
        public bool HasErrors { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }
} 