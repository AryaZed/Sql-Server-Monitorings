import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Grid,
  Paper,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Tabs,
  Tab,
  List,
  ListItem,
  ListItemText,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Button,
  IconButton,
  CircularProgress,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Refresh as RefreshIcon,
  Storage as StorageIcon,
  Save as SaveIcon,
  Memory as MemoryIcon,
  Build as BuildIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { databases, health } from '../services/api';

function DatabaseDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { connectionString } = useConnection();
  const [database, setDatabase] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState(0);
  
  useEffect(() => {
    if (!connectionString) return;
    
    fetchDatabaseDetails();
  }, [connectionString, id]);
  
  const fetchDatabaseDetails = async () => {
    setLoading(true);
    setError('');
    
    try {
      // In a real app, call the API
      // const response = await databases.getById(id, connectionString);
      // setDatabase(response.data);
      
      // For demo, mock data with some delay
      setTimeout(() => {
        // Mocked database details
        setDatabase({
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
          },
          tables: [
            { name: 'Person', rows: 19972, sizeKB: 15280, indexCount: 7 },
            { name: 'Product', rows: 504, sizeKB: 5120, indexCount: 5 },
            { name: 'SalesOrderHeader', rows: 31465, sizeKB: 25600, indexCount: 8 },
            { name: 'SalesOrderDetail', rows: 121317, sizeKB: 40960, indexCount: 6 },
            { name: 'Customer', rows: 19820, sizeKB: 12800, indexCount: 4 }
          ],
          fragmentation: [
            { table: 'SalesOrderDetail', index: 'IX_SalesOrderDetail_ProductID', fragmentationPercent: 55.8, pageCounts: 1250 },
            { table: 'SalesOrderHeader', index: 'IX_SalesOrderHeader_CustomerID', fragmentationPercent: 43.2, pageCounts: 980 },
            { table: 'Person', index: 'IX_Person_LastName_FirstName', fragmentationPercent: 35.7, pageCounts: 620 }
          ],
          missingIndexes: [
            { 
              table: 'SalesOrderDetail', 
              impact: 85.4, 
              columns: ['OrderQty', 'UnitPrice'],
              includeColumns: ['ProductID'],
              statement: 'CREATE NONCLUSTERED INDEX IX_SalesOrderDetail_OrderQty_UnitPrice ON Sales.SalesOrderDetail(OrderQty, UnitPrice) INCLUDE (ProductID)'
            },
            { 
              table: 'Person', 
              impact: 65.2, 
              columns: ['EmailAddress'],
              includeColumns: ['BusinessEntityID', 'PersonType'],
              statement: 'CREATE NONCLUSTERED INDEX IX_Person_EmailAddress ON Person.Person(EmailAddress) INCLUDE (BusinessEntityID, PersonType)'
            }
          ],
          spaceUsage: {
            dataSpaceMB: 200.6,
            logSpaceMB: 45.0,
            unallocatedSpaceMB: 21.4,
            reservedMB: 267.0,
            dataMB: 185.3,
            indexSizeMB: 60.4,
            unusedMB: 21.3
          },
          performanceMetrics: {
            bufferCacheHitRatio: 98.5,
            pageLifeExpectancy: 2450,
            batchRequestsPerSec: 145.3,
            userConnections: 12,
            deadlocks: 0
          }
        });
        
        setLoading(false);
      }, 1500);
    } catch (err) {
      console.error('Error fetching database details:', err);
      setError('Failed to fetch database details. Please try again.');
      setLoading(false);
    }
  };
  
  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };
  
  const handleBack = () => {
    navigate('/databases');
  };
  
  const handleRefresh = () => {
    fetchDatabaseDetails();
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
  
  const getHealthStatusChip = (status) => {
    if (status === 'Healthy') {
      return <Chip icon={<CheckCircleIcon />} label={status} color="success" />;
    } else if (status === 'Warning') {
      return <Chip icon={<WarningIcon />} label={status} color="warning" />;
    } else {
      return <Chip icon={<WarningIcon />} label={status} color="error" />;
    }
  };
  
  if (loading || !database) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <CircularProgress />
      </Box>
    );
  }
  
  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>
        <Button variant="outlined" startIcon={<ArrowBackIcon />} onClick={handleBack}>
          Back to Databases
        </Button>
      </Box>
    );
  }
  
  return (
    <Box className="dashboard-container">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3, alignItems: 'center' }}>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <IconButton onClick={handleBack} sx={{ mr: 1 }}>
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h4" component="h1">
            {database.name}
          </Typography>
          {getHealthStatusChip(database.health.status)}
        </Box>
        <Button 
          variant="outlined" 
          startIcon={<RefreshIcon />}
          onClick={handleRefresh}
        >
          Refresh
        </Button>
      </Box>
      
      {/* Overview Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Database Overview</Typography>
              <Divider sx={{ mb: 2 }} />
              <List dense disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="Status" />
                  <Chip label={database.status} color={database.status === 'Online' ? 'success' : 'error'} size="small" />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Size" />
                  <Typography variant="body2">{formatSize(database.size)}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Compatibility Level" />
                  <Typography variant="body2">{database.compatibilityLevel}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Recovery Model" />
                  <Typography variant="body2">{database.recoveryModel}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Collation" />
                  <Typography variant="body2">{database.collation}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Created" />
                  <Typography variant="body2">{formatDate(database.creationDate)}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Last Backup" />
                  <Typography variant="body2">{formatDate(database.lastBackupDate)}</Typography>
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Owner" />
                  <Typography variant="body2">{database.owner}</Typography>
                </ListItem>
              </List>
            </CardContent>
          </Card>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Files</Typography>
              <Divider sx={{ mb: 2 }} />
              {database.databaseFiles.map((file, index) => (
                <Box key={index} sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="subtitle2">{file.name}</Typography>
                    <Typography variant="body2">{formatSize(file.size)}</Typography>
                  </Box>
                  <Typography variant="body2" color="textSecondary">{file.type} file</Typography>
                  <Typography variant="caption" color="textSecondary" sx={{ fontSize: '0.7rem' }}>
                    {file.physicalPath}
                  </Typography>
                </Box>
              ))}
            </CardContent>
          </Card>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Health Issues</Typography>
              <Divider sx={{ mb: 2 }} />
              <List dense disablePadding>
                <ListItem disableGutters>
                  <ListItemText primary="Fragmented Indexes" />
                  <Chip 
                    label={database.health.fragmentedIndexesCount} 
                    color={database.health.fragmentedIndexesCount > 0 ? 'warning' : 'success'} 
                    size="small" 
                  />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Missing Indexes" />
                  <Chip 
                    label={database.health.missingIndexesCount} 
                    color={database.health.missingIndexesCount > 0 ? 'warning' : 'success'} 
                    size="small" 
                  />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Long Running Queries" />
                  <Chip 
                    label={database.health.longRunningQueries} 
                    color={database.health.longRunningQueries > 0 ? 'error' : 'success'} 
                    size="small" 
                  />
                </ListItem>
                <ListItem disableGutters>
                  <ListItemText primary="Unused Indexes" />
                  <Chip 
                    label={database.health.unusedIndexesCount} 
                    color={database.health.unusedIndexesCount > 0 ? 'info' : 'success'} 
                    size="small" 
                  />
                </ListItem>
              </List>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      
      {/* Tabs for detailed information */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs value={activeTab} onChange={handleTabChange} aria-label="database details tabs">
          <Tab label="Tables" />
          <Tab label="Performance" />
          <Tab label="Fragmentation" />
          <Tab label="Space Usage" />
        </Tabs>
      </Box>
      
      {/* Tables Tab */}
      {activeTab === 0 && (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Table Name</TableCell>
                <TableCell align="right">Rows</TableCell>
                <TableCell align="right">Size</TableCell>
                <TableCell align="right">Indexes</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {database.tables.map((table) => (
                <TableRow key={table.name}>
                  <TableCell component="th" scope="row">
                    {table.name}
                  </TableCell>
                  <TableCell align="right">{table.rows.toLocaleString()}</TableCell>
                  <TableCell align="right">{(table.sizeKB / 1024).toFixed(2)} MB</TableCell>
                  <TableCell align="right">{table.indexCount}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      
      {/* Performance Tab */}
      {activeTab === 1 && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardHeader title="Performance Metrics" />
              <Divider />
              <CardContent>
                <Grid container spacing={3}>
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle1" gutterBottom>Buffer Cache Hit Ratio</Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <Box sx={{ width: '100%', mr: 1 }}>
                        <LinearProgress 
                          variant="determinate" 
                          value={database.performanceMetrics.bufferCacheHitRatio} 
                          color={database.performanceMetrics.bufferCacheHitRatio > 90 ? 'success' : 'warning'} 
                        />
                      </Box>
                      <Box sx={{ minWidth: 35 }}>
                        <Typography variant="body2" color="text.secondary">
                          {database.performanceMetrics.bufferCacheHitRatio}%
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle1" gutterBottom>Page Life Expectancy</Typography>
                    <Typography variant="h6">
                      {database.performanceMetrics.pageLifeExpectancy} seconds
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      Recommended: {'>'} 300 seconds
                    </Typography>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                      <Typography variant="h4">{database.performanceMetrics.batchRequestsPerSec.toFixed(1)}</Typography>
                      <Typography variant="body2" color="textSecondary">Batch Requests/sec</Typography>
                    </Paper>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                      <Typography variant="h4">{database.performanceMetrics.userConnections}</Typography>
                      <Typography variant="body2" color="textSecondary">User Connections</Typography>
                    </Paper>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Paper sx={{ p: 2, textAlign: 'center' }}>
                      <Typography variant="h4">{database.performanceMetrics.deadlocks}</Typography>
                      <Typography variant="body2" color="textSecondary">Deadlocks</Typography>
                    </Paper>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
      
      {/* Fragmentation Tab */}
      {activeTab === 2 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={7}>
            <Card>
              <CardHeader title="Fragmented Indexes" />
              <Divider />
              <CardContent>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Table</TableCell>
                        <TableCell>Index</TableCell>
                        <TableCell align="right">Fragmentation</TableCell>
                        <TableCell align="right">Pages</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {database.fragmentation.map((item, index) => (
                        <TableRow key={index}>
                          <TableCell component="th" scope="row">
                            {item.table}
                          </TableCell>
                          <TableCell>{item.index}</TableCell>
                          <TableCell align="right">
                            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end' }}>
                              <Typography variant="body2" sx={{ mr: 1 }}>
                                {item.fragmentationPercent.toFixed(1)}%
                              </Typography>
                              <Chip 
                                size="small" 
                                label={
                                  item.fragmentationPercent > 50 ? 'High' : 
                                  item.fragmentationPercent > 30 ? 'Medium' : 'Low'
                                } 
                                color={
                                  item.fragmentationPercent > 50 ? 'error' : 
                                  item.fragmentationPercent > 30 ? 'warning' : 'success'
                                } 
                              />
                            </Box>
                          </TableCell>
                          <TableCell align="right">{item.pageCounts}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={5}>
            <Card>
              <CardHeader title="Missing Indexes" />
              <Divider />
              <CardContent>
                {database.missingIndexes.map((item, index) => (
                  <Box key={index} sx={{ mb: 3 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      <Typography variant="subtitle1" sx={{ flexGrow: 1 }}>
                        {item.table}
                      </Typography>
                      <Chip 
                        label={`Impact: ${item.impact.toFixed(1)}%`} 
                        color={item.impact > 80 ? 'error' : 'warning'} 
                        size="small"
                      />
                    </Box>
                    <Typography variant="body2" sx={{ mb: 1 }}>
                      <b>Columns:</b> {item.columns.join(', ')}
                    </Typography>
                    {item.includeColumns.length > 0 && (
                      <Typography variant="body2" sx={{ mb: 1 }}>
                        <b>Include:</b> {item.includeColumns.join(', ')}
                      </Typography>
                    )}
                    <Paper sx={{ p: 1.5, bgcolor: 'grey.100' }}>
                      <Typography variant="caption" component="pre" sx={{ fontFamily: 'monospace', fontSize: '0.7rem', whiteSpace: 'pre-wrap' }}>
                        {item.statement}
                      </Typography>
                    </Paper>
                  </Box>
                ))}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
      
      {/* Space Usage Tab */}
      {activeTab === 3 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="Space Allocation" />
              <Divider />
              <CardContent>
                <TableContainer>
                  <Table>
                    <TableBody>
                      <TableRow>
                        <TableCell component="th">Data Space</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.dataSpaceMB)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell component="th">Log Space</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.logSpaceMB)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell component="th">Unallocated Space</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.unallocatedSpaceMB)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell component="th">Total Reserved</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.reservedMB)}</TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="Data Usage" />
              <Divider />
              <CardContent>
                <TableContainer>
                  <Table>
                    <TableBody>
                      <TableRow>
                        <TableCell component="th">Data Size</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.dataMB)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell component="th">Index Size</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.indexSizeMB)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell component="th">Unused Space</TableCell>
                        <TableCell align="right">{formatSize(database.spaceUsage.unusedMB)}</TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
    </Box>
  );
}

export default DatabaseDetails; 