import React, { createContext, useContext, useState, useEffect } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';

const ConnectionContext = createContext();

export function ConnectionProvider({ children }) {
  const [connectionString, setConnectionString] = useState('');
  const [serverName, setServerName] = useState('');
  const [isConnected, setIsConnected] = useState(false);
  const [hubConnection, setHubConnection] = useState(null);

  useEffect(() => {
    // For demo, assume we're already connected to a default server
    const defaultConnection = 'Server=SQL-PROD-01;Database=master;Trusted_Connection=True;';
    const defaultServer = 'SQL-PROD-01';
    
    setConnectionString(defaultConnection);
    setServerName(defaultServer);
    setIsConnected(true);
    
    // Set up SignalR hub connection for real-time updates
    const newHubConnection = new HubConnectionBuilder()
      .withUrl('/monitoringHub')
      .withAutomaticReconnect()
      .build();
    
    setHubConnection(newHubConnection);
    
    // In a real app, we would start the connection
    // newHubConnection.start().catch(err => console.error('Error starting SignalR connection:', err));
    
    return () => {
      // In a real app, we would stop the connection when unmounting
      // if (newHubConnection) {
      //   newHubConnection.stop();
      // }
    };
  }, []);

  const connect = async (newConnectionString, newServerName) => {
    try {
      // In a real app, we would validate the connection string here
      // await api.testConnection(newConnectionString);
      
      setConnectionString(newConnectionString);
      setServerName(newServerName);
      setIsConnected(true);
      
      return true;
    } catch (error) {
      console.error('Connection failed:', error);
      return false;
    }
  };

  const disconnect = () => {
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
        hubConnection,
        connect,
        disconnect
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