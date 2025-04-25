import React, { useEffect } from 'react';
import { Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme, CssBaseline, Box, Toolbar } from '@mui/material';
import Navigation from './components/Navigation';
import Dashboard from './pages/Dashboard';
import MonitoringDashboard from './pages/MonitoringDashboard';
import Servers from './pages/Servers';
import Performance from './pages/Performance';
import Alerts from './pages/Alerts';
import Settings from './pages/Settings';
import UserManagement from './pages/UserManagement';
import NotFound from './pages/NotFound';
import AgentJobs from './pages/AgentJobs';
import DbccChecks from './pages/DbccChecks';
import IdentityColumns from './pages/IdentityColumns';
import DatabaseList from './pages/DatabaseList';
import DatabaseDetails from './pages/DatabaseDetails';
import QueryAnalyzer from './pages/QueryAnalyzer';
import Monitoring from './pages/Monitoring';
import { useConnection } from './context/ConnectionContext';
import signalRService from './services/signalrService';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f5f5f5',
    },
  },
});

function App() {
  // Initialize SignalR when the app starts
  useEffect(() => {
    // Start the SignalR connection when the app loads
    const initSignalR = async () => {
      try {
        await signalRService.start();
      } catch (err) {
        console.error('Failed to start SignalR connection:', err);
      }
    };
    
    initSignalR();
    
    // Clean up on unmount
    return () => {
      signalRService.stop();
    };
  }, []);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ display: 'flex' }}>
        <Navigation />
        <Box component="main" sx={{ flexGrow: 1, p: 3, width: '100%' }}>
          <Toolbar />
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/monitoring" element={<MonitoringDashboard />} />
            <Route path="/servers" element={<Servers />} />
            <Route path="/databases" element={<DatabaseList />} />
            <Route path="/databases/:id" element={<DatabaseDetails />} />
            <Route path="/performance" element={<Performance />} />
            <Route path="/queries" element={<QueryAnalyzer />} />
            <Route path="/issues" element={<Monitoring />} />
            <Route path="/backups" element={<Monitoring />} />
            <Route path="/agent-jobs" element={<AgentJobs />} />
            <Route path="/dbcc-checks" element={<DbccChecks />} />
            <Route path="/identity-columns" element={<IdentityColumns />} />
            <Route path="/availability-groups" element={<NotFound />} />
            <Route path="/log-shipping" element={<NotFound />} />
            <Route path="/mirroring" element={<NotFound />} />
            <Route path="/alerts" element={<Alerts />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/users" element={<UserManagement />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </Box>
      </Box>
    </ThemeProvider>
  );
}

export default App; 