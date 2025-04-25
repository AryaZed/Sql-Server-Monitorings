using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class DatabaseRepository : IDatabaseRepository
    {
        private readonly ILogger<DatabaseRepository> _logger;

        public DatabaseRepository(ILogger<DatabaseRepository> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetUserDatabasesAsync(string connectionString)
        {
            var databases = new List<string>();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE database_id > 4", // Skip system databases
                connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }

            return databases;
        }

        public async Task<Database> GetDatabaseDetailsAsync(string connectionString, string databaseName)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var database = new Database { Name = databaseName };

            // Get database properties
            using (var command = new SqlCommand(
                @"SELECT 
                    d.create_date,
                    d.compatibility_level,
                    d.recovery_model_desc,
                    d.is_encrypted,
                    (SELECT SUM(CAST(size as BIGINT) * 8 / 1024) FROM sys.master_files WHERE database_id = DB_ID(@dbName)) AS size_mb,
                    (SELECT MAX(backup_finish_date) FROM msdb.dbo.backupset WHERE database_name = @dbName AND type = 'D') AS last_backup_date
                FROM sys.databases d
                WHERE d.name = @dbName",
                connection))
            {
                command.Parameters.AddWithValue("@dbName", databaseName);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    database.CreatedDate = reader.GetDateTime(0);
                    database.CompatibilityLevel = reader.GetInt32(1);
                    database.RecoveryModel = Enum.Parse<RecoveryModel>(reader.GetString(2), true);
                    database.IsEncrypted = reader.GetBoolean(3);
                    database.SizeInMB = reader.GetInt64(4);
                    database.LastBackupDate = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);
                }
            }

            // Get database files
            using (var command = new SqlCommand(
                @"SELECT 
                    f.name,
                    f.physical_name,
                    f.size * 8 / 1024 AS size_mb,
                    CASE WHEN f.max_size = -1 THEN -1 ELSE f.max_size * 8 / 1024 END AS max_size_mb,
                    f.is_percent_growth,
                    f.growth,
                    f.type_desc,
                    FILEPROPERTY(f.name, 'SpaceUsed') * 8 / 1024 AS used_space_mb
                FROM sys.master_files f
                WHERE database_id = DB_ID(@dbName)",
                connection))
            {
                command.Parameters.AddWithValue("@dbName", databaseName);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var file = new DatabaseFile
                    {
                        Name = reader.GetString(0),
                        PhysicalPath = reader.GetString(1),
                        SizeInMB = reader.GetInt64(2),
                        MaxSizeInMB = reader.GetInt64(3),
                        GrowthType = reader.GetBoolean(4) ? FileGrowthType.Percent : FileGrowthType.MB,
                        GrowthValue = reader.GetInt32(5),
                        IsLogFile = reader.GetString(6).Equals("LOG", StringComparison.OrdinalIgnoreCase),
                    };

                    if (!reader.IsDBNull(7))
                    {
                        long usedSpaceMB = reader.GetInt64(7);
                        file.PercentUsed = (int)(usedSpaceMB * 100 / file.SizeInMB);
                    }

                    database.Files.Add(file);
                }
            }

            // Get tables (basic info)
            database.Tables = (await GetTablesAsync(connectionString, databaseName)).ToList();

            return database;
        }

        public async Task<IEnumerable<Table>> GetTablesAsync(string connectionString, string databaseName)
        {
            var tables = new List<Table>();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(
                @"USE @dbName;
                   SELECT 
                       s.name AS schema_name,
                       t.name AS table_name,
                       p.rows AS row_count,
                       SUM(a.total_pages) * 8 AS total_size_kb,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM sys.indexes i 
                           WHERE i.object_id = t.object_id AND i.is_primary_key = 1
                       ) THEN 1 ELSE 0 END AS has_primary_key,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM sys.indexes i 
                           WHERE i.object_id = t.object_id AND i.type = 1
                       ) THEN 1 ELSE 0 END AS has_clustered_index
                   FROM sys.tables t
                   JOIN sys.schemas s ON t.schema_id = s.schema_id
                   JOIN sys.indexes i ON t.object_id = i.object_id
                   JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                   JOIN sys.allocation_units a ON p.partition_id = a.container_id
                   WHERE t.is_ms_shipped = 0
                   GROUP BY s.name, t.name, t.object_id, p.rows
                   ORDER BY s.name, t.name",
                connection);
                
            // Update the USE statement properly as it can't be parameterized directly
            command.CommandText = command.CommandText.Replace("@dbName", $"[{databaseName}]");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = new Table
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1),
                    RowCount = reader.GetInt64(2),
                    SizeInKB = reader.GetInt64(3),
                    HasPrimaryKey = reader.GetInt32(4) == 1,
                    HasClusteredIndex = reader.GetInt32(5) == 1
                };

                tables.Add(table);
            }

            return tables;
        }

        public async Task<Table> GetTableDetailsAsync(string connectionString, string databaseName, string schemaName, string tableName)
        {
            // Get basic table info first
            var tables = await GetTablesAsync(connectionString, databaseName);
            var table = tables.FirstOrDefault(t => t.Schema == schemaName && t.Name == tableName);

            if (table == null)
            {
                return null;
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get columns
            using (var command = new SqlCommand(
                $@"USE [{databaseName}];
                   SELECT 
                       c.name AS column_name,
                       t.name AS type_name,
                       c.max_length,
                       c.is_nullable,
                       c.is_identity,
                       CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                       CASE WHEN fk.parent_column_id IS NOT NULL THEN 1 ELSE 0 END AS is_foreign_key
                   FROM sys.columns c
                   JOIN sys.types t ON c.user_type_id = t.user_type_id
                   JOIN sys.tables tbl ON c.object_id = tbl.object_id
                   JOIN sys.schemas s ON tbl.schema_id = s.schema_id
                   LEFT JOIN sys.index_columns pk ON 
                       c.object_id = pk.object_id AND 
                       c.column_id = pk.column_id AND
                       pk.index_id IN (SELECT index_id FROM sys.indexes WHERE is_primary_key = 1 AND object_id = c.object_id)
                   LEFT JOIN sys.foreign_key_columns fk ON 
                       c.object_id = fk.parent_object_id AND 
                       c.column_id = fk.parent_column_id
                   WHERE s.name = '{schemaName}' AND tbl.name = '{tableName}'
                   ORDER BY c.column_id",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var column = new Column
                    {
                        Name = reader.GetString(0),
                        DataType = reader.GetString(1),
                        MaxLength = reader.GetInt16(2),
                        IsNullable = reader.GetBoolean(3),
                        IsIdentity = reader.GetBoolean(4),
                        IsPrimaryKey = reader.GetInt32(5) == 1,
                        IsForeignKey = reader.GetInt32(6) == 1
                    };

                    table.Columns.Add(column);
                }
            }

            // Get indexes
            table.Indexes = (await GetIndexesAsync(connectionString, databaseName, schemaName, tableName)).ToList();

            // Get foreign keys
            using (var command = new SqlCommand(
                $@"USE [{databaseName}];
                   SELECT 
                       fk.name AS constraint_name,
                       rs.name AS referenced_schema,
                       rt.name AS referenced_table,
                       c.name AS column_name,
                       rc.name AS referenced_column,
                       fk.is_disabled,
                       fk.delete_referential_action,
                       fk.update_referential_action
                   FROM sys.foreign_keys fk
                   JOIN sys.tables t ON fk.parent_object_id = t.object_id
                   JOIN sys.schemas s ON t.schema_id = s.schema_id
                   JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
                   JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
                   JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                   JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                   JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
                   WHERE s.name = '{schemaName}' AND t.name = '{tableName}'
                   ORDER BY fk.name, fkc.constraint_column_id",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                string currentFkName = null;
                ForeignKey currentFk = null;

                while (await reader.ReadAsync())
                {
                    string fkName = reader.GetString(0);

                    if (currentFkName != fkName)
                    {
                        currentFkName = fkName;
                        currentFk = new ForeignKey
                        {
                            Name = fkName,
                            ReferencedSchema = reader.GetString(1),
                            ReferencedTable = reader.GetString(2),
                            IsDisabled = reader.GetBoolean(5),
                            IsCascadeDelete = reader.GetByte(6) == 1, // 1 = CASCADE
                            IsCascadeUpdate = reader.GetByte(7) == 1  // 1 = CASCADE
                        };
                        table.ForeignKeys.Add(currentFk);
                    }

                    currentFk.Columns.Add(new ForeignKeyColumn
                    {
                        Column = reader.GetString(3),
                        ReferencedColumn = reader.GetString(4)
                    });
                }
            }

            return table;
        }

        public async Task<IEnumerable<Domain.Models.Index>> GetIndexesAsync(string connectionString, string databaseName, string schemaName, string tableName)
        {
            var indexes = new List<Domain.Models.Index>();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get indexes
            using (var command = new SqlCommand(
                $@"USE [{databaseName}];
                   SELECT 
                       i.name AS index_name,
                       i.type_desc,
                       i.is_unique,
                       i.is_primary_key,
                       i.fill_factor,
                       STATS_DATE(i.object_id, i.index_id) AS statistics_date,
                       ps.avg_fragmentation_in_percent
                   FROM sys.indexes i
                   JOIN sys.tables t ON i.object_id = t.object_id
                   JOIN sys.schemas s ON t.schema_id = s.schema_id
                   LEFT JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps ON 
                       i.object_id = ps.object_id AND 
                       i.index_id = ps.index_id
                   WHERE s.name = '{schemaName}' AND t.name = '{tableName}' AND i.index_id > 0
                   ORDER BY i.name",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var index = new Domain.Models.Index
                    {
                        Name = reader.GetString(0),
                        IsClustered = reader.GetString(1).Contains("CLUSTERED", StringComparison.OrdinalIgnoreCase),
                        IsUnique = reader.GetBoolean(2),
                        IsPrimaryKey = reader.GetBoolean(3),
                        FillFactor = reader.GetByte(4)
                    };

                    if (!reader.IsDBNull(6))
                    {
                        index.FragmentationPercent = reader.GetDouble(6);
                    }

                    indexes.Add(index);
                }
            }

            // Get index columns
            foreach (var index in indexes)
            {
                using var columnCommand = new SqlCommand(
                    $@"USE [{databaseName}];
                       SELECT 
                           c.name AS column_name,
                           ic.is_included_column
                       FROM sys.index_columns ic
                       JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                       JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                       JOIN sys.tables t ON i.object_id = t.object_id
                       JOIN sys.schemas s ON t.schema_id = s.schema_id
                       WHERE s.name = '{schemaName}' AND t.name = '{tableName}' AND i.name = '{index.Name}'
                       ORDER BY ic.index_column_id",
                    connection);

                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    string columnName = columnReader.GetString(0);
                    bool isIncluded = columnReader.GetBoolean(1);

                    if (isIncluded)
                    {
                        index.IncludedColumns.Add(columnName);
                    }
                    else
                    {
                        index.Columns.Add(columnName);
                    }
                }
            }

            // Get index usage stats
            using (var usageCommand = new SqlCommand(
                $@"USE [{databaseName}];
                   SELECT 
                       i.name AS index_name,
                       ius.user_seeks,
                       ius.user_scans,
                       ius.user_lookups,
                       ius.user_updates,
                       ius.last_user_seek,
                       ius.last_user_scan,
                       ius.last_user_lookup
                   FROM sys.dm_db_index_usage_stats ius
                   JOIN sys.indexes i ON ius.object_id = i.object_id AND ius.index_id = i.index_id
                   JOIN sys.tables t ON i.object_id = t.object_id
                   JOIN sys.schemas s ON t.schema_id = s.schema_id
                   WHERE ius.database_id = DB_ID() 
                       AND s.name = '{schemaName}' 
                       AND t.name = '{tableName}'",
                connection))
            {
                using var usageReader = await usageCommand.ExecuteReaderAsync();
                while (await usageReader.ReadAsync())
                {
                    string indexName = usageReader.GetString(0);
                    var index = indexes.FirstOrDefault(i => i.Name == indexName);

                    if (index != null)
                    {
                        index.UsageStats = new IndexUsageStats
                        {
                            UserSeeks = usageReader.GetInt64(1),
                            UserScans = usageReader.GetInt64(2),
                            UserLookups = usageReader.GetInt64(3),
                            UserUpdates = usageReader.GetInt64(4)
                        };

                        // Get the most recent usage date
                        DateTime lastSeek = usageReader.IsDBNull(5) ? DateTime.MinValue : usageReader.GetDateTime(5);
                        DateTime lastScan = usageReader.IsDBNull(6) ? DateTime.MinValue : usageReader.GetDateTime(6);
                        DateTime lastLookup = usageReader.IsDBNull(7) ? DateTime.MinValue : usageReader.GetDateTime(7);

                        index.UsageStats.LastUsed = new[] { lastSeek, lastScan, lastLookup }.Max();
                    }
                }
            }

            return indexes;
        }

        public async Task<ServerConfiguration> GetServerConfigurationAsync(string connectionString)
        {
            var config = new ServerConfiguration();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get server configuration settings
            using (var command = new SqlCommand(
                @"SELECT 
                    name, 
                    value, 
                    value_in_use,
                    minimum, 
                    maximum, 
                    is_dynamic,
                    is_advanced,
                    description
                FROM sys.configurations
                ORDER BY name",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string name = reader.GetString(0);
                    int configValue = reader.GetInt32(2); // value_in_use
                    bool isDynamic = reader.GetBoolean(5);
                    bool isAdvanced = reader.GetBoolean(6);
                    string description = reader.IsDBNull(7) ? null : reader.GetString(7);

                    config.Settings.Add(new ConfigSetting
                    {
                        Name = name,
                        Value = configValue.ToString(),
                        Description = description,
                        IsDynamic = isDynamic,
                        IsAdvanced = isAdvanced
                    });

                    // Extract key configuration values
                    if (name == "max server memory (MB)")
                    {
                        config.MaxMemoryMB = configValue;
                    }
                    else if (name == "max worker threads")
                    {
                        config.MaxWorkerThreads = configValue;
                    }
                    else if (name == "max degree of parallelism")
                    {
                        config.MaxDegreeOfParallelism = configValue;
                    }
                    else if (name == "cost threshold for parallelism")
                    {
                        config.CostThresholdForParallelism = configValue;
                    }
                    else if (name == "optimize for ad hoc workloads")
                    {
                        config.OptimizeForAdHocWorkloads = configValue == 1;
                    }
                }
            }

            return config;
        }

        public async Task<ServerPerformanceMetrics> GetServerPerformanceMetricsAsync(string connectionString)
        {
            var metrics = new ServerPerformanceMetrics
            {
                CollectionTime = DateTime.Now,
                Cpu = new CPUMetrics(),
                Memory = new MemoryMetrics(),
                Disk = new DiskMetrics { IoStats = new List<DiskIoStatistic>(), TopIoQueries = new List<DiskIoIntensiveQuery>() },
                TopWaits = new List<WaitStatistic>()
            };

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get CPU metrics
            using (var command = new SqlCommand(
                @"SELECT 
                    cpu_count,
                    scheduler_count,
                    scheduler_total_count,
                    100 - SystemIdle AS [CPU Utilization %],
                    SQLProcessUtilization
                FROM (
                    SELECT SystemIdle, SQLProcessUtilization
                    FROM (
                        SELECT record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') AS SystemIdle,
                               record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS SQLProcessUtilization
                        FROM (
                            SELECT TOP 1 CONVERT(xml, record) AS record
                            FROM sys.dm_os_ring_buffers
                            WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                            AND record LIKE N'%<SystemHealth>%'
                            ORDER BY timestamp DESC
                        ) AS RingBufferInfo
                    ) AS CoreInfo
                ) AS Utilization
                CROSS JOIN sys.dm_os_sys_info",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    metrics.Cpu.UtilizationPercent = reader.GetDouble(3);
                }
            }

            // Get active worker threads and requests
            using (var command = new SqlCommand(
                @"SELECT 
                    COUNT(*) AS active_worker_threads,
                    (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE status = 'running') AS active_requests
                FROM sys.dm_os_workers
                WHERE state = 'RUNNING'",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    metrics.Cpu.ActiveWorkerThreads = reader.GetInt32(0);
                    metrics.Cpu.ActiveRequests = reader.GetInt32(1);
                }
            }

            // Get top CPU consuming queries
            using (var command = new SqlCommand(
                @"SELECT TOP 5
                    qs.query_id,
                    SUBSTRING(qt.query_sql_text, 1, 200) AS query_text,
                    qs.total_worker_time / 1000 AS cpu_time_ms,
                    qs.last_execution_time,
                    qs.execution_count
                FROM sys.query_store_query_text qt
                JOIN sys.query_store_query q ON qt.query_text_id = q.query_text_id
                JOIN sys.query_store_plan p ON q.query_id = p.query_id
                JOIN sys.query_store_runtime_stats qs ON p.plan_id = qs.plan_id
                WHERE qs.total_worker_time > 0
                ORDER BY qs.total_worker_time DESC",
                connection))
            {
                try
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        metrics.Cpu.TopCpuQueries.Add(new CpuIntensiveQuery
                        {
                            QueryId = reader.GetInt32(0),
                            QueryText = reader.GetString(1),
                            CpuTimeMs = reader.GetDouble(2),
                            LastExecutionTime = reader.GetDateTime(3),
                            ExecutionCount = reader.GetInt64(4)
                        });
                    }
                }
                catch (SqlException)
                {
                    // Query Store might not be enabled
                    _logger.LogWarning("Could not retrieve CPU-intensive queries. Query Store might not be enabled.");
                }
            }

            // Get memory metrics
            using (var command = new SqlCommand(
                @"SELECT
                    (physical_memory_in_use_kb / 1024) AS physical_memory_in_use_MB,
                    (total_server_memory_kb / 1024) AS total_server_memory_MB,
                    (target_server_memory_kb / 1024) AS target_server_memory_MB,
                    (memory_grants_pending) AS pending_memory_grants,
                    (connection_memory_kb / 1024) AS connection_memory_MB,
                    (plan_cache_size_kb / 1024) AS plan_cache_MB,
                    (lock_memory_kb / 1024) AS lock_memory_MB,
                    (buffer_pool_size_kb / 1024) AS buffer_pool_MB,
                    (CASE WHEN connection_memory_kb + (plan_cache_size_kb / 2) > (total_server_memory_kb * 0.7) THEN 1 ELSE 0 END) AS is_memory_pressure,
                    (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name = 'Page life expectancy' AND object_name LIKE '%Buffer Manager%') AS page_life_expectancy
                FROM sys.dm_os_process_memory
                CROSS JOIN sys.dm_os_performance_counters
                WHERE counter_name = 'Total Server Memory (KB)'
                AND object_name LIKE '%Memory Manager%'",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    metrics.Memory.TotalServerMemoryMB = reader.GetInt64(1);
                    metrics.Memory.TargetServerMemoryMB = reader.GetInt64(2);
                    metrics.Memory.PlanCacheMemoryMB = reader.GetInt64(5);
                    metrics.Memory.BufferPoolMemoryMB = reader.GetInt64(7);
                    metrics.Memory.IsMemoryPressure = reader.GetInt32(8) == 1;
                    metrics.Memory.PageLifeExpectancy = reader.GetDouble(9);
                }
            }

            // Get disk I/O statistics
            using (var command = new SqlCommand(
                @"SELECT TOP 10
                    DB_NAME(vfs.database_id) AS database_name,
                    mf.physical_name,
                    ios.io_stall_read_ms / NULLIF(ios.num_of_reads, 0) AS avg_read_latency_ms,
                    ios.io_stall_write_ms / NULLIF(ios.num_of_writes, 0) AS avg_write_latency_ms,
                    ios.num_of_bytes_read / NULLIF(ios.sample_ms, 0) * 1000 AS read_bytes_per_sec,
                    ios.num_of_bytes_written / NULLIF(ios.sample_ms, 0) * 1000 AS write_bytes_per_sec
                FROM sys.dm_io_virtual_file_stats(NULL, NULL) AS vfs
                JOIN sys.master_files AS mf ON vfs.database_id = mf.database_id AND vfs.file_id = mf.file_id
                CROSS APPLY sys.dm_io_stats_file(vfs.database_id, vfs.file_id) as ios
                WHERE vfs.database_id > 4 -- Skip system databases
                ORDER BY (ios.io_stall_read_ms + ios.io_stall_write_ms) DESC",
                connection))
            {
                try
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        metrics.Disk.IoStats.Add(new DiskIoStatistic
                        {
                            DatabaseName = reader.GetString(0),
                            FileName = reader.GetString(1),
                            ReadLatencyMs = reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                            WriteLatencyMs = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                            ReadBytesPersec = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                            WriteBytesPersec = reader.IsDBNull(5) ? 0 : reader.GetInt64(5)
                        });
                    }
                }
                catch (SqlException ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve disk I/O statistics.");
                }
            }

            // Get top I/O intensive queries
            using (var command = new SqlCommand(
                @"SELECT TOP 5
                    qs.query_id,
                    SUBSTRING(qt.query_sql_text, 1, 200) AS query_text,
                    qs.total_logical_reads,
                    qs.total_physical_reads,
                    qs.total_logical_writes,
                    qs.last_execution_time,
                    qs.execution_count
                FROM sys.query_store_query_text qt
                JOIN sys.query_store_query q ON qt.query_text_id = q.query_text_id
                JOIN sys.query_store_plan p ON q.query_id = p.query_id
                JOIN sys.query_store_runtime_stats qs ON p.plan_id = qs.plan_id
                WHERE qs.total_logical_reads + qs.total_physical_reads + qs.total_logical_writes > 0
                ORDER BY (qs.total_logical_reads + qs.total_physical_reads) DESC",
                connection))
            {
                try
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        metrics.Disk.TopIoQueries.Add(new DiskIoIntensiveQuery
                        {
                            QueryId = reader.GetInt32(0),
                            QueryText = reader.GetString(1),
                            LogicalReads = reader.GetInt64(2),
                            PhysicalReads = reader.GetInt64(3),
                            Writes = reader.GetInt64(4),
                            LastExecutionTime = reader.GetDateTime(5),
                            ExecutionCount = reader.GetInt64(6)
                        });
                    }
                }
                catch (SqlException)
                {
                    // Query Store might not be enabled
                    _logger.LogWarning("Could not retrieve I/O-intensive queries. Query Store might not be enabled.");
                }
            }

            // Get top wait stats
            using (var command = new SqlCommand(
                @"WITH Waits AS
                (
                    SELECT
                        wait_type,
                        wait_time_ms / 1000.0 AS wait_time_sec,
                        100.0 * wait_time_ms / SUM(wait_time_ms) OVER() AS percentage,
                        ROW_NUMBER() OVER(ORDER BY wait_time_ms DESC) AS rn,
                        signal_wait_time_ms,
                        waiting_tasks_count
                    FROM sys.dm_os_wait_stats
                    WHERE wait_type NOT IN (
                        'CLR_SEMAPHORE', 'LAZYWRITER_SLEEP', 'RESOURCE_QUEUE', 'SLEEP_TASK',
                        'SLEEP_SYSTEMTASK', 'SQLTRACE_BUFFER_FLUSH', 'WAITFOR', 'LOGMGR_QUEUE',
                        'CHECKPOINT_QUEUE', 'REQUEST_FOR_DEADLOCK_SEARCH', 'XE_TIMER_EVENT', 'BROKER_TO_FLUSH',
                        'BROKER_TASK_STOP', 'CLR_MANUAL_EVENT', 'CLR_AUTO_EVENT', 'DISPATCHER_QUEUE_SEMAPHORE',
                        'FT_IFTS_SCHEDULER_IDLE_WAIT', 'XE_DISPATCHER_WAIT', 'XE_DISPATCHER_JOIN',
                        'BROKER_EVENTHANDLER', 'TRACEWRITE', 'FT_IFTSHC_MUTEX', 'SQLTRACE_INCREMENTAL_FLUSH_SLEEP')
                )
                SELECT TOP 10
                    wait_type,
                    CAST(wait_time_sec AS DECIMAL(12, 2)) AS wait_time_sec,
                    CAST(percentage AS DECIMAL(12, 2)) AS percentage,
                    CAST(signal_wait_time_ms AS DECIMAL(12, 2)) AS signal_wait_time_ms,
                    waiting_tasks_count
                FROM Waits
                WHERE rn <= 10
                ORDER BY percentage DESC",
                connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var waitType = reader.GetString(0);
                    var waitTimeMs = (long)(reader.GetDecimal(1) * 1000);
                    var waitingTasks = reader.GetInt64(4);

                    var category = CategorizeWaitType(waitType);

                    metrics.TopWaits.Add(new WaitStatistic
                    {
                        WaitType = waitType,
                        WaitTimeMs = waitTimeMs,
                        WaitingTasksCount = waitingTasks,
                        Description = GetWaitTypeDescription(waitType),
                        Category = category
                    });
                }
            }

            return metrics;
        }

        public async Task ExecuteScriptAsync(string connectionString, string databaseName, string script)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Use the specified database
            if (!string.IsNullOrEmpty(databaseName))
            {
                using var useDbCommand = new SqlCommand($"USE [{databaseName}]", connection);
                await useDbCommand.ExecuteNonQueryAsync();
            }

            // Execute the script
            using var command = new SqlCommand(script, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            await command.ExecuteNonQueryAsync();
        }

        private WaitCategory CategorizeWaitType(string waitType)
        {
            if (waitType.Contains("CPU") || waitType.Contains("SOS_SCHEDULER"))
            {
                return WaitCategory.Cpu;
            }
            else if (waitType.Contains("MEMORY") || waitType.Contains("RESOURCE_SEMAPHORE"))
            {
                return WaitCategory.Memory;
            }
            else if (waitType.Contains("IO") || waitType.Contains("PAGEIO") || waitType.Contains("WRITE"))
            {
                return WaitCategory.Disk;
            }
            else if (waitType.Contains("NETWORK") || waitType.Contains("ASYNC_NETWORK"))
            {
                return WaitCategory.Network;
            }
            else if (waitType.Contains("LCK") || waitType.Contains("LOCK") || waitType.Contains("LATCH"))
            {
                return WaitCategory.Locking;
            }
            else
            {
                return WaitCategory.Other;
            }
        }

        private string GetWaitTypeDescription(string waitType)
        {
            // Return descriptions for common wait types
            return waitType switch
            {
                "CXPACKET" => "Occurs with parallel query execution. High waits may indicate excessive parallelism.",
                "PAGEIOLATCH_SH" => "Occurs when waiting for a data page to be read from disk into memory.",
                "PAGEIOLATCH_EX" => "Occurs when waiting for a data page to be written to disk.",
                "PAGELATCH_SH" => "Occurs when waiting for a buffer page latch in shared mode. May indicate contention.",
                "PAGELATCH_EX" => "Occurs when waiting for a buffer page latch in exclusive mode. May indicate contention.",
                "ASYNC_NETWORK_IO" => "Occurs when waiting for client application to consume data. May indicate slow client.",
                "LCK_M_IX" => "Intent exclusive lock wait. May indicate blocking.",
                "LCK_M_X" => "Exclusive lock wait. May indicate blocking.",
                "SOS_SCHEDULER_YIELD" => "Occurs when a task voluntarily yields the scheduler. May indicate CPU pressure.",
                "WRITELOG" => "Occurs when waiting for log flushes. May indicate slow I/O subsystem.",
                _ => "Wait type related to SQL Server resource management."
            };
        }

        public Task PurgeOldMetricsAsync(string connectionString, int retentionDays)
        {
            throw new NotImplementedException();
        }
    }
}
