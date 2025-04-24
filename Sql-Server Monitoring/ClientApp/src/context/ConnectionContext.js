import React, { createContext, useContext, useState, useEffect } from 'react';
import signalRService from '../services/signalrService';
import { servers } from '../services/api';

const ConnectionContext = createContext();

export function ConnectionProvider({ children }) {
  const [connectionString, setConnectionString] = useState('');
  const [serverName, setServerName] = useState('');
  const [isConnected, setIsConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  const [connectionError, setConnectionError] = useState('');

  useEffect(() => {
    // For demo, assume we're already connected to a default server
    const defaultConnection = 'Server=SQL-PROD-01;Database=master;Trusted_Connection=True;';
    const defaultServer = 'SQL-PROD-01';
    
    // Initialize the connection
    const initConnection = async () => {
      setConnectionString(defaultConnection);
      setServerName(defaultServer);
      setIsConnected(true);
      
      // Save to session storage
      sessionStorage.setItem('connectionString', defaultConnection);
      
      // Start the SignalR connection
      await signalRService.start();
      
      // Join the server group for real-time updates
      await signalRService.joinGroup(defaultServer);
    };
    
    initConnection();
    
    return () => {
      // Clean up SignalR connection when the app unmounts
      signalRService.stop();
    };
  }, []);

  const connect = async (newConnectionString, newServerName) => {
    try {
      setIsConnecting(true);
      setConnectionError('');
      
      // Test the connection first
      const response = await servers.testConnection(newConnectionString);
      
      if (response.data.success) {
        // Save to session storage
        sessionStorage.setItem('connectionString', newConnectionString);
        
        // Leave the current server group if any
        if (serverName) {
          await signalRService.leaveGroup(serverName);
        }
        
        // Update the context
        setConnectionString(newConnectionString);
        setServerName(newServerName);
        setIsConnected(true);
        
        // Join the new server group
        await signalRService.joinGroup(newServerName);
        
        setIsConnecting(false);
        return true;
      } else {
        setConnectionError(response.data.message || 'Failed to connect to server');
        setIsConnecting(false);
        return false;
      }
    } catch (error) {
      console.error('Connection failed:', error);
      setConnectionError(error.message || 'Failed to connect to server');
      setIsConnecting(false);
      return false;
    }
  };

  const disconnect = async () => {
    // Leave the current server group
    if (serverName) {
      await signalRService.leaveGroup(serverName);
    }
    
    // Clear connection data
    sessionStorage.removeItem('connectionString');
    setConnectionString('');
    setServerName('');
    setIsConnected(false);
  };

  return (
    <ConnectionContext.Provider 
      value={{ 
        connectionString,
        serverName,
        isConnected,
        isConnecting,
        connectionError,
        connect,
        disconnect,
        signalRService // Expose the SignalR service through the context
      }}
    >
      {children}
    </ConnectionContext.Provider>
  );
}

export function useConnection() {
  return useContext(ConnectionContext);
}

export default ConnectionContext; 