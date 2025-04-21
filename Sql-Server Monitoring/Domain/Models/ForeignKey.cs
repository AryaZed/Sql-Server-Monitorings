namespace Sql_Server_Monitoring.Domain.Models
{
    public class ForeignKey
    {
        public string Name { get; set; }
        public string ReferencedSchema { get; set; }
        public string ReferencedTable { get; set; }
        public List<ForeignKeyColumn> Columns { get; set; } = new List<ForeignKeyColumn>();
        public bool IsDisabled { get; set; }
        public bool IsCascadeDelete { get; set; }
        public bool IsCascadeUpdate { get; set; }
    }
}
