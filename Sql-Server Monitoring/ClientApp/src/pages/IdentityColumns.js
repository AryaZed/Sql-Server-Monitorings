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
  TextField,
  IconButton,
  LinearProgress,
  Grid,
  Card,
  CardContent,
  InputAdornment,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Check as CheckIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Edit as EditIcon,
  Save as SaveIcon,
  Tune as TuneIcon,
  TableChart as TableIcon,
  BarChart as ChartIcon,
} from '@mui/icons-material';
import { Chart } from 'react-google-charts';
import { useConnection } from '../context/ConnectionContext';
import identityColumnService from '../services/identityColumnService';

function IdentityColumns() {
  const { connection } = useConnection();
  const [selectedDatabase, setSelectedDatabase] = useState('');
  const [identityColumns, setIdentityColumns] = useState([]);
  const [issues, setIssues] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [reseedDialogOpen, setReseedDialogOpen] = useState(false);
  const [selectedColumn, setSelectedColumn] = useState(null);
  const [newSeedValue, setNewSeedValue] = useState('');
  const [isReseeding, setIsReseeding] = useState(false);
  const [reseedResult, setReseedResult] = useState(null);
  const [databases, setDatabases] = useState([]);
  const [view, setView] = useState('table'); // 'table' or 'chart'

  // Load databases when component mounts
  useEffect(() => {
    if (connection?.connectionString) {
      // In a real app, you'd fetch the list of databases
      // This is a placeholder - you would replace with actual API call
      setDatabases(['master', 'model', 'msdb', 'tempdb']);
    }
  }, [connection]);

  // Load identity columns when database selection changes
  useEffect(() => {
    if (connection?.connectionString && selectedDatabase) {
      loadIdentityColumnsData();
    }
  }, [connection, selectedDatabase]);

  // Load identity columns and issues
  const loadIdentityColumnsData = async () => {
    setLoading(true);
    setError(null);
    try {
      const columnsData = await identityColumnService.getIdentityColumns(
        connection.connectionString,
        selectedDatabase
      );
      setIdentityColumns(columnsData);

      const issuesData = await identityColumnService.analyzeIdentityColumns(
        connection.connectionString,
        selectedDatabase
      );
      setIssues(issuesData);
    } catch (err) {
      setError('Failed to load identity columns. ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Open reseed dialog
  const openReseedDialog = (column) => {
    setSelectedColumn(column);
    setNewSeedValue(column.currentValue.toString());
    setReseedResult(null);
    setReseedDialogOpen(true);
  };

  // Close reseed dialog
  const closeReseedDialog = () => {
    setReseedDialogOpen(false);
    setSelectedColumn(null);
    setNewSeedValue('');
    setReseedResult(null);
  };

  // Reseed identity column
  const reseedIdentityColumn = async () => {
    if (!selectedColumn || !newSeedValue) return;
    
    setIsReseeding(true);
    try {
      const result = await identityColumnService.reseedIdentityColumn(
        connection.connectionString,
        selectedDatabase,
        selectedColumn.schemaName,
        selectedColumn.tableName,
        selectedColumn.columnName,
        parseInt(newSeedValue, 10)
      );
      setReseedResult(result);
      
      // Reload data after successful reseed
      if (result.success) {
        await loadIdentityColumnsData();
      }
    } catch (err) {
      setError('Failed to reseed identity column. ' + err.message);
      setReseedResult({
        success: false,
        message: 'Failed to reseed identity column: ' + err.message
      });
    } finally {
      setIsReseeding(false);
    }
  };

  // Get severity based on percent used
  const getSeverity = (percentUsed) => {
    if (percentUsed > 95) return 'critical';
    if (percentUsed > 80) return 'high';
    if (percentUsed > 60) return 'medium';
    return 'low';
  };

  // Get usage chip
  const getUsageChip = (percentUsed) => {
    const severity = getSeverity(percentUsed);
    
    if (severity === 'critical') {
      return <Chip icon={<ErrorIcon />} label={`${percentUsed.toFixed(1)}%`} color="error" size="small" />;
    }
    if (severity === 'high') {
      return <Chip icon={<WarningIcon />} label={`${percentUsed.toFixed(1)}%`} color="warning" size="small" />;
    }
    if (severity === 'medium') {
      return <Chip icon={<WarningIcon />} label={`${percentUsed.toFixed(1)}%`} color="warning" size="small" variant="outlined" />;
    }
    return <Chip icon={<CheckIcon />} label={`${percentUsed.toFixed(1)}%`} color="success" size="small" />;
  };

  // Get data type info tooltip
  const getDataTypeInfo = (dataType) => {
    switch (dataType.toLowerCase()) {
      case 'tinyint':
        return 'Range: 0 to 255 (8-bit)';
      case 'smallint':
        return 'Range: -32,768 to 32,767 (16-bit)';
      case 'int':
        return 'Range: -2,147,483,648 to 2,147,483,647 (32-bit)';
      case 'bigint':
        return 'Range: -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 (64-bit)';
      default:
        return dataType;
    }
  };

  // Render chart view of identity columns
  const renderChart = () => {
    // Sort by percent used descending
    const sortedColumns = [...identityColumns].sort((a, b) => b.percentUsed - a.percentUsed);
    
    const chartData = [
      ['Column', 'Percent Used', { role: 'style' }, { role: 'tooltip' }],
      ...sortedColumns.map(col => {
        const severity = getSeverity(col.percentUsed);
        const color = severity === 'critical' ? '#f44336' : 
                    severity === 'high' ? '#ff9800' :
                    severity === 'medium' ? '#ffeb3b' : 
                    '#4caf50';
        
        return [
          `${col.tableName}.${col.columnName}`,
          col.percentUsed,
          color,
          `Table: ${col.schemaName}.${col.tableName}\nColumn: ${col.columnName}\nData Type: ${col.dataType}\nPercent Used: ${col.percentUsed.toFixed(1)}%\nCurrent Value: ${col.currentValue.toLocaleString()}`
        ];
      })
    ];

    const options = {
      title: 'Identity Column Usage',
      hAxis: { title: 'Percent Used', minValue: 0, maxValue: 100 },
      legend: { position: 'none' },
      bars: 'horizontal',
      height: 400
    };

    return (
      <Chart
        chartType="BarChart"
        data={chartData}
        options={options}
        width="100%"
        height="400px"
      />
    );
  };

  // Render reseed dialog
  const renderReseedDialog = () => {
    if (!selectedColumn) return null;
    
    return (
      <Dialog
        open={reseedDialogOpen}
        onClose={!isReseeding ? closeReseedDialog : undefined}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Reseed Identity Column</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Reseeding identity column <strong>{selectedColumn.schemaName}.{selectedColumn.tableName}.{selectedColumn.columnName}</strong>.
            Current value is <strong>{selectedColumn.currentValue.toLocaleString()}</strong>.
          </DialogContentText>
          
          <Alert severity="warning" sx={{ mb: 2 }}>
            Warning: Reseeding an identity column can affect database behavior if not done carefully.
            Consider scheduling this during a maintenance window.
          </Alert>
          
          <TextField
            label="New Seed Value"
            type="number"
            fullWidth
            value={newSeedValue}
            onChange={(e) => setNewSeedValue(e.target.value)}
            disabled={isReseeding}
            sx={{ mt: 2 }}
            helperText="The new seed value should be higher than any existing identity value to avoid duplicates."
          />
          
          {isReseeding && (
            <Box sx={{ width: '100%', mt: 2 }}>
              <LinearProgress />
            </Box>
          )}
          
          {reseedResult && (
            <Alert 
              severity={reseedResult.success ? "success" : "error"}
              sx={{ mt: 2 }}
            >
              {reseedResult.message}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={closeReseedDialog}
            disabled={isReseeding}
          >
            Cancel
          </Button>
          <Button 
            variant="contained" 
            startIcon={<SaveIcon />}
            onClick={reseedIdentityColumn}
            disabled={isReseeding || !newSeedValue}
            color="primary"
          >
            Reseed
          </Button>
        </DialogActions>
      </Dialog>
    );
  };

  return (
    <Box sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4" component="h1">Identity Columns</Typography>
        <Box>
          <Button 
            variant="outlined" 
            startIcon={view === 'table' ? <ChartIcon /> : <TableIcon />}
            onClick={() => setView(view === 'table' ? 'chart' : 'table')}
            sx={{ mr: 1 }}
          >
            {view === 'table' ? 'Chart View' : 'Table View'}
          </Button>
          <Button 
            variant="contained" 
            startIcon={<RefreshIcon />}
            onClick={loadIdentityColumnsData}
            disabled={!selectedDatabase}
          >
            Refresh
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>
      )}

      <Paper sx={{ p: 2, mb: 3 }}>
        <FormControl fullWidth>
          <InputLabel id="database-select-label">Database</InputLabel>
          <Select
            labelId="database-select-label"
            id="database-select"
            value={selectedDatabase}
            label="Database"
            onChange={(e) => setSelectedDatabase(e.target.value)}
          >
            <MenuItem value="">
              <em>Select a database</em>
            </MenuItem>
            {databases.map((db) => (
              <MenuItem key={db} value={db}>{db}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Paper>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
          <CircularProgress />
        </Box>
      ) : selectedDatabase ? (
        view === 'table' ? (
          <Paper>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Table</TableCell>
                    <TableCell>Column</TableCell>
                    <TableCell>Data Type</TableCell>
                    <TableCell>Current Value</TableCell>
                    <TableCell>Seed Value</TableCell>
                    <TableCell>Increment</TableCell>
                    <TableCell>Used %</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {identityColumns.length > 0 ? (
                    identityColumns.map((column, index) => (
                      <TableRow key={index}>
                        <TableCell>{column.schemaName}.{column.tableName}</TableCell>
                        <TableCell>{column.columnName}</TableCell>
                        <TableCell>
                          <Tooltip title={getDataTypeInfo(column.dataType)}>
                            <span>{column.dataType}</span>
                          </Tooltip>
                        </TableCell>
                        <TableCell align="right">{column.currentValue.toLocaleString()}</TableCell>
                        <TableCell align="right">{column.seedValue.toLocaleString()}</TableCell>
                        <TableCell align="right">{column.increment}</TableCell>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <Box sx={{ width: '100%', mr: 1 }}>
                              <LinearProgress 
                                variant="determinate" 
                                value={Math.min(column.percentUsed, 100)} 
                                color={
                                  column.percentUsed > 95 ? 'error' :
                                  column.percentUsed > 80 ? 'warning' :
                                  'primary'
                                }
                              />
                            </Box>
                            {getUsageChip(column.percentUsed)}
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="outlined"
                            size="small"
                            startIcon={<EditIcon />}
                            onClick={() => openReseedDialog(column)}
                          >
                            Reseed
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={8} align="center">
                        No identity columns found in database {selectedDatabase}
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        ) : (
          <Paper sx={{ p: 2 }}>
            {identityColumns.length > 0 ? renderChart() : (
              <Typography variant="body1" align="center">
                No identity columns found in database {selectedDatabase}
              </Typography>
            )}
          </Paper>
        )
      ) : (
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="body1">
            Please select a database to view identity columns
          </Typography>
        </Paper>
      )}

      {issues.length > 0 && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h5" sx={{ mb: 2 }}>Identity Column Issues</Typography>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Object</TableCell>
                  <TableCell>Issue</TableCell>
                  <TableCell>Severity</TableCell>
                  <TableCell>Recommended Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {issues.map((issue, index) => (
                  <TableRow key={index}>
                    <TableCell>{issue.affectedObject}</TableCell>
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

      {renderReseedDialog()}
    </Box>
  );
}

export default IdentityColumns; 