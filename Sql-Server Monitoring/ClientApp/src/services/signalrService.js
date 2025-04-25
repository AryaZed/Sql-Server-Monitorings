import * as signalR from '@microsoft/signalr';

/**
 * Service for managing SignalR connection and event handlers
 */
class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.connectionPromise = null;
    this.retryCount = 0;
    this.maxRetries = 3;
    this.listeners = new Map();
  }

  /**
   * Initialize and start the SignalR connection
   */
  async start() {
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    if (!this.connection) {
      this.createConnection();
    }

    // Return placeholder instead of actual connection if in development
    if (!this.connection || process.env.NODE_ENV === 'development') {
      console.warn('SignalR: Running in mock mode - backend connection is not available');
      this.isConnected = false;
      
      // Return a resolved promise so the app doesn't crash
      this.connectionPromise = Promise.resolve();
      return this.connectionPromise;
    }

    this.connectionPromise = this.connection
      .start()
      .then(() => {
        console.log('SignalR: Connected successfully');
        this.isConnected = true;
        this.retryCount = 0;
      })
      .catch(err => {
        console.error('SignalR: Connection failed', err);
        this.isConnected = false;
        this.connectionPromise = null;
        
        // Increment retry counter
        this.retryCount++;
        
        // Try to reconnect if we haven't exceeded max retries
        if (this.retryCount < this.maxRetries) {
          console.log(`SignalR: Retrying connection (${this.retryCount}/${this.maxRetries})`);
          return new Promise(resolve => {
            setTimeout(() => {
              resolve(this.start());
            }, 1000 * Math.pow(2, this.retryCount));
          });
        }
        
        // Return a resolved promise so the app doesn't crash even when connection fails
        return Promise.resolve();
      });

    return this.connectionPromise;
  }

  /**
   * Stop the SignalR connection
   */
  async stop() {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.stop();
        console.log('SignalR: Disconnected');
      } catch (err) {
        console.error('SignalR: Error stopping connection', err);
      }
    }
    this.isConnected = false;
    this.connectionPromise = null;
    this.listeners.clear();
  }

  /**
   * Join a monitoring group (typically a server name)
   * @param {string} serverName - Server name to join
   */
  async joinGroup(serverName) {
    if (!this.connection || !this.isConnected) {
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
    if (!this.connection || !this.isConnected) {
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
   * Unsubscribe from a SignalR event
   * @param {string} eventName - The event name to unsubscribe from
   * @param {function} callback - Callback function to remove
   */
  off(eventName, callback) {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  /**
   * Invoke a method on the SignalR connection
   * @param {string} methodName - The method name to invoke
   * @param {...any} args - Arguments to pass to the method
   * @returns {Promise<any>} - Promise that resolves with the method's return value
   */
  async invoke(methodName, ...args) {
    if (this.connection && this.isConnected) {
      try {
        return await this.connection.invoke(methodName, ...args);
      } catch (err) {
        console.error(`SignalR: Error invoking method ${methodName}`, err);
        throw err;
      }
    } else {
      console.warn(`SignalR: Can't invoke ${methodName} - connection not established`);
      // Return mock data
      return null;
    }
  }

  /**
   * Check if the SignalR connection is established
   * @returns {boolean} Connection status
   */
  isConnectedToHub() {
    return this.isConnected;
  }

  // Initialize the connection
  createConnection() {
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/monitoring')
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.previousRetryCount >= this.maxRetries) {
              console.warn('SignalR: Max retries reached. Will not attempt further reconnection.');
              return null;
            }
            
            // Exponential backoff
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Setup connection event handlers
      this.connection.onclose(this.onConnectionClosed.bind(this));
      this.connection.onreconnecting(this.onReconnecting.bind(this));
      this.connection.onreconnected(this.onReconnected.bind(this));
    } catch (err) {
      console.error('Error creating SignalR connection:', err);
      this.connection = null;
    }
  }

  // Connection closed handler
  onConnectionClosed(error) {
    this.isConnected = false;
    console.log('SignalR: Connection closed', error);
  }

  // Reconnecting handler
  onReconnecting(error) {
    this.isConnected = false;
    console.log('SignalR: Attempting to reconnect', error);
  }

  // Reconnected handler
  onReconnected(connectionId) {
    this.isConnected = true;
    console.log(`SignalR: Reconnected with connection ID ${connectionId}`);
  }
}

// Create a singleton instance
const signalRService = new SignalRService();

export default signalRService; 