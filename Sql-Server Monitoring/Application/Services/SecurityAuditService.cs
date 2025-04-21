using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Application.Services
{
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ILogger<SecurityAuditService> _logger;
        private static readonly string[] SensitiveKeywords = new[]
        {
            "password", "pwd", "secret", "ssn", "creditcard", "credit_card", "card_number",
            "cardnumber", "ccnumber", "cc_number", "security", "social", "dob", "birthdate",
            "birth_date", "address", "email", "phone", "mobile", "passport", "license", "token"
        };

        private static readonly Dictionary<string, SensitivityType> ColumnPatterns = new()
        {
            { "password|pwd", SensitivityType.Password },
            { "ssn|social[_\\s]?security", SensitivityType.SSN },
            { "credit[_\\s]?card|cc[_\\s]?num|card[_\\s]?num", SensitivityType.CreditCard },
            { "email", SensitivityType.Email },
            { "phone|mobile|cell", SensitivityType.PhoneNumber },
            { "address|street|city|state|zip|postal", SensitivityType.Address },
            { "dob|birth[_\\s]?date", SensitivityType.DateOfBirth },
            { "passport", SensitivityType.PassportNumber },
            { "license|driver", SensitivityType.DriversLicense },
            { "token|secret|key", SensitivityType.SecurityToken }
        };

        public SecurityAuditService(
            IDatabaseRepository databaseRepository,
            ILogger<SecurityAuditService> logger)
        {
            _databaseRepository = databaseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<DbIssue>> AuditPermissionsAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Auditing permissions for database '{databaseName}'");
                var issues = new List<DbIssue>();

                // Get user permissions
                var permissions = await GetUserPermissionsAsync(connectionString, databaseName);

                // Check for over-privileged users
                var overPrivilegedUsers = permissions
                    .Where(p => p.PermissionType.Contains("CONTROL") || 
                                p.PermissionType.Contains("OWNER") || 
                                p.PermissionType.Contains("ALTER ANY"))
                    .GroupBy(p => p.UserName)
                    .Where(g => !g.Key.Equals("dbo", StringComparison.OrdinalIgnoreCase) && 
                               !g.Key.Equals("sa", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                foreach (var user in overPrivilegedUsers)
                {
                    var highPrivileges = user.Select(p => p.PermissionType).Distinct().ToList();
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.High,
                        DatabaseName = databaseName,
                        Message = $"User '{user.Key}' has excessive privileges: {string.Join(", ", highPrivileges)}",
                        RecommendedAction = "Review and limit permissions using the principle of least privilege",
                        DetectionTime = DateTime.Now
                    });
                }

                // Check for public role permissions
                var publicPermissions = permissions
                    .Where(p => p.UserName.Equals("public", StringComparison.OrdinalIgnoreCase) &&
                                !p.PermissionType.Equals("CONNECT", StringComparison.OrdinalIgnoreCase) &&
                                !p.PermissionType.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (publicPermissions.Any())
                {
                    var publicPrivileges = publicPermissions.Select(p => $"{p.PermissionType} ON {p.ObjectName}").Distinct().ToList();
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.Medium,
                        DatabaseName = databaseName,
                        Message = $"Public role has potentially excessive permissions: {string.Join(", ", publicPrivileges)}",
                        RecommendedAction = "Review and limit PUBLIC role permissions to only necessary objects",
                        DetectionTime = DateTime.Now
                    });
                }

                // Check for guest user permissions
                var guestPermissions = permissions
                    .Where(p => p.UserName.Equals("guest", StringComparison.OrdinalIgnoreCase) &&
                                p.ObjectName != "")
                    .ToList();

                if (guestPermissions.Any())
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.Medium,
                        DatabaseName = databaseName,
                        Message = "Guest user has permissions in the database",
                        RecommendedAction = "Disable the guest user account if not needed",
                        DetectionTime = DateTime.Now
                    });
                }

                _logger.LogInformation($"Identified {issues.Count} permission issues for database '{databaseName}'");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auditing permissions for database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AuditSensitiveDataAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Auditing sensitive data for database '{databaseName}'");
                var issues = new List<DbIssue>();

                // Identify sensitive columns
                var sensitiveColumns = await IdentifySensitiveColumnsAsync(connectionString, databaseName);

                if (!sensitiveColumns.Any())
                {
                    _logger.LogInformation($"No sensitive columns identified in database '{databaseName}'");
                    return issues;
                }

                // Group columns by table to make issues more manageable
                var sensitiveDataByTable = sensitiveColumns
                    .GroupBy(c => $"{c.SchemaName}.{c.TableName}")
                    .ToList();

                foreach (var table in sensitiveDataByTable)
                {
                    // Check for unencrypted sensitive data
                    var unencryptedSensitiveColumns = table
                        .Where(c => !c.IsEncrypted)
                        .ToList();

                    if (unencryptedSensitiveColumns.Any())
                    {
                        var columnList = string.Join(", ", unencryptedSensitiveColumns.Select(c => c.ColumnName));
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Security,
                            Severity = IssueSeverity.High,
                            DatabaseName = databaseName,
                            AffectedObject = table.Key,
                            Message = $"Unencrypted sensitive data found in table {table.Key}: {columnList}",
                            RecommendedAction = "Consider encrypting sensitive data using SQL Server encryption features or hashing for passwords",
                            DetectionTime = DateTime.Now
                        });
                    }

                    // Check for sensitive data in non-production environments (if we can determine environment)
                    if (IsDevelopmentOrTestEnvironment(connectionString))
                    {
                        var sensitiveColumnsCount = table.Count();
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Security,
                            Severity = IssueSeverity.Medium,
                            DatabaseName = databaseName,
                            AffectedObject = table.Key,
                            Message = $"Sensitive data found in non-production environment: {sensitiveColumnsCount} sensitive columns in table {table.Key}",
                            RecommendedAction = "Consider using data masking or test data generation for non-production environments",
                            DetectionTime = DateTime.Now
                        });
                    }
                }

                _logger.LogInformation($"Identified {issues.Count} sensitive data issues for database '{databaseName}'");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auditing sensitive data for database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AuditLoginSecurityAsync(string connectionString)
        {
            try
            {
                _logger.LogInformation("Auditing login security at server level");
                var issues = new List<DbIssue>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check for SQL logins with weak password policies
                using (var command = new SqlCommand(@"
                    SELECT name, is_policy_checked, is_expiration_checked 
                    FROM sys.sql_logins 
                    WHERE is_policy_checked = 0 OR is_expiration_checked = 0", connection))
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string loginName = reader.GetString(0);
                        bool isPolicyChecked = reader.GetBoolean(1);
                        bool isExpirationChecked = reader.GetBoolean(2);

                        if (!isPolicyChecked || !isExpirationChecked)
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Security,
                                Severity = IssueSeverity.Medium,
                                Message = $"SQL Login '{loginName}' has weak password settings: " +
                                          $"Policy enforcement: {(isPolicyChecked ? "Enabled" : "Disabled")}, " +
                                          $"Expiration: {(isExpirationChecked ? "Enabled" : "Disabled")}",
                                RecommendedAction = "Enable password policy and expiration for SQL Logins",
                                DetectionTime = DateTime.Now
                            });
                        }
                    }
                }

                // Check for server roles with excessive members
                using (var command = new SqlCommand(@"
                    SELECT r.name AS role_name, COUNT(m.member_principal_id) AS member_count
                    FROM sys.server_role_members m
                    JOIN sys.server_principals r ON r.principal_id = m.role_principal_id
                    WHERE r.name IN ('sysadmin', 'securityadmin', 'serveradmin')
                    GROUP BY r.name", connection))
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string roleName = reader.GetString(0);
                        int memberCount = reader.GetInt32(1);

                        if (memberCount > 3)
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Security,
                                Severity = IssueSeverity.Medium,
                                Message = $"High-privilege server role '{roleName}' has {memberCount} members, which exceeds recommended limits",
                                RecommendedAction = "Review members of the server role and remove unnecessary permissions",
                                DetectionTime = DateTime.Now
                            });
                        }
                    }
                }

                // Check for SA account renamed and disabled
                using (var command = new SqlCommand(@"
                    SELECT name, is_disabled 
                    FROM sys.server_principals 
                    WHERE principal_id = 1", connection))
                {
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string saName = reader.GetString(0);
                        bool isDisabled = reader.GetBoolean(1);

                        if (saName == "sa" && !isDisabled)
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Security,
                                Severity = IssueSeverity.High,
                                Message = "The 'sa' account is enabled and has not been renamed",
                                RecommendedAction = "Consider renaming the 'sa' account and disabling it if not needed",
                                DetectionTime = DateTime.Now
                            });
                        }
                    }
                }

                _logger.LogInformation($"Identified {issues.Count} login security issues");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auditing login security");
                throw;
            }
        }

        public async Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(string connectionString, string databaseName, string userName = null)
        {
            var permissions = new List<UserPermission>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Base query to get permissions
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        p.grantee_principal_name AS user_name,
                        p.permission_name,
                        CASE
                            WHEN p.class_desc = 'DATABASE' THEN 'DATABASE'
                            WHEN p.class_desc = 'SCHEMA' THEN SCHEMA_NAME(p.major_id)
                            WHEN p.class_desc = 'OBJECT_OR_COLUMN' THEN OBJECT_SCHEMA_NAME(p.major_id) + '.' + OBJECT_NAME(p.major_id)
                            WHEN p.class_desc = 'TYPE' THEN TYPE_NAME(p.major_id)
                            ELSE p.class_desc
                        END AS object_name,
                        p.class_desc AS object_type,
                        p.state_desc AS state
                    FROM sys.database_permissions p
                    WHERE 1=1 
                    " + (userName != null ? "AND p.grantee_principal_name = @UserName" : "") + @"
                    ORDER BY p.grantee_principal_name, p.class_desc, p.permission_name";

                using var command = new SqlCommand(query, connection);
                if (userName != null)
                {
                    command.Parameters.AddWithValue("@UserName", userName);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permissions.Add(new UserPermission
                    {
                        UserName = reader.GetString(0),
                        PermissionType = reader.GetString(1),
                        ObjectName = reader.GetString(2),
                        ObjectType = reader.GetString(3),
                        State = reader.GetString(4)
                    });
                }

                _logger.LogInformation($"Retrieved {permissions.Count} permissions for database '{databaseName}'");
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user permissions for database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<SensitiveColumn>> IdentifySensitiveColumnsAsync(string connectionString, string databaseName)
        {
            var sensitiveColumns = new List<SensitiveColumn>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get all tables and their columns
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        s.name AS schema_name,
                        t.name AS table_name,
                        c.name AS column_name,
                        ty.name AS data_type,
                        CASE 
                            WHEN ty.name IN ('bit', 'tinyint', 'smallint', 'int', 'bigint', 'decimal', 'numeric', 'float', 'real', 'datetime', 'date', 'time', 'smalldatetime', 'datetime2') THEN 0
                            ELSE 1
                        END AS is_string_type,
                        CASE
                            WHEN c.encryption_type IS NOT NULL THEN 1
                            ELSE 0
                        END AS is_encrypted,
                        c.is_nullable
                    FROM sys.schemas s
                    JOIN sys.tables t ON s.schema_id = t.schema_id
                    JOIN sys.columns c ON t.object_id = c.object_id
                    JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                    WHERE t.is_ms_shipped = 0
                    ORDER BY s.name, t.name, c.name";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    string schemaName = reader.GetString(0);
                    string tableName = reader.GetString(1);
                    string columnName = reader.GetString(2);
                    string dataType = reader.GetString(3);
                    bool isStringType = reader.GetInt32(4) == 1;
                    bool isEncrypted = reader.GetInt32(5) == 1;
                    bool isNullable = reader.GetBoolean(6);

                    // Check if column name matches any sensitive patterns
                    if (IsLikelySensitiveColumn(columnName, dataType))
                    {
                        var sensitiveColumn = new SensitiveColumn
                        {
                            SchemaName = schemaName,
                            TableName = tableName,
                            ColumnName = columnName,
                            DataType = dataType,
                            SensitivityType = GetSensitivityType(columnName),
                            IsEncrypted = isEncrypted,
                            IsNullable = isNullable
                        };

                        sensitiveColumns.Add(sensitiveColumn);
                    }
                }

                _logger.LogInformation($"Identified {sensitiveColumns.Count} potentially sensitive columns in database '{databaseName}'");
                return sensitiveColumns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error identifying sensitive columns in database '{databaseName}'");
                throw;
            }
        }

        private bool IsLikelySensitiveColumn(string columnName, string dataType)
        {
            // Convert to lowercase for comparison
            columnName = columnName.ToLower();

            // Check if column name contains any sensitive keywords
            return SensitiveKeywords.Any(keyword => 
                columnName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                columnName.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private SensitivityType GetSensitivityType(string columnName)
        {
            columnName = columnName.ToLower();

            foreach (var pattern in ColumnPatterns)
            {
                if (Regex.IsMatch(columnName, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            return SensitivityType.Other;
        }

        private bool IsDevelopmentOrTestEnvironment(string connectionString)
        {
            // This is a simple heuristic - in a real implementation, you might have a more robust way
            // to determine the environment (e.g., from configuration)
            var builder = new SqlConnectionStringBuilder(connectionString);
            string serverName = builder.DataSource.ToLower();
            string databaseName = builder.InitialCatalog.ToLower();

            return serverName.Contains("dev") || 
                   serverName.Contains("test") || 
                   serverName.Contains("qa") ||
                   databaseName.Contains("dev") || 
                   databaseName.Contains("test") || 
                   databaseName.EndsWith("_qa");
        }
    }
} 