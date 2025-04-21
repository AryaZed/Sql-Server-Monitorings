namespace Sql_Server_Monitoring.Domain.Models
{
    public class Index
    {
        public string Name { get; set; }
        public bool IsClustered { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimaryKey { get; set; }
        public int FillFactor { get; set; }
        public double FragmentationPercent { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public List<string> IncludedColumns { get; set; } = new List<string>();
        public IndexUsageStats UsageStats { get; set; }
    }
}
