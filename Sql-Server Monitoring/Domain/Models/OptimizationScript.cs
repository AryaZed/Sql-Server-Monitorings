namespace Sql_Server_Monitoring.Domain.Models
{
    public class OptimizationScript
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Content { get; set; }
        public string Description { get; set; }
        public string DatabaseName { get; set; }
        public ScriptType Type { get; set; }
        public List<Guid> RelatedIssueIds { get; set; } = new List<Guid>();
        public DateTime GenerationTime { get; set; } = DateTime.Now;
        public bool IsExecuted { get; set; }
        public DateTime? ExecutionTime { get; set; }
        public string ExecutionResult { get; set; }
    }
}
