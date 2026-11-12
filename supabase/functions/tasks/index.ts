// Edge Function `tasks` — TZ section 3 / section 5.
//   POST /tasks/review-scan  (hourly cron, service_role only)
// S1: height_units > p99 of the board          -> review
// S2: player < 3 finished runs and top-10      -> review
// S3: input histogram flat/zero vs median      -> review
// S4: device_hash on >2 accounts in the top    -> review all
// S5 (GA vs DB population reconcile) is manual per TZ — not implemented here.

import { err, isServiceRole, json, serviceClient } from "../_shared/api.ts";

interface Flag {
  leaderboardId: string;
  playerId: string;
  runId: string;
  rule: string;
  note: string;
}

function percentile(values: number[], p: number): number {
  const s = [...values].sort((a, b) => a - b);
  if (s.length === 0) return 0;
  const idx = (s.length - 1) * p;
  const lo = Math.floor(idx), hi = Math.ceil(idx);
  return s[lo] + (s[hi] - s[lo]) * (idx - lo);
}

function median(values: number[]): number {
  return percentile(values, 0.5);
}

async function scanBoard(boardId: string, flags: Flag[]): Promise<void> {
  const db = serviceClient();
  const { data: rows } = await db
    .from("entries")
    .select(
      "player_id, height_units, run_id, players!inner(device_hash), runs!inner(verdict)",
    )
    .eq("leaderboard_id", boardId)
    .eq("runs.verdict", "verified")
    .order("height_units", { ascending: false })
    .limit(100);
  const entries = rows ?? [];
  if (entries.length === 0) return;

  const flag = (e: (typeof entries)[number], rule: string, note: string) =>
    flags.push({
      leaderboardId: boardId,
      playerId: e.player_id as string,
      runId: e.run_id as string,
      rule,
      note,
    });

  // S1: above p99 (only when the board has enough entries to be meaningful)
  if (entries.length >= 10) {
    const p99 = percentile(entries.map((e) => e.height_units as number), 0.99);
    for (const e of entries) {
      if ((e.height_units as number) > p99) {
        flag(e, "S1", `height ${e.height_units} > p99 ${p99.toFixed(0)}`);
      }
    }
  }

  // S2: new account (<3 finished runs total) inside the top-10
  for (const [i, e] of entries.entries()) {
    if (i >= 10) break;
    const { count } = await db
      .from("runs")
      .select("run_id", { count: "exact", head: true })
      .eq("player_id", e.player_id)
      .eq("status", "finished");
    if ((count ?? 0) < 3) {
      flag(e, "S2", `top-10 entry with only ${count ?? 0} finished runs`);
    }
  }

  // S3: aggregate input histograms across the entry runs; flag zero-total or
  // dominant-kind share far below the board median (suspiciously flat input)
  const shares: { e: (typeof entries)[number]; total: number; dom: number }[] = [];
  for (const e of entries) {
    const { data: chunks } = await db
      .from("run_chunks")
      .select("payload")
      .eq("run_id", e.run_id);
    const hist: Record<string, number> = {};
    for (const c of chunks ?? []) {
      const h = (c.payload as { inputHistogram?: Record<string, number> })
        .inputHistogram ?? {};
      for (const [k, v] of Object.entries(h)) hist[k] = (hist[k] ?? 0) + v;
    }
    const total = Object.values(hist).reduce((a, b) => a + b, 0);
    const dom = total > 0 ? Math.max(...Object.values(hist)) / total : 0;
    shares.push({ e, total, dom });
  }
  const medDom = median(shares.filter((s) => s.total > 0).map((s) => s.dom));
  for (const s of shares) {
    if (s.total === 0) flag(s.e, "S3", "zero input histogram");
    else if (medDom > 0 && s.dom < medDom * 0.5) {
      flag(s.e, "S3", `dominant input share ${s.dom.toFixed(2)} vs median ${medDom.toFixed(2)}`);
    }
  }

  // S4: same device_hash on >2 player accounts in the top
  const byDevice = new Map<string, typeof entries>();
  for (const e of entries) {
    const dh = (e.players as { device_hash: string | null }).device_hash;
    if (!dh) continue;
    const list = byDevice.get(dh) ?? [];
    list.push(e);
    byDevice.set(dh, list);
  }
  for (const [dh, list] of byDevice) {
    if (list.length > 2) {
      for (const e of list) {
        flag(e, "S4", `device_hash ${dh.slice(0, 8)}… on ${list.length} accounts`);
      }
    }
  }
}

Deno.serve(async (req) => {
  try {
    const parts = new URL(req.url).pathname.split("/").filter(Boolean);
    if (parts[0] === "tasks") parts.shift();
    if (req.method !== "POST" || parts[0] !== "review-scan") {
      return err(404, "not_found");
    }
    if (!isServiceRole(req)) return err(403, "forbidden");

    const db = serviceClient();
    const { data: boards } = await db
      .from("leaderboards")
      .select("id")
      .in("status", ["open", "closing"]);

    const flags: Flag[] = [];
    for (const b of boards ?? []) await scanBoard(b.id as string, flags);

    const seen = new Set<string>();
    for (const f of flags) {
      if (seen.has(f.runId)) continue; // one moderation row per run per scan
      seen.add(f.runId);
      await db.from("runs").update({ verdict: "review" })
        .eq("run_id", f.runId).eq("verdict", "verified");
      await db.from("moderation").insert({
        entry_leaderboard_id: f.leaderboardId,
        entry_player_id: f.playerId,
        action: "review",
        note: `${f.rule}: ${f.note}`,
      });
    }
    return json({ scanned: boards?.length ?? 0, flagged: seen.size });
  } catch (e) {
    return err(500, "internal", e instanceof Error ? e.message : String(e));
  }
});
