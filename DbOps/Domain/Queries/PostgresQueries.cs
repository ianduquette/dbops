namespace DbOps.Domain.Queries;

public static class PostgresQueries {
    public const string GetActiveSessions = @"
        SELECT
            pid,
            datname as database_name,
            usename as username,
            application_name,
            client_addr,
            client_hostname,
            state,
            COALESCE(query, '') as query,
            query_start,
            wait_event_type,
            wait_event,
            state_change,
            backend_start,
            xact_start,
            CASE
                WHEN state = 'active' THEN true
                ELSE false
            END as is_active
        FROM pg_stat_activity
        WHERE state != 'idle'
            AND pid != pg_backend_pid()
            AND datname IS NOT NULL
        ORDER BY datname, application_name, query_start;";

    public const string TestConnection = @"SELECT version();";

    public const string GetTrackActivityQuerySize = @"SHOW track_activity_query_size;";

    public const string SetTrackActivityQuerySize = @"
        -- Temporarily increase query tracking size for this session
        SET track_activity_query_size = 8192;";

    public const string GetSessionLocks = @"
        SELECT
            l.locktype,
            l.mode,
            l.granted,
            CASE
                WHEN l.relation IS NOT NULL THEN l.relation::regclass::text
                ELSE NULL
            END as relation_name,
            l.page,
            l.tuple,
            l.virtualxid,
            l.transactionid,
            l.classid,
            l.objid,
            l.objsubid
        FROM pg_locks l
        WHERE l.pid = @pid
        ORDER BY l.locktype, l.mode;";

    public const string GetBlockingRelationships = @"
        SELECT
            blocked_locks.pid AS blocked_pid,
            blocked_activity.usename AS blocked_user,
            blocked_activity.application_name AS blocked_app,
            blocking_locks.pid AS blocking_pid,
            blocking_activity.usename AS blocking_user,
            blocking_activity.application_name AS blocking_app,
            blocked_locks.locktype AS lock_type,
            blocked_locks.mode AS requested_mode,
            blocking_locks.mode AS held_mode,
            CASE
                WHEN blocked_locks.relation IS NOT NULL THEN blocked_locks.relation::regclass::text
                ELSE 'N/A'
            END as relation_name
        FROM pg_catalog.pg_locks blocked_locks
        JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
        JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
            AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
            AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
            AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
            AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
            AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
            AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
            AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
            AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
            AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
            AND blocking_locks.pid != blocked_locks.pid
        JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
        WHERE NOT blocked_locks.granted
        AND (blocked_locks.pid = @pid OR blocking_locks.pid = @pid);";
}
