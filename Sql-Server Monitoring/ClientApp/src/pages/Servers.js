import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Button,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Chip,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';

function Servers() {
  const { connectionString } = useConnection();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [servers, setServers] = useState([]);

  useEffect(() => {
    fetchServers();
  }, []);

  const fetchServers = async () => {
    setLoading(true);
    try {
      // In a real app, call API to fetch servers
      // const response = await api.getServers();
      // setServers(response.data);

      // For demo, mock data
      setTimeout(() => {
        setServers([
          {
            id: 1,
            name: 'SQL-PROD-01',
            ipAddress: '192.168.1.100',
            status: 'Online',
            version: 'SQL Server 2019 (15.0.4223.1)',
            edition: 'Enterprise',
            lastChecked: '2023-06-15T14:30:00Z',
            health: 'Healthy',
            cpuUsage: 45,
            memoryUsage: 62,
            diskUsage: 58,
          },
          {
            id: 2,
            name: 'SQL-PROD-02',
            ipAddress: '192.168.1.101',
            status: 'Online',
            version: 'SQL Server 2019 (15.0.4223.1)',
            edition: 'Enterprise',
            lastChecked: '2023-06-15T14:30:00Z',
            health: 'Warning',
            cpuUsage: 72,
            memoryUsage: 85,
            diskUsage: 65,
          },
          {
            id: 3,
            name: 'SQL-DEV-01',
            ipAddress: '192.168.1.102',
            status: 'Online',
            version: 'SQL Server 2019 (15.0.4223.1)',
            edition: 'Developer',
            lastChecked: '2023-06-15T14:30:00Z',
            health: 'Healthy',
            cpuUsage: 12,
            memoryUsage: 25,
            diskUsage: 34,
          },
          {
            id: 4,
            name: 'SQL-TEST-01',
            ipAddress: '192.168.1.103',
            status: 'Offline',
            version: 'SQL Server 2017 (14.0.3456.2)',
            edition: 'Standard',
            lastChecked: '2023-06-15T14:30:00Z',
            health: 'Critical',
            cpuUsage: 0,
            memoryUsage: 0,
            diskUsage: 65,
          },
        ]);
        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error fetching servers:', err);
      setError('Failed to fetch servers. Please try again.');
      setLoading(false);
    }
  };

  const getStatusChip = (status) => {
    if (status === 'Online') {
      return <Chip label={status} color="success" size="small" icon={<CheckCircleIcon />} />;
    } else {
      return <Chip label={status} color="error" size="small" icon={<ErrorIcon />} />;
    }
  };

  const getHealthChip = (health) => {
    if (health === 'Healthy') {
      return <Chip label={health} color="success" size="small" />;
    } else if (health === 'Warning') {
      return <Chip label={health} color="warning" size="small" icon={<WarningIcon />} />;
    } else {
      return <Chip label={health} color="error" size="small" icon={<ErrorIcon />} />;
    }
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleString();
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box className="dashboard-container">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3, alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          SQL Servers
        </Typography>
        <Box>
          <Button 
            variant="outlined" 
            startIcon={<RefreshIcon />} 
            onClick={fetchServers}
            sx={{ mr: 2 }}
          >
            Refresh
          </Button>
          <Button 
            variant="contained" 
            startIcon={<AddIcon />}
          >
            Add Server
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Server list */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Server Name</TableCell>
              <TableCell>IP Address</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Version</TableCell>
              <TableCell>Health</TableCell>
              <TableCell>Last Checked</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {servers.map((server) => (
              <TableRow key={server.id}>
                <TableCell>{server.name}</TableCell>
                <TableCell>{server.ipAddress}</TableCell>
                <TableCell>{getStatusChip(server.status)}</TableCell>
                <TableCell>
                  <Typography variant="body2">{server.version}</Typography>
                  <Typography variant="caption" color="textSecondary">
                    {server.edition}
                  </Typography>
                </TableCell>
                <TableCell>{getHealthChip(server.health)}</TableCell>
                <TableCell>{formatDate(server.lastChecked)}</TableCell>
                <TableCell>
                  <IconButton size="small" color="primary">
                    <EditIcon fontSize="small" />
                  </IconButton>
                  <IconButton size="small" color="error">
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Server performance overview */}
      <Typography variant="h5" sx={{ mt: 4, mb: 2 }}>
        Server Performance Overview
      </Typography>
      <Grid container spacing={3}>
        {servers.filter(server => server.status === 'Online').map((server) => (
          <Grid item xs={12} md={6} lg={4} key={server.id}>
            <Card>
              <CardHeader 
                title={server.name} 
                subheader={server.ipAddress}
                action={getHealthChip(server.health)}
              />
              <Divider />
              <CardContent>
                <Typography variant="body2" gutterBottom>
                  CPU Usage: {server.cpuUsage}%
                </Typography>
                <Box sx={{ width: '100%', mb: 2 }}>
                  <Box sx={{ 
                    height: 8, 
                    borderRadius: 5, 
                    bgcolor: 'grey.300',
                    position: 'relative' 
                  }}>
                    <Box sx={{ 
                      height: '100%', 
                      borderRadius: 5, 
                      position: 'absolute',
                      width: `${server.cpuUsage}%`,
                      bgcolor: server.cpuUsage > 80 ? 'error.main' : server.cpuUsage > 60 ? 'warning.main' : 'success.main'
                    }} />
                  </Box>
                </Box>
                
                <Typography variant="body2" gutterBottom>
                  Memory Usage: {server.memoryUsage}%
                </Typography>
                <Box sx={{ width: '100%', mb: 2 }}>
                  <Box sx={{ 
                    height: 8, 
                    borderRadius: 5, 
                    bgcolor: 'grey.300',
                    position: 'relative' 
                  }}>
                    <Box sx={{ 
                      height: '100%', 
                      borderRadius: 5, 
                      position: 'absolute',
                      width: `${server.memoryUsage}%`,
                      bgcolor: server.memoryUsage > 80 ? 'error.main' : server.memoryUsage > 60 ? 'warning.main' : 'success.main'
                    }} />
                  </Box>
                </Box>
                
                <Typography variant="body2" gutterBottom>
                  Disk Usage: {server.diskUsage}%
                </Typography>
                <Box sx={{ width: '100%' }}>
                  <Box sx={{ 
                    height: 8, 
                    borderRadius: 5, 
                    bgcolor: 'grey.300',
                    position: 'relative' 
                  }}>
                    <Box sx={{ 
                      height: '100%', 
                      borderRadius: 5, 
                      position: 'absolute',
                      width: `${server.diskUsage}%`,
                      bgcolor: server.diskUsage > 80 ? 'error.main' : server.diskUsage > 60 ? 'warning.main' : 'success.main'
                    }} />
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}

export default Servers; 