import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  CircularProgress,
  Tooltip,
  Alert,
  Snackbar,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  VpnKey as VpnKeyIcon,
  PersonAdd as PersonAddIcon,
} from '@mui/icons-material';
import { security } from '../services/api';

// Mock data for demonstration
const MOCK_USERS = [
  {
    id: 1,
    username: 'admin',
    email: 'admin@example.com',
    fullName: 'Admin User',
    role: 'Administrator',
    lastLogin: '2023-06-15T14:30:00Z',
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z',
  },
  {
    id: 2,
    username: 'dbadmin',
    email: 'dbadmin@example.com',
    fullName: 'Database Admin',
    role: 'DBA',
    lastLogin: '2023-06-14T09:15:00Z',
    isActive: true,
    createdAt: '2023-01-02T00:00:00Z',
  },
  {
    id: 3,
    username: 'developer',
    email: 'dev@example.com',
    fullName: 'Developer User',
    role: 'Developer',
    lastLogin: '2023-06-13T11:45:00Z',
    isActive: false,
    createdAt: '2023-01-03T00:00:00Z',
  },
  {
    id: 4,
    username: 'viewer',
    email: 'viewer@example.com',
    fullName: 'Read Only User',
    role: 'Viewer',
    lastLogin: '2023-06-10T16:20:00Z',
    isActive: true,
    createdAt: '2023-01-04T00:00:00Z',
  },
  {
    id: 5,
    username: 'support',
    email: 'support@example.com',
    fullName: 'Support User',
    role: 'Support',
    lastLogin: null,
    isActive: true,
    createdAt: '2023-01-05T00:00:00Z',
  },
];

const ROLES = [
  { id: 'Administrator', name: 'Administrator', description: 'Full access to all features' },
  { id: 'DBA', name: 'Database Administrator', description: 'Manage databases and server settings' },
  { id: 'Developer', name: 'Developer', description: 'View data and run queries' },
  { id: 'Support', name: 'Support', description: 'Monitor and troubleshoot issues' },
  { id: 'Viewer', name: 'Viewer', description: 'Read-only access to dashboards' },
];

function UserManagement() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  
  // Dialog states
  const [openUserDialog, setOpenUserDialog] = useState(false);
  const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
  const [openResetDialog, setOpenResetDialog] = useState(false);
  const [dialogLoading, setDialogLoading] = useState(false);
  const [currentUser, setCurrentUser] = useState(null);
  
  // Form states
  const [formUser, setFormUser] = useState({
    username: '',
    email: '',
    fullName: '',
    role: '',
    password: '',
    confirmPassword: '',
    isActive: true,
  });
  const [formErrors, setFormErrors] = useState({});
  
  useEffect(() => {
    fetchUsers();
  }, []);
  
  const fetchUsers = async () => {
    setLoading(true);
    try {
      // In a real app, call API to get users
      // const response = await security.getUsers();
      // setUsers(response.data);
      
      // For demo, use mock data
      setTimeout(() => {
        setUsers(MOCK_USERS);
        setLoading(false);
      }, 1000);
    } catch (err) {
      console.error('Error fetching users:', err);
      setError('Failed to fetch users. Please try again.');
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
  
  const handleOpenUserDialog = (user = null) => {
    if (user) {
      setCurrentUser(user);
      setFormUser({
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        role: user.role,
        password: '',
        confirmPassword: '',
        isActive: user.isActive,
      });
    } else {
      setCurrentUser(null);
      setFormUser({
        username: '',
        email: '',
        fullName: '',
        role: '',
        password: '',
        confirmPassword: '',
        isActive: true,
      });
    }
    setFormErrors({});
    setOpenUserDialog(true);
  };
  
  const handleCloseUserDialog = () => {
    setOpenUserDialog(false);
  };
  
  const handleOpenDeleteDialog = (user) => {
    setCurrentUser(user);
    setOpenDeleteDialog(true);
  };
  
  const handleCloseDeleteDialog = () => {
    setOpenDeleteDialog(false);
  };
  
  const handleOpenResetDialog = (user) => {
    setCurrentUser(user);
    setOpenResetDialog(true);
  };
  
  const handleCloseResetDialog = () => {
    setOpenResetDialog(false);
  };
  
  const handleFormChange = (e) => {
    const { name, value, checked } = e.target;
    if (name === 'isActive') {
      setFormUser((prev) => ({ ...prev, [name]: checked }));
    } else {
      setFormUser((prev) => ({ ...prev, [name]: value }));
    }
    
    // Clear validation error when field is edited
    if (formErrors[name]) {
      setFormErrors((prev) => ({ ...prev, [name]: '' }));
    }
  };
  
  const validateForm = () => {
    const errors = {};
    
    if (!formUser.username.trim()) {
      errors.username = 'Username is required';
    } else if (formUser.username.length < 3) {
      errors.username = 'Username must be at least 3 characters';
    }
    
    if (!formUser.email.trim()) {
      errors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formUser.email)) {
      errors.email = 'Email is invalid';
    }
    
    if (!formUser.fullName.trim()) {
      errors.fullName = 'Full name is required';
    }
    
    if (!formUser.role) {
      errors.role = 'Role is required';
    }
    
    // Only validate password for new users or if changing password
    if (!currentUser || formUser.password) {
      if (!currentUser && !formUser.password) {
        errors.password = 'Password is required';
      } else if (formUser.password && formUser.password.length < 8) {
        errors.password = 'Password must be at least 8 characters';
      }
      
      if (formUser.password !== formUser.confirmPassword) {
        errors.confirmPassword = 'Passwords do not match';
      }
    }
    
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };
  
  const handleSubmitUser = async () => {
    if (!validateForm()) {
      return;
    }
    
    setDialogLoading(true);
    try {
      if (currentUser) {
        // In a real app, call API to update user
        // await security.updateUser(currentUser.id, formUser);
        
        // For demo, update locally
        setUsers((prevUsers) =>
          prevUsers.map((user) =>
            user.id === currentUser.id
              ? {
                  ...user,
                  username: formUser.username,
                  email: formUser.email,
                  fullName: formUser.fullName,
                  role: formUser.role,
                  isActive: formUser.isActive,
                }
              : user
          )
        );
        setSuccessMessage('User updated successfully');
      } else {
        // In a real app, call API to create user
        // const response = await security.createUser(formUser);
        
        // For demo, create locally with a mock ID
        const newUser = {
          id: Math.max(...users.map((u) => u.id)) + 1,
          username: formUser.username,
          email: formUser.email,
          fullName: formUser.fullName,
          role: formUser.role,
          isActive: formUser.isActive,
          lastLogin: null,
          createdAt: new Date().toISOString(),
        };
        setUsers((prevUsers) => [...prevUsers, newUser]);
        setSuccessMessage('User created successfully');
      }
      
      setTimeout(() => {
        setDialogLoading(false);
        handleCloseUserDialog();
      }, 1000);
    } catch (err) {
      console.error('Error saving user:', err);
      setError('Failed to save user. Please try again.');
      setDialogLoading(false);
    }
  };
  
  const handleDeleteUser = async () => {
    if (!currentUser) return;
    
    setDialogLoading(true);
    try {
      // In a real app, call API to delete user
      // await security.deleteUser(currentUser.id);
      
      // For demo, remove locally
      setUsers((prevUsers) => prevUsers.filter((user) => user.id !== currentUser.id));
      
      setTimeout(() => {
        setDialogLoading(false);
        handleCloseDeleteDialog();
        setSuccessMessage('User deleted successfully');
      }, 1000);
    } catch (err) {
      console.error('Error deleting user:', err);
      setError('Failed to delete user. Please try again.');
      setDialogLoading(false);
    }
  };
  
  const handleResetPassword = async (password) => {
    if (!currentUser) return;
    
    setDialogLoading(true);
    try {
      // In a real app, call API to reset password
      // await security.resetPassword(currentUser.id, { password });
      
      setTimeout(() => {
        setDialogLoading(false);
        handleCloseResetDialog();
        setSuccessMessage('Password reset successfully');
      }, 1000);
    } catch (err) {
      console.error('Error resetting password:', err);
      setError('Failed to reset password. Please try again.');
      setDialogLoading(false);
    }
  };
  
  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  };
  
  const handleCloseSnackbar = () => {
    setSuccessMessage('');
    setError('');
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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1">
          User Management
        </Typography>
        <Button
          variant="contained"
          color="primary"
          startIcon={<PersonAddIcon />}
          onClick={() => handleOpenUserDialog()}
        >
          Add User
        </Button>
      </Box>
      
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      
      <Paper sx={{ width: '100%', overflow: 'hidden' }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Username</TableCell>
                <TableCell>Full Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Role</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Last Login</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users
                .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
                .map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>{user.username}</TableCell>
                    <TableCell>{user.fullName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <Chip
                        label={user.role}
                        color={
                          user.role === 'Administrator'
                            ? 'error'
                            : user.role === 'DBA'
                            ? 'warning'
                            : user.role === 'Developer'
                            ? 'info'
                            : 'default'
                        }
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={user.isActive ? 'Active' : 'Inactive'}
                        color={user.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{formatDate(user.lastLogin)}</TableCell>
                    <TableCell>
                      <Tooltip title="Edit User">
                        <IconButton size="small" onClick={() => handleOpenUserDialog(user)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Reset Password">
                        <IconButton size="small" onClick={() => handleOpenResetDialog(user)}>
                          <VpnKeyIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete User">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleOpenDeleteDialog(user)}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              {users.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    No users found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={users.length}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </Paper>
      
      {/* Add/Edit User Dialog */}
      <Dialog open={openUserDialog} onClose={handleCloseUserDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{currentUser ? 'Edit User' : 'Add New User'}</DialogTitle>
        <DialogContent>
          <Box component="form" sx={{ mt: 1 }}>
            <TextField
              margin="normal"
              required
              fullWidth
              label="Username"
              name="username"
              value={formUser.username}
              onChange={handleFormChange}
              error={!!formErrors.username}
              helperText={formErrors.username}
              disabled={dialogLoading}
            />
            <TextField
              margin="normal"
              required
              fullWidth
              label="Email Address"
              name="email"
              type="email"
              value={formUser.email}
              onChange={handleFormChange}
              error={!!formErrors.email}
              helperText={formErrors.email}
              disabled={dialogLoading}
            />
            <TextField
              margin="normal"
              required
              fullWidth
              label="Full Name"
              name="fullName"
              value={formUser.fullName}
              onChange={handleFormChange}
              error={!!formErrors.fullName}
              helperText={formErrors.fullName}
              disabled={dialogLoading}
            />
            <FormControl fullWidth margin="normal" error={!!formErrors.role}>
              <InputLabel id="role-select-label">Role</InputLabel>
              <Select
                labelId="role-select-label"
                name="role"
                value={formUser.role}
                onChange={handleFormChange}
                label="Role"
                disabled={dialogLoading}
              >
                {ROLES.map((role) => (
                  <MenuItem key={role.id} value={role.id}>
                    {role.name} - {role.description}
                  </MenuItem>
                ))}
              </Select>
              {formErrors.role && (
                <Typography variant="caption" color="error">
                  {formErrors.role}
                </Typography>
              )}
            </FormControl>
            
            {!currentUser && (
              <>
                <TextField
                  margin="normal"
                  required
                  fullWidth
                  name="password"
                  label="Password"
                  type="password"
                  value={formUser.password}
                  onChange={handleFormChange}
                  error={!!formErrors.password}
                  helperText={formErrors.password}
                  disabled={dialogLoading}
                />
                <TextField
                  margin="normal"
                  required
                  fullWidth
                  name="confirmPassword"
                  label="Confirm Password"
                  type="password"
                  value={formUser.confirmPassword}
                  onChange={handleFormChange}
                  error={!!formErrors.confirmPassword}
                  helperText={formErrors.confirmPassword}
                  disabled={dialogLoading}
                />
              </>
            )}
            
            <FormControl fullWidth margin="normal">
              <FormControlLabel
                control={
                  <Switch
                    checked={formUser.isActive}
                    onChange={handleFormChange}
                    name="isActive"
                    disabled={dialogLoading}
                  />
                }
                label="Active Account"
              />
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseUserDialog} disabled={dialogLoading}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmitUser}
            variant="contained"
            color="primary"
            disabled={dialogLoading}
            startIcon={dialogLoading && <CircularProgress size={20} />}
          >
            {dialogLoading ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
      
      {/* Delete User Dialog */}
      <Dialog open={openDeleteDialog} onClose={handleCloseDeleteDialog}>
        <DialogTitle>Delete User</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete the user "{currentUser?.fullName}"? This action cannot be
            undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDeleteDialog} disabled={dialogLoading}>
            Cancel
          </Button>
          <Button
            onClick={handleDeleteUser}
            variant="contained"
            color="error"
            disabled={dialogLoading}
            startIcon={dialogLoading && <CircularProgress size={20} />}
          >
            {dialogLoading ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
      
      {/* Reset Password Dialog */}
      <Dialog open={openResetDialog} onClose={handleCloseResetDialog}>
        <DialogTitle>Reset Password</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Reset password for user: {currentUser?.fullName}
          </DialogContentText>
          <Box component="form">
            <TextField
              autoFocus
              margin="dense"
              name="newPassword"
              label="New Password"
              type="password"
              fullWidth
              variant="outlined"
              value={formUser.password}
              onChange={(e) => setFormUser({ ...formUser, password: e.target.value })}
              error={!!formErrors.password}
              helperText={formErrors.password}
              disabled={dialogLoading}
            />
            <TextField
              margin="dense"
              name="confirmNewPassword"
              label="Confirm New Password"
              type="password"
              fullWidth
              variant="outlined"
              value={formUser.confirmPassword}
              onChange={(e) => setFormUser({ ...formUser, confirmPassword: e.target.value })}
              error={!!formErrors.confirmPassword}
              helperText={formErrors.confirmPassword}
              disabled={dialogLoading}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseResetDialog} disabled={dialogLoading}>
            Cancel
          </Button>
          <Button
            onClick={() => handleResetPassword(formUser.password)}
            variant="contained"
            color="primary"
            disabled={dialogLoading || !formUser.password || formUser.password !== formUser.confirmPassword}
            startIcon={dialogLoading && <CircularProgress size={20} />}
          >
            {dialogLoading ? 'Resetting...' : 'Reset Password'}
          </Button>
        </DialogActions>
      </Dialog>
      
      <Snackbar
        open={!!successMessage || !!error}
        autoHideDuration={5000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={error ? 'error' : 'success'}>
          {error || successMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
}

export default UserManagement; 