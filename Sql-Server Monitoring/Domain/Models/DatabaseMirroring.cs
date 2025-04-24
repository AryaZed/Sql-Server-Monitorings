using System;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class DatabaseMirroring
    {
        public string DatabaseName { get; set; }
        public string MirroringState { get; set; }
        public string MirroringRole { get; set; }
        public string MirroringPartner { get; set; }
        public string MirroringWitness { get; set; }
        public string MirroringOperatingMode { get; set; }
        public string MirroringWitnessState { get; set; }
        public string MirroringSafetyLevel { get; set; }
        public long MirroringRedoQueueSizeKB { get; set; }
        public TimeSpan? MirroringRedoTime { get; set; }
        public DateTime? MirroringLastMessageTime { get; set; }
        public TimeSpan MirroringLatency { get; set; }
        public string MirroringEndpointName { get; set; }
        public int MirroringEndpointPort { get; set; }
        public bool HasWarnings => MirroringState != "SYNCHRONIZED" || MirroringWitnessState != "CONNECTED" || MirroringRedoQueueSizeKB > 5000;
    }
} 