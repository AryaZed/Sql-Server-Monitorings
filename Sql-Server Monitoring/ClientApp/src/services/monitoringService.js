import { BehaviorSubject, Subject } from 'rxjs';
import signalRService from './signalrService';
import { monitoring } from './api';

// Observable subjects for real-time data
const cpuMetrics = new BehaviorSubject([]);
const memoryMetrics = new BehaviorSubject([]);
const diskMetrics = new BehaviorSubject([]);
const networkMetrics = new BehaviorSubject([]);
const databaseMetrics = new BehaviorSubject([]);
const queryMetrics = new BehaviorSubject([]);
const alertsSubject = new BehaviorSubject([]);
const monitoringStatus = new BehaviorSubject(false);
const serverHealthSubject = new BehaviorSubject({});

class MonitoringService {
  constructor() {
    // Observable streams - renamed to avoid conflict with getters
    this._statusSubject = new Subject();
    this._cpuMetricsSubject = new Subject();
    this._memoryMetricsSubject = new Subject();
    this._diskMetricsSubject = new Subject();
    this._serverHealthSubject = new Subject();
    this._alertsSubject = new Subject();
    
    // State
    this.isMonitoring = false;
    this.connectionString = '';
    this.serverName = '';
    
    // Mock data for testing without backend
    this.mockMode = true;
    this.mockInterval = null;
  }

  /**
   * Initialize the monitoring service and set up SignalR listeners
   */
  init() {
    if (this.isMonitoring) {
      return;
    }

    // Set up SignalR event handlers
    signalRService.on('MetricsUpdated', (data) => {
      this.handleMetricsUpdate(data);
    });

    signalRService.on('AlertRaised', (alert) => {
      this.handleNewAlert(alert);
    });

    signalRService.on('MonitoringStatusChanged', (status) => {
      monitoringStatus.next(status.isRunning);
    });

    signalRService.on('ConnectivityStatus', (status) => {
      const currentHealth = serverHealthSubject.value;
      serverHealthSubject.next({
        ...currentHealth,
        isConnected: status.isConnected,
        lastChecked: status.timestamp
      });
    });

    signalRService.on('DatabaseMetricsUpdated', (data) => {
      databaseMetrics.next(data);
    });

    signalRService.on('QueryMetricsUpdated', (data) => {
      queryMetrics.next(data);
    });

    this.isMonitoring = true;

    // If we're in mock mode, emit mock data periodically
    if (this.mockMode) {
      this.startMockDataEmission();
    }
  }

  /**
   * Start monitoring for a specific server
   * @param {string} serverId - Server ID to monitor
   * @param {string} connectionString - Connection string for the server
   */
  async startMonitoring(serverId, connectionString) {
    try {
      this.serverName = serverId;
      this.connectionString = connectionString;
      
      // Call the API to start monitoring
      const response = await monitoring.startMonitoring(connectionString);
      
      // Set monitoring status
      monitoringStatus.next(true);
      
      return response.data;
    } catch (error) {
      console.error('Failed to start monitoring:', error);
      throw error;
    }
  }

  /**
   * Stop monitoring service
   */
  async stopMonitoring() {
    try {
      // Call the API to stop monitoring
      const response = await monitoring.stopMonitoring();
      
      // Set monitoring status
      monitoringStatus.next(false);
      
      return response.data;
    } catch (error) {
      console.error('Failed to stop monitoring:', error);
      throw error;
    }
  }

  /**
   * Get current monitoring status
   */
  async getMonitoringStatus() {
    if (this.mockMode) {
      this.isMonitoring = true;
      this._statusSubject.next(true);
      return { isMonitoring: true };
    }
    
    try {
      const response = await monitoring.getSettings();
      this.isMonitoring = response.data.monitoringEnabled;
      monitoringStatus.next(this.isMonitoring);
      return response.data;
    } catch (error) {
      console.error('Failed to get monitoring status:', error);
      return { isMonitoring: false };
    }
  }

  /**
   * Get dashboard data with all metrics
   */
  async getDashboardData() {
    if (this.mockMode) {
      return this.getMockDashboardData();
    }
    
    try {
      const response = await monitoring.getDashboardData();
      return response.data;
    } catch (error) {
      console.error('Failed to get dashboard data:', error);
      return this.getMockDashboardData();
    }
  }

  /**
   * Get historical metrics for a specific type
   * @param {string} serverId - Server ID
   * @param {string} metricType - Type of metric (cpu, memory, disk, network)
   * @param {string} period - Time period (hour, day, week, month)
   */
  async getHistoricalMetrics(serverId, metricType, period) {
    try {
      const response = await monitoring.getHistoricalMetrics(serverId, metricType, period);
      return response.data;
    } catch (error) {
      console.error(`Failed to get ${metricType} metrics:`, error);
      throw error;
    }
  }

  /**
   * Handle incoming metrics updates from SignalR
   * @param {object} data - Metrics data
   */
  handleMetricsUpdate(data) {
    if (data.cpu) {
      const current = cpuMetrics.value;
      const updated = [...current, data.cpu].slice(-60); // Keep last 60 data points
      cpuMetrics.next(updated);
    }

    if (data.memory) {
      const current = memoryMetrics.value;
      const updated = [...current, data.memory].slice(-60);
      memoryMetrics.next(updated);
    }

    if (data.disk) {
      const current = diskMetrics.value;
      const updated = [...current, data.disk].slice(-60);
      diskMetrics.next(updated);
    }

    if (data.network) {
      const current = networkMetrics.value;
      const updated = [...current, data.network].slice(-60);
      networkMetrics.next(updated);
    }

    // Update overall server health
    serverHealthSubject.next({
      ...serverHealthSubject.value,
      cpu: data.cpu,
      memory: data.memory, 
      disk: data.disk,
      network: data.network,
      lastUpdated: new Date()
    });
  }

  /**
   * Handle new alerts from SignalR
   * @param {object} alert - Alert data
   */
  handleNewAlert(alert) {
    const current = alertsSubject.value;
    alertsSubject.next([alert, ...current]);
  }

  // Getters for the observable subjects
  get cpuMetrics$() {
    return cpuMetrics.asObservable();
  }

  get memoryMetrics$() {
    return memoryMetrics.asObservable();
  }

  get diskMetrics$() {
    return diskMetrics.asObservable();
  }

  get networkMetrics$() {
    return networkMetrics.asObservable();
  }

  get databaseMetrics$() {
    return databaseMetrics.asObservable();
  }

  get queryMetrics$() {
    return queryMetrics.asObservable();
  }

  get alerts$() {
    return alertsSubject.asObservable();
  }

  get status$() {
    return monitoringStatus.asObservable();
  }

  get serverHealth$() {
    return serverHealthSubject.asObservable();
  }

  // Start emitting mock data
  startMockDataEmission() {
    if (this.mockInterval) {
      clearInterval(this.mockInterval);
    }
    
    // Emit initial mock data
    this.emitMockData();
    
    // Set up interval to emit mock data
    this.mockInterval = setInterval(() => {
      this.emitMockData();
    }, 5000);
  }
  
  // Stop emitting mock data
  stopMockDataEmission() {
    if (this.mockInterval) {
      clearInterval(this.mockInterval);
      this.mockInterval = null;
    }
  }
  
  // Emit mock data for testing without backend
  emitMockData() {
    // Emit mock CPU metrics
    const cpuData = Array(10).fill(0).map(() => Math.floor(Math.random() * 100));
    this._cpuMetricsSubject.next(cpuData);
    cpuMetrics.next(cpuData);
    
    // Emit mock memory metrics
    const memoryData = Array(10).fill(0).map(() => Math.floor(Math.random() * 100));
    this._memoryMetricsSubject.next(memoryData);
    memoryMetrics.next(memoryData);
    
    // Emit mock disk metrics
    const diskData = Array(10).fill(0).map(() => Math.floor(Math.random() * 50));
    this._diskMetricsSubject.next(diskData);
    diskMetrics.next(diskData);
    
    // Emit mock server health
    const serverHealth = {
      isConnected: true,
      status: 'Healthy',
      cpu: Math.floor(Math.random() * 100),
      memory: Math.floor(Math.random() * 100),
      disk: Math.floor(Math.random() * 100),
      network: Math.floor(Math.random() * 100)
    };
    this._serverHealthSubject.next(serverHealth);
    serverHealthSubject.next(serverHealth);
    
    // Emit mock monitoring status
    this._statusSubject.next(true);
    monitoringStatus.next(true);
    
    // Occasionally emit mock alerts
    if (Math.random() > 0.7) {
      const mockAlerts = [
        { id: 1, message: 'High CPU usage detected', severity: 'warning', timestamp: new Date().toISOString() },
        { id: 2, message: 'Database growth exceeding threshold', severity: 'info', timestamp: new Date().toISOString() },
        { id: 3, message: 'Slow query performance', severity: 'warning', timestamp: new Date().toISOString() }
      ];
      this._alertsSubject.next(mockAlerts);
      alertsSubject.next(mockAlerts);
    }
  }
  
  // Get mock dashboard data
  getMockDashboardData() {
    return {
      serverCount: 3,
      databaseCount: 12,
      alerts: [
        { id: 1, message: 'High CPU usage detected', severity: 'warning', timestamp: new Date().toISOString() },
        { id: 2, message: 'Database growth exceeding threshold', severity: 'info', timestamp: new Date().toISOString() },
        { id: 3, message: 'Slow query performance', severity: 'warning', timestamp: new Date().toISOString() }
      ],
      performance: {
        cpu: Math.floor(Math.random() * 100),
        memory: Math.floor(Math.random() * 100),
        disk: Math.floor(Math.random() * 100),
        network: Math.floor(Math.random() * 100),
        activeConnections: Math.floor(Math.random() * 200)
      },
      databases: {
        total: 12,
        healthy: 8,
        warning: 3,
        critical: 1
      },
      issues: [
        { id: 1, title: 'High CPU Usage', description: 'CPU usage above threshold for extended period', severity: 'warning', createdAt: new Date().toISOString() },
        { id: 2, title: 'Missing Index', description: 'Potential missing index detected on Orders table', severity: 'info', createdAt: new Date().toISOString() },
        { id: 3, title: 'Long-running Transaction', description: 'Transaction running for more than 10 minutes', severity: 'warning', createdAt: new Date().toISOString() }
      ]
    };
  }
}

// Create a singleton instance
const monitoringService = new MonitoringService();

export default monitoringService; 