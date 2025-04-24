import { BehaviorSubject } from 'rxjs';
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
    this.isInitialized = false;
    this.currentServerId = null;
  }

  /**
   * Initialize the monitoring service and set up SignalR listeners
   */
  init() {
    if (this.isInitialized) {
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

    this.isInitialized = true;
  }

  /**
   * Start monitoring for a specific server
   * @param {string} serverId - Server ID to monitor
   * @param {string} connectionString - Connection string for the server
   */
  async startMonitoring(serverId, connectionString) {
    try {
      this.currentServerId = serverId;
      
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
    try {
      const response = await monitoring.getSettings();
      monitoringStatus.next(response.data.monitoringEnabled);
      return response.data;
    } catch (error) {
      console.error('Failed to get monitoring status:', error);
      throw error;
    }
  }

  /**
   * Get dashboard data with all metrics
   */
  async getDashboardData() {
    try {
      const response = await monitoring.getDashboardData();
      return response.data;
    } catch (error) {
      console.error('Failed to get dashboard data:', error);
      throw error;
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
}

// Create a singleton instance
const monitoringService = new MonitoringService();

export default monitoringService; 