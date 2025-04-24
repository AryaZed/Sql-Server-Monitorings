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
- ASP.NET Core for the backend API
- React for the frontend UI
- SQL Server to store monitoring data

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- SQL Server 2022 or later
- Node.js and npm (for React frontend)

### Installation

1. Clone the repository
```
git clone https://github.com/yourusername/sql-server-monitoring.git
```

2. Restore dependencies
```
dotnet restore
```

3. Setup the database
```
dotnet ef database update
```

4. Install frontend dependencies
```
cd ClientApp
npm install
```

5. Run the application
```
dotnet run
```

### Configuration

Configure monitored instances in the `appsettings.json` file:

```json
{
  "MonitoringSettings": {
    "MonitoringIntervalSeconds": 300,
    "MonitorCpu": true,
    "MonitorMemory": true,
    "MonitorDisk": true,
    "MonitorQueries": true,
    "MonitorBlocking": true,
    "MonitorDeadlocks": true,
    "HighCpuThresholdPercent": 85,
    "LowPageLifeExpectancyThreshold": 300,
    "LongRunningQueryThresholdSec": 30,
    "RetentionDays": 30
  }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 