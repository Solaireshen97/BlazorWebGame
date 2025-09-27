import axios, { AxiosInstance, AxiosResponse } from 'axios';
import {
  ApiResponse,
  BattleStateDto,
  StartBattleRequest,
  CharacterDto,
  ConnectionStatus
} from '../types';

export class ApiService {
  private api: AxiosInstance;
  private baseUrl: string;

  constructor(baseUrl: string = 'https://localhost:7000') {
    this.baseUrl = baseUrl;
    this.api = axios.create({
      baseURL: baseUrl,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor for logging
    this.api.interceptors.request.use(
      (config) => {
        console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
        return config;
      },
      (error) => {
        console.error('[API] Request error:', error);
        return Promise.reject(error);
      }
    );

    // Response interceptor for logging
    this.api.interceptors.response.use(
      (response) => {
        console.log(`[API] Response ${response.status} from ${response.config.url}`);
        return response;
      },
      (error) => {
        console.error('[API] Response error:', error);
        return Promise.reject(error);
      }
    );
  }

  // Test server connectivity
  async testConnection(): Promise<ConnectionStatus> {
    const startTime = Date.now();
    try {
      // Try to hit a simple endpoint to test connectivity
      const response = await this.api.get('/api/battle/state/00000000-0000-0000-0000-000000000000');
      const ping = Date.now() - startTime;
      
      return {
        isConnected: true,
        serverUrl: this.baseUrl,
        signalRConnected: false, // Will be set separately
        lastPing: ping
      };
    } catch (error: any) {
      return {
        isConnected: false,
        serverUrl: this.baseUrl,
        signalRConnected: false,
        error: error.message || 'Unknown error'
      };
    }
  }

  // Battle API methods
  async startBattle(request: StartBattleRequest): Promise<ApiResponse<BattleStateDto>> {
    try {
      const response: AxiosResponse<ApiResponse<BattleStateDto>> = await this.api.post('/api/battle/start', request);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  async getBattleState(battleId: string): Promise<ApiResponse<BattleStateDto>> {
    try {
      const response: AxiosResponse<ApiResponse<BattleStateDto>> = await this.api.get(`/api/battle/state/${battleId}`);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  async stopBattle(battleId: string): Promise<ApiResponse<boolean>> {
    try {
      const response: AxiosResponse<ApiResponse<boolean>> = await this.api.post(`/api/battle/stop/${battleId}`);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  // Character API methods (if available)
  async getCharacter(characterId: string): Promise<ApiResponse<CharacterDto>> {
    try {
      const response: AxiosResponse<ApiResponse<CharacterDto>> = await this.api.get(`/api/character/${characterId}`);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  // Generic GET method for testing endpoints
  async get<T>(endpoint: string): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.get(endpoint);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  // Generic POST method for testing endpoints
  async post<T>(endpoint: string, data?: any): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.post(endpoint, data);
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  }

  private handleError(error: any): Error {
    if (error.response) {
      // Server responded with error status
      const message = error.response.data?.message || `HTTP ${error.response.status}: ${error.response.statusText}`;
      return new Error(message);
    } else if (error.request) {
      // Request was made but no response received
      return new Error('No response from server - check if server is running');
    } else {
      // Something else happened
      return new Error(error.message || 'Unknown error occurred');
    }
  }

  // Utility method to change base URL
  setBaseUrl(baseUrl: string): void {
    this.baseUrl = baseUrl;
    this.api.defaults.baseURL = baseUrl;
  }

  getBaseUrl(): string {
    return this.baseUrl;
  }
}