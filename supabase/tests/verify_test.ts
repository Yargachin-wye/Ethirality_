// P1-P10 rejection coverage on generated chunks: one honest run plus one
// scenario per check. Mutations only touch payload fields, never the stored
// hashes, so each test trips exactly its own check (P10 mutates prevHash).

import {
  assertEquals,
  assertStringIncludes,
} from "https://deno.land/std@0.224.0/assert/mod.ts";
import { verifyRun, VerifyInput } from "../functions/_shared/verify.ts";
import { cloneRun, GeneratedRun, genHonestRun, LIMITS } from "./gen.ts";

const honest = await genHonestRun();

function input(run: GeneratedRun): VerifyInput {
  return {
    chunks: run.chunks,
    finish: run.finish,
    wallElapsedSeconds: run.wallElapsedSeconds,
    genesisHash: run.genesisHash,
    limits: LIMITS,
  };
}

function expectRejected(run: GeneratedRun, code: string) {
  const result = verifyRun(input(run));
  assertEquals(result.verdict, "rejected");
  if (result.verdict === "rejected") {
    assertStringIncludes(result.rejectReason, `${code}:`);
  }
}

Deno.test("honest run with pause, dashes, upgrade and death verifies", () => {
  const result = verifyRun(input(honest));
  assertEquals(result.verdict, "verified");
  if (result.verdict === "verified") {
    assertEquals(result.verifiedHeight, honest.finish.heightUnits);
  }
});

Deno.test("P1: more reported ticks than wall time (speedhack)", () => {
  const run = cloneRun(honest);
  run.wallElapsedSeconds = run.totalTicks * LIMITS.fixedDeltaTime * 0.5;
  expectRejected(run, "P1");
});

Deno.test("P3: teleport (y jump between checkpoints)", () => {
  const run = cloneRun(honest);
  const mid = run.chunks[5].payload.checkpoints.find((c) => c.tick === 2600)!;
  mid.y += 500;
  mid.vy = 10;
  expectRejected(run, "P3");
});

Deno.test("P7: claimed height exceeds checkpoint-derived height", () => {
  const run = cloneRun(honest);
  run.finish.heightUnits += 5000;
  expectRejected(run, "P7");
});

Deno.test("P9: non-finite checkpoint field", () => {
  const run = cloneRun(honest);
  run.chunks[1].payload.checkpoints[0].y = NaN;
  expectRejected(run, "P9");
});

Deno.test("P10: broken prevHash chain", () => {
  const run = cloneRun(honest);
  run.chunks[3].payload.prevHash = "0".repeat(64);
  expectRejected(run, "P10");
});
