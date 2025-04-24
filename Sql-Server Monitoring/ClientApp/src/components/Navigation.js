import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  Box,
  Drawer,
  AppBar,
  Toolbar,
  List,
  Typography,
  Divider,
  IconButton,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tooltip,
  Avatar,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Storage as StorageIcon,
  Equalizer as EqualizerIcon,
  Notifications as NotificationsIcon,
  Settings as SettingsIcon,
  People as PeopleIcon,
  Help as HelpIcon,
  AccountCircle as AccountCircleIcon,
  Logout as LogoutIcon,
  MonitorHeartOutlined as MonitoringIcon,
  Speed as SpeedIcon,
  Database as DatabaseIcon,
  Code as CodeIcon,
  Warning as WarningIcon,
  Backup as BackupIcon,
  Work as AgentJobsIcon,
  VerifiedUser as DbccCheckIcon,
  Key as IdentityColumnIcon,
  Hub as AvailabilityGroupIcon,
  Compare as LogShippingIcon,
  Sync as MirroringIcon,
} from '@mui/icons-material';

const drawerWidth = 240;

function Navigation() {
  const location = useLocation();
  const [open, setOpen] = useState(true);
  const [anchorEl, setAnchorEl] = useState(null);
  
  const handleDrawerToggle = () => {
    setOpen(!open);
  };
  
  const handleProfileMenuOpen = (event) => {
    setAnchorEl(event.currentTarget);
  };
  
  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };
  
  const menuItems = [
    { text: 'Dashboard', icon: <DashboardIcon />, path: '/' },
    { text: 'Monitoring', icon: <MonitoringIcon />, path: '/monitoring' },
    { text: 'Servers', icon: <StorageIcon />, path: '/servers' },
    { text: 'Databases', icon: <DatabaseIcon />, path: '/databases' },
    { text: 'Performance', icon: <SpeedIcon />, path: '/performance' },
    { text: 'Queries', icon: <CodeIcon />, path: '/queries' },
    { text: 'Issues', icon: <WarningIcon />, path: '/issues' },
    { text: 'Backups', icon: <BackupIcon />, path: '/backups' },
    { divider: true },
    { text: 'Agent Jobs', icon: <AgentJobsIcon />, path: '/agent-jobs' },
    { text: 'DBCC Checks', icon: <DbccCheckIcon />, path: '/dbcc-checks' },
    { text: 'Identity Columns', icon: <IdentityColumnIcon />, path: '/identity-columns' },
    { text: 'Availability Groups', icon: <AvailabilityGroupIcon />, path: '/availability-groups' },
    { text: 'Log Shipping', icon: <LogShippingIcon />, path: '/log-shipping' },
    { text: 'Mirroring', icon: <MirroringIcon />, path: '/mirroring' },
    { divider: true },
    { text: 'Alerts', icon: <NotificationsIcon />, path: '/alerts' },
    { text: 'Settings', icon: <SettingsIcon />, path: '/settings' },
    { text: 'User Management', icon: <PeopleIcon />, path: '/users' },
  ];
  
  return (
    <>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="toggle drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2 }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            SQL Server Monitoring
          </Typography>
          
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Tooltip title="Help">
              <IconButton color="inherit">
                <HelpIcon />
              </IconButton>
            </Tooltip>
            
            <Tooltip title="Notifications">
              <IconButton color="inherit">
                <NotificationsIcon />
              </IconButton>
            </Tooltip>
            
            <Tooltip title="Account">
              <IconButton
                edge="end"
                color="inherit"
                aria-label="account of current user"
                aria-haspopup="true"
                onClick={handleProfileMenuOpen}
              >
                <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.dark' }}>
                  <AccountCircleIcon />
                </Avatar>
              </IconButton>
            </Tooltip>
          </Box>
        </Toolbar>
      </AppBar>
      
      <Menu
        anchorEl={anchorEl}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        keepMounted
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
        open={Boolean(anchorEl)}
        onClose={handleProfileMenuClose}
      >
        <MenuItem onClick={handleProfileMenuClose}>
          <ListItemIcon>
            <AccountCircleIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Profile</ListItemText>
        </MenuItem>
        <MenuItem onClick={handleProfileMenuClose}>
          <ListItemIcon>
            <SettingsIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>My Account</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleProfileMenuClose}>
          <ListItemIcon>
            <LogoutIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Logout</ListItemText>
        </MenuItem>
      </Menu>
      
      <Drawer
        variant="permanent"
        open={open}
        sx={{
          width: open ? drawerWidth : 72,
          flexShrink: 0,
          transition: (theme) => theme.transitions.create('width', {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.enteringScreen,
          }),
          '& .MuiDrawer-paper': {
            width: open ? drawerWidth : 72,
            overflowX: 'hidden',
            transition: (theme) => theme.transitions.create('width', {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.enteringScreen,
            }),
          },
        }}
      >
        <Toolbar />
        <Box sx={{ overflow: 'auto', mt: 2 }}>
          <List>
            {menuItems.map((item, index) => (
              item.divider ? (
                <Divider key={`divider-${index}`} sx={{ my: 1 }} />
              ) : (
                <ListItem
                  button
                  key={item.text}
                  component={Link}
                  to={item.path}
                  selected={location.pathname === item.path}
                  sx={{
                    minHeight: 48,
                    justifyContent: open ? 'initial' : 'center',
                    px: 2.5,
                    '&.Mui-selected': {
                      backgroundColor: 'primary.light',
                      '&:hover': {
                        backgroundColor: 'primary.light',
                      },
                    },
                  }}
                >
                  <ListItemIcon
                    sx={{
                      minWidth: 0,
                      mr: open ? 3 : 'auto',
                      justifyContent: 'center',
                      color: location.pathname === item.path ? 'primary.main' : 'inherit',
                    }}
                  >
                    {item.icon}
                  </ListItemIcon>
                  <ListItemText
                    primary={item.text}
                    sx={{
                      opacity: open ? 1 : 0,
                      color: location.pathname === item.path ? 'primary.main' : 'inherit',
                    }}
                  />
                </ListItem>
              )
            ))}
          </List>
        </Box>
      </Drawer>
    </>
  );
}

export default Navigation; 