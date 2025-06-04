namespace DbOps.Queries;

public static class PostgresQueries {
    public const string GetActiveSessions = @"
        SELECT
            pid,
            datname as database_name,
            usename as username,
            application_name,
            client_addr,
            state,
            query,
            query_start,
            wait_event_type,
            wait_event,
            state_change,
            backend_start,
            xact_start
        FROM pg_stat_activity
        WHERE state != 'idle'
            AND pid != pg_backend_pid()
            AND datname IS NOT NULL
        ORDER BY datname, application_name, query_start;";

    public const string TestConnection = @"SELECT version();";
}
