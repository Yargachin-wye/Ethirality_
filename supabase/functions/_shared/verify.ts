// Synchronous telemetry checks P1-P10 (TZ section 4). Pure module: no I/O,
// no Supabase client — chunk hashes arrive precomputed at ingest time, so the
// whole verifier is deterministic and unit-testable with generated chunks.

import {
  CheckpointPayload,
  ChunkPayload,
  FinishPayload,
  RUN_EVENT_TYPES,
  RUN_INPUT_KINDS,
  RunEvent,
  RunLimits,
  UNITS_PER_HEIGHT,
} from "./types.ts";

export interface StoredChunk {
  seq: number;
  payload: ChunkPayload;
  hash: string; // sha256 of prevHash + canonical body, computed at ingest
}

export interface VerifyInput {
  chunks: StoredChunk[]; // ordered by seq
  finish: FinishPayload;
  wallElapsedSeconds: number; // finished_at - started_at
  genesisHash: string;
  limits: RunLimits;
}

export type VerifyResult =
  | { verdict: "verified"; verifiedHeight: number }
  | { verdict: "rejected"; rejectReason: string };

const PAUSE_EPS = 0.05;
const DASH_COOLDOWN_EPS = 0.05;

function isFiniteNum(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

function isInt(v: unknown): v is number {
  return typeof v === "number" && Number.isInteger(v);
}

function allCheckpoints(chunks: StoredChunk[]): CheckpointPayload[] {
  const out: CheckpointPayload[] = [];
  for (const c of chunks) {
    for (const cp of c.payload.checkpoints ?? []) out.push(cp);
  }
  return out;
}

function allEvents(chunks: StoredChunk[]): RunEvent[] {
  const out: RunEvent[] = [];
  for (const c of chunks) {
    for (const e of c.payload.events ?? []) out.push(e);
  }
  return out.sort((a, b) => a.tick - b.tick);
}

// Paused tick intervals [start, end). During a pause the world is frozen, so
// kinematics/cooldowns compare against ticks with pause time subtracted.
function pauseIntervals(chunks: StoredChunk[], finishTick: number): [number, number][] {
  const out: [number, number][] = [];
  let open = -1;
  for (const e of allEvents(chunks)) {
    if (e.type === "pauseStart") open = e.tick;
    else if (e.type === "pauseEnd" && open >= 0) {
      out.push([open, e.tick]);
      open = -1;
    }
  }
  if (open >= 0) out.push([open, finishTick]);
  return out;
}

function effectiveTicks(a: number, b: number, pauses: [number, number][]): number {
  let span = b - a;
  for (const [s, e] of pauses) {
    span -= Math.max(0, Math.min(b, e) - Math.max(a, s));
  }
  return Math.max(0, span);
}

// ---------- P9: structural validity (mirrors RunCheckpoint/RunInput.IsValid)

function checkpointValid(cp: CheckpointPayload): boolean {
  return cp != null &&
    isFiniteNum(cp.tick) && cp.tick >= 0 &&
    isFiniteNum(cp.y) && isFiniteNum(cp.vy) &&
    (cp.x === undefined || isFiniteNum(cp.x)) &&
    (cp.vx === undefined || isFiniteNum(cp.vx)) &&
    (cp.rotation === undefined || isFiniteNum(cp.rotation)) &&
    (cp.angularVelocity === undefined || isFiniteNum(cp.angularVelocity)) &&
    isInt(cp.hp) &&
    typeof cp.alive === "boolean" &&
    typeof cp.frozen === "boolean" &&
    typeof cp.dashing === "boolean" &&
    isFiniteNum(cp.dashCooldown) &&
    Array.isArray(cp.upgrades) && cp.upgrades.length <= 1024 &&
    cp.upgrades.every((u) => isInt(u));
}

function eventValid(e: RunEvent): boolean {
  return e != null &&
    (RUN_EVENT_TYPES as readonly string[]).includes(e.type) &&
    isFiniteNum(e.tick) && e.tick >= 0 &&
    (e.amount === undefined || (isFiniteNum(e.amount) && e.amount >= 0)) &&
    (e.upgradeId === undefined || isInt(e.upgradeId));
}

function histogramValid(h: ChunkPayload["inputHistogram"]): boolean {
  if (h == null || typeof h !== "object") return false;
  for (const [k, v] of Object.entries(h)) {
    if (!(RUN_INPUT_KINDS as readonly string[]).includes(k)) return false;
    if (!isInt(v) || v < 0) return false;
  }
  return true;
}

function p9Structural(input: VerifyInput): string | null {
  for (const c of input.chunks) {
    const p = c.payload;
    if (!isFiniteNum(p.tick) || p.tick < 0) return "chunk tick invalid";
    if (!Array.isArray(p.checkpoints)) return "checkpoints missing";
    if (!Array.isArray(p.events)) return "events missing";
    if (!histogramValid(p.inputHistogram)) return "inputHistogram invalid";
    if (!p.checkpoints.every(checkpointValid)) return "checkpoint IsValid failed";
    if (!p.events.every(eventValid)) return "event invalid";
  }
  return null;
}

// ---------- P10: seq/tick order + prevHash chain

function p10Chain(input: VerifyInput): string | null {
  const { chunks, finish, genesisHash } = input;
  let expectedPrev = genesisHash;
  let lastTick = -1;
  for (let i = 0; i < chunks.length; i++) {
    const c = chunks[i];
    if (c.seq !== i) return `seq gap at ${c.seq}, expected ${i}`;
    if (!isFiniteNum(c.payload.tick) || c.payload.tick < lastTick) {
      return `tick rollback at seq ${c.seq}`;
    }
    lastTick = c.payload.tick;
    if (c.payload.prevHash !== expectedPrev) {
      return `prevHash mismatch at seq ${c.seq}`;
    }
    expectedPrev = c.hash;
  }
  // checkpoint ticks must not regress across the whole stream
  const cps = allCheckpoints(chunks);
  for (let i = 1; i < cps.length; i++) {
    if (cps[i].tick < cps[i - 1].tick) return "checkpoint tick rollback";
  }
  return null;
}

// ---------- the individual checks

function p1Speedhack(input: VerifyInput): string | null {
  const { finish, wallElapsedSeconds, limits } = input;
  if (!isFiniteNum(finish.tick) || finish.tick < 0) return "finish tick invalid";
  const reported = finish.tick * limits.fixedDeltaTime;
  if (reported > wallElapsedSeconds * 1.25 + 1) {
    return `reported ${reported.toFixed(2)}s of ticks in ${wallElapsedSeconds.toFixed(2)}s wall time`;
  }
  return null;
}

function p2PauseAndDuration(input: VerifyInput): string | null {
  const { chunks, finish, wallElapsedSeconds, limits } = input;
  if (wallElapsedSeconds > limits.maxSessionSeconds * 1.05) {
    return `session ${wallElapsedSeconds.toFixed(0)}s over limit ${limits.maxSessionSeconds}s`;
  }
  const events = allEvents(chunks);
  let pauseTicks = 0;
  let openStart: number | null = null;
  for (const e of events) {
    if (e.type === "pauseStart") {
      if (openStart !== null) return "nested pauseStart";
      openStart = e.tick;
    } else if (e.type === "pauseEnd") {
      if (openStart === null) return "pauseEnd without pauseStart";
      pauseTicks += e.tick - openStart;
      openStart = null;
    }
  }
  if (openStart !== null) pauseTicks += finish.tick - openStart; // paused at finish
  const pauseSeconds = pauseTicks * limits.fixedDeltaTime;
  if (pauseSeconds > limits.maxPauseSeconds) {
    return `pause ${pauseSeconds.toFixed(0)}s over limit ${limits.maxPauseSeconds}s`;
  }
  return null;
}

function p3Kinematics(input: VerifyInput): string | null {
  const { limits, finish } = input;
  const cps = allCheckpoints(input.chunks);
  const pauses = pauseIntervals(input.chunks, finish.tick);
  const maxVy = limits.maxVerticalSpeed * 2.5;
  for (let i = 1; i < cps.length; i++) {
    const a = cps[i - 1];
    const b = cps[i];
    const dt = effectiveTicks(a.tick, b.tick, pauses) * limits.fixedDeltaTime;
    if (dt <= 0) continue; // ordering violations handled by P10
    const dy = b.y - a.y;
    if (dy / dt > maxVy) {
      return `vertical speed ${(dy / dt).toFixed(1)}u/s over ${maxVy.toFixed(1)}`;
    }
  }
  return null;
}

function p4Resources(input: VerifyInput): string | null {
  const { limits, finish } = input;
  const cps = allCheckpoints(input.chunks);
  const events = allEvents(input.chunks);
  const deathIdx = events.findIndex((e) => e.type === "death");
  if (deathIdx !== -1 && deathIdx !== events.length - 1) {
    return "death is not the last event";
  }
  let aliveFlips = 0;
  for (let i = 0; i < cps.length; i++) {
    const c = cps[i];
    if (c.hp < 0 || c.hp > limits.maxHp) return `hp ${c.hp} out of range`;
    if (i > 0) {
      const prev = cps[i - 1];
      let dmg = 0, heal = 0;
      for (const e of events) {
        if (e.tick > prev.tick && e.tick <= c.tick) {
          if (e.type === "damage") dmg += e.amount ?? 0;
          if (e.type === "heal") heal += e.amount ?? 0;
        }
      }
      const expected = prev.hp - dmg + heal;
      if (c.hp !== expected) {
        return `hp ${c.hp} != ${expected} at tick ${c.tick}`;
      }
      if (!prev.alive && c.alive) return "alive resurrected";
      if (prev.alive && !c.alive) aliveFlips++;
    }
  }
  if (aliveFlips > 1) return "alive flipped more than once";
  if (finish.endReason === "death") {
    if (deathIdx === -1) return "death finish without death event";
    if (cps.length > 0 && cps[cps.length - 1].alive) {
      return "death finish but last checkpoint alive";
    }
  }
  return null;
}

function p5MovementState(input: VerifyInput): string | null {
  const { limits, finish } = input;
  const pauses = pauseIntervals(input.chunks, finish.tick);
  let prev: CheckpointPayload | null = null;
  for (const chunk of input.chunks) {
    const freezeInputs = chunk.payload.inputHistogram.freeze ?? 0;
    let frozenFlips = 0;
    for (const c of chunk.payload.checkpoints) {
      if (prev !== null) {
        const dt = effectiveTicks(prev.tick, c.tick, pauses) * limits.fixedDeltaTime;
        if (dt > 0 && c.dashCooldown < prev.dashCooldown - dt - DASH_COOLDOWN_EPS) {
          return `dashCooldown dropped ${(
            prev.dashCooldown - c.dashCooldown
          ).toFixed(2)} in ${dt.toFixed(2)}s`;
        }
        if (!prev.dashing && c.dashing && dt > 0 && prev.dashCooldown > dt + DASH_COOLDOWN_EPS) {
          return `dash at tick ${c.tick} with cooldown ${prev.dashCooldown.toFixed(2)}`;
        }
        if (prev.frozen !== c.frozen) frozenFlips++;
      }
      prev = c;
    }
    if (frozenFlips > freezeInputs) {
      return `frozen flipped ${frozenFlips}x with ${freezeInputs} freeze inputs`;
    }
  }
  return null;
}

function p6Upgrades(input: VerifyInput): string | null {
  const allowed = new Set(input.limits.allowedUpgradeIds);
  const cps = allCheckpoints(input.chunks);
  const events = allEvents(input.chunks);
  for (const c of cps) {
    for (const u of c.upgrades) {
      if (!allowed.has(u)) return `upgrade id ${u} not in ruleset`;
    }
  }
  let upgradeEvents = 0;
  for (const e of events) {
    if (e.type === "upgrade") {
      upgradeEvents++;
      if (e.upgradeId === undefined || !allowed.has(e.upgradeId)) {
        return `upgrade event id ${e.upgradeId} not in ruleset`;
      }
    }
  }
  const finalLen = cps.length > 0 ? cps[cps.length - 1].upgrades.length : 0;
  if (upgradeEvents !== finalLen) {
    return `${upgradeEvents} upgrade events vs ${finalLen} final upgrades`;
  }
  return null;
}

function p7ClaimedHeight(input: VerifyInput): string | null {
  const { finish, limits } = input;
  const cps = allCheckpoints(input.chunks);
  if (!isFiniteNum(finish.heightUnits) || finish.heightUnits < 0) {
    return "claimed height invalid";
  }
  if (cps.length === 0) {
    return finish.heightUnits > limits.heightToleranceUnits
      ? "claimed height without checkpoints"
      : null;
  }
  const startY = cps[0].y;
  let maxY = startY;
  for (const c of cps) maxY = Math.max(maxY, c.y);
  const computed = Math.floor((maxY - startY) * UNITS_PER_HEIGHT);
  // Checkpoints are ~2s apart, so the true peak can exceed the sampled maximum
  // by up to about a second of maximum climb speed.
  const overshoot = limits.maxVerticalSpeed * 1.2 * UNITS_PER_HEIGHT;
  const diff = finish.heightUnits - computed;
  if (diff > limits.heightToleranceUnits + overshoot) {
    return `claimed ${finish.heightUnits} vs computed ${computed}`;
  }
  return null;
}

function p8Histogram(input: VerifyInput): string | null {
  const { chunks, finish, limits } = input;
  const runSeconds = finish.tick * limits.fixedDeltaTime;
  if (runSeconds <= limits.minHistogramRunSeconds) return null;
  let total = 0, look = 0;
  for (const c of chunks) {
    for (const [k, v] of Object.entries(c.payload.inputHistogram)) {
      total += v;
      if (k === "look") look += v;
    }
  }
  if (total === 0) return "empty inputHistogram on a long run";
  if (look === 0) return "no look inputs on a long run";
  return null;
}

export function verifyRun(input: VerifyInput): VerifyResult {
  // Only integrity and blatant-cheat checks reject a run; gameplay heuristics
  // (pauses, hp, dash, upgrades, input histogram) are not enforced so that
  // honest players are never kept out of the board.
  const checks: [string, (i: VerifyInput) => string | null][] = [
    ["P1", p1Speedhack],
    ["P3", p3Kinematics],
    ["P7", p7ClaimedHeight],
    ["P9", p9Structural],
    ["P10", p10Chain],
  ];
  for (const [code, check] of checks) {
    const reason = check(input);
    if (reason !== null) {
      return { verdict: "rejected", rejectReason: `${code}: ${reason}` };
    }
  }
  return { verdict: "verified", verifiedHeight: input.finish.heightUnits };
}
