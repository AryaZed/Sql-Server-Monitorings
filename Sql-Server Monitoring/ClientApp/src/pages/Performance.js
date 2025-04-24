import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Speed as SpeedIcon,
  Memory as MemoryIcon,
  Storage as StorageIcon,
  QueryStats as QueryStatsIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { Line, Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend
);

function Performance() {
  const { connectionString } = useConnection();
  const [servers, setServers] = useState([]);
  const [selectedServer, setSelectedServer] = useState('');
  const [timeRange, setTimeRange] = useState('1h');
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Performance data states
  const [cpuData, setCpuData] = useState({
    labels: [],
    datasets: [
      {
        label: 'CPU Usage %',
        data: [],
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.5)',
        tension: 0.4,
      },
    ],
  });

  const [memoryData, setMemoryData] = useState({
    labels: [],
    datasets: [
      {
        label: 'Memory Usage %',
        data: [],
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.5)',
        tension: 0.4,
      },
    ],
  });

  const [diskIoData, setDiskIoData] = useState({
    labels: [],
    datasets: [
      {
        label: 'Read MB/s',
        data: [],
        borderColor: 'rgb(54, 162, 235)',
        backgroundColor: 'rgba(54, 162, 235, 0.5)',
      },
      {
        label: 'Write MB/s',
        data: [],
        borderColor: 'rgb(255, 159, 64)',
        backgroundColor: 'rgba(255, 159, 64, 0.5)',
      },
    ],
  });

  const [waitStatsData, setWaitStatsData] = useState({
    labels: [],
    datasets: [
      {
        label: 'Wait Time (ms)',
        data: [],
        backgroundColor: 'rgba(153, 102, 255, 0.5)',
        borderColor: 'rgb(153, 102, 255)',
        borderWidth: 1,
      },
    ],
  });

  // Chart options
  const lineChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top',
      },
    },
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  useEffect(() => {
    // Fetch available servers
    const fetchServers = async () => {
      try {
        // In a real app, call API to get servers
        // const response = await api.getServers();
        // setServers(response.data);
        // if (response.data.length > 0) {
        //   setSelectedServer(response.data[0].id);
        // }

        // For demo, mock data
        const mockServers = [
          { id: 'server1', name: 'SQL-PROD-01' },
          { id: 'server2', name: 'SQL-PROD-02' },
          { id: 'server3', name: 'SQL-DEV-01' },
        ];
        setServers(mockServers);
        setSelectedServer('server1');
        setLoading(false);
      } catch (err) {
        console.error('Error fetching servers:', err);
        setError('Failed to fetch servers');
        setLoading(false);
      }
    };

    fetchServers();
  }, []);

  useEffect(() => {
    if (selectedServer) {
      fetchPerformanceData();
    }
  }, [selectedServer, timeRange]);

  const fetchPerformanceData = async () => {
    setLoading(true);
    try {
      // In a real app, call API to get performance data
      // const response = await api.getPerformanceData(selectedServer, timeRange);
      // Process and set data

      // For demo, generate mock data
      setTimeout(() => {
        // Generate time labels based on timeRange
        const now = new Date();
        const labels = [];
        const pointCount = timeRange === '1h' ? 60 : timeRange === '6h' ? 72 : timeRange === '24h' ? 96 : 60;
        const timeInterval = timeRange === '1h' ? 60 : timeRange === '6h' ? 300 : timeRange === '24h' ? 900 : 60;

        for (let i = pointCount - 1; i >= 0; i--) {
          const time = new Date(now.getTime() - i * timeInterval * 1000);
          labels.push(time.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
        }

        // CPU data
        const cpuValues = Array.from({ length: pointCount }, () => 
          Math.floor(20 + Math.random() * 60)
        );
        setCpuData({
          labels,
          datasets: [
            {
              ...cpuData.datasets[0],
              data: cpuValues,
            },
          ],
        });

        // Memory data
        const memoryValues = Array.from({ length: pointCount }, () => 
          Math.floor(50 + Math.random() * 30)
        );
        setMemoryData({
          labels,
          datasets: [
            {
              ...memoryData.datasets[0],
              data: memoryValues,
            },
          ],
        });

        // Disk IO data
        const readValues = Array.from({ length: pointCount }, () => 
          Math.floor(5 + Math.random() * 25)
        );
        const writeValues = Array.from({ length: pointCount }, () => 
          Math.floor(3 + Math.random() * 15)
        );
        setDiskIoData({
          labels,
          datasets: [
            {
              ...diskIoData.datasets[0],
              data: readValues,
            },
            {
              ...diskIoData.datasets[1],
              data: writeValues,
            },
          ],
        });

        // Wait stats data
        const waitTypes = [
          'ASYNC_NETWORK_IO',
          'PAGEIOLATCH_SH',
          'PAGELATCH_EX',
          'CXPACKET',
          'LCK_M_X',
          'WRITELOG',
          'SOS_SCHEDULER_YIELD',
        ];
        const waitValues = waitTypes.map(() => Math.floor(100 + Math.random() * 900));
        setWaitStatsData({
          labels: waitTypes,
          datasets: [
            {
              ...waitStatsData.datasets[0],
              data: waitValues,
            },
          ],
        });

        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error fetching performance data:', err);
      setError('Failed to fetch performance data');
      setLoading(false);
    }
  };

  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };

  const handleServerChange = (event) => {
    setSelectedServer(event.target.value);
  };

  const handleTimeRangeChange = (event) => {
    setTimeRange(event.target.value);
  };

  const handleRefresh = () => {
    fetchPerformanceData();
  };

  return (
    <Box className="dashboard-container">
      <Typography variant="h4" component="h1" gutterBottom>
        Performance Monitoring
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ mb: 3, display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <FormControl sx={{ minWidth: 200 }}>
          <InputLabel id="server-select-label">Server</InputLabel>
          <Select
            labelId="server-select-label"
            value={selectedServer}
            label="Server"
            onChange={handleServerChange}
            disabled={loading}
          >
            {servers.map((server) => (
              <MenuItem key={server.id} value={server.id}>
                {server.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl sx={{ minWidth: 150 }}>
          <InputLabel id="time-range-select-label">Time Range</InputLabel>
          <Select
            labelId="time-range-select-label"
            value={timeRange}
            label="Time Range"
            onChange={handleTimeRangeChange}
            disabled={loading}
          >
            <MenuItem value="1h">Last Hour</MenuItem>
            <MenuItem value="6h">Last 6 Hours</MenuItem>
            <MenuItem value="24h">Last 24 Hours</MenuItem>
            <MenuItem value="7d">Last 7 Days</MenuItem>
          </Select>
        </FormControl>

        <Button
          variant="outlined"
          startIcon={loading ? <CircularProgress size={20} /> : <RefreshIcon />}
          onClick={handleRefresh}
          disabled={loading}
        >
          Refresh
        </Button>
      </Box>

      <Tabs
        value={activeTab}
        onChange={handleTabChange}
        aria-label="performance tabs"
        sx={{ mb: 2 }}
      >
        <Tab icon={<SpeedIcon />} label="CPU" />
        <Tab icon={<MemoryIcon />} label="Memory" />
        <Tab icon={<StorageIcon />} label="Disk I/O" />
        <Tab icon={<QueryStatsIcon />} label="Wait Stats" />
      </Tabs>

      {activeTab === 0 && (
        <Card>
          <CardHeader title="CPU Usage" subheader="Percentage of CPU utilization over time" />
          <Divider />
          <CardContent sx={{ height: 400 }}>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                <CircularProgress />
              </Box>
            ) : (
              <Line data={cpuData} options={lineChartOptions} />
            )}
          </CardContent>
        </Card>
      )}

      {activeTab === 1 && (
        <Card>
          <CardHeader title="Memory Usage" subheader="Percentage of memory utilization over time" />
          <Divider />
          <CardContent sx={{ height: 400 }}>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                <CircularProgress />
              </Box>
            ) : (
              <Line data={memoryData} options={lineChartOptions} />
            )}
          </CardContent>
        </Card>
      )}

      {activeTab === 2 && (
        <Card>
          <CardHeader title="Disk I/O" subheader="Read and write operations over time (MB/s)" />
          <Divider />
          <CardContent sx={{ height: 400 }}>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                <CircularProgress />
              </Box>
            ) : (
              <Line data={diskIoData} options={lineChartOptions} />
            )}
          </CardContent>
        </Card>
      )}

      {activeTab === 3 && (
        <Card>
          <CardHeader title="Top Wait Stats" subheader="Most common wait types and their durations" />
          <Divider />
          <CardContent sx={{ height: 400 }}>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                <CircularProgress />
              </Box>
            ) : (
              <Bar 
                data={waitStatsData} 
                options={{
                  ...lineChartOptions,
                  indexAxis: 'y',
                }} 
              />
            )}
          </CardContent>
        </Card>
      )}
    </Box>
  );
}

export default Performance; 