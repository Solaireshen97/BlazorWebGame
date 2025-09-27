import React from 'react';
import { ConnectionStatus as IConnectionStatus } from '../types';

interface Props {
  status: IConnectionStatus;
  signalRState: string;
  onTestConnection: () => void;
  onToggleSignalR: () => void;
}

const ConnectionStatus: React.FC<Props> = ({
  status,
  signalRState,
  onTestConnection,
  onToggleSignalR
}) => {
  const getStatusColor = (connected: boolean) => {
    return connected ? '#4CAF50' : '#F44336';
  };

  const getStatusIcon = (connected: boolean) => {
    return connected ? '✅' : '❌';
  };

  return (
    <div style={{
      border: '1px solid #ddd',
      borderRadius: '8px',
      padding: '16px',
      margin: '16px 0',
      backgroundColor: '#fff'
    }}>
      <h3 style={{ margin: '0 0 16px 0', color: '#333' }}>连接状态</h3>
      
      <div style={{ marginBottom: '12px' }}>
        <strong>服务器地址:</strong> {status.serverUrl}
      </div>

      <div style={{ 
        display: 'flex',
        alignItems: 'center',
        marginBottom: '12px',
        color: getStatusColor(status.isConnected)
      }}>
        <span style={{ marginRight: '8px' }}>
          {getStatusIcon(status.isConnected)}
        </span>
        <strong>HTTP API 连接:</strong>
        <span style={{ marginLeft: '8px' }}>
          {status.isConnected ? '已连接' : '未连接'}
        </span>
        {status.lastPing && (
          <span style={{ marginLeft: '8px', color: '#666' }}>
            ({status.lastPing}ms)
          </span>
        )}
      </div>

      <div style={{ 
        display: 'flex',
        alignItems: 'center',
        marginBottom: '16px',
        color: getStatusColor(signalRState === 'Connected')
      }}>
        <span style={{ marginRight: '8px' }}>
          {getStatusIcon(signalRState === 'Connected')}
        </span>
        <strong>SignalR 连接:</strong>
        <span style={{ marginLeft: '8px' }}>
          {signalRState}
        </span>
      </div>

      {status.error && (
        <div style={{
          background: '#ffebee',
          color: '#c62828',
          padding: '8px',
          borderRadius: '4px',
          marginBottom: '16px',
          fontSize: '14px'
        }}>
          <strong>错误:</strong> {status.error}
        </div>
      )}

      <div style={{ display: 'flex', gap: '8px' }}>
        <button
          onClick={onTestConnection}
          style={{
            padding: '8px 16px',
            backgroundColor: '#2196F3',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer'
          }}
        >
          测试 HTTP 连接
        </button>
        
        <button
          onClick={onToggleSignalR}
          style={{
            padding: '8px 16px',
            backgroundColor: signalRState === 'Connected' ? '#F44336' : '#4CAF50',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer'
          }}
        >
          {signalRState === 'Connected' ? '断开 SignalR' : '连接 SignalR'}
        </button>
      </div>
    </div>
  );
};

export default ConnectionStatus;