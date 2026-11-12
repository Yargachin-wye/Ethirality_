-- Seed data: ruleset, current build, first open leaderboard.
insert into rulesets (
    ruleset_id, allowed_upgrade_ids, fixed_delta_time,
    max_vertical_speed, max_hp, checkpoint_seconds
) values (
    'openworld-height-v1',
    '{0,1,2,3,4}',        -- resId of Arrow/Dash/Jaw/Shield/Spike improvements
    0.02,
    30.0,
    100,
    2.0
);

insert into builds (build_id, game_version, ruleset_id, status) values
    ('dev-local', '0.1.0', 'openworld-height-v1', 'allowed');

insert into leaderboards (
    ruleset_id, seed, status, closes_at, max_session_seconds, max_pause_seconds
) values (
    'openworld-height-v1',
    1,
    'open',
    now() + interval '30 days',
    7200,
    900
);
