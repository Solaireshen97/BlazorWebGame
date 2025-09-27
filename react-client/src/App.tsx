import React, { useState, useEffect, useCallback } from 'react';
import { ApiService } from './services/ApiService';
import { SignalRService } from './services/SignalRService';
import { ConnectionStatus as IConnectionStatus, BattleStateDto, StartBattleRequest } from './types';
import ConnectionStatus from './components/ConnectionStatus';
import ServerConfig from './components/ServerConfig';
import BattleTest from './components/BattleTest';

const App: React.FC = () => {
  const [apiService] = useState(() => new ApiService());
  const [signalRService] = useState(() => new SignalRService());
  
  const [connectionStatus, setConnectionStatus] = useState<IConnectionStatus>({
    isConnected: false,
    serverUrl: 'https://localhost:7000',
    signalRConnected: false
  });
  
  const [signalRState, setSignalRState] = useState('Disconnected');
  const [currentBattle, setCurrentBattle] = useState<BattleStateDto | undefined>();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>();
  
  // Initialize SignalR event handlers
  useEffect(() => {
    signalRService.onBattleUpdate = (battleState: BattleStateDto) => {
      console.log('Battle update received:', battleState);
      setCurrentBattle(battleState);
    };
    
    signalRService.onNotification = (message: string) => {
      console.log('Notification received:', message);
      // You could show notifications in a toast or similar UI element
    };
    
    signalRService.onConnectionStateChanged = (state: string) => {
      setSignalRState(state);
      setConnectionStatus(prev => ({
        ...prev,
        signalRConnected: state === 'Connected'
      }));
    };
    
    return () => {
      signalRService.disconnect();
    };
  }, [signalRService]);

  const testConnection = useCallback(async () => {
    setIsLoading(true);
    setError(undefined);
    
    try {
      const status = await apiService.testConnection();
      setConnectionStatus(prev => ({
        ...prev,
        ...status
      }));
    } catch (err: any) {
      setError(err.message || 'Failed to test connection');
    } finally {
      setIsLoading(false);
    }
  }, [apiService]);

  const toggleSignalR = useCallback(async () => {
    setIsLoading(true);
    setError(undefined);
    
    try {
      if (signalRService.isConnected()) {
        await signalRService.disconnect();
        setSignalRState('Disconnected');
      } else {
        const connected = await signalRService.connect();
        setSignalRState(connected ? 'Connected' : 'Failed');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to toggle SignalR connection');
      setSignalRState('Failed');
    } finally {
      setIsLoading(false);
    }
  }, [signalRService]);

  const handleUrlChange = useCallback((newUrl: string) => {
    apiService.setBaseUrl(newUrl);
    signalRService.setBaseUrl(newUrl);
    setConnectionStatus(prev => ({
      ...prev,
      serverUrl: newUrl,
      isConnected: false,
      signalRConnected: false
    }));
    setSignalRState('Disconnected');
    setCurrentBattle(undefined);
  }, [apiService, signalRService]);

  const handleStartBattle = useCallback(async (request: StartBattleRequest) => {
    setIsLoading(true);
    setError(undefined);
    
    try {
      const response = await apiService.startBattle(request);
      if (response.success && response.data) {
        setCurrentBattle(response.data);
        // Join the battle group in SignalR if connected
        if (signalRService.isConnected()) {
          await signalRService.joinBattleGroup(response.data.battleId);
        }
      } else {
        setError(response.message || 'Failed to start battle');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to start battle');
    } finally {
      setIsLoading(false);
    }
  }, [apiService, signalRService]);

  const handleStopBattle = useCallback(async (battleId: string) => {
    setIsLoading(true);
    setError(undefined);
    
    try {
      const response = await apiService.stopBattle(battleId);
      if (response.success) {
        // Leave the battle group in SignalR if connected
        if (signalRService.isConnected()) {
          await signalRService.leaveBattleGroup(battleId);
        }
        setCurrentBattle(undefined);
      } else {
        setError(response.message || 'Failed to stop battle');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to stop battle');
    } finally {
      setIsLoading(false);
    }
  }, [apiService, signalRService]);

  const handleGetBattleState = useCallback(async (battleId: string) => {
    setIsLoading(true);
    setError(undefined);
    
    try {
      const response = await apiService.getBattleState(battleId);
      if (response.success && response.data) {
        setCurrentBattle(response.data);
      } else {
        setError(response.message || 'Failed to get battle state');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to get battle state');
    } finally {
      setIsLoading(false);
    }
  }, [apiService]);

  // Test connection on initial load
  useEffect(() => {
    testConnection();
  }, [testConnection]);

  return (
    <div style={{
      maxWidth: '800px',
      margin: '0 auto',
      padding: '20px',
      fontFamily: 'Arial, sans-serif'
    }}>
      <header style={{
        textAlign: 'center',
        marginBottom: '32px',
        padding: '20px',
        backgroundColor: '#2196F3',
        color: 'white',
        borderRadius: '8px'
      }}>
        <h1 style={{ margin: '0 0 8px 0' }}>BlazorWebGame React 客户端</h1>
        <p style={{ margin: 0, opacity: 0.9 }}>
          用于测试与 BlazorWebGame 服务器连接的最小 React 应用程序
        </p>
      </header>

      <ServerConfig
        currentUrl={connectionStatus.serverUrl}
        onUrlChange={handleUrlChange}
      />

      <ConnectionStatus
        status={connectionStatus}
        signalRState={signalRState}
        onTestConnection={testConnection}
        onToggleSignalR={toggleSignalR}
      />

      <BattleTest
        onStartBattle={handleStartBattle}
        onStopBattle={handleStopBattle}
        onGetBattleState={handleGetBattleState}
        currentBattle={currentBattle}
        isLoading={isLoading}
        error={error}
      />

      <footer style={{
        marginTop: '32px',
        padding: '16px',
        textAlign: 'center',
        backgroundColor: '#f5f5f5',
        borderRadius: '8px',
        fontSize: '14px',
        color: '#666'
      }}>
        <p style={{ margin: '0 0 8px 0' }}>
          这是一个用于评估前端重构到 React 的最小测试应用程序。
        </p>
        <p style={{ margin: 0 }}>
          它演示了与 BlazorWebGame 服务器的 HTTP API 和 SignalR 实时通信的基本连接。
        </p>
      </footer>
    </div>
  );
};

export default App;