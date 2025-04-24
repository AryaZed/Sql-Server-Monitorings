# SQL Server Monitoring Dashboard

This is a React-based dashboard for the SQL Server Monitoring application. It provides a user-friendly interface to monitor and manage SQL Server instances, track performance metrics, analyze queries, and more.

## Features

- Real-time server performance monitoring (CPU, Memory, Disk)
- Database health monitoring and management
- Query analysis and optimization
- Security auditing
- Backup management
- Issue detection and alerting

## Development Setup

### Prerequisites

- Node.js (v18 or later)
- npm (v9 or later)

### Installation

1. Navigate to the ClientApp directory:
   ```
   cd Sql-Server\ Monitoring/ClientApp
   ```

2. Install dependencies:
   ```
   npm install
   ```

3. Start the development server:
   ```
   npm start
   ```

The application will be available at http://localhost:3000.

## Development and Backend

The React application communicates with the .NET Core backend API. When running in development mode:

1. Start the .NET Core API:
   ```
   dotnet run
   ```

2. Start the React development server:
   ```
   cd ClientApp
   npm start
   ```

## Building for Production

To build the application for production:

```
cd ClientApp
npm run build
```

This will create a production-ready build in the `ClientApp/build` directory, which will be served by the .NET Core application.

## Technologies Used

- React
- React Router
- Material UI
- Chart.js
- SignalR (for real-time updates)
- Axios 