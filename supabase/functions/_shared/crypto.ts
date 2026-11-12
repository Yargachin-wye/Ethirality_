// Hash chain + HMAC helpers. Session keys are derived server-side as
// HMAC(RUN_SESSION_SECRET, runId) so only the sha256 of the key is stored.

const te = new TextEncoder();

export function toHex(bytes: ArrayBuffer | Uint8Array): string {
  const arr = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
  let out = "";
  for (const b of arr) out += b.toString(16).padStart(2, "0");
  return out;
}

export function fromHex(hex: string): Uint8Array<ArrayBuffer> {
  const out = new Uint8Array(new ArrayBuffer(hex.length / 2));
  for (let i = 0; i < out.length; i++) out[i] = parseInt(hex.substr(i * 2, 2), 16);
  return out;
}

export async function sha256Hex(data: string | Uint8Array): Promise<string> {
  const bytes = typeof data === "string" ? te.encode(data) : new Uint8Array(data);
  return toHex(await crypto.subtle.digest("SHA-256", bytes));
}

async function hmacKey(keyBytes: Uint8Array): Promise<CryptoKey> {
  return await crypto.subtle.importKey(
    "raw",
    new Uint8Array(keyBytes),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
}

export async function hmacSha256Hex(
  keyBytes: Uint8Array,
  data: string | Uint8Array,
): Promise<string> {
  const key = await hmacKey(keyBytes);
  const bytes = typeof data === "string" ? te.encode(data) : data;
  return toHex(await crypto.subtle.sign("HMAC", key, new Uint8Array(bytes)));
}

export function timingSafeEqualHex(a: string, b: string): boolean {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

// Session key for a run. The raw key is sent to the client once at
// runs/start; afterwards the server only needs it to verify chunk HMACs and
// can always re-derive it from the master secret.
export function deriveSessionKey(secret: string, runId: string): Promise<string> {
  return hmacSha256Hex(te.encode(secret), `session:${runId}`);
}

// Stable JSON: sorted object keys, no undefined. Both sides must produce
// identical bytes for hashing/HMAC to agree.
export function canonicalJson(value: unknown): string {
  if (value === null || typeof value !== "object") return JSON.stringify(value);
  if (Array.isArray(value)) {
    return "[" + value.map(canonicalJson).join(",") + "]";
  }
  const obj = value as Record<string, unknown>;
  const keys = Object.keys(obj)
    .filter((k) => obj[k] !== undefined)
    .sort();
  return "{" + keys.map((k) => JSON.stringify(k) + ":" + canonicalJson(obj[k])).join(",") + "}";
}

// Hash chain: chunkHash_i = sha256(prevHash + ":" + canonical(body)).
// The covered body is the chunk without seq/prevHash/hmac transport fields
// plus seq itself, so a reordered or rewritten chunk is detectable.
export function chunkBodyForHash(chunk: {
  seq: number;
  tick: number;
  checkpoints: unknown;
  events: unknown;
  inputHistogram: unknown;
}): string {
  return canonicalJson({
    seq: chunk.seq,
    tick: chunk.tick,
    checkpoints: chunk.checkpoints ?? [],
    events: chunk.events ?? [],
    inputHistogram: chunk.inputHistogram ?? {},
  });
}

export function computeChunkHash(prevHash: string, chunk: {
  seq: number;
  tick: number;
  checkpoints: unknown;
  events: unknown;
  inputHistogram: unknown;
}): Promise<string> {
  return sha256Hex(`${prevHash}:${chunkBodyForHash(chunk)}`);
}

// Genesis prevHash for seq=0, shared by client and server.
export function genesisHash(runId: string, sessionKey: string): Promise<string> {
  return sha256Hex(`${runId}:${sessionKey}:genesis`);
}
