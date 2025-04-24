import axios from 'axios';

const API_BASE_URL = '/api/agent-jobs';

const agentJobsService = {
  // Get all Agent jobs for a server
  getAgentJobs: async (serverId) => {
    try {
      const response = await axios.get(`${API_BASE_URL}/${serverId}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching agent jobs:', error);
      throw error;
    }
  },

  // Get job history for a specific job
  getJobHistory: async (serverId, jobName) => {
    try {
      const response = await axios.get(`${API_BASE_URL}/${serverId}/history`, {
        params: { jobName }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching job history:', error);
      throw error;
    }
  },

  // Run a specific job
  runJob: async (serverId, jobName) => {
    try {
      const response = await axios.post(`${API_BASE_URL}/${serverId}/run`, {
        jobName
      });
      return response.data;
    } catch (error) {
      console.error('Error running job:', error);
      throw error;
    }
  },

  // Enable a job
  enableJob: async (serverId, jobName) => {
    try {
      const response = await axios.post(`${API_BASE_URL}/${serverId}/enable`, {
        jobName
      });
      return response.data;
    } catch (error) {
      console.error('Error enabling job:', error);
      throw error;
    }
  },

  // Disable a job
  disableJob: async (serverId, jobName) => {
    try {
      const response = await axios.post(`${API_BASE_URL}/${serverId}/disable`, {
        jobName
      });
      return response.data;
    } catch (error) {
      console.error('Error disabling job:', error);
      throw error;
    }
  },

  // Get job execution statistics
  getJobStats: async (serverId, jobName, days = 30) => {
    try {
      const response = await axios.get(`${API_BASE_URL}/${serverId}/stats`, {
        params: { jobName, days }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching job stats:', error);
      throw error;
    }
  }
};

export default agentJobsService; 