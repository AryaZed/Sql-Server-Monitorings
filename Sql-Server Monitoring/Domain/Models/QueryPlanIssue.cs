using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public class QueryPlanIssue
    {
        public string IssueName { get; set; }
        public string Description { get; set; }
        public IssueSeverity Severity { get; set; }
        public string RecommendedAction { get; set; }
    }
}
