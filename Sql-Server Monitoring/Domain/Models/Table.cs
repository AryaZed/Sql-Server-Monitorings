namespace Sql_Server_Monitoring.Domain.Models
{
    public class Table
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public long RowCount { get; set; }
        public long SizeInKB { get; set; }
        public bool HasPrimaryKey { get; set; }
        public bool HasClusteredIndex { get; set; }
        public List<Column> Columns { get; set; } = new List<Column>();
        public List<Index> Indexes { get; set; } = new List<Index>();
        public List<ForeignKey> ForeignKeys { get; set; } = new List<ForeignKey>();
    }
}
