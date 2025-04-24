using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Data;

namespace Sql_Server_Monitoring.Application.Services
{
    public class AgentJobsService : IAgentJobsService
    {
        private readonly ILogger<AgentJobsService> _logger;
        private readonly IIssueRepository _issueRepository;
        private readonly IAlertService _alertService;

        public AgentJobsService(
            ILogger<AgentJobsService> logger,
            IIssueRepository issueRepository,
            IAlertService alertService)
        {
            _logger = logger;
            _issueRepository = issueRepository;
            _alertService = alertService;
        }

        public async Task<IEnumerable<AgentJob>> GetAllJobsAsync(string connectionString)
        {
            var jobs = new List<AgentJob>();

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get list of all jobs
                string jobsSql = @"
                    SELECT 
                        j.job_id,
                        j.name,
                        j.description,
                        j.enabled,
                        j.date_created,
                        j.date_modified,
                        c.name AS category,
                        SUSER_SNAME(j.owner_sid) AS owner,
                        CASE 
                            WHEN ja.run_requested_date IS NOT NULL AND ja.stop_execution_date IS NULL THEN 'Running'
                            WHEN j.enabled = 0 THEN 'Disabled'
                            ELSE 'Idle'
                        END AS current_status,
                        jh.run_date AS last_run_date,
                        js.next_run_date,
                        CASE jh.run_status
                            WHEN 0 THEN 'Failed'
                            WHEN 1 THEN 'Succeeded'
                            WHEN 2 THEN 'Retry'
                            WHEN 3 THEN 'Canceled'
                            WHEN 4 THEN 'In Progress'
                            ELSE 'Unknown'
                        END AS last_run_outcome,
                        CAST(((jh.run_duration / 10000 * 3600) + 
                              ((jh.run_duration % 10000) / 100 * 60) + 
                              (jh.run_duration % 100)) AS INT) AS last_run_duration_seconds,
                        (SELECT COUNT(*) FROM msdb.dbo.sysjobhistory h 
                         WHERE h.job_id = j.job_id AND h.step_id = 0 AND h.run_status = 1) AS success_count,
                        (SELECT COUNT(*) FROM msdb.dbo.sysjobhistory h 
                         WHERE h.job_id = j.job_id AND h.step_id = 0 AND h.run_status = 0) AS failure_count
                    FROM 
                        msdb.dbo.sysjobs j
                    LEFT JOIN 
                        msdb.dbo.syscategories c ON j.category_id = c.category_id
                    LEFT JOIN 
                        msdb.dbo.sysjobactivity ja ON j.job_id = ja.job_id AND ja.session_id = (SELECT MAX(session_id) FROM msdb.dbo.sysjobactivity)
                    LEFT JOIN 
                        (SELECT job_id, MAX(instance_id) AS instance_id 
                         FROM msdb.dbo.sysjobhistory 
                         WHERE step_id = 0 
                         GROUP BY job_id) AS last_hist ON j.job_id = last_hist.job_id
                    LEFT JOIN 
                        msdb.dbo.sysjobhistory jh ON last_hist.job_id = jh.job_id AND last_hist.instance_id = jh.instance_id
                    LEFT JOIN 
                        msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
                    ORDER BY 
                        j.name";

                await using var jobsCommand = new SqlCommand(jobsSql, connection);
                await using var jobsReader = await jobsCommand.ExecuteReaderAsync();

                while (await jobsReader.ReadAsync())
                {
                    var job = new AgentJob
                    {
                        JobId = jobsReader["job_id"].ToString(),
                        JobName = jobsReader["name"].ToString(),
                        Description = jobsReader["description"]?.ToString(),
                        Enabled = (bool)jobsReader["enabled"],
                        CreatedDate = jobsReader["date_created"] != DBNull.Value ? (DateTime)jobsReader["date_created"] : DateTime.MinValue,
                        LastModifiedDate = jobsReader["date_modified"] != DBNull.Value ? (DateTime)jobsReader["date_modified"] : DateTime.MinValue,
                        Category = jobsReader["category"]?.ToString(),
                        Owner = jobsReader["owner"]?.ToString(),
                        CurrentStatus = jobsReader["current_status"]?.ToString(),
                        LastRunOutcome = jobsReader["last_run_outcome"]?.ToString(),
                        SuccessCount = jobsReader["success_count"] != DBNull.Value ? Convert.ToInt32(jobsReader["success_count"]) : 0,
                        FailureCount = jobsReader["failure_count"] != DBNull.Value ? Convert.ToInt32(jobsReader["failure_count"]) : 0
                    };

                    if (jobsReader["last_run_date"] != DBNull.Value)
                    {
                        job.LastRunDate = (DateTime)jobsReader["last_run_date"];
                    }

                    if (jobsReader["next_run_date"] != DBNull.Value)
                    {
                        job.NextRunDate = (DateTime)jobsReader["next_run_date"];
                    }

                    if (jobsReader["last_run_duration_seconds"] != DBNull.Value)
                    {
                        job.LastRunDuration = TimeSpan.FromSeconds(Convert.ToInt32(jobsReader["last_run_duration_seconds"]));
                    }

                    jobs.Add(job);
                }

                await jobsReader.CloseAsync();

                // Get job history for each job
                foreach (var job in jobs)
                {
                    job.History = await GetJobHistoryAsync(connection, job.JobId);
                    job.Steps = await GetJobStepsAsync(connection, job.JobId);
                }

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SQL Agent jobs");
                throw;
            }
        }

        public async Task<AgentJob> GetJobDetailsByNameAsync(string connectionString, string jobName)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string jobSql = @"
                    SELECT job_id FROM msdb.dbo.sysjobs WHERE name = @JobName";

                await using var command = new SqlCommand(jobSql, connection);
                command.Parameters.AddWithValue("@JobName", jobName);
                var jobId = await command.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(jobId))
                {
                    return null;
                }

                var jobs = await GetAllJobsAsync(connectionString);
                return jobs.FirstOrDefault(j => j.JobId == jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving SQL Agent job details for {jobName}");
                throw;
            }
        }

        public async Task<IEnumerable<AgentJob>> GetFailedJobsAsync(string connectionString, int lookbackHours = 24)
        {
            try
            {
                var allJobs = await GetAllJobsAsync(connectionString);
                var cutoffTime = DateTime.Now.AddHours(-lookbackHours);

                return allJobs.Where(job => 
                    job.LastRunDate != null && 
                    job.LastRunDate > cutoffTime && 
                    job.LastRunOutcome == "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving failed SQL Agent jobs for the past {lookbackHours} hours");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeJobsAsync(string connectionString, string databaseName = null)
        {
            var issues = new List<DbIssue>();

            try
            {
                var allJobs = await GetAllJobsAsync(connectionString);
                
                // Find failed jobs in the last 24 hours
                var failedJobs = allJobs.Where(j => 
                    j.LastRunDate.HasValue && 
                    j.LastRunDate.Value > DateTime.Now.AddHours(-24) && 
                    j.LastRunOutcome == "Failed");
                
                foreach (var job in failedJobs)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.AgentJob,
                        Severity = IssueSeverity.High,
                        Message = $"SQL Agent job '{job.JobName}' failed on {job.LastRunDate?.ToString("yyyy-MM-dd HH:mm:ss")}",
                        RecommendedAction = "Review job history for detailed error information",
                        DatabaseName = databaseName,
                        AffectedObject = job.JobName,
                        DetectionTime = DateTime.Now
                    };
                    
                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);
                    
                    // Create alert for failed job
                    await _alertService.CreateAlertAsync(
                        connectionString,
                        databaseName,
                        AlertType.JobFailure,
                        $"SQL Agent job '{job.JobName}' failed",
                        issue.RecommendedAction,
                        IssueSeverity.High);
                }
                
                // Find disabled jobs that should be enabled (assuming critical jobs should be running)
                var criticalJobCategories = new[] { "Database Maintenance", "Backup", "Replication", "Log Shipping" };
                var disabledCriticalJobs = allJobs.Where(j => 
                    !j.Enabled && 
                    criticalJobCategories.Contains(j.Category, StringComparer.OrdinalIgnoreCase));
                
                foreach (var job in disabledCriticalJobs)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.AgentJob,
                        Severity = IssueSeverity.Medium,
                        Message = $"Critical SQL Agent job '{job.JobName}' in category '{job.Category}' is disabled",
                        RecommendedAction = "Review and enable the job if it should be running",
                        DatabaseName = databaseName,
                        AffectedObject = job.JobName,
                        DetectionTime = DateTime.Now
                    };
                    
                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);
                }
                
                // Find jobs that haven't run in a long time (for enabled jobs)
                var inactiveJobs = allJobs.Where(j => 
                    j.Enabled && 
                    (!j.LastRunDate.HasValue || j.LastRunDate.Value < DateTime.Now.AddDays(-7)));
                
                foreach (var job in inactiveJobs)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.AgentJob,
                        Severity = IssueSeverity.Low,
                        Message = $"SQL Agent job '{job.JobName}' has not run in the last 7 days",
                        RecommendedAction = "Verify that the job schedule is configured correctly",
                        DatabaseName = databaseName,
                        AffectedObject = job.JobName,
                        DetectionTime = DateTime.Now
                    };
                    
                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing SQL Agent jobs");
            }

            return issues;
        }

        private async Task<List<AgentJobHistory>> GetJobHistoryAsync(SqlConnection connection, string jobId)
        {
            var history = new List<AgentJobHistory>();

            string historySql = @"
                SELECT TOP 30
                    CONVERT(DATETIME, RTRIM(h.run_date)) + 
                    STUFF(STUFF(RIGHT('000000' + RTRIM(h.run_time), 6), 5, 0, ':'), 3, 0, ':') AS run_datetime,
                    CASE h.run_status
                        WHEN 0 THEN 'Failed'
                        WHEN 1 THEN 'Succeeded'
                        WHEN 2 THEN 'Retry'
                        WHEN 3 THEN 'Canceled'
                        WHEN 4 THEN 'In Progress'
                        ELSE 'Unknown'
                    END AS outcome,
                    CAST(((h.run_duration / 10000 * 3600) + 
                          ((h.run_duration % 10000) / 100 * 60) + 
                          (h.run_duration % 100)) AS INT) AS run_duration_seconds,
                    h.message,
                    h.retries_attempted,
                    j.originating_server
                FROM 
                    msdb.dbo.sysjobhistory h
                INNER JOIN 
                    msdb.dbo.sysjobs j ON h.job_id = j.job_id
                WHERE 
                    h.job_id = @JobId AND h.step_id = 0
                ORDER BY 
                    run_datetime DESC";

            await using var command = new SqlCommand(historySql, connection);
            command.Parameters.AddWithValue("@JobId", jobId);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                history.Add(new AgentJobHistory
                {
                    RunDate = (DateTime)reader["run_datetime"],
                    Outcome = reader["outcome"].ToString(),
                    Duration = TimeSpan.FromSeconds(Convert.ToInt32(reader["run_duration_seconds"])),
                    Message = reader["message"].ToString(),
                    RetryAttempt = reader["retries_attempted"] != DBNull.Value ? Convert.ToInt32(reader["retries_attempted"]) : 0,
                    Server = reader["originating_server"].ToString()
                });
            }

            return history;
        }

        private async Task<List<AgentJobStep>> GetJobStepsAsync(SqlConnection connection, string jobId)
        {
            var steps = new List<AgentJobStep>();

            string stepsSql = @"
                SELECT 
                    s.step_id,
                    s.step_name,
                    s.subsystem,
                    s.command,
                    CASE s.on_success_action
                        WHEN 1 THEN 'Quit with success'
                        WHEN 2 THEN 'Quit with failure'
                        WHEN 3 THEN 'Go to next step'
                        WHEN 4 THEN 'Go to step ' + CAST(s.on_success_step_id AS VARCHAR)
                        ELSE 'Unknown'
                    END AS on_success_action,
                    CASE s.on_fail_action
                        WHEN 1 THEN 'Quit with success'
                        WHEN 2 THEN 'Quit with failure'
                        WHEN 3 THEN 'Go to next step'
                        WHEN 4 THEN 'Go to step ' + CAST(s.on_fail_step_id AS VARCHAR)
                        ELSE 'Unknown'
                    END AS on_fail_action,
                    jsh.run_status,
                    jsh.run_date,
                    jsh.run_duration
                FROM 
                    msdb.dbo.sysjobsteps s
                LEFT JOIN 
                    (SELECT 
                        job_id, step_id, 
                        MAX(instance_id) AS last_instance_id
                     FROM 
                        msdb.dbo.sysjobhistory
                     WHERE 
                        job_id = @JobId AND step_id > 0
                     GROUP BY 
                        job_id, step_id) AS last_step_hist 
                    ON s.job_id = last_step_hist.job_id AND s.step_id = last_step_hist.step_id
                LEFT JOIN 
                    msdb.dbo.sysjobhistory jsh 
                    ON last_step_hist.job_id = jsh.job_id 
                    AND last_step_hist.step_id = jsh.step_id 
                    AND last_step_hist.last_instance_id = jsh.instance_id
                WHERE 
                    s.job_id = @JobId
                ORDER BY 
                    s.step_id";

            await using var command = new SqlCommand(stepsSql, connection);
            command.Parameters.AddWithValue("@JobId", jobId);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var step = new AgentJobStep
                {
                    StepId = reader["step_id"].ToString(),
                    StepName = reader["step_name"].ToString(),
                    Subsystem = reader["subsystem"].ToString(),
                    Command = reader["command"].ToString(),
                    OnSuccessAction = reader["on_success_action"].ToString(),
                    OnFailAction = reader["on_fail_action"].ToString()
                };

                if (reader["run_status"] != DBNull.Value)
                {
                    var runStatus = Convert.ToInt32(reader["run_status"]);
                    step.LastRunOutcome = runStatus switch
                    {
                        0 => "Failed",
                        1 => "Succeeded",
                        2 => "Retry",
                        3 => "Canceled",
                        4 => "In Progress",
                        _ => "Unknown"
                    };
                }

                if (reader["run_date"] != DBNull.Value)
                {
                    step.LastRunDate = (DateTime)reader["run_date"];
                }

                if (reader["run_duration"] != DBNull.Value)
                {
                    var durationValue = Convert.ToInt32(reader["run_duration"]);
                    var hours = durationValue / 10000;
                    var minutes = (durationValue % 10000) / 100;
                    var seconds = durationValue % 100;
                    step.LastRunDuration = new TimeSpan(hours, minutes, seconds);
                }

                steps.Add(step);
            }

            return steps;
        }
    }
} 