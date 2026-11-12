-- Leaderboard schema (TZ section 2). All access goes through Edge Functions
-- running as service_role; anon/authenticated roles get no direct grants.

create table rulesets (
    ruleset_id           text primary key,
    allowed_upgrade_ids  int[] not null default '{}',
    fixed_delta_time     real not null default 0.02,
    max_vertical_speed   real not null default 30.0,   -- units/sec before x1.2 headroom
    max_hp               int  not null default 100,
    checkpoint_seconds   real not null default 2.0,
    created_at           timestamptz not null default now()
);

create table players (
    id           uuid primary key default gen_random_uuid(),
    anon_user_id uuid not null unique references auth.users (id),
    nickname     text,
    device_hash  text,
    created_at   timestamptz not null default now(),
    banned_at    timestamptz
);

create table builds (
    build_id     text primary key,
    game_version text not null,
    ruleset_id   text not null references rulesets (ruleset_id),
    status       text not null default 'allowed' check (status in ('allowed', 'revoked'))
);

create table leaderboards (
    id                  uuid primary key default gen_random_uuid(),
    ruleset_id          text not null references rulesets (ruleset_id),
    seed                int  not null,
    status              text not null default 'open' check (status in ('open', 'closing', 'archived')),
    closes_at           timestamptz,
    max_session_seconds int not null default 7200,
    max_pause_seconds   int not null default 900,
    created_at          timestamptz not null default now()
);

create table runs (
    run_id           uuid primary key default gen_random_uuid(),
    player_id        uuid not null references players (id),
    leaderboard_id   uuid not null references leaderboards (id),
    build_id         text not null references builds (build_id),
    session_key_hash text not null,
    started_at       timestamptz not null default now(),
    expires_at       timestamptz not null,
    last_tick        bigint not null default 0,
    status           text not null default 'active' check (status in ('active', 'finished', 'expired')),
    verdict          text not null default 'pending' check (verdict in ('pending', 'verified', 'rejected', 'review')),
    reject_reason    text,
    claimed_height   bigint,
    verified_height  bigint,
    end_reason       text,
    finished_at      timestamptz
);

-- One active run per player (partial unique index).
create unique index runs_one_active_per_player
    on runs (player_id) where status = 'active';

create index runs_leaderboard_idx on runs (leaderboard_id);
create index runs_player_idx on runs (player_id);

create table run_chunks (
    run_id      uuid not null references runs (run_id),
    seq         int  not null,
    received_at timestamptz not null default now(),
    payload     jsonb not null,
    hmac_ok     bool not null,
    chunk_hash  text not null,
    primary key (run_id, seq)
);

create table entries (
    leaderboard_id uuid not null references leaderboards (id),
    player_id      uuid not null references players (id),
    height_units   bigint not null,
    run_id         uuid not null references runs (run_id),
    achieved_at    timestamptz not null default now(),
    primary key (leaderboard_id, player_id)
);

create index entries_height_idx on entries (leaderboard_id, height_units desc);

create table moderation (
    id                   bigint generated always as identity primary key,
    entry_leaderboard_id uuid not null,
    entry_player_id      uuid not null,
    action               text not null,
    note                 text,
    created_at           timestamptz not null default now(),
    foreign key (entry_leaderboard_id, entry_player_id)
        references entries (leaderboard_id, player_id)
);

-- Clients never write verified_height / entries: no grants at all, RLS on.
alter table rulesets    enable row level security;
alter table players     enable row level security;
alter table builds      enable row level security;
alter table leaderboards enable row level security;
alter table runs        enable row level security;
alter table run_chunks  enable row level security;
alter table entries     enable row level security;
alter table moderation  enable row level security;

revoke all on rulesets, players, builds, leaderboards, runs, run_chunks, entries, moderation
    from anon, authenticated;
