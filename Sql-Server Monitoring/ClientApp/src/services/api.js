import axios from 'axios';

// Base API instance with common config
const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add a request interceptor to include connection string in headers
api.interceptors.request.use(
  (config) => {
    const connectionString = sessionStorage.getItem('connectionString');
    if (connectionString) {
      config.headers['X-Connection-String'] = connectionString;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Monitoring API
export const monitoring = {
  getSettings: () => api.get('/monitoring/settings'),
  updateSettings: (settings) => api.post('/monitoring/settings', settings),
  startMonitoring: (connectionString) => api.post('/monitoring/start', { connectionString }),
  stopMonitoring: () => api.post('/monitoring/stop'),
  getDashboardData: () => api.get('/monitoring/dashboard'),
};

// Databases API
export const databases = {
  getAll: () => api.get('/databases'),
  getById: (id) => api.get(`/databases/${id}`),
  getFragmentation: (id) => api.get(`/databases/${id}/fragmentation`),
  getMissingIndexes: (id) => api.get(`/databases/${id}/missing-indexes`),
  getSpaceUsage: (id) => api.get(`/databases/${id}/space-usage`),
  getTables: (id) => api.get(`/databases/${id}/tables`),
};

// Query Analysis API
export const queries = {
  analyzeQuery: (query, connectionString) => 
    api.post('/queries/analyze', { query, connectionString }),
  getSlowQueries: () => api.get('/queries/slow'),
  getQueryPlan: (queryId) => api.get(`/queries/${queryId}/plan`),
  getQueryHistory: (queryId) => api.get(`/queries/${queryId}/history`),
};

// Server Health API
export const health = {
  getServerHealth: () => api.get('/health/server'),
  getDatabaseHealth: (databaseId) => api.get(`/health/database/${databaseId}`),
  runHealthCheck: () => api.post('/health/check'),
};

// Alerts API
export const alerts = {
  getAll: () => api.get('/alerts'),
  getById: (id) => api.get(`/alerts/${id}`),
  resolve: (id) => api.post(`/alerts/${id}/resolve`),
  getSettings: () => api.get('/alerts/settings'),
  updateSettings: (settings) => api.post('/alerts/settings', settings),
};

// Security/User Management API
export const security = {
  getUsers: () => api.get('/security/users'),
  getUserById: (id) => api.get(`/security/users/${id}`),
  createUser: (user) => api.post('/security/users', user),
  updateUser: (id, user) => api.put(`/security/users/${id}`, user),
  deleteUser: (id) => api.delete(`/security/users/${id}`),
  resetPassword: (id, passwordData) => api.post(`/security/users/${id}/reset-password`, passwordData),
};

// Servers API
export const servers = {
  getAll: () => api.get('/servers'),
  getById: (id) => api.get(`/servers/${id}`),
  create: (server) => api.post('/servers', server),
  update: (id, server) => api.put(`/servers/${id}`, server),
  delete: (id) => api.delete(`/servers/${id}`),
  testConnection: (connectionString) => api.post('/servers/test-connection', { connectionString }),
};

// Export all APIs
export default {
  monitoring,
  databases,
  queries,
  health,
  alerts,
  security,
  servers,
}; 