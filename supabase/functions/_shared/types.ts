// Wire protocol shared between the Unity client (RunRecorder) and Edge
// Functions. Mirrors Leaderboard/RunInput.cs and Leaderboard/RunScore.cs.

export const RUN_INPUT_KINDS = ["look", "freeze", "dash", "shot"] as const;
export type RunInputKind = typeof RUN_INPUT_KINDS[number];

export type InputHistogram = Partial<Record<RunInputKind, number>>;

// RunCheckpoint without the fields the client may omit on the wire.
// tick is per-checkpoint so the server can do kinematics across chunk borders.
export interface CheckpointPayload {
  tick: number;
  x?: number;
  y: number;
  vx?: number;
  vy: number;
  rotation?: number;
  angularVelocity?: number;
  hp: number;
  alive: boolean;
  frozen: boolean;
  dashing: boolean;
  dashCooldown: number;
  upgrades: number[];
}

export const RUN_EVENT_TYPES = [
  "damage",
  "heal",
  "upgrade",
  "pauseStart",
  "pauseEnd",
  "death",
] as const;
export type RunEventType = typeof RUN_EVENT_TYPES[number];

export interface RunEvent {
  type: RunEventType;
  tick: number;
  amount?: number; // damage / heal
  upgradeId?: number; // upgrade
}

// Body of POST /runs/{id}/chunks. prevHash chains chunks; hmac covers the
// canonical form of everything except the hmac field itself.
export interface ChunkPayload {
  seq: number;
  tick: number;
  checkpoints: CheckpointPayload[];
  events: RunEvent[];
  inputHistogram: InputHistogram;
  prevHash: string;
}

export interface FinishPayload {
  tick: number;
  heightUnits: number;
  endReason: "death" | "sceneExit" | "applicationQuit" | string;
  lastHash: string;
}

// Server-side limits resolved from rulesets + leaderboards rows.
export interface RunLimits {
  rulesetId: string;
  allowedUpgradeIds: number[];
  fixedDeltaTime: number;
  maxVerticalSpeed: number;
  maxHp: number;
  maxSessionSeconds: number;
  maxPauseSeconds: number;
  minHistogramRunSeconds: number;
  heightToleranceUnits: number;
}

export const UNITS_PER_HEIGHT = 100; // RunScore.UnitsPerHeight
