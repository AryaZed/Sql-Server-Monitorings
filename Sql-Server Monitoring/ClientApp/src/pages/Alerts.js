import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  IconButton,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Grid,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  InputAdornment,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Delete as DeleteIcon,
  Check as CheckIcon,
  Search as SearchIcon,
  Notifications as NotificationsIcon,
  NotificationsActive as NotificationsActiveIcon,
  NotificationsOff as NotificationsOffIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';

function Alerts() {
  const { connectionString } = useConnection();
  const [loading, setLoading] = useState(true);
  const [alerts, setAlerts] = useState([]);
  const [filteredAlerts, setFilteredAlerts] = useState([]);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [severityFilter, setSeverityFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [error, setError] = useState('');
  const [stats, setStats] = useState({
    critical: 0,
    warning: 0,
    info: 0,
    total: 0
  });

  useEffect(() => {
    fetchAlerts();
  }, []);

  useEffect(() => {
    // Apply filters
    let result = [...alerts];
    
    // Apply search term
    if (searchTerm) {
      const searchLower = searchTerm.toLowerCase();
      result = result.filter(alert => 
        alert.message.toLowerCase().includes(searchLower) ||
        alert.source.toLowerCase().includes(searchLower)
      );
    }
    
    // Apply severity filter
    if (severityFilter !== 'all') {
      result = result.filter(alert => alert.severity === severityFilter);
    }
    
    // Apply status filter
    if (statusFilter !== 'all') {
      result = result.filter(alert => alert.status === statusFilter);
    }
    
    setFilteredAlerts(result);
  }, [alerts, searchTerm, severityFilter, statusFilter]);

  const fetchAlerts = async () => {
    setLoading(true);
    try {
      // In a real app, call API to fetch alerts
      // const response = await api.getAlerts();
      // setAlerts(response.data);
      
      // For demo, mock data
      setTimeout(() => {
        const mockAlerts = [
          {
            id: 1,
            severity: 'critical',
            status: 'active',
            message: 'CPU usage exceeded 90% threshold on SQL-PROD-01',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T14:30:00Z',
            details: 'CPU usage at 92% for over 5 minutes',
          },
          {
            id: 2,
            severity: 'warning',
            status: 'active',
            message: 'Memory usage approaching threshold on SQL-PROD-02',
            source: 'SQL-PROD-02',
            timestamp: '2023-06-15T13:45:00Z',
            details: 'Memory usage at 85% and increasing',
          },
          {
            id: 3,
            severity: 'info',
            status: 'resolved',
            message: 'Database backup completed successfully',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T12:00:00Z',
            details: 'AdventureWorks full backup completed in 3 minutes',
            resolvedAt: '2023-06-15T12:00:01Z'
          },
          {
            id: 4,
            severity: 'critical',
            status: 'resolved',
            message: 'Deadlock detected on SQL-PROD-02',
            source: 'SQL-PROD-02',
            timestamp: '2023-06-15T11:30:00Z',
            details: 'Deadlock occurred between process 52 and 58',
            resolvedAt: '2023-06-15T11:35:22Z'
          },
          {
            id: 5,
            severity: 'warning',
            status: 'active',
            message: 'Disk space below 15% on SQL-PROD-01 D: drive',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T10:15:00Z',
            details: '12% free space remaining on D: drive',
          },
          {
            id: 6,
            severity: 'info',
            status: 'active',
            message: 'SQL Server restarted on SQL-DEV-01',
            source: 'SQL-DEV-01',
            timestamp: '2023-06-15T09:00:00Z',
            details: 'Service restarted as part of scheduled maintenance',
          },
          {
            id: 7,
            severity: 'warning',
            status: 'resolved',
            message: 'Long-running query detected on SQL-PROD-01',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T08:45:00Z',
            details: 'Query running for over 15 minutes, affecting system performance',
            resolvedAt: '2023-06-15T09:02:33Z'
          },
          {
            id: 8,
            severity: 'critical',
            status: 'active',
            message: 'Transaction log full on SQL-PROD-02 WideWorldImporters',
            source: 'SQL-PROD-02',
            timestamp: '2023-06-15T08:30:00Z',
            details: 'Transaction log space usage at 100%, operations blocked',
          },
          {
            id: 9,
            severity: 'info',
            status: 'active',
            message: 'Index maintenance completed on SQL-PROD-01',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T07:00:00Z',
            details: 'Scheduled index maintenance completed successfully',
          },
          {
            id: 10,
            severity: 'warning',
            status: 'resolved',
            message: 'Blocking detected on SQL-PROD-01',
            source: 'SQL-PROD-01',
            timestamp: '2023-06-15T06:30:00Z',
            details: 'Head blocker session ID: 138, blocking 12 sessions',
            resolvedAt: '2023-06-15T06:45:18Z'
          },
          {
            id: 11,
            severity: 'critical',
            status: 'resolved',
            message: 'SQL Server service stopped unexpectedly on SQL-TEST-01',
            source: 'SQL-TEST-01',
            timestamp: '2023-06-15T05:15:00Z',
            details: 'Service stopped due to system error, automatic restart failed',
            resolvedAt: '2023-06-15T06:22:05Z'
          },
          {
            id: 12,
            severity: 'info',
            status: 'active',
            message: 'New database created on SQL-DEV-01',
            source: 'SQL-DEV-01',
            timestamp: '2023-06-15T04:00:00Z',
            details: 'Database TestDB created by user admin',
          },
        ];
        
        setAlerts(mockAlerts);
        setFilteredAlerts(mockAlerts);
        
        // Calculate stats
        const criticalCount = mockAlerts.filter(a => a.severity === 'critical').length;
        const warningCount = mockAlerts.filter(a => a.severity === 'warning').length;
        const infoCount = mockAlerts.filter(a => a.severity === 'info').length;
        
        setStats({
          critical: criticalCount,
          warning: warningCount,
          info: infoCount,
          total: mockAlerts.length
        });
        
        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error fetching alerts:', err);
      setError('Failed to fetch alerts. Please try again.');
      setLoading(false);
    }
  };

  const handleChangePage = (event, newPage) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleResolveAlert = (id) => {
    // In a real app, call API to resolve the alert
    // await api.resolveAlert(id);
    
    // For demo, update locally
    setAlerts(alerts.map(alert => 
      alert.id === id ? 
      { ...alert, status: 'resolved', resolvedAt: new Date().toISOString() } : 
      alert
    ));
  };

  const handleSearchChange = (event) => {
    setSearchTerm(event.target.value);
  };

  const handleSeverityFilterChange = (event) => {
    setSeverityFilter(event.target.value);
  };

  const handleStatusFilterChange = (event) => {
    setStatusFilter(event.target.value);
  };

  const getSeverityIcon = (severity) => {
    switch (severity) {
      case 'critical':
        return <ErrorIcon sx={{ color: 'error.main' }} />;
      case 'warning':
        return <WarningIcon sx={{ color: 'warning.main' }} />;
      case 'info':
        return <InfoIcon sx={{ color: 'info.main' }} />;
      default:
        return null;
    }
  };

  const getSeverityChip = (severity) => {
    switch (severity) {
      case 'critical':
        return <Chip label="Critical" color="error" size="small" icon={<ErrorIcon />} />;
      case 'warning':
        return <Chip label="Warning" color="warning" size="small" icon={<WarningIcon />} />;
      case 'info':
        return <Chip label="Info" color="info" size="small" icon={<InfoIcon />} />;
      default:
        return null;
    }
  };

  const getStatusChip = (status) => {
    switch (status) {
      case 'active':
        return <Chip label="Active" color="error" size="small" icon={<NotificationsActiveIcon />} />;
      case 'resolved':
        return <Chip label="Resolved" color="success" size="small" icon={<CheckIcon />} />;
      default:
        return null;
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
      <Typography variant="h4" component="h1" gutterBottom>
        Alerts & Notifications
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Summary Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <NotificationsIcon sx={{ fontSize: 40, color: 'primary.main', mb: 1 }} />
              <Typography variant="h5">{stats.total}</Typography>
              <Typography color="textSecondary">Total Alerts</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <ErrorIcon sx={{ fontSize: 40, color: 'error.main', mb: 1 }} />
              <Typography variant="h5">{stats.critical}</Typography>
              <Typography color="textSecondary">Critical</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <WarningIcon sx={{ fontSize: 40, color: 'warning.main', mb: 1 }} />
              <Typography variant="h5">{stats.warning}</Typography>
              <Typography color="textSecondary">Warnings</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <InfoIcon sx={{ fontSize: 40, color: 'info.main', mb: 1 }} />
              <Typography variant="h5">{stats.info}</Typography>
              <Typography color="textSecondary">Informational</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              placeholder="Search alerts..."
              value={searchTerm}
              onChange={handleSearchChange}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
              size="small"
            />
          </Grid>
          <Grid item xs={12} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel id="severity-filter-label">Severity</InputLabel>
              <Select
                labelId="severity-filter-label"
                value={severityFilter}
                label="Severity"
                onChange={handleSeverityFilterChange}
              >
                <MenuItem value="all">All Severities</MenuItem>
                <MenuItem value="critical">Critical</MenuItem>
                <MenuItem value="warning">Warning</MenuItem>
                <MenuItem value="info">Info</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel id="status-filter-label">Status</InputLabel>
              <Select
                labelId="status-filter-label"
                value={statusFilter}
                label="Status"
                onChange={handleStatusFilterChange}
              >
                <MenuItem value="all">All Statuses</MenuItem>
                <MenuItem value="active">Active</MenuItem>
                <MenuItem value="resolved">Resolved</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} md={2}>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={fetchAlerts}
            >
              Refresh
            </Button>
          </Grid>
        </Grid>
      </Paper>

      {/* Alerts Table */}
      <Paper>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Severity</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Message</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>Time</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredAlerts
                .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
                .map((alert) => (
                  <TableRow key={alert.id}>
                    <TableCell>{getSeverityChip(alert.severity)}</TableCell>
                    <TableCell>{getStatusChip(alert.status)}</TableCell>
                    <TableCell>
                      <Typography variant="body2">{alert.message}</Typography>
                      <Typography variant="caption" color="textSecondary">
                        {alert.details}
                      </Typography>
                    </TableCell>
                    <TableCell>{alert.source}</TableCell>
                    <TableCell>
                      <Typography variant="body2">{formatDate(alert.timestamp)}</Typography>
                      {alert.resolvedAt && (
                        <Typography variant="caption" color="textSecondary">
                          Resolved: {formatDate(alert.resolvedAt)}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right">
                      {alert.status === 'active' && (
                        <Button
                          size="small"
                          variant="outlined"
                          color="success"
                          startIcon={<CheckIcon />}
                          onClick={() => handleResolveAlert(alert.id)}
                        >
                          Resolve
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              {filteredAlerts.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <Box sx={{ py: 3 }}>
                      <NotificationsOffIcon sx={{ fontSize: 40, color: 'text.secondary', mb: 1 }} />
                      <Typography>No alerts found</Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={filteredAlerts.length}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </Paper>
    </Box>
  );
}

export default Alerts; 