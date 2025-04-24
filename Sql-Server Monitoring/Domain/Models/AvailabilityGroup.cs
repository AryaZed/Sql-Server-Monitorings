using System;
using System.Collections.Generic;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class AvailabilityGroup
    {
        public string GroupId { get; set; }
        public string Name { get; set; }
        public string ClusterName { get; set; }
        public string PrimaryReplicaServerName { get; set; }
        public string FailoverMode { get; set; }
        public string AvailabilityMode { get; set; }
        public string BackupPreference { get; set; }
        public string HealthState { get; set; }
        public string OperationalState { get; set; }
        public bool BasicAvailabilityGroup { get; set; }
        public bool IsDatabaseHealthDetectionEnabled { get; set; }
        public bool IsDistributedAvailabilityGroup { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AvailabilityReplica> Replicas { get; set; } = new List<AvailabilityReplica>();
        public List<AvailabilityDatabase> Databases { get; set; } = new List<AvailabilityDatabase>();
        public List<AvailabilityGroupListener> Listeners { get; set; } = new List<AvailabilityGroupListener>();
    }

    public class AvailabilityReplica
    {
        public string ReplicaId { get; set; }
        public string ServerName { get; set; }
        public string EndpointUrl { get; set; }
        public string AvailabilityMode { get; set; }
        public string FailoverMode { get; set; }
        public string JoinState { get; set; }
        public string Role { get; set; }
        public string OperationalState { get; set; }
        public string ConnectionState { get; set; }
        public bool SynchronizationHealth { get; set; }
        public DateTime LastConnectTime { get; set; }
        public DateTime LastJoinTime { get; set; }
    }

    public class AvailabilityDatabase
    {
        public string DatabaseName { get; set; }
        public string SynchronizationState { get; set; }
        public string SynchronizationHealth { get; set; }
        public bool IsSuspended { get; set; }
        public string SuspendReason { get; set; }
        public long LastHardenedLsn { get; set; }
        public long LastReceivedLsn { get; set; }
        public long LastSentLsn { get; set; }
        public long LastRedoneLogRate { get; set; }
        public DateTime LastRedoneTime { get; set; }
        public long LogSendQueueSize { get; set; }
        public long LogSendRate { get; set; }
        public long RedoQueueSize { get; set; }
        public long RedoRate { get; set; }
        public long FileStreamSendRate { get; set; }
        public TimeSpan EstimatedRecoveryTime { get; set; }
    }

    public class AvailabilityGroupListener
    {
        public string ListenerId { get; set; }
        public string ListenerName { get; set; }
        public string Port { get; set; }
        public string IpAddress { get; set; }
        public string SubnetMask { get; set; }
        public string State { get; set; }
    }
} 