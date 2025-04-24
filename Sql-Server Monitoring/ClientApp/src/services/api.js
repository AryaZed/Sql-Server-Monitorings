import axios from 'axios';

// Base API instance with common config
const api = axios.create({
  baseURL: '/api/v1',
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
  getServerMetrics: (serverId) => api.get(`/monitoring/metrics/${serverId}`),
  getHistoricalMetrics: (serverId, metricType, period) => 
    api.get(`/monitoring/metrics/${serverId}/history?type=${metricType}&period=${period}`),
};

// Databases API
export const databases = {
  getAll: () => api.get('/databases'),
  getById: (id) => api.get(`/databases/${id}`),
  getFragmentation: (id) => api.get(`/databases/${id}/fragmentation`),
  getMissingIndexes: (id) => api.get(`/databases/${id}/missing-indexes`),
  getSpaceUsage: (id) => api.get(`/databases/${id}/space-usage`),
  getTables: (id) => api.get(`/databases/${id}/tables`),
  createDatabase: (databaseInfo) => api.post('/databases', databaseInfo),
  updateDatabase: (id, databaseInfo) => api.put(`/databases/${id}`, databaseInfo),
  deleteDatabase: (id) => api.delete(`/databases/${id}`),
  getGrowthHistory: (id) => api.get(`/databases/${id}/growth-history`),
  getConnections: (id) => api.get(`/databases/${id}/connections`),
};

// Query Analysis API
export const queries = {
  analyzeQuery: (query, connectionString) => 
    api.post('/query-analyzer/analyze', { query, connectionString }),
  getSlowQueries: () => api.get('/query-analyzer/slow'),
  getQueryPlan: (queryId) => api.get(`/query-analyzer/queries/${queryId}/plan`),
  getQueryHistory: (queryId) => api.get(`/query-analyzer/queries/${queryId}/history`),
  getTopResourceConsumers: () => api.get('/query-analyzer/top-consumers'),
  getParallelQueries: () => api.get('/query-analyzer/parallel'),
  getBlockingQueries: () => api.get('/query-analyzer/blocking'),
};

// Query Controller API
export const queryExecution = {
  executeQuery: (query, dbName) => api.post('/query/execute', { query, dbName }),
  cancelQuery: (queryId) => api.post(`/query/${queryId}/cancel`),
  saveQuery: (query, name, description) => 
    api.post('/query/save', { query, name, description }),
  getSavedQueries: () => api.get('/query/saved'),
  getQueryResults: (queryId) => api.get(`/query/${queryId}/results`),
};

// Database Health API
export const health = {
  getServerHealth: () => api.get('/database-health/server'),
  getDatabaseHealth: (databaseId) => api.get(`/database-health/database/${databaseId}`),
  runHealthCheck: () => api.post('/database-health/check'),
  getHealthHistory: (databaseId) => api.get(`/database-health/history/${databaseId}`),
  getHealthThresholds: () => api.get('/database-health/thresholds'),
  updateHealthThresholds: (thresholds) => api.post('/database-health/thresholds', thresholds),
  getPerformanceCounters: () => api.get('/database-health/performance-counters'),
};

// Database Comparison API
export const databaseComparison = {
  compare: (sourceDbId, targetDbId) => 
    api.post('/database-comparison/compare', { sourceDbId, targetDbId }),
  getSchemaDifferences: (comparisonId) => 
    api.get(`/database-comparison/${comparisonId}/schema-differences`),
  getDataDifferences: (comparisonId, tableName) => 
    api.get(`/database-comparison/${comparisonId}/data-differences?tableName=${tableName}`),
  getStoredProcDifferences: (comparisonId) => 
    api.get(`/database-comparison/${comparisonId}/stored-procedure-differences`),
  getComparisonHistory: () => api.get('/database-comparison/history'),
};

// Stored Procedures API
export const storedProcedures = {
  getAll: (databaseId) => api.get(`/stored-procedures?databaseId=${databaseId}`),
  getById: (id) => api.get(`/stored-procedures/${id}`),
  execute: (id, parameters) => api.post(`/stored-procedures/${id}/execute`, { parameters }),
  analyzePerformance: (id) => api.get(`/stored-procedures/${id}/performance`),
  getExecutionHistory: (id) => api.get(`/stored-procedures/${id}/execution-history`),
  getSourceCode: (id) => api.get(`/stored-procedures/${id}/source`),
  updateSourceCode: (id, sourceCode) => api.put(`/stored-procedures/${id}/source`, { sourceCode }),
};

// Security API
export const security = {
  getUsers: () => api.get('/security/users'),
  getUserById: (id) => api.get(`/security/users/${id}`),
  createUser: (user) => api.post('/security/users', user),
  updateUser: (id, user) => api.put(`/security/users/${id}`, user),
  deleteUser: (id) => api.delete(`/security/users/${id}`),
  resetPassword: (id, passwordData) => api.post(`/security/users/${id}/reset-password`, passwordData),
  getPermissions: (userId) => api.get(`/security/users/${userId}/permissions`),
  updatePermissions: (userId, permissions) => api.put(`/security/users/${userId}/permissions`, permissions),
  auditLogin: () => api.get('/security/audit/logins'),
  auditSchemaChanges: () => api.get('/security/audit/schema-changes'),
  auditAccessAttempts: () => api.get('/security/audit/access-attempts'),
  getTDEStatus: () => api.get('/security/tde-status'),
  getEncryptionKeys: () => api.get('/security/encryption-keys'),
};

// Backup API
export const backup = {
  getBackupHistory: (databaseId) => api.get(`/backup/${databaseId}/history`),
  createBackup: (databaseId, backupType, location) => 
    api.post('/backup', { databaseId, backupType, location }),
  restoreBackup: (databaseId, backupFilePath, newDbName) => 
    api.post('/backup/restore', { databaseId, backupFilePath, newDbName }),
  getRestoreHistory: () => api.get('/backup/restore-history'),
  verifyBackup: (backupFilePath) => api.post('/backup/verify', { backupFilePath }),
  getBackupSettings: () => api.get('/backup/settings'),
  updateBackupSettings: (settings) => api.put('/backup/settings', settings),
  checkBackupStatus: (backupId) => api.get(`/backup/${backupId}/status`)
};

// Optimization API
export const optimization = {
  getRecommendations: (databaseId) => api.get(`/optimization/${databaseId}/recommendations`),
  applyRecommendation: (recommendationId) => api.post(`/optimization/recommendations/${recommendationId}/apply`),
  getDatabaseIndexes: (databaseId) => api.get(`/optimization/${databaseId}/indexes`),
  rebuildIndex: (indexId) => api.post(`/optimization/indexes/${indexId}/rebuild`),
  reorganizeIndex: (indexId) => api.post(`/optimization/indexes/${indexId}/reorganize`),
  updateStatistics: (databaseId, tableName) => 
    api.post(`/optimization/${databaseId}/statistics`, { tableName }),
  getStatistics: (databaseId) => api.get(`/optimization/${databaseId}/statistics`),
  generateOptimizationScript: (databaseId) => api.get(`/optimization/${databaseId}/generate-script`),
  getDatabaseConfiguration: (databaseId) => api.get(`/optimization/${databaseId}/configuration`),
  updateDatabaseConfiguration: (databaseId, config) => 
    api.put(`/optimization/${databaseId}/configuration`, config),
};

// Issues API
export const issues = {
  getAll: () => api.get('/issues'),
  getById: (id) => api.get(`/issues/${id}`),
  resolve: (id, resolution) => api.post(`/issues/${id}/resolve`, { resolution }),
  snooze: (id, duration) => api.post(`/issues/${id}/snooze`, { duration }),
  getRecommendations: (id) => api.get(`/issues/${id}/recommendations`),
  getSummary: () => api.get('/issues/summary'),
  getHistory: () => api.get('/issues/history'),
  getByCategory: (category) => api.get(`/issues/category/${category}`),
  getByServer: (serverId) => api.get(`/issues/server/${serverId}`)
};

// Query Analysis API
export const queryAnalysis = {
  analyzePerformance: (queryText) => api.post('/query-analysis/performance', { queryText }),
  getTopQueries: (databaseId) => api.get(`/query-analysis/${databaseId}/top-queries`),
  getQueryDetails: (queryId) => api.get(`/query-analysis/query/${queryId}`),
  suggestIndexes: (queryId) => api.get(`/query-analysis/query/${queryId}/suggest-indexes`),
  getQueryExecutionStats: (queryId) => api.get(`/query-analysis/query/${queryId}/execution-stats`),
  getWaitStats: () => api.get('/query-analysis/wait-stats'),
  analyzeParameterSensitivity: (queryId) => api.get(`/query-analysis/query/${queryId}/parameter-sensitivity`),
  getMissingIndexes: (databaseId) => api.get(`/query-analysis/${databaseId}/missing-indexes`)
};

// Servers API
export const servers = {
  getAll: () => api.get('/servers'),
  getById: (id) => api.get(`/servers/${id}`),
  create: (server) => api.post('/servers', server),
  update: (id, server) => api.put(`/servers/${id}`, server),
  delete: (id) => api.delete(`/servers/${id}`),
  testConnection: (connectionString) => api.post('/servers/test-connection', { connectionString }),
  getServerInstances: () => api.get('/servers/instances'),
  getServerProperties: (id) => api.get(`/servers/${id}/properties`),
  restart: (id) => api.post(`/servers/${id}/restart`),
  getServerLogs: (id) => api.get(`/servers/${id}/logs`),
};

// Alerts API
export const alerts = {
  getAll: () => api.get('/alerts'),
  getById: (id) => api.get(`/alerts/${id}`),
  create: (alert) => api.post('/alerts', alert),
  update: (id, alert) => api.put(`/alerts/${id}`, alert),
  delete: (id) => api.delete(`/alerts/${id}`),
  resolve: (id) => api.post(`/alerts/${id}/resolve`),
  getSettings: () => api.get('/alerts/settings'),
  updateSettings: (settings) => api.post('/alerts/settings', settings),
  getRecent: () => api.get('/alerts/recent'),
  getByType: (type) => api.get(`/alerts/type/${type}`),
  getBySeverity: (severity) => api.get(`/alerts/severity/${severity}`),
};

// Export all APIs
export default {
  monitoring,
  databases,
  queries,
  queryExecution,
  health,
  databaseComparison,
  storedProcedures,
  security,
  backup,
  optimization,
  issues,
  queryAnalysis,
  servers,
  alerts,
}; 