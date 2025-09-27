import React, { useState } from 'react';
import { BattleStateDto, StartBattleRequest } from '../types';

interface Props {
  onStartBattle: (request: StartBattleRequest) => Promise<void>;
  onStopBattle: (battleId: string) => Promise<void>;
  onGetBattleState: (battleId: string) => Promise<void>;
  currentBattle?: BattleStateDto;
  isLoading: boolean;
  error?: string;
}

const BattleTest: React.FC<Props> = ({
  onStartBattle,
  onStopBattle,
  onGetBattleState,
  currentBattle,
  isLoading,
  error
}) => {
  const [characterId, setCharacterId] = useState('test-character-1');
  const [enemyId, setEnemyId] = useState('goblin');
  const [battleId, setBattleId] = useState('');

  const handleStartBattle = async () => {
    await onStartBattle({
      characterId,
      enemyId
    });
  };

  const handleStopBattle = async () => {
    if (currentBattle?.battleId) {
      await onStopBattle(currentBattle.battleId);
    } else if (battleId) {
      await onStopBattle(battleId);
    }
  };

  const handleGetBattleState = async () => {
    if (currentBattle?.battleId) {
      await onGetBattleState(currentBattle.battleId);
    } else if (battleId) {
      await onGetBattleState(battleId);
    }
  };

  return (
    <div style={{
      border: '1px solid #ddd',
      borderRadius: '8px',
      padding: '16px',
      margin: '16px 0',
      backgroundColor: '#fff'
    }}>
      <h3 style={{ margin: '0 0 16px 0', color: '#333' }}>战斗系统测试</h3>

      {error && (
        <div style={{
          background: '#ffebee',
          color: '#c62828',
          padding: '8px',
          borderRadius: '4px',
          marginBottom: '16px',
          fontSize: '14px'
        }}>
          <strong>错误:</strong> {error}
        </div>
      )}

      <div style={{ marginBottom: '16px' }}>
        <div style={{ marginBottom: '8px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>
            角色 ID:
          </label>
          <input
            type="text"
            value={characterId}
            onChange={(e) => setCharacterId(e.target.value)}
            style={{
              width: '100%',
              padding: '8px',
              border: '1px solid #ddd',
              borderRadius: '4px'
            }}
          />
        </div>

        <div style={{ marginBottom: '8px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>
            敌人 ID:
          </label>
          <input
            type="text"
            value={enemyId}
            onChange={(e) => setEnemyId(e.target.value)}
            style={{
              width: '100%',
              padding: '8px',
              border: '1px solid #ddd',
              borderRadius: '4px'
            }}
          />
        </div>

        <div style={{ marginBottom: '16px' }}>
          <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>
            战斗 ID (用于查询/停止):
          </label>
          <input
            type="text"
            value={battleId}
            onChange={(e) => setBattleId(e.target.value)}
            placeholder={currentBattle?.battleId || '输入战斗ID或开始新战斗'}
            style={{
              width: '100%',
              padding: '8px',
              border: '1px solid #ddd',
              borderRadius: '4px'
            }}
          />
        </div>
      </div>

      <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
        <button
          onClick={handleStartBattle}
          disabled={isLoading}
          style={{
            padding: '8px 16px',
            backgroundColor: isLoading ? '#ccc' : '#4CAF50',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: isLoading ? 'not-allowed' : 'pointer'
          }}
        >
          {isLoading ? '处理中...' : '开始战斗'}
        </button>

        <button
          onClick={handleGetBattleState}
          disabled={isLoading}
          style={{
            padding: '8px 16px',
            backgroundColor: isLoading ? '#ccc' : '#2196F3',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: isLoading ? 'not-allowed' : 'pointer'
          }}
        >
          查询战斗状态
        </button>

        <button
          onClick={handleStopBattle}
          disabled={isLoading}
          style={{
            padding: '8px 16px',
            backgroundColor: isLoading ? '#ccc' : '#F44336',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: isLoading ? 'not-allowed' : 'pointer'
          }}
        >
          停止战斗
        </button>
      </div>

      {currentBattle && (
        <div style={{
          background: '#f5f5f5',
          padding: '12px',
          borderRadius: '4px',
          marginTop: '16px'
        }}>
          <h4 style={{ margin: '0 0 8px 0' }}>当前战斗状态:</h4>
          <div style={{ fontSize: '14px', lineHeight: '1.4' }}>
            <div><strong>战斗 ID:</strong> {currentBattle.battleId}</div>
            <div><strong>角色:</strong> {currentBattle.characterId}</div>
            <div><strong>敌人:</strong> {currentBattle.enemyId}</div>
            <div><strong>状态:</strong> {currentBattle.isActive ? '进行中' : '已结束'}</div>
            <div><strong>玩家生命值:</strong> {currentBattle.playerHealth}/{currentBattle.playerMaxHealth}</div>
            <div><strong>敌人生命值:</strong> {currentBattle.enemyHealth}/{currentBattle.enemyMaxHealth}</div>
            <div><strong>最后更新:</strong> {new Date(currentBattle.lastUpdated).toLocaleString()}</div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BattleTest;