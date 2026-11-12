-- Atomic entry upsert used by runs/finish: keeps the best height only.
create or replace function record_entry(
    p_leaderboard_id uuid,
    p_player_id uuid,
    p_height_units bigint,
    p_run_id uuid
) returns void
language plpgsql
security definer
set search_path = public
as $$
begin
    insert into entries (leaderboard_id, player_id, height_units, run_id)
    values (p_leaderboard_id, p_player_id, p_height_units, p_run_id)
    on conflict (leaderboard_id, player_id) do update
        set height_units = greatest(entries.height_units, excluded.height_units),
            run_id       = case
                               when excluded.height_units > entries.height_units
                                   then excluded.run_id
                               else entries.run_id
                           end,
            achieved_at  = case
                               when excluded.height_units > entries.height_units
                                   then now()
                               else entries.achieved_at
                           end;
end;
$$;

-- Lazily expire sessions past expires_at. Called at the top of every RPC path
-- so expired runs stop accepting chunks/finish even without a cron.
create or replace function expire_stale_runs() returns int
language sql
security definer
set search_path = public
as $$
    with updated as (
        update runs
        set status = 'expired'
        where status = 'active' and expires_at < now()
        returning run_id
    )
    select count(*)::int from updated;
$$;

revoke all on function record_entry(uuid, uuid, bigint, uuid) from anon, authenticated;
revoke all on function expire_stale_runs() from anon, authenticated;
