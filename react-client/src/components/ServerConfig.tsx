import React, { useState } from 'react';

interface Props {
  currentUrl: string;
  onUrlChange: (url: string) => void;
}

const ServerConfig: React.FC<Props> = ({ currentUrl, onUrlChange }) => {
  const [url, setUrl] = useState(currentUrl);

  const predefinedUrls = [
    'https://localhost:7000',
    'http://localhost:5239',
    'https://localhost:7051',
    'http://localhost:5190'
  ];

  const handleApply = () => {
    onUrlChange(url);
  };

  return (
    <div style={{
      border: '1px solid #ddd',
      borderRadius: '8px',
      padding: '16px',
      margin: '16px 0',
      backgroundColor: '#fff'
    }}>
      <h3 style={{ margin: '0 0 16px 0', color: '#333' }}>服务器配置</h3>
      
      <div style={{ marginBottom: '12px' }}>
        <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>
          服务器地址:
        </label>
        <input
          type="text"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          style={{
            width: '100%',
            padding: '8px',
            border: '1px solid #ddd',
            borderRadius: '4px',
            marginBottom: '8px'
          }}
        />
      </div>

      <div style={{ marginBottom: '16px' }}>
        <label style={{ display: 'block', marginBottom: '8px', fontWeight: 'bold' }}>
          快速选择:
        </label>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
          {predefinedUrls.map((presetUrl) => (
            <button
              key={presetUrl}
              onClick={() => setUrl(presetUrl)}
              style={{
                padding: '4px 12px',
                backgroundColor: url === presetUrl ? '#2196F3' : '#f0f0f0',
                color: url === presetUrl ? 'white' : '#333',
                border: '1px solid #ddd',
                borderRadius: '4px',
                cursor: 'pointer',
                fontSize: '12px'
              }}
            >
              {presetUrl}
            </button>
          ))}
        </div>
      </div>

      <button
        onClick={handleApply}
        disabled={url === currentUrl}
        style={{
          padding: '8px 16px',
          backgroundColor: url === currentUrl ? '#ccc' : '#4CAF50',
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          cursor: url === currentUrl ? 'not-allowed' : 'pointer'
        }}
      >
        应用设置
      </button>

      <div style={{
        marginTop: '12px',
        padding: '8px',
        backgroundColor: '#e3f2fd',
        borderRadius: '4px',
        fontSize: '12px',
        color: '#1976d2'
      }}>
        <strong>提示:</strong> 确保服务器正在运行在指定的地址和端口上。
        默认情况下，BlazorWebGame 服务器运行在 https://localhost:7000。
      </div>
    </div>
  );
};

export default ServerConfig;