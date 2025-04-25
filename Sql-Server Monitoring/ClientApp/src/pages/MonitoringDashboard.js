import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Box,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
  CardHeader,
  IconButton,
  Divider,
  Chip,
  LinearProgress,
  Button,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Menu,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Tooltip,
  Badge
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  Warning as WarningIcon,
  Speed as SpeedIcon,
  Memory as MemoryIcon,
  Save as SaveIcon,
  Info as InfoIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Notifications as NotificationsIcon,
  PlayArrow as PlayArrowIcon,
  Stop as StopIcon,
  MoreVert as MoreVertIcon,
  Storage as DatabaseIcon,
  Security as SecurityIcon,
  Backup as BackupIcon,
  Code as CodeIcon,
  Settings as SettingsIcon,
  Sync as SyncIcon
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import monitoringService from '../services/monitoringService';
import signalRService from '../services/signalrService';
import { monitoring, databases, health, queries, issues, backup } from '../services/api';
import { Line, Doughnut, Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip as ChartTooltip,
  Legend,
  ArcElement
} from 'chart.js';
import { format } from 'date-fns';

// Register ChartJS components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  ChartTooltip,
  Legend,
  ArcElement
);

// Tab panel component
function TabPanel(props) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`monitoring-tabpanel-${index}`}
      aria-labelledby={`monitoring-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

function MonitoringDashboard() {
  const { connectionString, serverName, isConnected, signalRService } = useConnection();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isMonitoring, setIsMonitoring] = useState(false);
  const [tabIndex, setTabIndex] = useState(0);
  const [refreshInterval, setRefreshInterval] = useState(30000); // 30 seconds default
  const [lastRefresh, setLastRefresh] = useState(new Date());
  
  // Dashboard data
  const [dashboardData, setDashboardData] = useState(null);
  const [serverHealth, setServerHealth] = useState(null);
  const [databaseList, setDatabaseList] = useState([]);
  const [alertList, setAlertList] = useState([]);
  const [issueList, setIssueList] = useState([]);
  const [topQueries, setTopQueries] = useState([]);
  const [backupStatus, setBackupStatus] = useState([]);
  
  // Chart data
  const [cpuChartData, setCpuChartData] = useState({
    labels: Array.from({ length: 10 }, (_, i) => `${10 - i} min ago`),
    datasets: [
      {
        label: 'CPU Usage %',
        data: Array(10).fill(0),
        borderColor: 'rgba(75, 192, 192, 1)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        tension: 0.4,
      },
    ],
  });
  const [memoryChartData, setMemoryChartData] = useState({
    labels: Array.from({ length: 10 }, (_, i) => `${10 - i} min ago`),
    datasets: [
      {
        label: 'Memory Usage %',
        data: Array(10).fill(0),
        borderColor: 'rgba(255, 99, 132, 1)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        tension: 0.4,
      },
    ],
  });
  const [diskChartData, setDiskChartData] = useState({
    labels: Array.from({ length: 10 }, (_, i) => `${10 - i} min ago`),
    datasets: [
      {
        label: 'Disk I/O (MB/s)',
        data: Array(10).fill(0),
        borderColor: 'rgba(54, 162, 235, 1)',
        backgroundColor: 'rgba(54, 162, 235, 0.2)',
        tension: 0.4,
      },
    ],
  });

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        max: 100,
      },
    },
    plugins: {
      legend: {
        position: 'top',
      },
    },
  };

  // Initialize monitoring service and set up event handlers
  useEffect(() => {
    if (!connectionString) return;

    monitoringService.init();
    
    // Subscribe to monitoring status changes
    const statusSubscription = monitoringService.status$.subscribe(status => {
      setIsMonitoring(status);
    });
    
    // Subscribe to server health updates
    const healthSubscription = monitoringService.serverHealth$.subscribe(health => {
      setServerHealth(health);
    });
    
    // Set up interval for dashboard refresh
    const refreshTimer = setInterval(() => {
      refreshData();
      setLastRefresh(new Date());
    }, refreshInterval);

    // Clean up on unmount
    return () => {
      statusSubscription.unsubscribe();
      healthSubscription.unsubscribe();
      clearInterval(refreshTimer);
    };
  }, [connectionString, refreshInterval]);

  // Initial data load
  useEffect(() => {
    if (!connectionString) return;
    
    const loadInitialData = async () => {
      setLoading(true);
      try {
        // Get monitoring status
        await monitoringService.getMonitoringStatus();
        
        // Fetch dashboard data
        await fetchDashboardData();
        
        // Check monitoring server status
        fetchServerHealth();
        
        // Fetch databases
        fetchDatabases();
        
        // Fetch issues
        fetchIssues();
        
        // Fetch top queries
        fetchTopQueries();
        
        // Fetch backup status
        fetchBackupStatus();
        
      } catch (err) {
        console.error('Error loading initial dashboard data:', err);
        setError('Failed to load dashboard data');
      } finally {
        setLoading(false);
      }
    };
    
    loadInitialData();
  }, [connectionString]);

  // CPU metrics subscription
  useEffect(() => {
    if (!connectionString) return;
    
    const subscription = monitoringService.cpuMetrics$.subscribe(metrics => {
      if (metrics && metrics.length > 0) {
        setCpuChartData(prev => ({
          ...prev,
          labels: metrics.map((_, i) => `${metrics.length - i} min ago`),
          datasets: [{
            ...prev.datasets[0],
            data: metrics
          }]
        }));
      }
    });
    
    return () => subscription.unsubscribe();
  }, [connectionString]);

  // Memory metrics subscription
  useEffect(() => {
    if (!connectionString) return;
    
    const subscription = monitoringService.memoryMetrics$.subscribe(metrics => {
      if (metrics && metrics.length > 0) {
        setMemoryChartData(prev => ({
          ...prev,
          labels: metrics.map((_, i) => `${metrics.length - i} min ago`),
          datasets: [{
            ...prev.datasets[0],
            data: metrics
          }]
        }));
      }
    });
    
    return () => subscription.unsubscribe();
  }, [connectionString]);

  // Disk metrics subscription
  useEffect(() => {
    if (!connectionString) return;
    
    const subscription = monitoringService.diskMetrics$.subscribe(metrics => {
      if (metrics && metrics.length > 0) {
        setDiskChartData(prev => ({
          ...prev,
          labels: metrics.map((_, i) => `${metrics.length - i} min ago`),
          datasets: [{
            ...prev.datasets[0],
            data: metrics
          }]
        }));
      }
    });
    
    return () => subscription.unsubscribe();
  }, [connectionString]);

  // Alerts subscription
  useEffect(() => {
    if (!connectionString) return;
    
    const subscription = monitoringService.alerts$.subscribe(alerts => {
      if (alerts) {
        setAlertList(alerts);
      }
    });
    
    return () => subscription.unsubscribe();
  }, [connectionString]);

  // Fetch dashboard data
  const fetchDashboardData = async () => {
    try {
      const data = await monitoringService.getDashboardData();
      setDashboardData(data);
    } catch (err) {
      console.error('Error fetching dashboard data:', err);
    }
  };

  // Fetch server health
  const fetchServerHealth = async () => {
    try {
      const response = await health.getServerHealth();
      setServerHealth(response.data);
    } catch (err) {
      console.error('Error fetching server health:', err);
    }
  };

  // Fetch databases
  const fetchDatabases = async () => {
    try {
      const response = await databases.getAll();
      setDatabaseList(response.data);
    } catch (err) {
      console.error('Error fetching databases:', err);
    }
  };

  // Fetch issues
  const fetchIssues = async () => {
    try {
      const response = await issues.getAll();
      setIssueList(response.data);
    } catch (err) {
      console.error('Error fetching issues:', err);
    }
  };

  // Fetch top queries
  const fetchTopQueries = async () => {
    try {
      const response = await queries.getTopResourceConsumers();
      setTopQueries(response.data);
    } catch (err) {
      console.error('Error fetching top queries:', err);
    }
  };

  // Fetch backup status
  const fetchBackupStatus = async () => {
    try {
      const response = await backup.getBackupHistory();
      setBackupStatus(response.data);
    } catch (err) {
      console.error('Error fetching backup status:', err);
    }
  };

  // Start monitoring
  const startMonitoring = async () => {
    try {
      setLoading(true);
      await monitoringService.startMonitoring(serverName, connectionString);
      setLoading(false);
    } catch (err) {
      console.error('Error starting monitoring:', err);
      setError('Failed to start monitoring');
      setLoading(false);
    }
  };

  // Stop monitoring
  const stopMonitoring = async () => {
    try {
      setLoading(true);
      await monitoringService.stopMonitoring();
      setLoading(false);
    } catch (err) {
      console.error('Error stopping monitoring:', err);
      setError('Failed to stop monitoring');
      setLoading(false);
    }
  };

  // Refresh data manually
  const refreshData = async () => {
    setLoading(true);
    try {
      await fetchDashboardData();
      await fetchServerHealth();
      await fetchDatabases();
      await fetchIssues();
      await fetchTopQueries();
      await fetchBackupStatus();
      setLastRefresh(new Date());
    } catch (err) {
      console.error('Error refreshing data:', err);
      setError('Failed to refresh data');
    } finally {
      setLoading(false);
    }
  };

  // Handle tab change
  const handleTabChange = (event, newValue) => {
    setTabIndex(newValue);
  };

  // Format date
  const formatDate = (dateString) => {
    try {
      return format(new Date(dateString), 'MMM dd, yyyy HH:mm:ss');
    } catch (err) {
      return dateString;
    }
  };

  // Get severity chip
  const getSeverityChip = (severity) => {
    switch (severity?.toLowerCase()) {
      case 'critical':
        return <Chip size="small" label="Critical" color="error" />;
      case 'warning':
        return <Chip size="small" label="Warning" color="warning" />;
      case 'information':
      case 'info':
        return <Chip size="small" label="Info" color="info" />;
      default:
        return <Chip size="small" label={severity} color="default" />;
    }
  };

  if (!connectionString) {
    return (
      <Box sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h5" gutterBottom>
          No SQL Server Connection
        </Typography>
        <Typography paragraph>
          Please connect to a SQL Server to use the monitoring dashboard.
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ flexGrow: 1 }}>
      {/* Header */}
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4" component="h1" gutterBottom>
          SQL Server Monitoring
        </Typography>
        
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Typography variant="body2" color="textSecondary">
            Last refreshed: {formatDate(lastRefresh)}
          </Typography>
          
          <Tooltip title="Refresh Data">
            <IconButton onClick={refreshData} disabled={loading}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
          
          {isMonitoring ? (
            <Button
              variant="outlined"
              color="error"
              startIcon={<StopIcon />}
              onClick={stopMonitoring}
              disabled={loading}
            >
              Stop Monitoring
            </Button>
          ) : (
            <Button
              variant="contained"
              color="primary"
              startIcon={<PlayArrowIcon />}
              onClick={startMonitoring}
              disabled={loading}
            >
              Start Monitoring
            </Button>
          )}
        </Box>
      </Box>

      {/* Server info */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <StorageIcon color="primary" />
            <Typography variant="h6">{serverName}</Typography>
            {serverHealth?.isConnected ? 
              <Chip size="small" label="Connected" color="success" /> : 
              <Chip size="small" label="Disconnected" color="error" />
            }
          </Box>
          
          <Box>
            <Chip 
              icon={<SyncIcon />} 
              label={isMonitoring ? "Monitoring Active" : "Monitoring Inactive"} 
              color={isMonitoring ? "success" : "default"}
            />
          </Box>
        </Box>
        
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
            {error}
          </Alert>
        )}
        
        {loading && <LinearProgress sx={{ mb: 2 }} />}
      </Paper>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={tabIndex} onChange={handleTabChange} aria-label="monitoring tabs">
          <Tab label="Overview" id="monitoring-tab-0" aria-controls="monitoring-tabpanel-0" />
          <Tab label="Performance" id="monitoring-tab-1" aria-controls="monitoring-tabpanel-1" />
          <Tab label="Databases" id="monitoring-tab-2" aria-controls="monitoring-tabpanel-2" />
          <Tab label="Queries" id="monitoring-tab-3" aria-controls="monitoring-tabpanel-3" />
          <Tab label="Issues" id="monitoring-tab-4" aria-controls="monitoring-tabpanel-4" />
          <Tab label="Backups" id="monitoring-tab-5" aria-controls="monitoring-tabpanel-5" />
        </Tabs>
      </Box>

      {/* Overview Tab */}
      <TabPanel value={tabIndex} index={0}>
        <Grid container spacing={3}>
          {/* Server Summary */}
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Typography variant="h6" gutterBottom>
                Server Summary
              </Typography>
              <Divider sx={{ mb: 2 }} />
              
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">CPU Usage</Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <LinearProgress 
                      variant="determinate" 
                      value={serverHealth?.cpu || 0} 
                      sx={{ flexGrow: 1, height: 10, borderRadius: 5 }}
                      color={serverHealth?.cpu > 80 ? "error" : serverHealth?.cpu > 60 ? "warning" : "primary"}
                    />
                    <Typography>{serverHealth?.cpu || 0}%</Typography>
                  </Box>
                </Box>
                
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Memory Usage</Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <LinearProgress 
                      variant="determinate" 
                      value={serverHealth?.memory || 0} 
                      sx={{ flexGrow: 1, height: 10, borderRadius: 5 }}
                      color={serverHealth?.memory > 80 ? "error" : serverHealth?.memory > 60 ? "warning" : "primary"}
                    />
                    <Typography>{serverHealth?.memory || 0}%</Typography>
                  </Box>
                </Box>
                
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Disk I/O (MB/s)</Typography>
                  <Typography variant="h6">{serverHealth?.disk || 0}</Typography>
                </Box>
                
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Network (MB/s)</Typography>
                  <Typography variant="h6">{serverHealth?.network || 0}</Typography>
                </Box>
                
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Active Connections</Typography>
                  <Typography variant="h6">{dashboardData?.performance?.activeConnections || 0}</Typography>
                </Box>
              </Box>
            </Paper>
          </Grid>
          
          {/* Alerts */}
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Typography variant="h6" gutterBottom>
                Recent Alerts
              </Typography>
              <Divider sx={{ mb: 2 }} />
              
              {alertList && alertList.length > 0 ? (
                <List>
                  {alertList.slice(0, 5).map((alert, index) => (
                    <ListItem key={index} divider={index < alertList.length - 1}>
                      <ListItemIcon>
                        {alert.severity === 'critical' ? (
                          <ErrorIcon color="error" />
                        ) : alert.severity === 'warning' ? (
                          <WarningIcon color="warning" />
                        ) : (
                          <InfoIcon color="info" />
                        )}
                      </ListItemIcon>
                      <ListItemText 
                        primary={alert.message} 
                        secondary={formatDate(alert.timestamp)}
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="textSecondary" align="center">
                  No recent alerts
                </Typography>
              )}
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button size="small" component={Link} to="/alerts">
                  View All Alerts
                </Button>
              </Box>
            </Paper>
          </Grid>
          
          {/* Database Summary */}
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Typography variant="h6" gutterBottom>
                Database Summary
              </Typography>
              <Divider sx={{ mb: 2 }} />
              
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" color="text.secondary">Total Databases</Typography>
                <Typography variant="h4">{databaseList.length || 0}</Typography>
              </Box>
              
              {dashboardData?.databases && (
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'space-between', mb: 2 }}>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Healthy</Typography>
                    <Typography variant="h5" color="success.main">
                      {dashboardData.databases.healthy || 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Warning</Typography>
                    <Typography variant="h5" color="warning.main">
                      {dashboardData.databases.warning || 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Critical</Typography>
                    <Typography variant="h5" color="error.main">
                      {dashboardData.databases.critical || 0}
                    </Typography>
                  </Box>
                </Box>
              )}
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button size="small" component={Link} to="/databases">
                  View All Databases
                </Button>
              </Box>
            </Paper>
          </Grid>
          
          {/* CPU Chart */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">CPU Usage History</Typography>
                <SpeedIcon color="primary" />
              </Box>
              <Divider sx={{ mb: 2 }} />
              
              <Box sx={{ height: 250 }}>
                <Line data={cpuChartData} options={chartOptions} />
              </Box>
            </Paper>
          </Grid>
          
          {/* Memory Chart */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">Memory Usage History</Typography>
                <MemoryIcon color="primary" />
              </Box>
              <Divider sx={{ mb: 2 }} />
              
              <Box sx={{ height: 250 }}>
                <Line data={memoryChartData} options={chartOptions} />
              </Box>
            </Paper>
          </Grid>
          
          {/* Issues Summary */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Typography variant="h6" gutterBottom>
                Top Issues
              </Typography>
              <Divider sx={{ mb: 2 }} />
              
              {issueList && issueList.length > 0 ? (
                <List>
                  {issueList.slice(0, 5).map((issue, index) => (
                    <ListItem key={index} divider={index < 4}>
                      <ListItemText 
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            {issue.title}
                            {getSeverityChip(issue.severity)}
                          </Box>
                        } 
                        secondary={issue.description} 
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="textSecondary" align="center">
                  No issues found
                </Typography>
              )}
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button size="small" component={Link} to="/issues">
                  View All Issues
                </Button>
              </Box>
            </Paper>
          </Grid>
          
          {/* Top Queries */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 2, height: '100%' }}>
              <Typography variant="h6" gutterBottom>
                Top Resource-Consuming Queries
              </Typography>
              <Divider sx={{ mb: 2 }} />
              
              {topQueries && topQueries.length > 0 ? (
                <List>
                  {topQueries.slice(0, 5).map((query, index) => (
                    <ListItem key={index} divider={index < 4}>
                      <ListItemText 
                        primary={
                          <Box component="div" sx={{ 
                            whiteSpace: 'nowrap',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            maxWidth: '100%'
                          }}>
                            {query.query}
                          </Box>
                        } 
                        secondary={`Duration: ${query.duration}ms | CPU: ${query.cpu}ms | Reads: ${query.reads}`} 
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="textSecondary" align="center">
                  No query data available
                </Typography>
              )}
              
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button size="small" component={Link} to="/queries">
                  View All Queries
                </Button>
              </Box>
            </Paper>
          </Grid>
        </Grid>
      </TabPanel>
      
      {/* Additional tabs would be implemented here */}
      <TabPanel value={tabIndex} index={1}>
        <Typography variant="h6">Performance Monitoring</Typography>
        <Typography paragraph>
          Detailed performance metrics for your SQL Server instance.
        </Typography>
      </TabPanel>
      
      <TabPanel value={tabIndex} index={2}>
        <Typography variant="h6">Database Management</Typography>
        <Typography paragraph>
          Manage and monitor all databases on this server.
        </Typography>
      </TabPanel>
      
      <TabPanel value={tabIndex} index={3}>
        <Typography variant="h6">Query Analysis</Typography>
        <Typography paragraph>
          Analyze and optimize SQL queries running on your server.
        </Typography>
      </TabPanel>
      
      <TabPanel value={tabIndex} index={4}>
        <Typography variant="h6">Issues & Recommendations</Typography>
        <Typography paragraph>
          View and resolve detected issues in your SQL Server environment.
        </Typography>
      </TabPanel>
      
      <TabPanel value={tabIndex} index={5}>
        <Typography variant="h6">Backup Management</Typography>
        <Typography paragraph>
          View backup history and manage backup operations.
        </Typography>
      </TabPanel>
    </Box>
  );
}

export default MonitoringDashboard; 