import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Divider,
  TextField,
  Button,
  FormControlLabel,
  Switch,
  Alert,
  Snackbar,
  InputAdornment,
  CircularProgress,
  Paper,
  Tabs,
  Tab,
} from '@mui/material';
import {
  Save as SaveIcon,
  Notifications as NotificationsIcon,
  Storage as StorageIcon,
  Security as SecurityIcon,
  Assessment as AssessmentIcon,
  Email as EmailIcon,
} from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';
import { monitoring } from '../services/api';

function TabPanel(props) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`settings-tabpanel-${index}`}
      aria-labelledby={`settings-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

function a11yProps(index) {
  return {
    id: `settings-tab-${index}`,
    'aria-controls': `settings-tabpanel-${index}`,
  };
}

function Settings() {
  const { connectionString } = useConnection();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [activeTab, setActiveTab] = useState(0);
  
  // Settings state
  const [generalSettings, setGeneralSettings] = useState({
    serverRefreshInterval: 30,
    dashboardRefreshInterval: 60,
    retentionPeriod: 30,
    timeZone: 'UTC',
    dateFormat: 'yyyy-MM-dd HH:mm:ss',
    theme: 'light',
  });
  
  const [monitoringSettings, setMonitoringSettings] = useState({
    cpuAlertThreshold: 85,
    memoryAlertThreshold: 90,
    diskSpaceAlertThreshold: 90,
    longRunningQueryThreshold: 300,
    deadlockMonitoringEnabled: true,
    blockingMonitoringEnabled: true,
    connectionMonitoringEnabled: true,
  });
  
  const [alertSettings, setAlertSettings] = useState({
    emailAlertsEnabled: true,
    emailRecipients: 'admin@example.com',
    slackAlertsEnabled: false,
    slackWebhook: '',
    alertCooldownMinutes: 15,
    criticalAlertsOnly: false,
    dailySummaryEnabled: true,
    dailySummaryTime: '08:00',
  });
  
  const [backupSettings, setBackupSettings] = useState({
    autoBackupEnabled: false,
    fullBackupSchedule: '0 0 * * 0', // Cron expression: Sunday at midnight
    differentialBackupSchedule: '0 0 * * 1-6', // Cron expression: Monday-Saturday at midnight
    logBackupSchedule: '0 */6 * * *', // Cron expression: Every 6 hours
    backupLocation: 'C:\\SQLBackups',
    retentionDays: 30,
    compressionEnabled: true,
    verifyBackup: true,
    backupEncryptionEnabled: false,
  });
  
  useEffect(() => {
    if (!connectionString) return;
    
    const fetchSettings = async () => {
      setLoading(true);
      try {
        // In a real app, call API to get settings
        // const response = await monitoring.getSettings();
        // const settings = response.data;
        // setGeneralSettings(settings.general);
        // setMonitoringSettings(settings.monitoring);
        // setAlertSettings(settings.alerts);
        // setBackupSettings(settings.backups);
        
        // For demo, use the default values set above
        setTimeout(() => {
          setLoading(false);
        }, 1000);
      } catch (err) {
        console.error('Error fetching settings:', err);
        setError('Failed to fetch settings. Please try again.');
        setLoading(false);
      }
    };
    
    fetchSettings();
  }, [connectionString]);
  
  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };
  
  const handleGeneralSettingsChange = (e) => {
    const { name, value, type, checked } = e.target;
    setGeneralSettings(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };
  
  const handleMonitoringSettingsChange = (e) => {
    const { name, value, type, checked } = e.target;
    setMonitoringSettings(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : type === 'number' ? parseInt(value, 10) : value
    }));
  };
  
  const handleAlertSettingsChange = (e) => {
    const { name, value, type, checked } = e.target;
    setAlertSettings(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };
  
  const handleBackupSettingsChange = (e) => {
    const { name, value, type, checked } = e.target;
    setBackupSettings(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };
  
  const handleSaveSettings = async () => {
    setSaving(true);
    setError('');
    
    try {
      // In a real app, call API to save settings
      // await monitoring.updateSettings({
      //   general: generalSettings,
      //   monitoring: monitoringSettings,
      //   alerts: alertSettings,
      //   backups: backupSettings
      // });
      
      // For demo, simulate API call
      setTimeout(() => {
        setSaving(false);
        setSuccessMessage('Settings saved successfully');
      }, 1000);
    } catch (err) {
      console.error('Error saving settings:', err);
      setError('Failed to save settings. Please try again.');
      setSaving(false);
    }
  };
  
  const handleCloseSnackbar = () => {
    setSuccessMessage('');
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
        Settings
      </Typography>
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      <Paper sx={{ mb: 3 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange} aria-label="settings tabs">
            <Tab label="General" icon={<AssessmentIcon />} iconPosition="start" {...a11yProps(0)} />
            <Tab label="Monitoring" icon={<StorageIcon />} iconPosition="start" {...a11yProps(1)} />
            <Tab label="Alerts" icon={<NotificationsIcon />} iconPosition="start" {...a11yProps(2)} />
            <Tab label="Backups" icon={<SecurityIcon />} iconPosition="start" {...a11yProps(3)} />
          </Tabs>
        </Box>
        
        {/* General Settings */}
        <TabPanel value={activeTab} index={0}>
          <Card>
            <CardHeader title="General Settings" />
            <Divider />
            <CardContent>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Server Refresh Interval (seconds)"
                    name="serverRefreshInterval"
                    type="number"
                    value={generalSettings.serverRefreshInterval}
                    onChange={handleGeneralSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 5, max: 3600 },
                      endAdornment: <InputAdornment position="end">sec</InputAdornment>
                    }}
                    margin="normal"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Dashboard Refresh Interval (seconds)"
                    name="dashboardRefreshInterval"
                    type="number"
                    value={generalSettings.dashboardRefreshInterval}
                    onChange={handleGeneralSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 10, max: 3600 },
                      endAdornment: <InputAdornment position="end">sec</InputAdornment>
                    }}
                    margin="normal"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Data Retention Period (days)"
                    name="retentionPeriod"
                    type="number"
                    value={generalSettings.retentionPeriod}
                    onChange={handleGeneralSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1, max: 365 },
                      endAdornment: <InputAdornment position="end">days</InputAdornment>
                    }}
                    margin="normal"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Time Zone"
                    name="timeZone"
                    select
                    value={generalSettings.timeZone}
                    onChange={handleGeneralSettingsChange}
                    SelectProps={{
                      native: true,
                    }}
                    margin="normal"
                  >
                    <option value="UTC">UTC</option>
                    <option value="America/New_York">Eastern Time (ET)</option>
                    <option value="America/Chicago">Central Time (CT)</option>
                    <option value="America/Denver">Mountain Time (MT)</option>
                    <option value="America/Los_Angeles">Pacific Time (PT)</option>
                    <option value="Europe/London">London (GMT)</option>
                    <option value="Europe/Paris">Paris (CET)</option>
                  </TextField>
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Date Format"
                    name="dateFormat"
                    select
                    value={generalSettings.dateFormat}
                    onChange={handleGeneralSettingsChange}
                    SelectProps={{
                      native: true,
                    }}
                    margin="normal"
                  >
                    <option value="yyyy-MM-dd HH:mm:ss">2023-06-15 14:30:00 (ISO)</option>
                    <option value="MM/dd/yyyy h:mm:ss a">06/15/2023 2:30:00 PM (US)</option>
                    <option value="dd/MM/yyyy HH:mm:ss">15/06/2023 14:30:00 (EU)</option>
                  </TextField>
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Theme"
                    name="theme"
                    select
                    value={generalSettings.theme}
                    onChange={handleGeneralSettingsChange}
                    SelectProps={{
                      native: true,
                    }}
                    margin="normal"
                  >
                    <option value="light">Light</option>
                    <option value="dark">Dark</option>
                    <option value="system">Use System Theme</option>
                  </TextField>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </TabPanel>
        
        {/* Monitoring Settings */}
        <TabPanel value={activeTab} index={1}>
          <Card>
            <CardHeader title="Monitoring Settings" />
            <Divider />
            <CardContent>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="CPU Alert Threshold (%)"
                    name="cpuAlertThreshold"
                    type="number"
                    value={monitoringSettings.cpuAlertThreshold}
                    onChange={handleMonitoringSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1, max: 100 },
                      endAdornment: <InputAdornment position="end">%</InputAdornment>
                    }}
                    margin="normal"
                    helperText="Alert will be triggered when CPU usage exceeds this threshold"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Memory Alert Threshold (%)"
                    name="memoryAlertThreshold"
                    type="number"
                    value={monitoringSettings.memoryAlertThreshold}
                    onChange={handleMonitoringSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1, max: 100 },
                      endAdornment: <InputAdornment position="end">%</InputAdornment>
                    }}
                    margin="normal"
                    helperText="Alert will be triggered when memory usage exceeds this threshold"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Disk Space Alert Threshold (%)"
                    name="diskSpaceAlertThreshold"
                    type="number"
                    value={monitoringSettings.diskSpaceAlertThreshold}
                    onChange={handleMonitoringSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1, max: 100 },
                      endAdornment: <InputAdornment position="end">%</InputAdornment>
                    }}
                    margin="normal"
                    helperText="Alert will be triggered when disk usage exceeds this threshold"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Long Running Query Threshold (seconds)"
                    name="longRunningQueryThreshold"
                    type="number"
                    value={monitoringSettings.longRunningQueryThreshold}
                    onChange={handleMonitoringSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1 },
                      endAdornment: <InputAdornment position="end">sec</InputAdornment>
                    }}
                    margin="normal"
                    helperText="Queries running longer than this will be flagged"
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={monitoringSettings.deadlockMonitoringEnabled}
                        onChange={handleMonitoringSettingsChange}
                        name="deadlockMonitoringEnabled"
                        color="primary"
                      />
                    }
                    label="Monitor Deadlocks"
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={monitoringSettings.blockingMonitoringEnabled}
                        onChange={handleMonitoringSettingsChange}
                        name="blockingMonitoringEnabled"
                        color="primary"
                      />
                    }
                    label="Monitor Blocking"
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={monitoringSettings.connectionMonitoringEnabled}
                        onChange={handleMonitoringSettingsChange}
                        name="connectionMonitoringEnabled"
                        color="primary"
                      />
                    }
                    label="Monitor Connections"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </TabPanel>
        
        {/* Alert Settings */}
        <TabPanel value={activeTab} index={2}>
          <Card>
            <CardHeader title="Alert Settings" />
            <Divider />
            <CardContent>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <FormControlLabel
                      control={
                        <Switch
                          checked={alertSettings.emailAlertsEnabled}
                          onChange={handleAlertSettingsChange}
                          name="emailAlertsEnabled"
                          color="primary"
                        />
                      }
                      label="Enable Email Alerts"
                    />
                    <EmailIcon color={alertSettings.emailAlertsEnabled ? 'primary' : 'disabled'} />
                  </Box>
                  <TextField
                    fullWidth
                    label="Email Recipients"
                    name="emailRecipients"
                    value={alertSettings.emailRecipients}
                    onChange={handleAlertSettingsChange}
                    disabled={!alertSettings.emailAlertsEnabled}
                    helperText="Separate multiple emails with commas"
                    margin="normal"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <FormControlLabel
                      control={
                        <Switch
                          checked={alertSettings.slackAlertsEnabled}
                          onChange={handleAlertSettingsChange}
                          name="slackAlertsEnabled"
                          color="primary"
                        />
                      }
                      label="Enable Slack Alerts"
                    />
                  </Box>
                  <TextField
                    fullWidth
                    label="Slack Webhook URL"
                    name="slackWebhook"
                    value={alertSettings.slackWebhook}
                    onChange={handleAlertSettingsChange}
                    disabled={!alertSettings.slackAlertsEnabled}
                    margin="normal"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Alert Cooldown (minutes)"
                    name="alertCooldownMinutes"
                    type="number"
                    value={alertSettings.alertCooldownMinutes}
                    onChange={handleAlertSettingsChange}
                    InputProps={{ 
                      inputProps: { min: 1, max: 1440 },
                      endAdornment: <InputAdornment position="end">min</InputAdornment>
                    }}
                    margin="normal"
                    helperText="Minimum time between repeated alerts"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={alertSettings.criticalAlertsOnly}
                        onChange={handleAlertSettingsChange}
                        name="criticalAlertsOnly"
                        color="primary"
                      />
                    }
                    label="Send Critical Alerts Only"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={alertSettings.dailySummaryEnabled}
                        onChange={handleAlertSettingsChange}
                        name="dailySummaryEnabled"
                        color="primary"
                      />
                    }
                    label="Send Daily Summary Email"
                  />
                </Grid>
                {alertSettings.dailySummaryEnabled && (
                  <Grid item xs={12} md={6}>
                    <TextField
                      fullWidth
                      label="Daily Summary Time"
                      name="dailySummaryTime"
                      type="time"
                      value={alertSettings.dailySummaryTime}
                      onChange={handleAlertSettingsChange}
                      margin="normal"
                      helperText="Time to send daily summary (server time)"
                    />
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </TabPanel>
        
        {/* Backup Settings */}
        <TabPanel value={activeTab} index={3}>
          <Card>
            <CardHeader title="Backup Settings" />
            <Divider />
            <CardContent>
              <Grid container spacing={3}>
                <Grid item xs={12}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={backupSettings.autoBackupEnabled}
                        onChange={handleBackupSettingsChange}
                        name="autoBackupEnabled"
                        color="primary"
                      />
                    }
                    label="Enable Automatic Backups"
                  />
                </Grid>
                
                {backupSettings.autoBackupEnabled && (
                  <>
                    <Grid item xs={12} md={4}>
                      <TextField
                        fullWidth
                        label="Full Backup Schedule (Cron)"
                        name="fullBackupSchedule"
                        value={backupSettings.fullBackupSchedule}
                        onChange={handleBackupSettingsChange}
                        margin="normal"
                        helperText="e.g. 0 0 * * 0 (every Sunday at midnight)"
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <TextField
                        fullWidth
                        label="Differential Backup Schedule (Cron)"
                        name="differentialBackupSchedule"
                        value={backupSettings.differentialBackupSchedule}
                        onChange={handleBackupSettingsChange}
                        margin="normal"
                        helperText="e.g. 0 0 * * 1-6 (Mon-Sat at midnight)"
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <TextField
                        fullWidth
                        label="Log Backup Schedule (Cron)"
                        name="logBackupSchedule"
                        value={backupSettings.logBackupSchedule}
                        onChange={handleBackupSettingsChange}
                        margin="normal"
                        helperText="e.g. 0 */6 * * * (every 6 hours)"
                      />
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <TextField
                        fullWidth
                        label="Backup Location"
                        name="backupLocation"
                        value={backupSettings.backupLocation}
                        onChange={handleBackupSettingsChange}
                        margin="normal"
                        helperText="Path where backups will be stored"
                      />
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <TextField
                        fullWidth
                        label="Retention Period (days)"
                        name="retentionDays"
                        type="number"
                        value={backupSettings.retentionDays}
                        onChange={handleBackupSettingsChange}
                        InputProps={{ 
                          inputProps: { min: 1, max: 3650 },
                          endAdornment: <InputAdornment position="end">days</InputAdornment>
                        }}
                        margin="normal"
                        helperText="How long to keep backups"
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={backupSettings.compressionEnabled}
                            onChange={handleBackupSettingsChange}
                            name="compressionEnabled"
                            color="primary"
                          />
                        }
                        label="Enable Compression"
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={backupSettings.verifyBackup}
                            onChange={handleBackupSettingsChange}
                            name="verifyBackup"
                            color="primary"
                          />
                        }
                        label="Verify Backups"
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={backupSettings.backupEncryptionEnabled}
                            onChange={handleBackupSettingsChange}
                            name="backupEncryptionEnabled"
                            color="primary"
                          />
                        }
                        label="Enable Encryption"
                      />
                    </Grid>
                  </>
                )}
              </Grid>
            </CardContent>
          </Card>
        </TabPanel>
      </Paper>
      
      <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          color="primary"
          startIcon={saving ? <CircularProgress size={20} /> : <SaveIcon />}
          onClick={handleSaveSettings}
          disabled={saving}
        >
          {saving ? 'Saving...' : 'Save Settings'}
        </Button>
      </Box>
      
      <Snackbar 
        open={!!successMessage} 
        autoHideDuration={5000} 
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleCloseSnackbar} severity="success">
          {successMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
}

export default Settings; 