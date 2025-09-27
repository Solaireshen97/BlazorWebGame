import * as signalR from '@microsoft/signalr';
import { BattleStateDto } from '../types';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private baseUrl: string;

  constructor(baseUrl: string = 'https://localhost:7000') {
    this.baseUrl = baseUrl;
  }

  async connect(): Promise<boolean> {
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseUrl}/gamehub`, {
          // Configure options if needed
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      // Start the connection
      await this.connection.start();
      console.log('[SignalR] Connected successfully');
      return true;
    } catch (error) {
      console.error('[SignalR] Connection failed:', error);
      return false;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      console.log('[SignalR] Disconnected');
    }
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  getConnectionState(): string {
    if (!this.connection) return 'Disconnected';
    
    switch (this.connection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Unknown';
    }
  }

  // Event handlers setup
  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle battle updates
    this.connection.on('BattleUpdate', (battleState: BattleStateDto) => {
      console.log('[SignalR] Battle update received:', battleState);
      this.onBattleUpdate?.(battleState);
    });

    // Handle notifications
    this.connection.on('Notification', (message: string) => {
      console.log('[SignalR] Notification received:', message);
      this.onNotification?.(message);
    });

    // Handle connection events
    this.connection.onreconnecting((error) => {
      console.log('[SignalR] Reconnecting...', error);
      this.onConnectionStateChanged?.('Reconnecting');
    });

    this.connection.onreconnected((connectionId) => {
      console.log('[SignalR] Reconnected with ID:', connectionId);
      this.onConnectionStateChanged?.('Connected');
    });

    this.connection.onclose((error) => {
      console.log('[SignalR] Connection closed:', error);
      this.onConnectionStateChanged?.('Disconnected');
    });
  }

  // Battle-specific methods
  async joinBattleGroup(battleId: string): Promise<void> {
    if (this.connection && this.isConnected()) {
      try {
        await this.connection.invoke('JoinBattleGroup', battleId);
        console.log(`[SignalR] Joined battle group: ${battleId}`);
      } catch (error) {
        console.error('[SignalR] Failed to join battle group:', error);
        throw error;
      }
    } else {
      throw new Error('SignalR connection is not established');
    }
  }

  async leaveBattleGroup(battleId: string): Promise<void> {
    if (this.connection && this.isConnected()) {
      try {
        await this.connection.invoke('LeaveBattleGroup', battleId);
        console.log(`[SignalR] Left battle group: ${battleId}`);
      } catch (error) {
        console.error('[SignalR] Failed to leave battle group:', error);
        throw error;
      }
    }
  }

  // Event callbacks - can be set by components
  onBattleUpdate?: (battleState: BattleStateDto) => void;
  onNotification?: (message: string) => void;
  onConnectionStateChanged?: (state: string) => void;

  // Utility methods
  setBaseUrl(baseUrl: string): void {
    this.baseUrl = baseUrl;
  }

  getBaseUrl(): string {
    return this.baseUrl;
  }
}