import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || '';

const dbccCheckService = {
  // Get DBCC check history
  getDbccCheckHistory: async (connectionString) => {
    try {
      const response = await axios.get(`${API_URL}/api/DbccCheck/history`, {
        params: { connectionString }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching DBCC check history:', error);
      throw error;
    }
  },

  // Analyze DBCC checks
  analyzeDbccChecks: async (connectionString) => {
    try {
      const response = await axios.get(`${API_URL}/api/DbccCheck/analyze`, {
        params: { connectionString }
      });
      return response.data;
    } catch (error) {
      console.error('Error analyzing DBCC checks:', error);
      throw error;
    }
  },

  // Run DBCC CHECKDB on a specific database
  runDbccCheck: async (connectionString, databaseName) => {
    try {
      const response = await axios.post(`${API_URL}/api/DbccCheck/run`, null, {
        params: { connectionString, databaseName }
      });
      return response.data;
    } catch (error) {
      console.error(`Error running DBCC CHECKDB on ${databaseName}:`, error);
      throw error;
    }
  }
};

export default dbccCheckService; 