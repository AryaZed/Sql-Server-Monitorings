using System;
using System.Collections.Generic;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class AgentJob
    {
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string Category { get; set; }
        public string Owner { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime? LastRunDate { get; set; }
        public DateTime? NextRunDate { get; set; }
        public string LastRunOutcome { get; set; }
        public TimeSpan? LastRunDuration { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<AgentJobHistory> History { get; set; } = new List<AgentJobHistory>();
        public List<AgentJobStep> Steps { get; set; } = new List<AgentJobStep>();
    }

    public class AgentJobHistory
    {
        public DateTime RunDate { get; set; }
        public string Outcome { get; set; }
        public TimeSpan Duration { get; set; }
        public string Message { get; set; }
        public int RetryAttempt { get; set; }
        public string Server { get; set; }
    }

    public class AgentJobStep
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string Subsystem { get; set; }
        public string Command { get; set; }
        public string OnSuccessAction { get; set; }
        public string OnFailAction { get; set; }
        public string LastRunOutcome { get; set; }
        public DateTime? LastRunDate { get; set; }
        public TimeSpan? LastRunDuration { get; set; }
    }
} 