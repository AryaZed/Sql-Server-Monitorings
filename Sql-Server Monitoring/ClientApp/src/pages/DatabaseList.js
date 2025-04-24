import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Divider,
  Grid,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  InputAdornment,
  Chip,
  IconButton,
  CircularProgress,
  Alert,
  Button,
} from '@mui/material';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  CheckCircle as CheckCircleIcon,
  Cancel as CancelIcon,
  Info as InfoIcon,
  ArrowForward as ArrowForwardIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { databases, health } from '../services/api';

function DatabaseList() {
  const { connectionString } = useConnection();
  const [databaseList, setDatabaseList] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  
  useEffect(() => {
    if (!connectionString) return;
    
    fetchDatabases();
  }, [connectionString]);
  
  const fetchDatabases = async () => {
    setLoading(true);
    setError('');
    
    try {
      // In a real app, call the API
      // const response = await databases.getAll(connectionString);
      // setDatabaseList(response.data);
      
      // For demo, mock data
      setTimeout(() => {
        const mockDatabases = [
          {
            id: 1,
            name: 'master',
            status: 'Online',
            size: 205.5,
            compatibilityLevel: 150,
            recoveryModel: 'Simple',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-14T18:30:00Z',
            creationDate: '2019-11-15T10:23:15Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'master.mdf', type: 'Data', size: 195.5, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\master.mdf' },
              { name: 'mastlog.ldf', type: 'Log', size: 10.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\mastlog.ldf' }
            ],
            health: {
              status: 'Healthy',
              fragmentedIndexesCount: 0,
              missingIndexesCount: 0,
              longRunningQueries: 0,
              unusedIndexesCount: 0
            }
          },
          {
            id: 2,
            name: 'model',
            status: 'Online',
            size: 170.2,
            compatibilityLevel: 150,
            recoveryModel: 'Simple',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-14T18:30:00Z',
            creationDate: '2019-11-15T10:23:15Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'model.mdf', type: 'Data', size: 160.2, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\model.mdf' },
              { name: 'modellog.ldf', type: 'Log', size: 10.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\modellog.ldf' }
            ],
            health: {
              status: 'Healthy',
              fragmentedIndexesCount: 0,
              missingIndexesCount: 0,
              longRunningQueries: 0,
              unusedIndexesCount: 0
            }
          },
          {
            id: 3,
            name: 'msdb',
            status: 'Online',
            size: 450.3,
            compatibilityLevel: 150,
            recoveryModel: 'Simple',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-14T18:30:00Z',
            creationDate: '2019-11-15T10:23:15Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'msdbdata.mdf', type: 'Data', size: 440.3, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\MSDBData.mdf' },
              { name: 'msdblog.ldf', type: 'Log', size: 10.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\MSDBLog.ldf' }
            ],
            health: {
              status: 'Healthy',
              fragmentedIndexesCount: 0,
              missingIndexesCount: 0,
              longRunningQueries: 0,
              unusedIndexesCount: 0
            }
          },
          {
            id: 4,
            name: 'tempdb',
            status: 'Online',
            size: 834.7,
            compatibilityLevel: 150,
            recoveryModel: 'Simple',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: null,
            creationDate: '2023-06-12T08:15:20Z', // Recreated on server restart
            owner: 'sa',
            databaseFiles: [
              { name: 'tempdb.mdf', type: 'Data', size: 824.7, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\tempdb.mdf' },
              { name: 'templog.ldf', type: 'Log', size: 10.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\templog.ldf' }
            ],
            health: {
              status: 'Healthy',
              fragmentedIndexesCount: 0,
              missingIndexesCount: 0,
              longRunningQueries: 0,
              unusedIndexesCount: 0
            }
          },
          {
            id: 5,
            name: 'AdventureWorks',
            status: 'Online',
            size: 245.6,
            compatibilityLevel: 150,
            recoveryModel: 'Full',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-14T22:00:00Z',
            creationDate: '2023-02-10T15:45:30Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'AdventureWorks.mdf', type: 'Data', size: 200.6, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\AdventureWorks.mdf' },
              { name: 'AdventureWorks_log.ldf', type: 'Log', size: 45.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\AdventureWorks_log.ldf' }
            ],
            health: {
              status: 'Warning',
              fragmentedIndexesCount: 12,
              missingIndexesCount: 3,
              longRunningQueries: 1,
              unusedIndexesCount: 8
            }
          },
          {
            id: 6,
            name: 'Northwind',
            status: 'Online',
            size: 82.3,
            compatibilityLevel: 140,
            recoveryModel: 'Simple',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-13T22:00:00Z',
            creationDate: '2023-01-05T11:20:15Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'Northwind.mdf', type: 'Data', size: 75.3, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\Northwind.mdf' },
              { name: 'Northwind_log.ldf', type: 'Log', size: 7.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\Northwind_log.ldf' }
            ],
            health: {
              status: 'Warning',
              fragmentedIndexesCount: 5,
              missingIndexesCount: 0,
              longRunningQueries: 0,
              unusedIndexesCount: 4
            }
          },
          {
            id: 7,
            name: 'WideWorldImporters',
            status: 'Online',
            size: 512.1,
            compatibilityLevel: 150,
            recoveryModel: 'Full',
            collation: 'SQL_Latin1_General_CP1_CI_AS',
            lastBackupDate: '2023-06-14T22:00:00Z',
            creationDate: '2023-03-20T09:15:45Z',
            owner: 'sa',
            databaseFiles: [
              { name: 'WideWorldImporters.mdf', type: 'Data', size: 480.1, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\WideWorldImporters.mdf' },
              { name: 'WideWorldImporters_log.ldf', type: 'Log', size: 32.0, physicalPath: 'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.SQLEXPRESS\\MSSQL\\DATA\\WideWorldImporters_log.ldf' }
            ],
            health: {
              status: 'Critical',
              fragmentedIndexesCount: 28,
              missingIndexesCount: 7,
              longRunningQueries: 3,
              unusedIndexesCount: 12
            }
          }
        ];
        
        setDatabaseList(mockDatabases);
        setLoading(false);
      }, 1500);
    } catch (err) {
      console.error('Error fetching databases:', err);
      setError('Failed to fetch database list. Please try again.');
      setLoading(false);
    }
  };
  
  const handleSearchChange = (e) => {
    setSearchTerm(e.target.value);
  };
  
  const handleRefresh = () => {
    fetchDatabases();
  };
  
  const filteredDatabases = databaseList.filter(db => 
    db.name.toLowerCase().includes(searchTerm.toLowerCase())
  );
  
  const getStatusChip = (status) => {
    if (status === 'Online') {
      return <Chip icon={<CheckCircleIcon />} label={status} color="success" size="small" />;
    } else {
      return <Chip icon={<CancelIcon />} label={status} color="error" size="small" />;
    }
  };
  
  const getHealthChip = (health) => {
    if (health.status === 'Healthy') {
      return <Chip label={health.status} color="success" size="small" />;
    } else if (health.status === 'Warning') {
      return <Chip label={health.status} color="warning" size="small" />;
    } else {
      return <Chip label={health.status} color="error" size="small" />;
    }
  };
  
  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleString();
  };
  
  const formatSize = (sizeInMB) => {
    if (sizeInMB < 1024) {
      return `${sizeInMB.toFixed(2)} MB`;
    } else {
      return `${(sizeInMB / 1024).toFixed(2)} GB`;
    }
  };
  
  const getDatabaseSummary = () => {
    const total = databaseList.length;
    const healthy = databaseList.filter(db => db.health.status === 'Healthy').length;
    const warning = databaseList.filter(db => db.health.status === 'Warning').length;
    const critical = databaseList.filter(db => db.health.status === 'Critical').length;
    const totalSize = databaseList.reduce((sum, db) => sum + db.size, 0);
    
    return { total, healthy, warning, critical, totalSize };
  };
  
  const summary = getDatabaseSummary();
  
  return (
    <Box className="dashboard-container">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3, alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          Databases
        </Typography>
        <Button 
          variant="outlined" 
          startIcon={<RefreshIcon />}
          onClick={handleRefresh}
          disabled={loading}
        >
          Refresh
        </Button>
      </Box>
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* Database Summary Cards */}
          <Grid container spacing={3} sx={{ mb: 3 }}>
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent sx={{ textAlign: 'center' }}>
                  <StorageIcon sx={{ fontSize: 40, color: 'primary.main', mb: 1 }} />
                  <Typography variant="h5">{summary.total}</Typography>
                  <Typography color="textSecondary">Total Databases</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent sx={{ textAlign: 'center' }}>
                  <CheckCircleIcon sx={{ fontSize: 40, color: 'success.main', mb: 1 }} />
                  <Typography variant="h5">{summary.healthy}</Typography>
                  <Typography color="textSecondary">Healthy</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent sx={{ textAlign: 'center' }}>
                  <InfoIcon sx={{ fontSize: 40, color: 'warning.main', mb: 1 }} />
                  <Typography variant="h5">{summary.warning}</Typography>
                  <Typography color="textSecondary">Warning</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="h5">{formatSize(summary.totalSize)}</Typography>
                  <Typography color="textSecondary">Total Size</Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
          
          {/* Search Bar */}
          <Box sx={{ mb: 3 }}>
            <TextField
              fullWidth
              placeholder="Search databases..."
              variant="outlined"
              value={searchTerm}
              onChange={handleSearchChange}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Box>
          
          {/* Database Table */}
          <TableContainer component={Paper}>
            <Table sx={{ minWidth: 650 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell align="center">Status</TableCell>
                  <TableCell align="center">Health</TableCell>
                  <TableCell align="right">Size</TableCell>
                  <TableCell>Recovery Model</TableCell>
                  <TableCell>Last Backup</TableCell>
                  <TableCell align="center">Details</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredDatabases.map((db) => (
                  <TableRow key={db.id}>
                    <TableCell component="th" scope="row">
                      <Typography variant="body1">{db.name}</Typography>
                      <Typography variant="caption" color="textSecondary">
                        Level {db.compatibilityLevel}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">
                      {getStatusChip(db.status)}
                    </TableCell>
                    <TableCell align="center">
                      {getHealthChip(db.health)}
                    </TableCell>
                    <TableCell align="right">
                      {formatSize(db.size)}
                    </TableCell>
                    <TableCell>{db.recoveryModel}</TableCell>
                    <TableCell>{formatDate(db.lastBackupDate)}</TableCell>
                    <TableCell align="center">
                      <IconButton 
                        component={Link} 
                        to={`/databases/${db.id}`}
                        color="primary"
                        size="small"
                      >
                        <ArrowForwardIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
                {filteredDatabases.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      <Typography sx={{ py: 2 }}>
                        No databases found matching '{searchTerm}'
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
    </Box>
  );
}

export default DatabaseList; 