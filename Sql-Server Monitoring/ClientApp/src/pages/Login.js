import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Grid,
  Alert,
  IconButton,
  InputAdornment,
  Paper,
} from '@mui/material';
import { Visibility, VisibilityOff, Storage } from '@mui/icons-material';
import { useConnection } from '../context/ConnectionContext';

function Login() {
  const [server, setServer] = useState('');
  const [database, setDatabase] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  
  const { connectToServer } = useConnection();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!server) {
      setError('Server name is required');
      return;
    }
    
    try {
      setIsLoading(true);
      setError('');
      
      // Build connection string
      let connectionString = `Server=${server};`;
      
      if (database) {
        connectionString += `Database=${database};`;
      }
      
      if (username && password) {
        connectionString += `User Id=${username};Password=${password};`;
      } else {
        connectionString += 'Integrated Security=True;';
      }
      
      // Connect to server
      await connectToServer(connectionString);
      
      // Navigate to dashboard
      navigate('/');
    } catch (err) {
      setError(err.message || 'Failed to connect to server');
    } finally {
      setIsLoading(false);
    }
  };

  const handleClickShowPassword = () => {
    setShowPassword(!showPassword);
  };

  return (
    <Box
      sx={{
        height: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'background.default',
      }}
    >
      <Container maxWidth="sm">
        <Paper elevation={3}>
          <Box sx={{ pt: 4, px: 4, pb: 3 }}>
            <Box sx={{ mb: 3, display: 'flex', justifyContent: 'center' }}>
              <Storage fontSize="large" color="primary" />
              <Typography
                variant="h4"
                component="h1"
                sx={{ ml: 1, fontWeight: 'bold' }}
              >
                SQL Server Monitor
              </Typography>
            </Box>
            
            {error && (
              <Alert severity="error" sx={{ mb: 3 }}>
                {error}
              </Alert>
            )}
            
            <form onSubmit={handleSubmit}>
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Server"
                    variant="outlined"
                    placeholder="localhost\\SQLEXPRESS"
                    value={server}
                    onChange={(e) => setServer(e.target.value)}
                    required
                  />
                </Grid>
                
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Database (optional)"
                    variant="outlined"
                    placeholder="master"
                    value={database}
                    onChange={(e) => setDatabase(e.target.value)}
                  />
                </Grid>
                
                <Grid item xs={12}>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    Authentication
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Leave blank to use Windows Authentication
                  </Typography>
                </Grid>
                
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Username"
                    variant="outlined"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                  />
                </Grid>
                
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Password"
                    variant="outlined"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    InputProps={{
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label="toggle password visibility"
                            onClick={handleClickShowPassword}
                            edge="end"
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                </Grid>
                
                <Grid item xs={12}>
                  <Button
                    type="submit"
                    variant="contained"
                    color="primary"
                    fullWidth
                    size="large"
                    disabled={isLoading}
                    sx={{ mt: 2 }}
                  >
                    {isLoading ? 'Connecting...' : 'Connect'}
                  </Button>
                </Grid>
              </Grid>
            </form>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}

export default Login; 