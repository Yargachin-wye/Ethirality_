// Deterministic generator of an honest ranked run: checkpoints every 100
// ticks, one pause, two dashes, one upgrade, damage+heal, death at the end.
// Produces the exact chunk format the Unity RunRecorder will send, including
// the prevHash chain.

import {
  computeChunkHash,
  deriveSessionKey,
  genesisHash,
} from "../functions/_shared/crypto.ts";
import {
  CheckpointPayload,
  ChunkPayload,
  FinishPayload,
  RunLimits,
  UNITS_PER_HEIGHT,
} from "../functions/_shared/types.ts";
import { StoredChunk } from "../functions/_shared/verify.ts";

export const LIMITS: RunLimits = {
  rulesetId: "openworld-height-v1",
  allowedUpgradeIds: [0, 1, 2, 3, 4],
  fixedDeltaTime: 0.02,
  maxVerticalSpeed: 30,
  maxHp: 100,
  maxSessionSeconds: 7200,
  maxPauseSeconds: 900,
  minHistogramRunSeconds: 30,
  heightToleranceUnits: 100,
};

export interface GeneratedRun {
  runId: string;
  sessionKey: string;
  chunks: StoredChunk[];
  finish: FinishPayload;
  genesisHash: string;
  wallElapsedSeconds: number;
  totalTicks: number;
}

const CP_EVERY = 100;
const CHUNK_TICKS = 500;
const PAUSE: [number, number] = [1500, 1800];
const DASH_TICKS = new Set([600, 3100]);
const DEATH_TICK = 4000;

function vyAt(tick: number): number {
  if (tick >= 2500 && tick < 3100) return -5; // descent stretch
  if (tick >= 3100) return 8;
  return 10;
}

function effTicks(a: number, b: number): number {
  let span = b - a;
  span -= Math.max(0, Math.min(b, PAUSE[1]) - Math.max(a, PAUSE[0]));
  return Math.max(0, span);
}

function cpTicks(totalTicks: number): number[] {
  const out: number[] = [];
  for (let t = 0; t <= totalTicks; t += CP_EVERY) {
    if (t > PAUSE[0] && t < PAUSE[1]) continue; // frozen during pause
    out.push(t);
  }
  return out;
}

export async function genHonestRun(totalTicks = DEATH_TICK): Promise<GeneratedRun> {
  const fdt = LIMITS.fixedDeltaTime;
  const runId = "11111111-2222-4333-8444-555555555555";
  const secret = Deno.env.get("RUN_SESSION_SECRET") ?? "unit-test-secret";
  const sessionKey = await deriveSessionKey(secret, runId);
  const genesis = await genesisHash(runId, sessionKey);

  const ticks = cpTicks(totalTicks);
  const cps: CheckpointPayload[] = [];
  let y = 0;
  let hp = 100;
  for (let i = 0; i < ticks.length; i++) {
    const t = ticks[i];
    if (i > 0) y += 0.5 * (vyAt(ticks[i - 1]) + vyAt(t)) * effTicks(ticks[i - 1], t) * fdt;
    if (t > 750 && t <= 950) hp = 85;
    else if (t > 950 && t < DEATH_TICK - CP_EVERY) hp = 100;
    else if (t >= DEATH_TICK - CP_EVERY) hp = t === ticks[ticks.length - 1] ? 0 : 100;
    const dashing = DASH_TICKS.has(t);
    const prevT = i > 0 ? ticks[i - 1] : null;
    const dashCooldown = dashing
      ? 2.0
      : prevT !== null && DASH_TICKS.has(prevT)
      ? Math.max(0, 2.0 - effTicks(prevT, t) * fdt)
      : 0;
    cps.push({
      tick: t,
      y,
      vy: vyAt(t),
      hp,
      alive: t < ticks[ticks.length - 1],
      frozen: false,
      dashing,
      dashCooldown,
      upgrades: t > 2350 ? [2] : [],
    });
  }

  const events = [
    { type: "damage" as const, tick: 750, amount: 15 },
    { type: "heal" as const, tick: 950, amount: 15 },
    { type: "pauseStart" as const, tick: PAUSE[0] },
    { type: "pauseEnd" as const, tick: PAUSE[1] },
    { type: "upgrade" as const, tick: 2350, upgradeId: 2 },
    { type: "damage" as const, tick: DEATH_TICK - 1, amount: 100 },
    { type: "death" as const, tick: DEATH_TICK },
  ];

  const chunks: StoredChunk[] = [];
  let prevHash = genesis;
  for (let seq = 0; seq * CHUNK_TICKS < totalTicks; seq++) {
    const lo = seq * CHUNK_TICKS, hi = lo + CHUNK_TICKS;
    const inRange = (t: number) => seq === 0 ? t >= lo && t <= hi : t > lo && t <= hi;
    const payload: ChunkPayload = {
      seq,
      tick: hi,
      checkpoints: cps.filter((c) => inRange(c.tick)),
      events: events.filter((e) => inRange(e.tick)),
      inputHistogram: {
        look: 50,
        shot: 10,
        freeze: 0,
        dash: cps.filter((c) => inRange(c.tick) && c.dashing).length,
      },
      prevHash,
    };
    const hash = await computeChunkHash(prevHash, payload);
    chunks.push({ seq, payload, hash });
    prevHash = hash;
  }

  const startY = cps[0].y;
  const maxY = Math.max(...cps.map((c) => c.y));
  const finish: FinishPayload = {
    tick: totalTicks,
    heightUnits: Math.floor((maxY - startY) * UNITS_PER_HEIGHT),
    endReason: "death",
    lastHash: prevHash,
  };

  return {
    runId,
    sessionKey,
    chunks,
    finish,
    genesisHash: genesis,
    // wall clock a bit longer than tick time — honest slack
    wallElapsedSeconds: totalTicks * fdt + 10,
    totalTicks,
  };
}

export function cloneRun(r: GeneratedRun): GeneratedRun {
  return {
    ...r,
    chunks: r.chunks.map((c) => ({
      seq: c.seq,
      hash: c.hash,
      payload: JSON.parse(JSON.stringify(c.payload)),
    })),
    finish: { ...r.finish },
  };
}
