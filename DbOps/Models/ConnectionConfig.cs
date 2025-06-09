using System.Text.Json.Serialization;

namespace DbOps.Models;

public class ConnectionConfig {
    [JsonPropertyName("configVersion")]
    public string ConfigVersion { get; set; } = "1.0";

    [JsonPropertyName("defaultConnectionId")]
    public string? DefaultConnectionId { get; set; }

    [JsonPropertyName("connections")]
    public List<DatabaseConnection> Connections { get; set; } = new();

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Helper methods
    public DatabaseConnection? GetDefaultConnection() {
        if (string.IsNullOrEmpty(DefaultConnectionId)) {
            return Connections.FirstOrDefault(c => c.IsDefault);
        }
        return Connections.FirstOrDefault(c => c.Id == DefaultConnectionId);
    }

    public DatabaseConnection? GetConnection(string id) {
        return Connections.FirstOrDefault(c => c.Id == id);
    }

    public void AddConnection(DatabaseConnection connection) {
        if (string.IsNullOrEmpty(connection.Id)) {
            connection.Id = DatabaseConnection.GenerateId();
        }

        // If this is the first connection, make it default
        if (Connections.Count == 0) {
            connection.IsDefault = true;
            DefaultConnectionId = connection.Id;
        }

        // If this connection is marked as default, unmark others
        if (connection.IsDefault) {
            foreach (var conn in Connections) {
                conn.IsDefault = false;
            }
            DefaultConnectionId = connection.Id;
        }

        Connections.Add(connection);
        LastModified = DateTime.UtcNow;
    }

    public bool RemoveConnection(string id) {
        var connection = GetConnection(id);
        if (connection == null) return false;

        var wasDefault = connection.IsDefault || DefaultConnectionId == id;
        var removed = Connections.Remove(connection);

        if (removed && wasDefault && Connections.Count > 0) {
            // Set the first remaining connection as default
            var newDefault = Connections.First();
            newDefault.IsDefault = true;
            DefaultConnectionId = newDefault.Id;
        } else if (Connections.Count == 0) {
            DefaultConnectionId = null;
        }

        if (removed) {
            LastModified = DateTime.UtcNow;
        }

        return removed;
    }

    public void SetDefaultConnection(string id) {
        var connection = GetConnection(id);
        if (connection == null) return;

        // Unmark all connections as default
        foreach (var conn in Connections) {
            conn.IsDefault = false;
        }

        // Mark the specified connection as default
        connection.IsDefault = true;
        DefaultConnectionId = id;
        LastModified = DateTime.UtcNow;
    }

    public bool HasDuplicateConnection(DatabaseConnection newConnection) {
        return Connections.Any(c => c.Id != newConnection.Id && c.UniqueKey == newConnection.UniqueKey);
    }

    public List<DatabaseConnection> GetConnectionsSortedByUsage() {
        return Connections
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.LastUsed)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public void UpdateConnectionLastUsed(string id) {
        var connection = GetConnection(id);
        if (connection != null) {
            connection.UpdateLastUsed();
            LastModified = DateTime.UtcNow;
        }
    }

    // Validation
    public bool IsValid() {
        return !string.IsNullOrEmpty(ConfigVersion) &&
               Connections.All(c => c.IsValid()) &&
               (string.IsNullOrEmpty(DefaultConnectionId) || GetConnection(DefaultConnectionId) != null);
    }

    public List<string> GetValidationErrors() {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConfigVersion)) {
            errors.Add("Configuration version is required");
        }

        if (!string.IsNullOrEmpty(DefaultConnectionId) && GetConnection(DefaultConnectionId) == null) {
            errors.Add("Default connection ID references a non-existent connection");
        }

        // Check for duplicate connections
        var duplicateGroups = Connections
            .GroupBy(c => c.UniqueKey)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups) {
            errors.Add($"Duplicate connections found: {group.Key}");
        }

        // Validate individual connections
        for (int i = 0; i < Connections.Count; i++) {
            var connectionErrors = Connections[i].GetValidationErrors();
            foreach (var error in connectionErrors) {
                errors.Add($"Connection {i + 1}: {error}");
            }
        }

        return errors;
    }

    // Create an empty configuration
    public static ConnectionConfig CreateEmpty() {
        return new ConnectionConfig {
            ConfigVersion = "1.0",
            Connections = new List<DatabaseConnection>(),
            LastModified = DateTime.UtcNow
        };
    }
}