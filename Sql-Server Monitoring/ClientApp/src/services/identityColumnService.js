import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || '';

const identityColumnService = {
  // Get identity columns for a database
  getIdentityColumns: async (connectionString, databaseName) => {
    try {
      const response = await axios.get(`${API_URL}/api/IdentityColumn`, {
        params: { connectionString, databaseName }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching identity columns:', error);
      throw error;
    }
  },

  // Analyze identity columns for potential issues
  analyzeIdentityColumns: async (connectionString, databaseName) => {
    try {
      const response = await axios.get(`${API_URL}/api/IdentityColumn/analyze`, {
        params: { connectionString, databaseName }
      });
      return response.data;
    } catch (error) {
      console.error('Error analyzing identity columns:', error);
      throw error;
    }
  },

  // Reseed an identity column
  reseedIdentityColumn: async (connectionString, databaseName, schemaName, tableName, columnName, newSeedValue) => {
    try {
      const response = await axios.post(`${API_URL}/api/IdentityColumn/reseed`, null, {
        params: { 
          connectionString,
          databaseName,
          schemaName,
          tableName,
          columnName,
          newSeedValue
        }
      });
      return response.data;
    } catch (error) {
      console.error(`Error reseeding identity column ${schemaName}.${tableName}.${columnName}:`, error);
      throw error;
    }
  }
};

export default identityColumnService; 