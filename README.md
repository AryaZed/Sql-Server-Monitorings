# SQL Server Monitoring

A comprehensive monitoring solution for SQL Server instances that helps database administrators track performance metrics, identify issues, and optimize database performance.

## Features

- **Real-time Monitoring**: Track CPU, memory, disk usage, and other critical performance metrics
- **Query Performance Analysis**: Identify long-running and resource-intensive queries
- **Blocking Detection**: Find and resolve blocking issues in your SQL Server instances
- **Customizable Alerting**: Configure alerts for various performance thresholds
- **Historical Analysis**: Store and analyze performance data over time

## Technical Overview

This project is built using:
- ASP.NET Core 9.0 for the backend API
- React for the frontend UI
- SQL Server to store monitoring data
- SignalR for real-time notifications
- Material-UI for frontend components

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- SQL Server 2022 or later
- Node.js 18+ and npm (for React frontend)
- Docker and Docker Compose (optional, for containerized deployment)

### Installation Options

#### Option 1: Local Development Setup

1. Clone the repository
```
git clone https://github.com/yourusername/sql-server-monitoring.git
cd sql-server-monitoring
```

2. Restore dependencies
```
dotnet restore
```

3. Update the connection string in `appsettings.json` to point to your SQL Server instance

4. Install frontend dependencies
```
cd Sql-Server\ Monitoring/ClientApp
npm install
```

5. Run the application (from the root directory)
```
cd ..
cd ..
dotnet run --project "Sql-Server Monitoring/Sql-Server Monitoring.csproj"
```

#### Option 2: Docker Deployment

1. Clone the repository
```
git clone https://github.com/yourusername/sql-server-monitoring.git
cd sql-server-monitoring
```

2. Create a directory for SSL certificates
```
mkdir -p https
```

3. Generate a self-signed certificate (for development only)
```
dotnet dev-certs https -ep https/aspnetapp.pfx -p password
dotnet dev-certs https --trust
```

4. Start the containers
```
docker-compose up -d
```

5. Access the application at https://localhost:5001

### Security Considerations

- Update the default passwords in docker-compose.yml before deploying to production
- Use proper SSL certificates for production deployment
- Implement a proper identity provider instead of the default test configuration
- Use least privilege accounts for SQL Server connections

### Configuration

Configure monitored instances and monitoring settings in the application UI, or adjust default values in the `appsettings.json` file:

```json
{
  "Monitoring": {
    "DefaultRetentionDays": 30,
    "DefaultIntervalSeconds": 300,
    "DefaultBackupPath": "C:\\SqlBackups"
  }
}
```

## Troubleshooting

### Common Issues

1. **Connection Failed**: Verify SQL Server connection string and ensure SQL Server is running
2. **Frontend Not Loading**: Check Node.js version and npm dependencies
3. **Certificate Issues**: For Docker deployment, ensure proper SSL certificate configuration

### Logs

- Backend logs are available in the application console and log files
- Docker logs can be viewed with `docker-compose logs sql-server-monitoring`

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 