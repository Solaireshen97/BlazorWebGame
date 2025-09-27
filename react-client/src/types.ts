// TypeScript interfaces matching the server's DTOs

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors: string[];
  timestamp: string;
}

export interface BattleStateDto {
  battleId: string;
  characterId: string;
  enemyId: string;
  partyId?: string;
  isActive: boolean;
  playerHealth: number;
  playerMaxHealth: number;
  enemyHealth: number;
  enemyMaxHealth: number;
  lastUpdated: string;
  battleType: BattleType;
  partyMemberIds: string[];
  players: BattleParticipantDto[];
  enemies: BattleParticipantDto[];
  status: BattleStatus;
  playerTargets: Record<string, string>;
  recentActions: BattleActionDto[];
  result?: BattleResultDto;
}

export interface BattleParticipantDto {
  id: string;
  name: string;
  health: number;
  maxHealth: number;
  attackPower: number;
  attacksPerSecond: number;
  attackCooldown: number;
  equippedSkills: string[];
  skillCooldowns: Record<string, number>;
  isPlayer: boolean;
}

export interface BattleActionDto {
  actorId: string;
  actorName: string;
  targetId: string;
  targetName: string;
  actionType: BattleActionType;
  damage: number;
  skillId?: string;
  timestamp: string;
  isCritical: boolean;
}

export interface BattleResultDto {
  victory: boolean;
  experienceGained: number;
  goldGained: number;
  itemsLooted: string[];
  completedAt: string;
}

export interface StartBattleRequest {
  characterId: string;
  enemyId: string;
  partyId?: string;
}

export interface CharacterDto {
  id: string;
  name: string;
  health: number;
  maxHealth: number;
  gold: number;
  isDead: boolean;
  revivalTimeRemaining: number;
  currentAction: string;
  selectedBattleProfession: string;
  lastUpdated: string;
}

export enum BattleType {
  Normal = 0,
  Dungeon = 1
}

export enum BattleStatus {
  Active = 0,
  Paused = 1,
  Completed = 2,
  Cancelled = 3
}

export enum BattleActionType {
  Attack = 0,
  UseSkill = 1,
  Defend = 2,
  Move = 3,
  UseItem = 4
}

export interface ConnectionStatus {
  isConnected: boolean;
  serverUrl: string;
  signalRConnected: boolean;
  lastPing?: number;
  error?: string;
}