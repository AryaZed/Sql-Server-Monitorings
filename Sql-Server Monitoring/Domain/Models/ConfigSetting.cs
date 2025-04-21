namespace Sql_Server_Monitoring.Domain.Models
{
    public class ConfigSetting
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
        public bool IsDynamic { get; set; }
        public bool IsAdvanced { get; set; }
    }
}
