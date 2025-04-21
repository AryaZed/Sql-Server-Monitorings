namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public class UserPermission
    {
        public string UserName { get; set; }
        public string DatabaseName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public string Permission { get; set; }
        public string GrantedBy { get; set; }
        public DateTime GrantedDate { get; set; }
        public string PermissionType { get; set; }
        public string State { get; set; }
    }
}
