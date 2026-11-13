// Edge Function `leaderboards` — TZ section 3.
//   GET /leaderboards/{id}?offset&limit
//   GET /leaderboards/current      -> resolves to the newest open board
// Top-100 of verified entries plus the caller's own entry and rank.
// Entries whose run was moved to `review` by the cron scan are hidden.

import { authedUser, err, json, serviceClient } from "../_shared/api.ts";

const MAX_LIMIT = 100;
// Naive per-isolate rate limit: 30 req/min per auth subject. Edge isolates are
// ephemeral, so this is best-effort, not a strong anti-abuse control.
const RATE_LIMIT = 30;
const rateWindow = new Map<string, { count: number; resetAt: number }>();

function rateLimited(key: string): boolean {
  const now = Date.now();
  const w = rateWindow.get(key);
  if (!w || w.resetAt < now) {
    rateWindow.set(key, { count: 1, resetAt: now + 60_000 });
    return false;
  }
  w.count++;
  return w.count > RATE_LIMIT;
}

Deno.serve(async (req) => {
  try {
    if (req.method !== "GET") return err(405, "method_not_allowed");
    const user = await authedUser(req);
    if (user === null) return err(401, "unauthorized");
    if (rateLimited(user.userId)) return err(429, "rate_limited");

    const url = new URL(req.url);
    const parts = url.pathname.split("/").filter(Boolean);
    if (parts[0] === "leaderboards") parts.shift();
    if (!parts[0]) return err(404, "not_found");

    const offset = Math.max(0, parseInt(url.searchParams.get("offset") ?? "0") || 0);
    const limit = Math.min(
      MAX_LIMIT,
      Math.max(1, parseInt(url.searchParams.get("limit") ?? "100") || 100),
    );

    const db = serviceClient();

    let boardId = parts[0];
    let board;
    if (boardId === "current") {
      const { data } = await db
        .from("leaderboards")
        .select("id, ruleset_id, seed, status, closes_at")
        .eq("status", "open")
        .order("closes_at", { ascending: false })
        .limit(1)
        .maybeSingle();
      board = data;
      if (board) boardId = board.id;
    } else {
      const { data } = await db
        .from("leaderboards")
        .select("id, ruleset_id, seed, status, closes_at")
        .eq("id", boardId)
        .maybeSingle();
      board = data;
    }
    if (!board) return err(404, "not_found");

    // public top: verified runs only
    const { data: rows, error } = await db
      .from("entries")
      .select("player_id, height_units, achieved_at, players!inner(nickname), runs!inner(verdict)")
      .eq("leaderboard_id", boardId)
      .eq("runs.verdict", "verified")
      .order("height_units", { ascending: false })
      .range(offset, offset + limit - 1);
    if (error) return err(500, "db_error", error.message);

    const top = (rows ?? []).map((r, i) => ({
      rank: offset + i + 1,
      nickname: (r.players as unknown as { nickname: string | null }).nickname,
      heightUnits: r.height_units,
      achievedAt: r.achieved_at,
    }));

    // caller's own entry + rank
    const { data: me } = await db
      .from("players")
      .select("id")
      .eq("anon_user_id", user.userId)
      .maybeSingle();
    let self = null;
    if (me) {
      const { data: myEntry } = await db
        .from("entries")
        .select("height_units, achieved_at, runs!inner(verdict)")
        .eq("leaderboard_id", boardId)
        .eq("player_id", me.id)
        .maybeSingle();
      if (myEntry) {
        const { count } = await db
          .from("entries")
          .select("player_id, runs!inner(verdict)", { count: "exact", head: true })
          .eq("leaderboard_id", boardId)
          .eq("runs.verdict", "verified")
          .gt("height_units", myEntry.height_units);
        self = {
          rank: (count ?? 0) + 1,
          heightUnits: myEntry.height_units,
          achievedAt: myEntry.achieved_at,
          verdict: (myEntry.runs as unknown as { verdict: string }).verdict,
          public: (myEntry.runs as unknown as { verdict: string }).verdict === "verified",
        };
      }
    }

    return json({
      leaderboard: board,
      offset,
      limit,
      top,
      self,
    }, 200, { "Cache-Control": "public, max-age=30" });
  } catch (e) {
    return err(500, "internal", e instanceof Error ? e.message : String(e));
  }
});
