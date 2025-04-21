namespace Sql_Server_Monitoring.Domain.Models
{
    public class Column
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
    }
}
