using DbOps.Models;
using DbOps.Queries;
using Npgsql;

namespace DbOps.Services;

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
        } catch {
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
                    State = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CurrentQuery = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    QueryStart = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    WaitEventType = reader.IsDBNull(8) ? null : reader.GetString(8),
                    WaitEvent = reader.IsDBNull(9) ? null : reader.GetString(9),
                    StateChange = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    BackendStart = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    TransactionStart = reader.IsDBNull(12) ? null : reader.GetDateTime(12)
                };

                sessions.Add(session);
            }
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to retrieve sessions: {ex.Message}", ex);
        }

        return sessions;
    }

    public string GetConnectionInfo() {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        return $"{builder.Host}:{builder.Port}/{builder.Database}";
    }
}
