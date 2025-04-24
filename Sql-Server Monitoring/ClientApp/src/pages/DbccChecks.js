import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  CircularProgress,
  Alert,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  IconButton,
  Grid,
  Card,
  CardContent,
  CardActions,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Check as CheckIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  PlayArrow as RunIcon,
  Report as ReportIcon,
  CalendarToday as CalendarIcon,
  TimerOutlined as DurationIcon,
  VerifiedUser as IntegrityIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import dbccCheckService from '../services/dbccCheckService';

function DbccChecks() {
  const { connection } = useConnection();
  const [dbccChecks, setDbccChecks] = useState([]);
  const [issues, setIssues] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [runDbccDialogOpen, setRunDbccDialogOpen] = useState(false);
  const [selectedDatabase, setSelectedDatabase] = useState(null);
  const [runningDbcc, setRunningDbcc] = useState(false);
  const [runDbccResult, setRunDbccResult] = useState(null);

  // Load DBCC check data when the component mounts or connection changes
  useEffect(() => {
    if (connection?.connectionString) {
      loadDbccCheckData();
    }
  }, [connection]);

  // Load DBCC check history and analyze for issues
  const loadDbccCheckData = async () => {
    setLoading(true);
    setError(null);
    try {
      const historyData = await dbccCheckService.getDbccCheckHistory(connection.connectionString);
      setDbccChecks(historyData);

      const issuesData = await dbccCheckService.analyzeDbccChecks(connection.connectionString);
      setIssues(issuesData);
    } catch (err) {
      setError('Failed to load DBCC check data. ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Run DBCC CHECKDB
  const runDbccCheck = async () => {
    if (!selectedDatabase) return;
    
    setRunningDbcc(true);
    try {
      const result = await dbccCheckService.runDbccCheck(
        connection.connectionString,
        selectedDatabase
      );
      setRunDbccResult(result);
      
      // Reload data after successful run
      await loadDbccCheckData();
    } catch (err) {
      setError('Failed to run DBCC CHECKDB. ' + err.message);
      setRunDbccResult({
        success: false,
        message: 'Failed to run DBCC CHECKDB: ' + err.message
      });
    } finally {
      setRunningDbcc(false);
    }
  };

  // Open run DBCC dialog for a specific database
  const openRunDbccDialog = (databaseName) => {
    setSelectedDatabase(databaseName);
    setRunDbccResult(null);
    setRunDbccDialogOpen(true);
  };

  // Close run DBCC dialog
  const closeRunDbccDialog = () => {
    setRunDbccDialogOpen(false);
    setSelectedDatabase(null);
    setRunDbccResult(null);
  };

  // Get status chip based on check data
  const getStatusChip = (dbCheck) => {
    if (dbCheck.hasErrors) {
      return <Chip icon={<ErrorIcon />} label="Corruption Detected" color="error" size="small" />;
    }
    
    const lastGoodCheckDate = new Date(dbCheck.lastGoodCheckDate);
    const daysSinceLastCheck = Math.floor((new Date() - lastGoodCheckDate) / (1000 * 60 * 60 * 24));
    
    if (lastGoodCheckDate.getFullYear() <= 1970 || daysSinceLastCheck > 14) {
      return <Chip icon={<WarningIcon />} label="Check Needed" color="warning" size="small" />;
    }
    
    if (daysSinceLastCheck > 7) {
      return <Chip icon={<WarningIcon />} label="Check Soon" color="warning" size="small" />;
    }
    
    return <Chip icon={<CheckIcon />} label="Healthy" color="success" size="small" />;
  };

  // Format date for display
  const formatDate = (dateString) => {
    if (!dateString || new Date(dateString).getFullYear() <= 1970) {
      return 'Never';
    }
    return new Date(dateString).toLocaleString();
  };

  // Find relevant issues for a database
  const getDatabaseIssues = (databaseName) => {
    return issues.filter(issue => issue.databaseName === databaseName);
  };

  // Render run DBCC dialog
  const renderRunDbccDialog = () => {
    return (
      <Dialog
        open={runDbccDialogOpen}
        onClose={!runningDbcc ? closeRunDbccDialog : undefined}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Run DBCC CHECKDB</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Running DBCC CHECKDB for database <strong>{selectedDatabase}</strong>. 
            This will verify the physical and logical integrity of the database.
            
            {runningDbcc && (
              <Box sx={{ display: 'flex', justifyContent: 'center', my: 3 }}>
                <CircularProgress />
              </Box>
            )}
            
            {runDbccResult && (
              <Alert 
                severity={runDbccResult.success ? "success" : "error"}
                sx={{ mt: 2 }}
              >
                {runDbccResult.message}
              </Alert>
            )}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={closeRunDbccDialog}
            disabled={runningDbcc}
          >
            Close
          </Button>
          <Button 
            variant="contained" 
            startIcon={<RunIcon />}
            onClick={runDbccCheck}
            disabled={runningDbcc || !selectedDatabase}
            color="primary"
          >
            Run DBCC CHECKDB
          </Button>
        </DialogActions>
      </Dialog>
    );
  };

  return (
    <Box sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4" component="h1">Database Integrity Checks</Typography>
        <Button 
          variant="contained" 
          startIcon={<RefreshIcon />}
          onClick={loadDbccCheckData}
        >
          Refresh
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>
      )}

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <IntegrityIcon color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Database Integrity</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary">
                DBCC CHECKDB verifies the physical and logical integrity of all objects in a database.
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <CalendarIcon color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Recommended Frequency</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary">
                Microsoft recommends running DBCC CHECKDB at least once a week for production databases.
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <DurationIcon color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Runtime Impact</Typography>
              </Box>
              <Typography variant="body2" color="text.secondary">
                DBCC CHECKDB is resource-intensive. For large databases, consider running during maintenance windows.
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Database</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last Good Check</TableCell>
                  <TableCell>Check Type</TableCell>
                  <TableCell>Issues</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {dbccChecks.length > 0 ? (
                  dbccChecks.map((dbCheck) => {
                    const dbIssues = getDatabaseIssues(dbCheck.databaseName);
                    return (
                      <TableRow key={dbCheck.databaseName}>
                        <TableCell>{dbCheck.databaseName}</TableCell>
                        <TableCell>{getStatusChip(dbCheck)}</TableCell>
                        <TableCell>{formatDate(dbCheck.lastGoodCheckDate)}</TableCell>
                        <TableCell>{dbCheck.checkType}</TableCell>
                        <TableCell>
                          {dbIssues.length > 0 ? (
                            <Tooltip title={dbIssues[0].message}>
                              <Chip 
                                icon={<ReportIcon />} 
                                label={`${dbIssues.length} ${dbIssues.length === 1 ? 'issue' : 'issues'}`} 
                                color="warning" 
                                size="small" 
                              />
                            </Tooltip>
                          ) : (
                            <Chip 
                              icon={<CheckIcon />} 
                              label="No issues" 
                              color="success" 
                              size="small" 
                            />
                          )}
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="outlined"
                            size="small"
                            startIcon={<RunIcon />}
                            onClick={() => openRunDbccDialog(dbCheck.databaseName)}
                          >
                            Run DBCC
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })
                ) : (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      No DBCC check data available
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {issues.length > 0 && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h5" sx={{ mb: 2 }}>Detected Issues</Typography>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Database</TableCell>
                  <TableCell>Issue</TableCell>
                  <TableCell>Severity</TableCell>
                  <TableCell>Recommended Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {issues.map((issue, index) => (
                  <TableRow key={index}>
                    <TableCell>{issue.databaseName}</TableCell>
                    <TableCell>{issue.message}</TableCell>
                    <TableCell>
                      <Chip 
                        label={issue.severity} 
                        color={
                          issue.severity === 'Critical' ? 'error' :
                          issue.severity === 'High' ? 'error' :
                          issue.severity === 'Medium' ? 'warning' :
                          'info'
                        }
                        size="small" 
                      />
                    </TableCell>
                    <TableCell>{issue.recommendedAction}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}

      {renderRunDbccDialog()}
    </Box>
  );
}

export default DbccChecks; 