using DbOps.Domain.Models;
using DbOps.Domain.Queries;
using Npgsql;

namespace DbOps.Domain.Services;

public class SyncPostgresService {
    private readonly string _connectionString;

    public SyncPostgresService(string host, int port, string database, string username, string password) {
        var builder = new NpgsqlConnectionStringBuilder {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            Timeout = 10,
            CommandTimeout = 10,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 5
        };
        _connectionString = builder.ToString();
    }

    public bool TestConnection() {
        try {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(PostgresQueries.TestConnection, connection);
            command.CommandTimeout = 10;
            var result = command.ExecuteScalar();

            return result != null;
        } catch (NpgsqlException ex) {
            // Log specific PostgreSQL errors for debugging
            System.Diagnostics.Debug.WriteLine($"PostgreSQL connection test failed: {ex.SqlState} - {ex.Message}");
            return false;
        } catch (Exception ex) {
            // Log general connection errors
            System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    public List<DatabaseSession> GetActiveSessions() {
        var sessions = new List<DatabaseSession>();

        try {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(PostgresQueries.GetActiveSessions, connection);
            command.CommandTimeout = 10;
            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                var session = new DatabaseSession {
                    Pid = reader.GetInt32(0),
                    DatabaseName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ApplicationName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ClientAddress = reader.IsDBNull(4) ? "" : reader.GetValue(4)?.ToString() ?? "",
                    ClientHostname = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    State = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    CurrentQuery = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    QueryStart = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    WaitEventType = reader.IsDBNull(9) ? null : reader.GetString(9),
                    WaitEvent = reader.IsDBNull(10) ? null : reader.GetString(10),
                    StateChange = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    BackendStart = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                    TransactionStart = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                    IsActive = !reader.IsDBNull(14) && reader.GetBoolean(14),
                    ServerName = GetConnectionInfo() // Set server name from connection info
                };

                sessions.Add(session);
            }
        } catch (NpgsqlException ex) {
            // Provide more specific error messages for PostgreSQL errors
            var errorMessage = ex.SqlState switch {
                "08000" => "Connection exception - database server may be down",
                "08003" => "Connection does not exist - connection was lost",
                "08006" => "Connection failure - network or server issue",
                "28000" => "Invalid authorization - check username/password",
                "3D000" => "Invalid catalog name - database does not exist",
                _ => $"Database connection error: {ex.Message}"
            };
            throw new InvalidOperationException(errorMessage, ex);
        } catch (Exception ex) {
            // Handle other connection-related exceptions
            var errorMessage = ex.Message.Contains("timeout")
                ? "Connection timeout - database server may be overloaded or unreachable"
                : $"Failed to retrieve sessions: {ex.Message}";
            throw new InvalidOperationException(errorMessage, ex);
        }

        return sessions;
    }

    public List<DatabaseLock> GetSessionLocks(int pid) {
        var locks = new List<DatabaseLock>();

        try {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(PostgresQueries.GetSessionLocks, connection);
            command.Parameters.AddWithValue("@pid", pid);
            command.CommandTimeout = 10;
            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                var lockItem = new DatabaseLock {
                    LockType = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Mode = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Granted = !reader.IsDBNull(2) && reader.GetBoolean(2),
                    RelationName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Page = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Tuple = reader.IsDBNull(5) ? null : reader.GetInt16(5),
                    VirtualXid = reader.IsDBNull(6) ? null : reader.GetString(6),
                    TransactionId = reader.IsDBNull(7) ? null : (uint)reader.GetInt64(7),
                    ClassId = reader.IsDBNull(8) ? null : (uint)reader.GetInt64(8),
                    ObjId = reader.IsDBNull(9) ? null : (uint)reader.GetInt64(9),
                    ObjSubId = reader.IsDBNull(10) ? null : reader.GetInt16(10)
                };

                locks.Add(lockItem);
            }
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to retrieve locks for PID {pid}: {ex.Message}", ex);
        }

        return locks;
    }

    public List<BlockingRelationship> GetBlockingRelationships(int pid) {
        var relationships = new List<BlockingRelationship>();

        try {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(PostgresQueries.GetBlockingRelationships, connection);
            command.Parameters.AddWithValue("@pid", pid);
            command.CommandTimeout = 10;
            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                var relationship = new BlockingRelationship {
                    BlockedPid = reader.GetInt32(0),
                    BlockedUser = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    BlockedApp = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    BlockingPid = reader.GetInt32(3),
                    BlockingUser = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    BlockingApp = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    LockType = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    RequestedMode = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    HeldMode = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    RelationName = reader.IsDBNull(9) ? "" : reader.GetString(9)
                };

                relationships.Add(relationship);
            }
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to retrieve blocking relationships for PID {pid}: {ex.Message}", ex);
        }

        return relationships;
    }

    public void LoadLockingInformation(DatabaseSession session) {
        try {
            session.Locks = GetSessionLocks(session.Pid);
            session.BlockingRelationships = GetBlockingRelationships(session.Pid);
        } catch (Exception ex) {
            // Log error but don't fail the entire operation
            session.Locks = new List<DatabaseLock>();
            session.BlockingRelationships = new List<BlockingRelationship>();
            throw new InvalidOperationException($"Failed to load locking information for session {session.Pid}: {ex.Message}", ex);
        }
    }

    public string GetConnectionInfo() {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        return $"{builder.Host}:{builder.Port}/{builder.Database}";
    }

    public int GetTrackActivityQuerySize() {
        try {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(PostgresQueries.GetTrackActivityQuerySize, connection);
            command.CommandTimeout = 10;
            var result = command.ExecuteScalar();

            if (result != null && int.TryParse(result.ToString(), out int size)) {
                return size;
            }
            return -1; // Unknown
        } catch {
            return -1; // Error
        }
    }
}
