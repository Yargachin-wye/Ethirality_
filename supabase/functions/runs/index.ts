// Edge Function `runs` — TZ section 3.
//   POST /runs/start
//   POST /runs/{id}/chunks
//   POST /runs/{id}/finish
//   GET  /runs/{id}
//
// HMAC contract (must match the Unity client):
//   sessionKey  = HMAC_SHA256(RUN_SESSION_SECRET, "session:"+runId)   [hex]
//   genesisHash = SHA256(runId + ":" + sessionKey + ":genesis")       [hex]
//   chunkHash_i = SHA256(prevHash + ":" + canonicalJson({seq,tick,
//                 checkpoints,events,inputHistogram}))                [hex]
//   hmac_i      = HMAC_SHA256(sessionKeyBytes, ascii(chunkHash_i))    [hex]

import {
  authedUser,
  env,
  err,
  isServiceRole,
  json,
  noContent,
  serviceClient,
} from "../_shared/api.ts";
import {
  computeChunkHash,
  deriveSessionKey,
  fromHex,
  genesisHash,
  hmacSha256Hex,
  sha256Hex,
  timingSafeEqualHex,
} from "../_shared/crypto.ts";
import { ChunkPayload, RunLimits } from "../_shared/types.ts";
import { StoredChunk, verifyRun } from "../_shared/verify.ts";

function sessionSecret(): string {
  return env("RUN_SESSION_SECRET");
}

async function playerForUser(userId: string) {
  const { data, error } = await serviceClient()
    .from("players")
    .select("*")
    .eq("anon_user_id", userId)
    .maybeSingle();
  if (error) throw error;
  return data;
}

async function loadRunOwned(runId: string, userId: string) {
  const { data, error } = await serviceClient()
    .from("runs")
    .select("*, players!inner(anon_user_id)")
    .eq("run_id", runId)
    .maybeSingle();
  if (error) throw error;
  if (!data) return { run: null, owned: false };
  return { run: data, owned: data.players.anon_user_id === userId };
}

// ---------- POST /runs/start

async function handleStart(req: Request, userId: string): Promise<Response> {
  const db = serviceClient();
  const body = await req.json().catch(() => ({}));
  const { buildId, leaderboardId, nickname, deviceHash } = body as {
    buildId?: string;
    leaderboardId?: string;
    nickname?: string;
    deviceHash?: string;
  };
  if (!buildId) return err(400, "bad_request", "buildId required");

  await db.rpc("expire_stale_runs");

  // upsert player bound to the anonymous auth user
  let player = await playerForUser(userId);
  if (player === null) {
    const { data, error } = await db
      .from("players")
      .insert({ anon_user_id: userId, nickname, device_hash: deviceHash })
      .select()
      .single();
    if (error) return err(500, "db_error", error.message);
    player = data;
  } else if (nickname !== undefined || deviceHash !== undefined) {
    await db.from("players").update({
      nickname: nickname ?? player.nickname,
      device_hash: deviceHash ?? player.device_hash,
    }).eq("id", player.id);
  }
  if (player.banned_at !== null) return err(403, "player_banned");

  const { data: build } = await db
    .from("builds")
    .select("*")
    .eq("build_id", buildId)
    .maybeSingle();
  if (!build) return err(400, "unknown_build");
  if (build.status !== "allowed") return err(403, "build_revoked");

  let lbQuery = db.from("leaderboards").select("*").eq("status", "open");
  if (leaderboardId) lbQuery = lbQuery.eq("id", leaderboardId);
  else lbQuery = lbQuery.eq("ruleset_id", build.ruleset_id);
  const { data: boards } = await lbQuery
    .order("created_at", { ascending: false })
    .limit(1);
  const board = boards?.[0];
  if (!board) return err(409, "no_open_leaderboard");
  if (board.closes_at && new Date(board.closes_at) < new Date()) {
    return err(409, "leaderboard_closed");
  }

  const { data: active } = await db
    .from("runs")
    .select("run_id")
    .eq("player_id", player.id)
    .eq("status", "active")
    .maybeSingle();
  if (active) return err(409, "run_already_active", active.run_id);

  const { data: ruleset } = await db
    .from("rulesets")
    .select("fixed_delta_time")
    .eq("ruleset_id", board.ruleset_id)
    .single();

  const runId = crypto.randomUUID();
  const sessionKey = await deriveSessionKey(sessionSecret(), runId);
  const expiresAt = new Date(Date.now() + board.max_session_seconds * 1000);

  const { error: insErr } = await db.from("runs").insert({
    run_id: runId,
    player_id: player.id,
    leaderboard_id: board.id,
    build_id: buildId,
    session_key_hash: await sha256Hex(sessionKey),
    expires_at: expiresAt.toISOString(),
  });
  if (insErr) {
    if (insErr.code === "23505") return err(409, "run_already_active");
    return err(500, "db_error", insErr.message);
  }

  return json({
    runId,
    leaderboardId: board.id,
    seed: board.seed,
    sessionKey,
    expiresAt: expiresAt.toISOString(),
    fixedDeltaTime: ruleset?.fixed_delta_time ?? 0.02,
  });
}

// ---------- POST /runs/{id}/chunks

async function handleChunks(
  req: Request,
  run: Record<string, unknown>,
): Promise<Response> {
  const db = serviceClient();
  const body = (await req.json().catch(() => null)) as
    | (ChunkPayload & { hmac?: string })
    | null;
  if (body === null) return err(400, "bad_request");
  const { seq, tick, checkpoints, events, inputHistogram, prevHash, hmac } = body;
  if (
    !Number.isInteger(seq) || seq < 0 ||
    typeof tick !== "number" || !Number.isFinite(tick) ||
    typeof prevHash !== "string" || typeof hmac !== "string"
  ) {
    return err(400, "bad_request");
  }

  const runId = run.run_id as string;
  const chunkCore = { seq, tick, checkpoints: checkpoints ?? [], events: events ?? [], inputHistogram: inputHistogram ?? {} };
  const chunkHash = await computeChunkHash(prevHash, chunkCore);

  const { data: last } = await db
    .from("run_chunks")
    .select("seq, chunk_hash, payload")
    .eq("run_id", runId)
    .order("seq", { ascending: false })
    .limit(1);
  const lastSeq = last && last.length > 0 ? (last[0].seq as number) : -1;

  if (seq <= lastSeq) {
    // idempotent replay: identical chunk -> 204, different payload -> 409
    const { data: stored } = await db
      .from("run_chunks")
      .select("payload, chunk_hash")
      .eq("run_id", runId)
      .eq("seq", seq)
      .single();
    if (!stored) return err(409, "seq_conflict");
    // hash covers seq+body+prevHash, so equality is enough for idempotency
    return stored.chunk_hash === chunkHash
      ? noContent()
      : err(409, "seq_conflict");
  }
  if (seq !== lastSeq + 1) return err(409, "seq_gap");
  if (tick < (run.last_tick as number)) return err(409, "tick_rollback");

  const sessionKey = await deriveSessionKey(sessionSecret(), runId);
  const expectedPrev = lastSeq < 0
    ? await genesisHash(runId, sessionKey)
    : (last![0].chunk_hash as string);
  if (prevHash !== expectedPrev) return err(409, "prev_hash_mismatch");

  const expectedHmac = await hmacSha256Hex(fromHex(sessionKey), chunkHash);
  const hmacOk = timingSafeEqualHex(hmac.toLowerCase(), expectedHmac);
  if (!hmacOk) return err(401, "bad_hmac");

  const { error: insErr } = await db.from("run_chunks").insert({
    run_id: runId,
    seq,
    payload: { ...chunkCore, prevHash }, // P10 needs prevHash back at finish
    hmac_ok: hmacOk,
    chunk_hash: chunkHash,
  });
  if (insErr) {
    if (insErr.code === "23505") return err(409, "seq_conflict");
    return err(500, "db_error", insErr.message);
  }
  await db
    .from("runs")
    .update({ last_tick: Math.max(run.last_tick as number, Math.floor(tick)) })
    .eq("run_id", runId);
  return noContent();
}

// ---------- POST /runs/{id}/finish

async function handleFinish(
  req: Request,
  run: Record<string, unknown>,
): Promise<Response> {
  const db = serviceClient();
  const body = (await req.json().catch(() => null)) as {
    tick?: number;
    heightUnits?: number;
    endReason?: string;
    lastHash?: string;
  } | null;
  if (
    body === null || typeof body.tick !== "number" ||
    typeof body.heightUnits !== "number" || typeof body.lastHash !== "string"
  ) {
    return err(400, "bad_request");
  }
  const runId = run.run_id as string;

  if (run.status === "finished") {
    return json({
      verdict: run.verdict,
      rejectReason: run.reject_reason,
      verifiedHeight: run.verified_height,
    });
  }

  // atomically close the session; loses the race -> return existing verdict
  const { data: closed } = await db
    .from("runs")
    .update({
      status: "finished",
      claimed_height: Math.floor(body.heightUnits),
      end_reason: body.endReason ?? null,
      finished_at: new Date().toISOString(),
      last_tick: Math.max(run.last_tick as number, Math.floor(body.tick)),
    })
    .eq("run_id", runId)
    .eq("status", "active")
    .gt("expires_at", new Date().toISOString())
    .select()
    .maybeSingle();
  if (!closed) {
    const { data: fresh } = await db
      .from("runs")
      .select("status, verdict, reject_reason, verified_height")
      .eq("run_id", runId)
      .single();
    if (fresh?.status === "finished") {
      return json({
        verdict: fresh.verdict,
        rejectReason: fresh.reject_reason,
        verifiedHeight: fresh.verified_height,
      });
    }
    await db.from("runs").update({ status: "expired" }).eq("run_id", runId)
      .eq("status", "active");
    return err(410, "run_expired");
  }

  const { data: chunkRows } = await db
    .from("run_chunks")
    .select("seq, payload, chunk_hash")
    .eq("run_id", runId)
    .order("seq", { ascending: true });
  const chunks: StoredChunk[] = (chunkRows ?? []).map((r) => ({
    seq: r.seq as number,
    payload: r.payload as ChunkPayload,
    hash: r.chunk_hash as string,
  }));

  const { data: board } = await db
    .from("leaderboards")
    .select("*, rulesets!inner(*)")
    .eq("id", run.leaderboard_id)
    .single();
  const rs = board?.rulesets;
  const limits: RunLimits = {
    rulesetId: board?.ruleset_id,
    allowedUpgradeIds: rs?.allowed_upgrade_ids ?? [],
    fixedDeltaTime: rs?.fixed_delta_time ?? 0.02,
    maxVerticalSpeed: rs?.max_vertical_speed ?? 30,
    maxHp: rs?.max_hp ?? 100,
    maxSessionSeconds: board?.max_session_seconds ?? 7200,
    maxPauseSeconds: board?.max_pause_seconds ?? 900,
    minHistogramRunSeconds: 30,
    heightToleranceUnits: 100, // +-1 game unit around checkpoint-derived height
  };

  const sessionKey = await deriveSessionKey(sessionSecret(), runId);
  const wallElapsedSeconds =
    (new Date(closed.finished_at as string).getTime() -
      new Date(run.started_at as string).getTime()) / 1000;

  const result = verifyRun({
    chunks,
    finish: {
      tick: body.tick,
      heightUnits: body.heightUnits,
      endReason: body.endReason ?? "unknown",
      lastHash: body.lastHash,
    },
    wallElapsedSeconds,
    genesisHash: await genesisHash(runId, sessionKey),
    limits,
  });

  const update: Record<string, unknown> = { verdict: result.verdict };
  if (result.verdict === "verified") {
    update.verified_height = result.verifiedHeight;
  } else {
    update.reject_reason = result.rejectReason;
  }
  await db.from("runs").update(update).eq("run_id", runId);

  if (result.verdict === "verified") {
    await db.rpc("record_entry", {
      p_leaderboard_id: run.leaderboard_id,
      p_player_id: run.player_id,
      p_height_units: result.verifiedHeight,
      p_run_id: runId,
    });
  }

  return json({
    verdict: result.verdict,
    rejectReason: result.verdict === "rejected" ? result.rejectReason : null,
    verifiedHeight: result.verdict === "verified" ? result.verifiedHeight : null,
  });
}

// ---------- GET /runs/{id}

function handleGetRun(run: Record<string, unknown>): Response {
  return json({
    runId: run.run_id,
    status: run.status,
    verdict: run.verdict,
    rejectReason: run.reject_reason,
    claimedHeight: run.claimed_height,
    verifiedHeight: run.verified_height,
    startedAt: run.started_at,
    expiresAt: run.expires_at,
    lastTick: run.last_tick,
  });
}

// ---------- router

Deno.serve(async (req) => {
  try {
    const parts = new URL(req.url).pathname.split("/").filter(Boolean);
    if (parts[0] === "runs") parts.shift(); // strip function mount name

    const user = await authedUser(req);
    if (user === null) return err(401, "unauthorized");

    if (req.method === "POST" && parts[0] === "start") {
      return await handleStart(req, user.userId);
    }

    const runId = parts[0];
    if (!runId) return err(404, "not_found");
    const { run, owned } = await loadRunOwned(runId, user.userId);
    if (!run) return err(404, "not_found");
    if (!owned) return err(403, "forbidden");

    if (req.method === "GET" && parts.length === 1) {
      return handleGetRun(run);
    }
    if (req.method === "POST" && parts[1] === "chunks") {
      if (run.status !== "active") return err(409, `run_${run.status}`);
      return await handleChunks(req, run);
    }
    if (req.method === "POST" && parts[1] === "finish") {
      return await handleFinish(req, run);
    }
    return err(404, "not_found");
  } catch (e) {
    return err(500, "internal", e instanceof Error ? e.message : String(e));
  }
});
