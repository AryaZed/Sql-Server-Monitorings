namespace Sql_Server_Monitoring.Domain.Models
{
    public class DbIssue
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IssueType Type { get; set; }
        public IssueSeverity Severity { get; set; }
        public string Message { get; set; }
        public string RecommendedAction { get; set; }
        public string SqlScript { get; set; }
        public string AffectedObject { get; set; }
        public string DatabaseName { get; set; }
        public DateTime DetectionTime { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedTime { get; set; }
    }
}
