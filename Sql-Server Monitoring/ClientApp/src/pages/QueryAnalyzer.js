import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Divider,
  List,
  ListItem,
  ListItemText,
  Chip,
  CircularProgress,
  Tab,
  Tabs,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
  Alert,
} from '@mui/material';
import {
  PlayArrow as RunIcon,
  Delete as ClearIcon,
  Save as SaveIcon,
  ContentCopy as CopyIcon,
  Help as HelpIcon,
  Check as CheckIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { queries } from '../services/api';

function QueryAnalyzer() {
  const { connectionString } = useConnection();
  const [query, setQuery] = useState('');
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [slowQueries, setSlowQueries] = useState([]);
  const [analysisResult, setAnalysisResult] = useState(null);
  const [selectedTabIndex, setSelectedTabIndex] = useState(0);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  
  useEffect(() => {
    if (!connectionString) return;
    
    const fetchSlowQueries = async () => {
      setLoading(true);
      try {
        // In a real app, get data from API
        // const response = await queries.getSlowQueries(connectionString);
        // setSlowQueries(response.data);
        
        // For demo, mock data
        setTimeout(() => {
          setSlowQueries([
            {
              id: 1,
              text: 'SELECT p.*, c.CategoryName FROM Products p JOIN Categories c ON p.CategoryID = c.CategoryID WHERE p.UnitPrice > 50',
              durationMs: 2500,
              cpuTime: 1200,
              logicalReads: 5860,
              executionCount: 245,
              lastExecutionTime: '2023-06-15T14:23:45Z'
            },
            {
              id: 2,
              text: 'SELECT o.*, c.CompanyName, e.LastName FROM Orders o JOIN Customers c ON o.CustomerID = c.CustomerID JOIN Employees e ON o.EmployeeID = e.EmployeeID WHERE o.OrderDate > @date',
              durationMs: 3200,
              cpuTime: 1850,
              logicalReads: 8740,
              executionCount: 325,
              lastExecutionTime: '2023-06-15T12:35:22Z'
            },
            {
              id: 3,
              text: 'SELECT * FROM OrderDetails WHERE Quantity > 50',
              durationMs: 1800,
              cpuTime: 950,
              logicalReads: 3420,
              executionCount: 128,
              lastExecutionTime: '2023-06-15T10:12:18Z'
            },
            {
              id: 4,
              text: 'UPDATE Products SET UnitPrice = UnitPrice * 1.1 WHERE CategoryID = 5',
              durationMs: 1500,
              cpuTime: 780,
              logicalReads: 2150,
              executionCount: 45,
              lastExecutionTime: '2023-06-14T16:45:30Z'
            }
          ]);
          setLoading(false);
        }, 1000);
      } catch (err) {
        console.error('Error fetching slow queries:', err);
        setError('Failed to fetch slow queries. Please try again.');
        setLoading(false);
      }
    };
    
    fetchSlowQueries();
  }, [connectionString]);
  
  const handleQueryChange = (e) => {
    setQuery(e.target.value);
  };
  
  const handleClear = () => {
    setQuery('');
    setAnalysisResult(null);
  };
  
  const handleTabChange = (event, newValue) => {
    setSelectedTabIndex(newValue);
  };
  
  const handleUseQuery = (queryText) => {
    setQuery(queryText);
    setSelectedTabIndex(0);
  };
  
  const handleCopyQuery = (queryText) => {
    navigator.clipboard.writeText(queryText);
  };
  
  const handleAnalyzeQuery = async () => {
    if (!query.trim()) {
      setError('Please enter a SQL query to analyze.');
      return;
    }
    
    setIsAnalyzing(true);
    setError('');
    
    try {
      // In a real app, call API
      // const response = await queries.analyzeQuery(query, connectionString);
      // setAnalysisResult(response.data);
      
      // For demo, mock analysis result
      setTimeout(() => {
        // Mock SQL query analysis result
        const mockedAnalysisResult = {
          executionStats: {
            estimatedCost: 2.45,
            estimatedRows: 357,
            executionTime: 1250, // in ms
            cpuTime: 850, // in ms
            logicalReads: 3560,
            physicalReads: 12,
          },
          queryPlan: {
            operators: [
              { 
                name: 'Hash Match', 
                cost: 0.85, 
                estimatedRows: 357,
                warnings: ['Spills to TempDB'] 
              },
              { 
                name: 'Clustered Index Scan', 
                cost: 0.65, 
                estimatedRows: 525,
                table: 'Products',
                warnings: [],
              },
              { 
                name: 'Index Seek', 
                cost: 0.35, 
                estimatedRows: 25,
                table: 'Categories',
                index: 'PK_Categories',
                warnings: []
              }
            ],
            missingIndexes: [
              {
                table: 'Products',
                impact: 85.4,
                createStatement: 'CREATE NONCLUSTERED INDEX IX_Products_UnitPrice ON Products(UnitPrice) INCLUDE (ProductName, CategoryID)'
              }
            ]
          },
          recommendations: [
            {
              type: 'Missing Index',
              severity: 'High',
              description: 'Creating an index on UnitPrice column could improve query performance by 85%',
              script: 'CREATE NONCLUSTERED INDEX IX_Products_UnitPrice ON Products(UnitPrice) INCLUDE (ProductName, CategoryID)'
            },
            {
              type: 'Join Algorithm',
              severity: 'Medium',
              description: 'Consider rewriting the query to use a MERGE JOIN instead of HASH JOIN to avoid TempDB spills',
              script: null
            },
            {
              type: 'Column Selection',
              severity: 'Low',
              description: 'Avoid using SELECT * and explicitly specify required columns to reduce I/O',
              script: null
            }
          ]
        };
        
        setAnalysisResult(mockedAnalysisResult);
        setIsAnalyzing(false);
      }, 2500);
    } catch (err) {
      console.error('Error analyzing query:', err);
      setError('Failed to analyze query. Please check the syntax and try again.');
      setIsAnalyzing(false);
    }
  };
  
  const renderSeverityChip = (severity) => {
    const color = severity === 'High' ? 'error' : severity === 'Medium' ? 'warning' : 'info';
    return <Chip size="small" label={severity} color={color} />;
  };
  
  return (
    <Box className="dashboard-container">
      <Typography variant="h4" component="h1" gutterBottom>
        Query Analyzer
      </Typography>
      
      <Tabs
        value={selectedTabIndex}
        onChange={handleTabChange}
        aria-label="query analyzer tabs"
        sx={{ mb: 2 }}
      >
        <Tab label="Analyze Query" />
        <Tab label="Slow Queries" />
      </Tabs>
      
      {/* Error message */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Analyze Query Tab */}
      {selectedTabIndex === 0 && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Paper sx={{ p: 2 }}>
              <TextField
                fullWidth
                multiline
                rows={6}
                label="SQL Query"
                placeholder="Enter your SQL query here..."
                variant="outlined"
                value={query}
                onChange={handleQueryChange}
                disabled={isAnalyzing}
              />
              <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button
                  variant="outlined"
                  startIcon={<ClearIcon />}
                  onClick={handleClear}
                  sx={{ mr: 1 }}
                  disabled={!query || isAnalyzing}
                >
                  Clear
                </Button>
                <Button
                  variant="contained"
                  startIcon={isAnalyzing ? <CircularProgress size={20} /> : <RunIcon />}
                  onClick={handleAnalyzeQuery}
                  disabled={!query || isAnalyzing}
                >
                  {isAnalyzing ? 'Analyzing...' : 'Analyze Query'}
                </Button>
              </Box>
            </Paper>
          </Grid>
          
          {analysisResult && (
            <>
              <Grid item xs={12} md={6}>
                <Card>
                  <CardHeader title="Execution Statistics" />
                  <Divider />
                  <CardContent>
                    <TableContainer>
                      <Table size="small">
                        <TableBody>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              Estimated Cost
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.estimatedCost}</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              Estimated Rows
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.estimatedRows}</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              Execution Time
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.executionTime} ms</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              CPU Time
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.cpuTime} ms</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              Logical Reads
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.logicalReads}</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell component="th" sx={{ fontWeight: 'bold' }}>
                              Physical Reads
                            </TableCell>
                            <TableCell align="right">{analysisResult.executionStats.physicalReads}</TableCell>
                          </TableRow>
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </CardContent>
                </Card>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Card sx={{ height: '100%' }}>
                  <CardHeader title="Recommendations" />
                  <Divider />
                  <CardContent sx={{ p: 0 }}>
                    <List dense>
                      {analysisResult.recommendations.map((rec, index) => (
                        <ListItem 
                          key={index}
                          secondaryAction={
                            rec.script && (
                              <Tooltip title="Copy script">
                                <IconButton edge="end" onClick={() => handleCopyQuery(rec.script)}>
                                  <CopyIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )
                          }
                        >
                          <ListItemText
                            primary={
                              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                <Typography variant="subtitle2">{rec.type}</Typography>
                                <Box sx={{ ml: 1 }}>{renderSeverityChip(rec.severity)}</Box>
                              </Box>
                            }
                            secondary={rec.description}
                          />
                        </ListItem>
                      ))}
                    </List>
                  </CardContent>
                </Card>
              </Grid>
              
              <Grid item xs={12}>
                <Card>
                  <CardHeader 
                    title="Query Plan Operators" 
                    subheader="Execution plan operators and their statistics"
                  />
                  <Divider />
                  <CardContent>
                    <TableContainer>
                      <Table>
                        <TableHead>
                          <TableRow>
                            <TableCell>Operator</TableCell>
                            <TableCell>Table</TableCell>
                            <TableCell align="right">Est. Cost</TableCell>
                            <TableCell align="right">Est. Rows</TableCell>
                            <TableCell>Warnings</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {analysisResult.queryPlan.operators.map((op, index) => (
                            <TableRow key={index}>
                              <TableCell>{op.name}</TableCell>
                              <TableCell>{op.table || '—'}</TableCell>
                              <TableCell align="right">{op.cost}</TableCell>
                              <TableCell align="right">{op.estimatedRows}</TableCell>
                              <TableCell>
                                {op.warnings.length > 0 ? (
                                  <Chip
                                    icon={<WarningIcon />}
                                    label={op.warnings[0]}
                                    size="small"
                                    color="warning"
                                  />
                                ) : (
                                  <Chip
                                    icon={<CheckIcon />}
                                    label="No issues"
                                    size="small"
                                    color="success"
                                  />
                                )}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </CardContent>
                </Card>
              </Grid>
              
              {analysisResult.queryPlan.missingIndexes.length > 0 && (
                <Grid item xs={12}>
                  <Card>
                    <CardHeader 
                      title="Missing Indexes" 
                      action={
                        <Tooltip title="Missing indexes can significantly improve query performance">
                          <IconButton>
                            <HelpIcon />
                          </IconButton>
                        </Tooltip>
                      }
                    />
                    <Divider />
                    <CardContent>
                      {analysisResult.queryPlan.missingIndexes.map((index, i) => (
                        <Box key={i} sx={{ mb: 2 }}>
                          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                            <Typography variant="subtitle1">
                              Missing index on {index.table}
                            </Typography>
                            <Chip 
                              label={`Impact: ${index.impact.toFixed(1)}%`} 
                              color="error" 
                              size="small"
                              sx={{ ml: 2 }}
                            />
                          </Box>
                          <Paper sx={{ p: 1.5, bgcolor: 'grey.100' }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                              <Typography variant="body2" component="code" sx={{ fontFamily: 'monospace' }}>
                                {index.createStatement}
                              </Typography>
                              <Tooltip title="Copy script">
                                <IconButton size="small" onClick={() => handleCopyQuery(index.createStatement)}>
                                  <CopyIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            </Box>
                          </Paper>
                        </Box>
                      ))}
                    </CardContent>
                  </Card>
                </Grid>
              )}
            </>
          )}
        </Grid>
      )}
      
      {/* Slow Queries Tab */}
      {selectedTabIndex === 1 && (
        <>
          {loading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <>
              <Alert severity="info" sx={{ mb: 3 }}>
                These are the top resource-intensive queries detected in your SQL Server. Click on a query to analyze it.
              </Alert>
              
              <TableContainer component={Paper}>
                <Table sx={{ minWidth: 650 }}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Query Text</TableCell>
                      <TableCell align="right">Duration (ms)</TableCell>
                      <TableCell align="right">CPU Time (ms)</TableCell>
                      <TableCell align="right">Logical Reads</TableCell>
                      <TableCell align="right">Executions</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {slowQueries.map((query) => (
                      <TableRow key={query.id}>
                        <TableCell>
                          <Typography variant="body2" noWrap sx={{ maxWidth: 500 }}>
                            {query.text}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">{query.durationMs}</TableCell>
                        <TableCell align="right">{query.cpuTime}</TableCell>
                        <TableCell align="right">{query.logicalReads}</TableCell>
                        <TableCell align="right">{query.executionCount}</TableCell>
                        <TableCell>
                          <Tooltip title="Analyze this query">
                            <IconButton size="small" onClick={() => handleUseQuery(query.text)}>
                              <InfoIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Copy query">
                            <IconButton size="small" onClick={() => handleCopyQuery(query.text)}>
                              <CopyIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </>
          )}
        </>
      )}
    </Box>
  );
}

export default QueryAnalyzer; 