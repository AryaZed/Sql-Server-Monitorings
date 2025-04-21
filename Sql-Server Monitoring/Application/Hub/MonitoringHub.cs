namespace Sql_Server_Monitoring.Application.Hub
{
    public class MonitoringHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ILogger<MonitoringHub> _logger;

        public MonitoringHub(ILogger<MonitoringHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string serverName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, serverName);
            _logger.LogInformation($"Client {Context.ConnectionId} joined group: {serverName}");
        }

        public async Task LeaveGroup(string serverName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, serverName);
            _logger.LogInformation($"Client {Context.ConnectionId} left group: {serverName}");
        }
    }
}
