import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

/**
 * Service for managing SignalR connection and event handlers
 */
class SignalRService {
  constructor() {
    this.connection = null;
    this.connected = false;
    this.listeners = new Map();
  }

  /**
   * Initialize and start the SignalR connection
   */
  async start() {
    if (this.connection) {
      return;
    }

    // Create the connection
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/monitoring')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry pattern
      .configureLogging(LogLevel.Information)
      .build();

    // Set up reconnection event
    this.connection.onreconnected(() => {
      console.log('SignalR connection reestablished');
      this.connected = true;
      // Re-join groups if needed
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.connected = false;
    });

    // Start the connection
    try {
      await this.connection.start();
      console.log('SignalR connection established');
      this.connected = true;
      return true;
    } catch (err) {
      console.error('Error starting SignalR connection:', err);
      this.connected = false;
      return false;
    }
  }

  /**
   * Stop the SignalR connection
   */
  async stop() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.connected = false;
      this.listeners.clear();
    }
  }

  /**
   * Join a monitoring group (typically a server name)
   * @param {string} serverName - Server name to join
   */
  async joinGroup(serverName) {
    if (!this.connection || !this.connected) {
      await this.start();
    }

    try {
      await this.connection.invoke('JoinGroup', serverName);
      console.log(`Joined group: ${serverName}`);
      return true;
    } catch (err) {
      console.error(`Error joining group ${serverName}:`, err);
      return false;
    }
  }

  /**
   * Leave a monitoring group
   * @param {string} serverName - Server name to leave
   */
  async leaveGroup(serverName) {
    if (!this.connection || !this.connected) {
      return false;
    }

    try {
      await this.connection.invoke('LeaveGroup', serverName);
      console.log(`Left group: ${serverName}`);
      return true;
    } catch (err) {
      console.error(`Error leaving group ${serverName}:`, err);
      return false;
    }
  }

  /**
   * Subscribe to a SignalR event
   * @param {string} eventName - The event name to subscribe to
   * @param {function} callback - Callback function when event is received
   * @returns {function} - Function to unsubscribe from the event
   */
  on(eventName, callback) {
    if (!this.connection) {
      console.warn('Attempted to subscribe to event before connection was established');
      return () => {};
    }

    // Add listener to internal map to track them
    if (!this.listeners.has(eventName)) {
      this.listeners.set(eventName, new Set());
      
      // Register the handler with SignalR
      this.connection.on(eventName, (...args) => {
        // Call all registered callbacks for this event
        const callbacks = this.listeners.get(eventName);
        if (callbacks) {
          callbacks.forEach(cb => cb(...args));
        }
      });
    }
    
    // Add this specific callback to our set
    this.listeners.get(eventName).add(callback);
    
    // Return a function to unsubscribe this specific callback
    return () => {
      const callbacks = this.listeners.get(eventName);
      if (callbacks) {
        callbacks.delete(callback);
        
        // If no more callbacks for this event, remove the SignalR handler
        if (callbacks.size === 0) {
          this.connection.off(eventName);
          this.listeners.delete(eventName);
        }
      }
    };
  }

  /**
   * Check if the SignalR connection is established
   * @returns {boolean} Connection status
   */
  isConnected() {
    return this.connected;
  }
}

// Create a singleton instance
const signalRService = new SignalRService();

export default signalRService; 