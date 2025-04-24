import React, { useState, useEffect, useRef } from 'react';
import {
  Box,
  Grid,
  Typography,
  Card,
  CardContent,
  CardHeader,
  Button,
  Switch,
  FormControlLabel,
  Divider,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  Paper,
  TextField,
  InputAdornment,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Refresh as RefreshIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { monitoring } from '../services/api';
import { Line, Bar } from 'react-chartjs-2';
import { format } from 'date-fns';

function Monitoring() {
  const { connectionString, hubConnection } = useConnection();
  const [isMonitoring, setIsMonitoring] = useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [settings, setSettings] = useState({
    intervalSeconds: 30,
    retentionHours: 24,
    cpuAlertThreshold: 80,
    memoryAlertThreshold: 85,
    diskAlertThreshold: 90,
    enableEmailAlerts: false,
    enableSlackAlerts: false,
  });
  const [isEditingSettings, setIsEditingSettings] = useState(false);
  const [tempSettings, setTempSettings] = useState({...settings});
  
  // Performance metrics
  const [cpuData, setCpuData] = useState({
    labels: Array.from({ length: 20 }, (_, i) => ''),
    datasets: [{
      label: 'CPU Usage %',
      data: Array(20).fill(0),
      borderColor: 'rgb(75, 192, 192)',
      backgroundColor: 'rgba(75, 192, 192, 0.5)',
      tension: 0.4,
    }]
  });
  
  const [memoryData, setMemoryData] = useState({
    labels: Array.from({ length: 20 }, (_, i) => ''),
    datasets: [{
      label: 'Memory Usage %',
      data: Array(20).fill(0),
      borderColor: 'rgb(255, 99, 132)',
      backgroundColor: 'rgba(255, 99, 132, 0.5)',
      tension: 0.4,
    }]
  });
  
  const [diskIoData, setDiskIoData] = useState({
    labels: Array.from({ length: 20 }, (_, i) => ''),
    datasets: [
      {
        label: 'Read (MB/s)',
        data: Array(20).fill(0),
        borderColor: 'rgb(54, 162, 235)',
        backgroundColor: 'rgba(54, 162, 235, 0.5)',
      },
      {
        label: 'Write (MB/s)',
        data: Array(20).fill(0),
        borderColor: 'rgb(255, 159, 64)',
        backgroundColor: 'rgba(255, 159, 64, 0.5)',
      }
    ]
  });
  
  const [waitStats, setWaitStats] = useState({
    labels: [],
    datasets: [{
      label: 'Wait Time (ms)',
      data: [],
      backgroundColor: 'rgba(153, 102, 255, 0.5)',
      borderColor: 'rgb(153, 102, 255)',
      borderWidth: 1,
    }]
  });
  
  const lineChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: {
      duration: 500,
    },
    scales: {
      y: {
        beginAtZero: true,
        max: 100,
      },
      x: {
        ticks: {
          maxRotation: 0,
          autoSkip: true,
          maxTicksLimit: 6,
        }
      }
    },
    plugins: {
      legend: {
        position: 'top',
      },
    },
  };

  const diskIoOptions = {
    ...lineChartOptions,
    scales: {
      ...lineChartOptions.scales,
      y: {
        beginAtZero: true,
      }
    }
  };
  
  const waitStatsOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top',
      },
    },
  };

  useEffect(() => {
    if (!connectionString) return;
    
    const fetchMonitoringStatus = async () => {
      setLoading(true);
      try {
        // In a real app, fetch monitoring settings from API
        // const response = await monitoring.getSettings();
        // setSettings(response.data);
        // setIsMonitoring(response.data.monitoringEnabled);
        
        // For demo, simulate
        setIsMonitoring(false);
        setTimeout(() => setLoading(false), 1000);
      } catch (err) {
        console.error('Error fetching monitoring status:', err);
        setError('Failed to fetch monitoring status.');
        setLoading(false);
      }
    };
    
    fetchMonitoringStatus();
    
    // Set up SignalR for real-time updates
    if (hubConnection) {
      hubConnection.on('MetricsUpdated', handleMetricsUpdate);
    }
    
    return () => {
      if (hubConnection) {
        hubConnection.off('MetricsUpdated');
      }
    };
  }, [connectionString, hubConnection]);
  
  const handleMetricsUpdate = (metrics) => {
    const timestamp = format(new Date(metrics.collectionTime), 'HH:mm:ss');
    
    // Update CPU chart
    setCpuData(prev => ({
      labels: [...prev.labels.slice(1), timestamp],
      datasets: [{
        ...prev.datasets[0],
        data: [...prev.datasets[0].data.slice(1), metrics.cpu.usage]
      }]
    }));
    
    // Update Memory chart
    setMemoryData(prev => ({
      labels: [...prev.labels.slice(1), timestamp],
      datasets: [{
        ...prev.datasets[0],
        data: [...prev.datasets[0].data.slice(1), metrics.memory.usagePercent]
      }]
    }));
    
    // Update Disk IO chart
    setDiskIoData(prev => ({
      labels: [...prev.labels.slice(1), timestamp],
      datasets: [
        {
          ...prev.datasets[0],
          data: [...prev.datasets[0].data.slice(1), metrics.disk.readMBps]
        },
        {
          ...prev.datasets[1],
          data: [...prev.datasets[1].data.slice(1), metrics.disk.writeMBps]
        }
      ]
    }));
    
    // Update Wait Stats
    if (metrics.topWaits && metrics.topWaits.length > 0) {
      setWaitStats({
        labels: metrics.topWaits.map(w => w.waitType),
        datasets: [{
          ...waitStats.datasets[0],
          data: metrics.topWaits.map(w => w.waitTimeMs)
        }]
      });
    }
  };

  const handleStartMonitoring = async () => {
    try {
      setLoading(true);
      // In real app, call API to start monitoring
      // await monitoring.startMonitoring(connectionString);
      
      // For demo
      setTimeout(() => {
        setIsMonitoring(true);
        setLoading(false);
        
        // Simulate metrics updates
        const interval = setInterval(() => {
          const cpuUsage = Math.floor(35 + Math.random() * 30);
          const memoryUsage = Math.floor(55 + Math.random() * 25);
          const readMBps = Math.floor(10 + Math.random() * 40);
          const writeMBps = Math.floor(5 + Math.random() * 30);
          
          const mockMetrics = {
            collectionTime: new Date().toISOString(),
            cpu: { 
              usage: cpuUsage,
              coreCount: 8
            },
            memory: {
              usagePercent: memoryUsage,
              totalGb: 32,
              availableGb: 32 * (1 - memoryUsage/100),
              sqlServerGb: 32 * (memoryUsage/100) * 0.8
            },
            disk: {
              iops: readMBps * 20 + writeMBps * 15,
              readMBps,
              writeMBps
            },
            topWaits: [
              { waitType: 'ASYNC_NETWORK_IO', waitTimeMs: Math.floor(800 + Math.random() * 500), waitCategory: 'Network' },
              { waitType: 'PAGEIOLATCH_SH', waitTimeMs: Math.floor(600 + Math.random() * 400), waitCategory: 'IO' },
              { waitType: 'PAGELATCH_EX', waitTimeMs: Math.floor(300 + Math.random() * 300), waitCategory: 'Latch' },
              { waitType: 'CXPACKET', waitTimeMs: Math.floor(200 + Math.random() * 200), waitCategory: 'CPU' },
              { waitType: 'LCK_M_X', waitTimeMs: Math.floor(100 + Math.random() * 150), waitCategory: 'Lock' }
            ]
          };
          
          handleMetricsUpdate(mockMetrics);
        }, 2000);
        
        return () => clearInterval(interval);
      }, 1500);
      
    } catch (err) {
      console.error('Error starting monitoring:', err);
      setError('Failed to start monitoring.');
      setLoading(false);
    }
  };
  
  const handleStopMonitoring = async () => {
    try {
      setLoading(true);
      // In real app, call API to stop monitoring
      // await monitoring.stopMonitoring();
      
      // For demo
      setTimeout(() => {
        setIsMonitoring(false);
        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error stopping monitoring:', err);
      setError('Failed to stop monitoring.');
      setLoading(false);
    }
  };
  
  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };
  
  const handleEditSettings = () => {
    setTempSettings({...settings});
    setIsEditingSettings(true);
  };
  
  const handleSaveSettings = async () => {
    try {
      setLoading(true);
      // In real app, call API to save settings
      // await monitoring.updateSettings(tempSettings);
      
      // For demo
      setTimeout(() => {
        setSettings({...tempSettings});
        setIsEditingSettings(false);
        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error saving settings:', err);
      setError('Failed to save monitoring settings.');
      setLoading(false);
    }
  };
  
  const handleCancelEdit = () => {
    setIsEditingSettings(false);
  };
  
  const handleSettingChange = (e) => {
    const { name, value, type, checked } = e.target;
    setTempSettings(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : type === 'number' ? parseInt(value, 10) : value
    }));
  };

  return (
    <Box className="dashboard-container">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3, alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          SQL Server Monitoring
        </Typography>
        <Box>
          {isMonitoring ? (
            <Button 
              variant="contained" 
              color="error"
              startIcon={<StopIcon />}
              onClick={handleStopMonitoring}
              disabled={loading}
              sx={{ mr: 2 }}
            >
              Stop Monitoring
            </Button>
          ) : (
            <Button 
              variant="contained" 
              color="success"
              startIcon={<StartIcon />}
              onClick={handleStartMonitoring}
              disabled={loading}
              sx={{ mr: 2 }}
            >
              Start Monitoring
            </Button>
          )}
          <Button 
            variant="outlined"
            startIcon={<SettingsIcon />}
            onClick={handleEditSettings}
            disabled={loading || isEditingSettings}
          >
            Settings
          </Button>
        </Box>
      </Box>
      
      {loading && !isMonitoring && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      )}
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      {isEditingSettings ? (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography variant="h6" sx={{ mb: 2 }}>Monitoring Settings</Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Update Interval (seconds)"
                name="intervalSeconds"
                type="number"
                value={tempSettings.intervalSeconds}
                onChange={handleSettingChange}
                InputProps={{ inputProps: { min: 5, max: 3600 } }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Data Retention (hours)"
                name="retentionHours"
                type="number"
                value={tempSettings.retentionHours}
                onChange={handleSettingChange}
                InputProps={{ inputProps: { min: 1, max: 720 } }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="CPU Alert Threshold (%)"
                name="cpuAlertThreshold"
                type="number"
                value={tempSettings.cpuAlertThreshold}
                onChange={handleSettingChange}
                InputProps={{ 
                  inputProps: { min: 1, max: 100 },
                  endAdornment: <InputAdornment position="end">%</InputAdornment>
                }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Memory Alert Threshold (%)"
                name="memoryAlertThreshold"
                type="number"
                value={tempSettings.memoryAlertThreshold}
                onChange={handleSettingChange}
                InputProps={{ 
                  inputProps: { min: 1, max: 100 },
                  endAdornment: <InputAdornment position="end">%</InputAdornment>
                }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Disk Alert Threshold (%)"
                name="diskAlertThreshold"
                type="number"
                value={tempSettings.diskAlertThreshold}
                onChange={handleSettingChange}
                InputProps={{ 
                  inputProps: { min: 1, max: 100 },
                  endAdornment: <InputAdornment position="end">%</InputAdornment>
                }}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={tempSettings.enableEmailAlerts}
                    onChange={handleSettingChange}
                    name="enableEmailAlerts"
                  />
                }
                label="Enable Email Alerts"
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={
                  <Switch
                    checked={tempSettings.enableSlackAlerts}
                    onChange={handleSettingChange}
                    name="enableSlackAlerts"
                  />
                }
                label="Enable Slack Alerts"
              />
            </Grid>
            <Grid item xs={12}>
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
                <Button variant="outlined" onClick={handleCancelEdit} sx={{ mr: 2 }}>
                  Cancel
                </Button>
                <Button variant="contained" onClick={handleSaveSettings}>
                  Save Settings
                </Button>
              </Box>
            </Grid>
          </Grid>
        </Paper>
      ) : (
        <>
          {isMonitoring && (
            <>
              <Alert 
                severity="info" 
                sx={{ mb: 3 }}
                action={
                  <Button color="inherit" size="small">
                    View Details
                  </Button>
                }
              >
                Monitoring is active. Data is being collected every {settings.intervalSeconds} seconds.
              </Alert>
              
              <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
                <Tabs value={activeTab} onChange={handleTabChange} aria-label="monitoring tabs">
                  <Tab label="Performance" />
                  <Tab label="Disk I/O" />
                  <Tab label="Wait Statistics" />
                </Tabs>
              </Box>
              
              {activeTab === 0 && (
                <Grid container spacing={3}>
                  <Grid item xs={12} lg={6}>
                    <Card>
                      <CardHeader title="CPU Usage %" />
                      <Divider />
                      <CardContent sx={{ height: 350 }}>
                        <Line data={cpuData} options={lineChartOptions} />
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} lg={6}>
                    <Card>
                      <CardHeader title="Memory Usage %" />
                      <Divider />
                      <CardContent sx={{ height: 350 }}>
                        <Line data={memoryData} options={lineChartOptions} />
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              )}
              
              {activeTab === 1 && (
                <Card>
                  <CardHeader title="Disk I/O (MB/s)" />
                  <Divider />
                  <CardContent sx={{ height: 400 }}>
                    <Line data={diskIoData} options={diskIoOptions} />
                  </CardContent>
                </Card>
              )}
              
              {activeTab === 2 && (
                <Card>
                  <CardHeader title="Top Wait Types (ms)" />
                  <Divider />
                  <CardContent sx={{ height: 400 }}>
                    <Bar data={waitStats} options={waitStatsOptions} />
                  </CardContent>
                </Card>
              )}
            </>
          )}
          
          {!isMonitoring && !loading && (
            <Paper sx={{ p: 4, textAlign: 'center', mt: 4, bgcolor: 'background.default' }}>
              <Typography variant="h6" gutterBottom>
                Monitoring is currently inactive
              </Typography>
              <Typography variant="body1" color="text.secondary" paragraph>
                Start monitoring to collect real-time performance metrics for your SQL Server instance.
              </Typography>
              <Button 
                variant="contained" 
                color="primary"
                startIcon={<StartIcon />}
                onClick={handleStartMonitoring}
                sx={{ mt: 2 }}
              >
                Start Monitoring
              </Button>
            </Paper>
          )}
        </>
      )}
    </Box>
  );
}

export default Monitoring; 