import axios from 'axios';

// Create an axios instance with default config
const api = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Intercept responses to handle errors globally
api.interceptors.response.use(
  response => response,
  error => {
    // Check if it's a network error (backend not available)
    if (error.message === 'Network Error' || !error.response) {
      console.warn('API: Backend not available - returning mock data');
      // Return mock data based on the request URL
      return Promise.resolve({
        data: getMockData(error.config.url),
        status: 200,
        statusText: 'OK (Mock)',
        headers: {},
        config: error.config
      });
    }
    
    return Promise.reject(error);
  }
);

// Helper function to get mock data based on the API endpoint
function getMockData(url) {
  if (!url) return null;
  
  // Generate random data for different endpoint types
  if (url.includes('/servers')) {
    return mockServers;
  } else if (url.includes('/databases')) {
    return mockDatabases;
  } else if (url.includes('/performance')) {
    return mockPerformanceData;
  } else if (url.includes('/queries')) {
    return mockQueries;
  } else if (url.includes('/issues')) {
    return mockIssues;
  } else if (url.includes('/alerts')) {
    return mockAlerts;
  } else if (url.includes('/backups')) {
    return mockBackups;
  } else if (url.includes('/settings')) {
    return mockSettings;
  } else if (url.includes('/health')) {
    return mockHealth;
  } else if (url.includes('/monitoring/status')) {
    return { isMonitoring: true };
  } else if (url.includes('/dashboard')) {
    return mockDashboard;
  } else {
    return { message: 'Mock data not available for this endpoint' };
  }
}

// Mock data for various endpoints
const mockServers = [
  { id: 1, name: 'SQL-SERVER-01', version: 'SQL Server 2019', isConnected: true, status: 'Healthy' },
  { id: 2, name: 'SQL-SERVER-02', version: 'SQL Server 2017', isConnected: true, status: 'Warning' },
  { id: 3, name: 'SQL-SERVER-03', version: 'SQL Server 2016', isConnected: false, status: 'Critical' }
];

const mockDatabases = [
  { id: 1, name: 'AdventureWorks', serverId: 1, size: '1.2 GB', status: 'Online', lastBackup: '2023-10-01T12:00:00Z', recovery: 'Full' },
  { id: 2, name: 'Northwind', serverId: 1, size: '350 MB', status: 'Online', lastBackup: '2023-10-01T12:00:00Z', recovery: 'Simple' },
  { id: 3, name: 'WideWorldImporters', serverId: 2, size: '2.1 GB', status: 'Online', lastBackup: '2023-09-29T12:00:00Z', recovery: 'Full' },
  { id: 4, name: 'ReportServer', serverId: 3, size: '500 MB', status: 'Offline', lastBackup: '2023-09-25T12:00:00Z', recovery: 'Full' }
];

const mockPerformanceData = {
  cpu: Array(10).fill(0).map(() => Math.floor(Math.random() * 100)),
  memory: Array(10).fill(0).map(() => Math.floor(Math.random() * 100)),
  disk: Array(10).fill(0).map(() => Math.floor(Math.random() * 200)),
  network: Array(10).fill(0).map(() => Math.floor(Math.random() * 100)),
  connections: Math.floor(Math.random() * 150),
  transactions: Math.floor(Math.random() * 1000)
};

const mockQueries = [
  { id: 1, query: 'SELECT * FROM Customers WHERE Region = @Region', duration: 1250, cpu: 800, reads: 12500, executions: 1500 },
  { id: 2, query: 'UPDATE Orders SET Status = @Status WHERE OrderDate < @Date', duration: 850, cpu: 550, reads: 8900, executions: 450 },
  { id: 3, query: 'SELECT o.OrderID, c.CustomerName FROM Orders o JOIN Customers c ON o.CustomerID = c.CustomerID', duration: 3200, cpu: 1800, reads: 25800, executions: 250 }
];

const mockIssues = [
  { id: 1, title: 'High CPU Usage', description: 'Server has sustained CPU usage above 90% for more than 30 minutes', severity: 'Critical', createdAt: '2023-10-01T10:15:00Z' },
  { id: 2, title: 'Missing Index', description: 'Potential missing index detected on Orders.CustomerID', severity: 'Warning', createdAt: '2023-10-01T09:30:00Z' },
  { id: 3, title: 'Database Growth', description: 'Database WideWorldImporters has grown by 25% in the past week', severity: 'Information', createdAt: '2023-09-29T14:20:00Z' }
];

const mockAlerts = [
  { id: 1, message: 'Server SQL-SERVER-01 CPU usage above 95%', severity: 'critical', timestamp: '2023-10-01T10:15:00Z', isRead: false },
  { id: 2, message: 'Database WideWorldImporters approaching storage limit', severity: 'warning', timestamp: '2023-10-01T09:30:00Z', isRead: true },
  { id: 3, message: 'Successful backup completed for AdventureWorks', severity: 'info', timestamp: '2023-09-30T12:00:00Z', isRead: true }
];

const mockBackups = [
  { id: 1, databaseName: 'AdventureWorks', backupType: 'Full', startTime: '2023-10-01T12:00:00Z', endTime: '2023-10-01T12:15:00Z', size: '1.1 GB', status: 'Completed' },
  { id: 2, databaseName: 'Northwind', backupType: 'Differential', startTime: '2023-10-01T12:30:00Z', endTime: '2023-10-01T12:35:00Z', size: '250 MB', status: 'Completed' },
  { id: 3, databaseName: 'WideWorldImporters', backupType: 'Log', startTime: '2023-10-01T13:00:00Z', endTime: '2023-10-01T13:02:00Z', size: '75 MB', status: 'Completed' }
];

const mockSettings = {
  monitoringInterval: 5,
  retentionPeriod: 30,
  alertNotifications: true,
  emailSettings: {
    smtpServer: 'smtp.example.com',
    port: 587,
    useSsl: true,
    username: 'monitor@example.com',
    recipients: ['admin@example.com']
  }
};

const mockHealth = {
  status: 'Healthy',
  components: [
    { name: 'SQL Server Connection', status: 'Healthy', description: 'Connection to SQL Server is working properly' },
    { name: 'Web API', status: 'Healthy', description: 'Web API is responding to requests' },
    { name: 'Monitoring Service', status: 'Healthy', description: 'Monitoring service is collecting data' }
  ]
};

const mockDashboard = {
  serverCount: 3,
  databaseCount: 15,
  activeConnections: 42,
  pendingQueries: 8,
  serverHealth: {
    healthy: 1,
    warning: 1,
    critical: 1
  },
  databaseHealth: {
    healthy: 10,
    warning: 3,
    critical: 2
  },
  performance: {
    cpu: 65,
    memory: 78,
    disk: 45,
    network: 22,
    activeConnections: 42
  },
  recentIssues: mockIssues,
  alerts: mockAlerts.slice(0, 3),
  topQueries: mockQueries
};

// API methods organized by domain
const servers = {
  getAll: () => api.get('/api/servers'),
  getById: (id) => api.get(`/api/servers/${id}`),
  create: (serverData) => api.post('/api/servers', serverData),
  update: (id, serverData) => api.put(`/api/servers/${id}`, serverData),
  delete: (id) => api.delete(`/api/servers/${id}`),
  testConnection: (connectionString) => api.post('/api/servers/test-connection', { connectionString }),
  getPerformance: (id) => api.get(`/api/servers/${id}/performance`)
};

const databases = {
  getAll: () => api.get('/api/databases'),
  getByServerId: (serverId) => api.get(`/api/servers/${serverId}/databases`),
  getById: (id) => api.get(`/api/databases/${id}`),
  create: (databaseData) => api.post('/api/databases', databaseData),
  update: (id, databaseData) => api.put(`/api/databases/${id}`, databaseData),
  delete: (id) => api.delete(`/api/databases/${id}`),
  analyze: (id) => api.post(`/api/databases/${id}/analyze`),
  optimize: (id) => api.post(`/api/databases/${id}/optimize`),
  getSize: (id) => api.get(`/api/databases/${id}/size`),
  getTables: (id) => api.get(`/api/databases/${id}/tables`),
  getIndices: (id) => api.get(`/api/databases/${id}/indices`)
};

const health = {
  getServerHealth: () => api.get('/api/health/server'),
  getDatabaseHealth: (id) => api.get(`/api/health/database/${id}`),
  getSystemOverview: () => api.get('/api/health/overview'),
  getHealthHistory: (days) => api.get(`/api/health/history?days=${days || 7}`)
};

const queries = {
  getTopResourceConsumers: () => api.get('/api/queries/top'),
  getByDatabase: (databaseId) => api.get(`/api/databases/${databaseId}/queries`),
  getById: (id) => api.get(`/api/queries/${id}`),
  analyze: (queryText) => api.post('/api/queries/analyze', { queryText }),
  getHistory: (days) => api.get(`/api/queries/history?days=${days || 7}`)
};

const issues = {
  getAll: () => api.get('/api/issues'),
  getById: (id) => api.get(`/api/issues/${id}`),
  markAsResolved: (id) => api.put(`/api/issues/${id}/resolve`),
  dismiss: (id) => api.put(`/api/issues/${id}/dismiss`),
  getSuggestions: (id) => api.get(`/api/issues/${id}/suggestions`)
};

const backup = {
  getBackupHistory: () => api.get('/api/backups/history'),
  getDatabaseBackups: (databaseId) => api.get(`/api/databases/${databaseId}/backups`),
  createBackup: (databaseId, backupType) => api.post('/api/backups', { databaseId, backupType }),
  getBackupPlan: () => api.get('/api/backups/plan'),
  updateBackupPlan: (planData) => api.put('/api/backups/plan', planData)
};

const monitoring = {
  getStatus: () => api.get('/api/monitoring/status'),
  start: (serverId) => api.post('/api/monitoring/start', { serverId }),
  stop: (serverId) => api.post('/api/monitoring/stop', { serverId }),
  getDashboardData: () => api.get('/api/monitoring/dashboard'),
  getMetricHistory: (metricName, days) => api.get(`/api/monitoring/metrics/${metricName}?days=${days || 7}`)
};

const users = {
  getAll: () => api.get('/api/users'),
  getById: (id) => api.get(`/api/users/${id}`),
  create: (userData) => api.post('/api/users', userData),
  update: (id, userData) => api.put(`/api/users/${id}`, userData),
  delete: (id) => api.delete(`/api/users/${id}`),
  getCurrentUser: () => api.get('/api/users/current')
};

const security = {
  login: (credentials) => api.post('/api/auth/login', credentials),
  logout: () => api.post('/api/auth/logout'),
  refreshToken: () => api.post('/api/auth/refresh-token'),
  changePassword: (passwordData) => api.post('/api/auth/change-password', passwordData),
  getPermissions: () => api.get('/api/security/permissions')
};

// Export all API domains
export {
  servers,
  databases,
  health,
  queries,
  issues,
  backup,
  monitoring,
  users,
  security,
  api
}; 