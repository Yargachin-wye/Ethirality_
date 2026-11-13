// HTTP + Supabase helpers for Edge Functions. All secrets come from env;
// local dev provides them via `supabase start` defaults or .env.

import { createClient, SupabaseClient } from "jsr:@supabase/supabase-js@2";

export function env(name: string): string {
  const v = Deno.env.get(name);
  if (!v) throw new Error(`missing env var ${name}`);
  return v;
}

let _service: SupabaseClient | null = null;

// service_role client — bypasses RLS. All client tables are locked down, so
// every row access is explicitly mediated by these functions.
export function serviceClient(): SupabaseClient {
  if (_service === null) {
    _service = createClient(env("SUPABASE_URL"), env("SUPABASE_SERVICE_ROLE_KEY"), {
      auth: { persistSession: false },
    });
  }
  return _service;
}

export interface AuthedUser {
  userId: string; // auth.users.id of the registered (email) account
}

// Validates the user JWT carried in Authorization: Bearer. Anonymous
// sessions and accounts without an email are refused: ranked play requires
// a registered account.
export async function authedUser(req: Request): Promise<AuthedUser | null> {
  const auth = req.headers.get("Authorization");
  if (!auth?.startsWith("Bearer ")) return null;
  const client = createClient(env("SUPABASE_URL"), env("SUPABASE_ANON_KEY"), {
    auth: { persistSession: false },
  });
  const { data, error } = await client.auth.getUser(auth.slice(7));
  if (error || !data.user || data.user.is_anonymous || !data.user.email) return null;
  return { userId: data.user.id };
}

// True when the caller bears the service_role JWT (used by cron routes).
export function isServiceRole(req: Request): boolean {
  const auth = req.headers.get("Authorization");
  if (!auth?.startsWith("Bearer ")) return false;
  try {
    const payload = JSON.parse(atob(auth.slice(7).split(".")[1]));
    return payload.role === "service_role";
  } catch {
    return false;
  }
}

export function json(body: unknown, status = 200, headers: HeadersInit = {}): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json", ...headers },
  });
}

export function noContent(): Response {
  return new Response(null, { status: 204 });
}

export function err(status: number, code: string, message?: string): Response {
  return json({ error: code, message }, status);
}
