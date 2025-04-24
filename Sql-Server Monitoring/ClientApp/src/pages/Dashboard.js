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
  List,
  ListItem,
  ListItemText,
  Chip,
  LinearProgress,
  Button,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  ListItemIcon,
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
  ArrowForward as ArrowForwardIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { monitoring, databases, health, queries } from '../services/api';
import { Line, Doughnut } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  BarElement,
} from 'chart.js';

// Register ChartJS components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  BarElement
);

function Dashboard() {
  const { connectionString, serverName, hubConnection } = useConnection();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dashboardData, setDashboardData] = useState(null);
  const [databases, setDatabases] = useState([]);
  const [issues, setIssues] = useState([]);
  const [slowQueries, setSlowQueries] = useState([]);
  const [cpuHistory, setCpuHistory] = useState({
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
  const [memoryHistory, setMemoryHistory] = useState({
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

  useEffect(() => {
    if (!connectionString) return;

    const fetchDashboardData = async () => {
      setLoading(true);
      try {
        // Mock data for demo purposes
        // In real app, use API calls to get real data
        // Example: const metricsResponse = await monitoring.getMetrics(connectionString);

        // Mock performance metrics
        setDashboardData({
          servers: {
            total: 4,
            online: 3,
            offline: 1,
            critical: 1,
            warning: 1,
            healthy: 2,
          },
          databases: {
            total: 12,
            healthy: 8,
            warning: 3,
            critical: 1,
          },
          performance: {
            cpuUsage: 45,
            memoryUsage: 62,
            diskIoRate: 125,
            networkRate: 42,
            activeConnections: 28,
            cpuHistory: [35, 42, 38, 41, 44, 45, 43, 40, 38, 45],
            memoryHistory: [58, 60, 65, 62, 59, 57, 61, 63, 62, 62],
          },
          alerts: {
            critical: 2,
            warning: 3,
            info: 4,
            total: 9,
            recent: [
              {
                id: 1,
                severity: 'critical',
                message: 'CPU usage exceeded 90% threshold on SQL-PROD-01',
                timestamp: '2023-06-15T14:30:00Z',
              },
              {
                id: 2,
                severity: 'warning',
                message: 'Memory usage approaching threshold on SQL-PROD-02',
                timestamp: '2023-06-15T13:45:00Z',
              },
              {
                id: 3,
                severity: 'info',
                message: 'Database backup completed successfully',
                timestamp: '2023-06-15T12:00:00Z',
              },
            ],
          },
          topQueries: [
            {
              id: 1,
              query: 'SELECT * FROM Orders WHERE OrderDate > @date',
              duration: 1250,
              cpu: 850,
              reads: 3560,
              executionCount: 245,
            },
            {
              id: 2,
              query: 'UPDATE Products SET UnitPrice = UnitPrice * 1.1 WHERE CategoryID = 5',
              duration: 950,
              cpu: 780,
              reads: 2150,
              executionCount: 45,
            },
            {
              id: 3,
              query: 'SELECT p.*, c.CategoryName FROM Products p JOIN Categories c ON p.CategoryID = c.CategoryID',
              duration: 850,
              cpu: 650,
              reads: 1860,
              executionCount: 325,
            },
          ],
        });

        // Mock databases
        setDatabases([
          { id: 1, name: 'AdventureWorks', size: 245.6, status: 'Online', compatibilityLevel: 150 },
          { id: 2, name: 'Northwind', size: 82.3, status: 'Online', compatibilityLevel: 140 },
          { id: 3, name: 'WideWorldImporters', size: 512.1, status: 'Online', compatibilityLevel: 150 }
        ]);

        // Mock issues
        setIssues([
          { id: 1, title: 'High CPU Usage', description: 'Server experiencing sustained CPU usage over 80%', severity: 'Warning' },
          { id: 2, title: 'Missing Index', description: 'Missing index on Orders.CustomerID column', severity: 'Information' },
          { id: 3, title: 'Fragmented Index', description: 'Fragmentation > 90% on Products_PK', severity: 'Warning' }
        ]);

        // Mock slow queries
        setSlowQueries([
          { id: 1, text: 'SELECT * FROM Products p JOIN Categories c ON p.CategoryID = c.CategoryID', durationMs: 2500 },
          { id: 2, text: 'SELECT * FROM OrderDetails WHERE Quantity > 50', durationMs: 1800 }
        ]);

        // Mock CPU history
        setCpuHistory(prev => ({
          ...prev,
          datasets: [{
            ...prev.datasets[0],
            data: [35, 42, 45, 48, 50, 47, 45, 42, 40, 45]
          }]
        }));

        // Mock memory history
        setMemoryHistory(prev => ({
          ...prev,
          datasets: [{
            ...prev.datasets[0],
            data: [55, 58, 60, 62, 65, 64, 62, 60, 58, 62]
          }]
        }));
      } catch (err) {
        console.error('Error fetching dashboard data:', err);
        setError('Failed to load dashboard data. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();

    // Set up SignalR for real-time updates
    if (hubConnection) {
      hubConnection.on('UpdatedMetrics', (newMetrics) => {
        setDashboardData(prev => ({
          ...prev,
          performance: {
            ...prev.performance,
            cpuUsage: newMetrics.cpu.usage,
            memoryUsage: newMetrics.memory.usagePercent,
            cpuHistory: [...prev.performance.cpuHistory.slice(1), newMetrics.cpu.usage],
            memoryHistory: [...prev.performance.memoryHistory.slice(1), newMetrics.memory.usagePercent]
          }
        }));
      });
      
      hubConnection.on('NewIssueDetected', (newIssue) => {
        setIssues(prev => [newIssue, ...prev].slice(0, 10));
      });
    }

    return () => {
      if (hubConnection) {
        hubConnection.off('UpdatedMetrics');
        hubConnection.off('NewIssueDetected');
      }
    };
  }, [connectionString, hubConnection]);

  const refreshData = () => {
    // Re-fetch data
    // In a real app, call the API again
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleString([], { 
      hour: '2-digit', 
      minute: '2-digit',
      month: 'short',
      day: 'numeric',
    });
  };

  const getSeverityChip = (severity) => {
    switch (severity) {
      case 'critical':
        return <Chip label="Critical" color="error" size="small" icon={<ErrorIcon />} />;
      case 'warning':
        return <Chip label="Warning" color="warning" size="small" icon={<WarningIcon />} />;
      case 'info':
        return <Chip label="Info" color="info" size="small" />;
      default:
        return null;
    }
  };

  // Chart data
  const serverStatusData = {
    labels: ['Online', 'Offline'],
    datasets: [
      {
        label: 'Server Status',
        data: dashboardData ? [dashboardData.servers.online, dashboardData.servers.offline] : [0, 0],
        backgroundColor: ['rgba(75, 192, 192, 0.5)', 'rgba(255, 99, 132, 0.5)'],
        borderColor: ['rgb(75, 192, 192)', 'rgb(255, 99, 132)'],
        borderWidth: 1,
      },
    ],
  };
  
  const serverHealthData = {
    labels: ['Healthy', 'Warning', 'Critical'],
    datasets: [
      {
        label: 'Server Health',
        data: dashboardData ? 
          [dashboardData.servers.healthy, dashboardData.servers.warning, dashboardData.servers.critical] : 
          [0, 0, 0],
        backgroundColor: [
          'rgba(75, 192, 192, 0.5)',
          'rgba(255, 205, 86, 0.5)',
          'rgba(255, 99, 132, 0.5)',
        ],
        borderColor: [
          'rgb(75, 192, 192)',
          'rgb(255, 205, 86)',
          'rgb(255, 99, 132)',
        ],
        borderWidth: 1,
      },
    ],
  };
  
  const databaseHealthData = {
    labels: ['Healthy', 'Warning', 'Critical'],
    datasets: [
      {
        label: 'Database Health',
        data: dashboardData ? 
          [dashboardData.databases.healthy, dashboardData.databases.warning, dashboardData.databases.critical] : 
          [0, 0, 0],
        backgroundColor: [
          'rgba(75, 192, 192, 0.5)',
          'rgba(255, 205, 86, 0.5)',
          'rgba(255, 99, 132, 0.5)',
        ],
        borderColor: [
          'rgb(75, 192, 192)',
          'rgb(255, 205, 86)',
          'rgb(255, 99, 132)',
        ],
        borderWidth: 1,
      },
    ],
  };
  
  const performanceHistoryData = {
    labels: ['1h ago', '50m ago', '40m ago', '30m ago', '20m ago', '10m ago', 'Now'],
    datasets: [
      {
        label: 'CPU Usage %',
        data: dashboardData ? dashboardData.performance.cpuHistory : [],
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.5)',
        tension: 0.4,
      },
      {
        label: 'Memory Usage %',
        data: dashboardData ? dashboardData.performance.memoryHistory : [],
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.5)',
        tension: 0.4,
      },
    ],
  };

  if (loading && !dashboardData) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error" variant="h6">{error}</Typography>
        <Button 
          variant="contained" 
          onClick={refreshData}
          startIcon={<RefreshIcon />}
          sx={{ mt: 2 }}
        >
          Retry
        </Button>
      </Box>
    );
  }

  return (
    <Box className="dashboard-container">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3, alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          SQL Server Dashboard
        </Typography>
        <Button 
          variant="outlined" 
          startIcon={<RefreshIcon />}
          onClick={refreshData}
        >
          Refresh
        </Button>
      </Box>

      {/* Status Summary Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <StorageIcon sx={{ fontSize: 40, color: 'primary.main', mb: 1 }} />
              <Typography variant="h5">{dashboardData.servers.total}</Typography>
              <Typography color="textSecondary">Servers</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <SpeedIcon sx={{ fontSize: 40, color: 'success.main', mb: 1 }} />
              <Typography variant="h5">{dashboardData.performance.cpuUsage}%</Typography>
              <Typography color="textSecondary">CPU Usage</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <MemoryIcon sx={{ fontSize: 40, color: 'info.main', mb: 1 }} />
              <Typography variant="h5">{dashboardData.performance.memoryUsage}%</Typography>
              <Typography color="textSecondary">Memory Usage</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <NotificationsIcon sx={{ fontSize: 40, color: 'error.main', mb: 1 }} />
              <Typography variant="h5">{dashboardData.alerts.total}</Typography>
              <Typography color="textSecondary">Active Alerts</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      
      {/* Server and Database Status */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title="Server Status" />
            <Divider />
            <CardContent sx={{ height: 220, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
              <Box sx={{ width: '100%', height: '100%' }}>
                <Doughnut data={serverStatusData} options={chartOptions} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title="Server Health" />
            <Divider />
            <CardContent sx={{ height: 220, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
              <Box sx={{ width: '100%', height: '100%' }}>
                <Doughnut data={serverHealthData} options={chartOptions} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardHeader title="Database Health" />
            <Divider />
            <CardContent sx={{ height: 220, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
              <Box sx={{ width: '100%', height: '100%' }}>
                <Doughnut data={databaseHealthData} options={chartOptions} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      
      {/* Recent Alerts and Top Queries */}
      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader 
              title="Recent Alerts" 
              action={
                <Button 
                  component={Link} 
                  to="/alerts" 
                  size="small" 
                  endIcon={<ArrowForwardIcon />}
                >
                  View All
                </Button>
              }
            />
            <Divider />
            <CardContent sx={{ p: 0 }}>
              <List>
                {dashboardData.alerts.recent.map((alert) => (
                  <ListItem key={alert.id} divider>
                    <ListItemIcon>
                      {alert.severity === 'critical' && <ErrorIcon color="error" />}
                      {alert.severity === 'warning' && <WarningIcon color="warning" />}
                      {alert.severity === 'info' && <CheckCircleIcon color="info" />}
                    </ListItemIcon>
                    <ListItemText
                      primary={alert.message}
                      secondary={formatDate(alert.timestamp)}
                    />
                  </ListItem>
                ))}
                {dashboardData.alerts.recent.length === 0 && (
                  <ListItem>
                    <ListItemText primary="No recent alerts" />
                  </ListItem>
                )}
              </List>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader 
              title="Performance History" 
              action={
                <Button 
                  component={Link} 
                  to="/performance" 
                  size="small"
                  endIcon={<ArrowForwardIcon />}
                >
                  Details
                </Button>
              }
            />
            <Divider />
            <CardContent sx={{ height: 250 }}>
              <Line data={performanceHistoryData} options={chartOptions} />
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          <Card>
            <CardHeader 
              title="Top Resource-Intensive Queries" 
              action={
                <Button 
                  component={Link} 
                  to="/performance" 
                  size="small"
                  endIcon={<ArrowForwardIcon />}
                >
                  Analyze
                </Button>
              }
            />
            <Divider />
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Query</TableCell>
                    <TableCell align="right">Duration (ms)</TableCell>
                    <TableCell align="right">CPU Time (ms)</TableCell>
                    <TableCell align="right">Logical Reads</TableCell>
                    <TableCell align="right">Executions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {dashboardData.topQueries.map((query) => (
                    <TableRow key={query.id}>
                      <TableCell>
                        <Typography variant="body2" noWrap sx={{ maxWidth: 500 }}>
                          {query.query}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">{query.duration}</TableCell>
                      <TableCell align="right">{query.cpu}</TableCell>
                      <TableCell align="right">{query.reads}</TableCell>
                      <TableCell align="right">{query.executionCount}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}

export default Dashboard; 