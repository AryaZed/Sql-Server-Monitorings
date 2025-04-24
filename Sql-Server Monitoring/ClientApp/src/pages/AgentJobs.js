import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
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
  DialogActions,
  IconButton,
  Grid,
  TextField,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Check as CheckIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  Sync as SyncIcon,
  PlayArrow as RunIcon,
  Visibility as ViewIcon,
  Timeline as TimelineIcon,
  PowerSettingsNew as PowerIcon,
} from '@mui/icons-material';
import { Chart } from 'react-google-charts';
import { useConnection } from '../context/ConnectionContext';
import agentJobsService from '../services/agentJobsService';

// Helper function to format duration
const formatDuration = (duration) => {
  if (!duration) return 'N/A';
  const hours = Math.floor(duration.totalSeconds / 3600);
  const minutes = Math.floor((duration.totalSeconds % 3600) / 60);
  const seconds = Math.floor(duration.totalSeconds % 60);
  return `${hours > 0 ? hours + 'h ' : ''}${minutes > 0 ? minutes + 'm ' : ''}${seconds}s`;
};

const AgentJobs = () => {
  const { selectedServer } = useConnection();
  const [jobs, setJobs] = useState([]);
  const [jobHistory, setJobHistory] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [tabIndex, setTabIndex] = useState(0);
  const [selectedJob, setSelectedJob] = useState(null);
  const [jobRunDialogOpen, setJobRunDialogOpen] = useState(false);
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState(null);
  const [jobRunInProgress, setJobRunInProgress] = useState(false);
  const [jobRunResult, setJobRunResult] = useState(null);
  const [statsDialogOpen, setStatsDialogOpen] = useState(false);
  const [jobStats, setJobStats] = useState(null);
  const [statsLoading, setStatsLoading] = useState(false);
  const [statsError, setStatsError] = useState(null);
  const [statsDays, setStatsDays] = useState(30);

  useEffect(() => {
    if (selectedServer) {
      loadJobs();
    }
  }, [selectedServer]);

  const loadJobs = async () => {
    if (!selectedServer) return;
    
    setLoading(true);
    setError(null);
    try {
      const allJobs = await agentJobsService.getAgentJobs(selectedServer);
      setJobs(allJobs);
    } catch (err) {
      console.error('Error loading SQL Agent jobs:', err);
      setError('Failed to load SQL Agent jobs. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const loadJobHistory = async (jobName) => {
    setHistoryLoading(true);
    setHistoryError(null);
    try {
      const history = await agentJobsService.getJobHistory(selectedServer, jobName);
      setJobHistory(history);
    } catch (err) {
      console.error('Error loading job history:', err);
      setHistoryError('Failed to load job history. Please try again later.');
    } finally {
      setHistoryLoading(false);
    }
  };

  const handleOpenJobHistory = (job) => {
    setSelectedJob(job);
    loadJobHistory(job.name);
    setHistoryDialogOpen(true);
  };

  const handleRunJob = async () => {
    if (!selectedJob) return;
    
    setJobRunInProgress(true);
    setJobRunResult(null);
    try {
      const result = await agentJobsService.runJob(selectedServer, selectedJob.name);
      setJobRunResult({
        success: true,
        message: `Job '${selectedJob.name}' started successfully.`
      });
      // Refresh job list after a short delay
      setTimeout(loadJobs, 2000);
    } catch (err) {
      console.error('Error running job:', err);
      setJobRunResult({
        success: false,
        message: `Failed to run job: ${err.message || 'Unknown error'}`
      });
    } finally {
      setJobRunInProgress(false);
    }
  };

  const handleTabChange = (event, newValue) => {
    setTabIndex(newValue);
  };

  const getStatusChip = (status) => {
    const statusMap = {
      'Succeeded': { color: 'success', icon: <CheckIcon /> },
      'Failed': { color: 'error', icon: <ErrorIcon /> },
      'Running': { color: 'info', icon: <CircularProgress size={14} /> },
      'Idle': { color: 'default', icon: null },
      'Suspended': { color: 'warning', icon: <WarningIcon /> }
    };
    
    const defaultStatus = { color: 'default', icon: null };
    const { color, icon } = statusMap[status] || defaultStatus;
    
    return (
      <Chip
        label={status}
        color={color}
        size="small"
        icon={icon}
      />
    );
  };

  const handleEnableDisableJob = async (job, enable) => {
    try {
      setLoading(true);
      if (enable) {
        await agentJobsService.enableJob(selectedServer, job.name);
      } else {
        await agentJobsService.disableJob(selectedServer, job.name);
      }
      // Refresh jobs after enabling/disabling
      await loadJobs();
    } catch (err) {
      console.error(`Error ${enable ? 'enabling' : 'disabling'} job:`, err);
      setError(`Failed to ${enable ? 'enable' : 'disable'} job. Please try again later.`);
    } finally {
      setLoading(false);
    }
  };

  const loadJobStats = async (jobName, days = 30) => {
    setStatsLoading(true);
    setStatsError(null);
    try {
      const stats = await agentJobsService.getJobStats(selectedServer, jobName, days);
      setJobStats(stats);
    } catch (err) {
      console.error('Error loading job stats:', err);
      setStatsError('Failed to load job statistics. Please try again later.');
    } finally {
      setStatsLoading(false);
    }
  };

  const handleOpenJobStats = (job) => {
    setSelectedJob(job);
    loadJobStats(job.name, statsDays);
    setStatsDialogOpen(true);
  };

  const handleStatsDaysChange = (event) => {
    const days = event.target.value;
    setStatsDays(days);
    if (selectedJob) {
      loadJobStats(selectedJob.name, days);
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">SQL Server Agent Jobs</Typography>
        <Button
          variant="contained"
          startIcon={<RefreshIcon />}
          onClick={loadJobs}
          disabled={loading}
        >
          Refresh
        </Button>
      </Box>
      
      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}
      
      <Paper sx={{ mb: 3 }}>
        <Tabs value={tabIndex} onChange={handleTabChange}>
          <Tab label="All Jobs" />
          <Tab label="Failed Jobs" />
          <Tab label="Running Jobs" />
        </Tabs>
        
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Job Name</TableCell>
                  <TableCell>Category</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last Run</TableCell>
                  <TableCell>Next Run</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {jobs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      No jobs found
                    </TableCell>
                  </TableRow>
                ) : (
                  jobs
                    .filter(job => {
                      if (tabIndex === 1) return job.lastRunStatus === 'Failed';
                      if (tabIndex === 2) return job.currentStatus === 'Running';
                      return true;
                    })
                    .map(job => (
                      <TableRow key={job.name}>
                        <TableCell>{job.name}</TableCell>
                        <TableCell>{job.category}</TableCell>
                        <TableCell>{getStatusChip(job.currentStatus)}</TableCell>
                        <TableCell>
                          {job.lastRunDate ? new Date(job.lastRunDate).toLocaleString() : 'Never'}
                          {job.lastRunStatus && ` (${job.lastRunStatus})`}
                        </TableCell>
                        <TableCell>
                          {job.nextRunDate ? new Date(job.nextRunDate).toLocaleString() : 'Not scheduled'}
                        </TableCell>
                        <TableCell>
                          <Tooltip title="Run Job">
                            <IconButton
                              onClick={() => {
                                setSelectedJob(job);
                                setJobRunDialogOpen(true);
                              }}
                              disabled={job.currentStatus === 'Running'}
                            >
                              <RunIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="View History">
                            <IconButton onClick={() => handleOpenJobHistory(job)}>
                              <ViewIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="View Statistics">
                            <IconButton onClick={() => handleOpenJobStats(job)}>
                              <TimelineIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title={job.enabled ? "Disable Job" : "Enable Job"}>
                            <IconButton 
                              onClick={() => handleEnableDisableJob(job, !job.enabled)}
                              color={job.enabled ? "error" : "success"}
                            >
                              <PowerIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>
      
      {/* Run Job Dialog */}
      <Dialog open={jobRunDialogOpen} onClose={() => setJobRunDialogOpen(false)}>
        <DialogTitle>Run Job</DialogTitle>
        <DialogContent>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Are you sure you want to run the job <strong>{selectedJob?.name}</strong>?
          </Typography>
          
          {jobRunInProgress && (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
              <CircularProgress />
            </Box>
          )}
          
          {jobRunResult && (
            <Alert severity={jobRunResult.success ? 'success' : 'error'} sx={{ mt: 2 }}>
              {jobRunResult.message}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setJobRunDialogOpen(false)}>Cancel</Button>
          <Button 
            variant="contained" 
            onClick={handleRunJob}
            disabled={jobRunInProgress}
          >
            Run Job
          </Button>
        </DialogActions>
      </Dialog>
      
      {/* Job History Dialog */}
      <Dialog 
        open={historyDialogOpen} 
        onClose={() => setHistoryDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Job History: {selectedJob?.name}</DialogTitle>
        <DialogContent>
          {historyLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
              <CircularProgress />
            </Box>
          ) : historyError ? (
            <Alert severity="error">{historyError}</Alert>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Run Date</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Duration</TableCell>
                    <TableCell>Message</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {jobHistory.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} align="center">
                        No history available
                      </TableCell>
                    </TableRow>
                  ) : (
                    jobHistory.map((history, index) => (
                      <TableRow key={index}>
                        <TableCell>{new Date(history.runDate).toLocaleString()}</TableCell>
                        <TableCell>{getStatusChip(history.status)}</TableCell>
                        <TableCell>{history.duration}</TableCell>
                        <TableCell>{history.message}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setHistoryDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Job Statistics Dialog */}
      <Dialog
        open={statsDialogOpen}
        onClose={() => setStatsDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          {selectedJob && `Job Statistics: ${selectedJob.name}`}
        </DialogTitle>
        <DialogContent>
          {statsError && <Alert severity="error" sx={{ mb: 2 }}>{statsError}</Alert>}
          
          <Box sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
            <FormControl sx={{ minWidth: 120, mr: 2 }}>
              <InputLabel>Time Period</InputLabel>
              <TextField
                select
                value={statsDays}
                onChange={handleStatsDaysChange}
                label="Time Period"
                variant="outlined"
                size="small"
              >
                <MenuItem value={7}>Last 7 days</MenuItem>
                <MenuItem value={14}>Last 14 days</MenuItem>
                <MenuItem value={30}>Last 30 days</MenuItem>
                <MenuItem value={60}>Last 60 days</MenuItem>
                <MenuItem value={90}>Last 90 days</MenuItem>
              </TextField>
            </FormControl>
            <Button 
              variant="outlined" 
              startIcon={<RefreshIcon />}
              onClick={() => selectedJob && loadJobStats(selectedJob.name, statsDays)}
              disabled={statsLoading}
            >
              Refresh
            </Button>
          </Box>
          
          {statsLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
              <CircularProgress />
            </Box>
          ) : jobStats ? (
            <Box>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Paper sx={{ p: 2 }}>
                    <Typography variant="h6" gutterBottom>Execution Summary</Typography>
                    <Table size="small">
                      <TableBody>
                        <TableRow>
                          <TableCell>Total Executions</TableCell>
                          <TableCell>{jobStats.totalExecutions || 0}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Successful Executions</TableCell>
                          <TableCell>{jobStats.successfulExecutions || 0}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Failed Executions</TableCell>
                          <TableCell>{jobStats.failedExecutions || 0}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Success Rate</TableCell>
                          <TableCell>
                            {jobStats.totalExecutions ? 
                              `${((jobStats.successfulExecutions / jobStats.totalExecutions) * 100).toFixed(2)}%` : 
                              'N/A'}
                          </TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Average Duration</TableCell>
                          <TableCell>{formatDuration(jobStats.averageDuration)}</TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </Paper>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Paper sx={{ p: 2, height: '100%' }}>
                    <Typography variant="h6" gutterBottom>Status Distribution</Typography>
                    {jobStats.totalExecutions > 0 ? (
                      <Chart
                        chartType="PieChart"
                        data={[
                          ['Status', 'Count'],
                          ['Succeeded', jobStats.successfulExecutions || 0],
                          ['Failed', jobStats.failedExecutions || 0],
                          ['Other', (jobStats.totalExecutions - (jobStats.successfulExecutions + jobStats.failedExecutions)) || 0]
                        ]}
                        options={{
                          colors: ['#4caf50', '#f44336', '#ff9800'],
                          legend: { position: 'bottom' },
                          chartArea: { width: '80%', height: '80%' }
                        }}
                        width="100%"
                        height="200px"
                      />
                    ) : (
                      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px' }}>
                        <Typography variant="body2" color="text.secondary">No data available</Typography>
                      </Box>
                    )}
                  </Paper>
                </Grid>
                {jobStats.executionTrend && (
                  <Grid item xs={12}>
                    <Paper sx={{ p: 2 }}>
                      <Typography variant="h6" gutterBottom>Execution Trend</Typography>
                      <Chart
                        chartType="LineChart"
                        data={[
                          ['Date', 'Duration (seconds)', 'Status'],
                          ...jobStats.executionTrend.map(point => [
                            new Date(point.date).toLocaleDateString(),
                            point.durationSeconds || 0,
                            point.status
                          ])
                        ]}
                        options={{
                          hAxis: { title: 'Date' },
                          vAxis: { title: 'Duration (seconds)' },
                          legend: { position: 'bottom' },
                          pointSize: 5,
                          seriesType: 'line',
                        }}
                        width="100%"
                        height="300px"
                      />
                    </Paper>
                  </Grid>
                )}
              </Grid>
            </Box>
          ) : (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
              <Typography variant="body1">No statistics available</Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setStatsDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AgentJobs; 