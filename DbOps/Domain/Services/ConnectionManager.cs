using System.Text.Json;
using DbOps.Domain.Models;
using Npgsql;

namespace DbOps.Domain.Services;

public class ConnectionManager {
    private readonly EncryptionService _encryptionService;
    private readonly string _configFilePath;
    private ConnectionConfig _config;

    public ConnectionManager() {
        _encryptionService = new EncryptionService();
        _configFilePath = GetConfigFilePath();
        _config = LoadConfiguration();
    }

    public event Action<List<DatabaseConnection>>? ConnectionsChanged;
    public event Action<string>? ErrorOccurred;
    public event Action<string>? ConnectionDeleted;

    // Properties
    public List<DatabaseConnection> Connections => _config.Connections;
    public DatabaseConnection? DefaultConnection => _config.GetDefaultConnection();

    // Load connections from configuration file
    public List<DatabaseConnection> LoadConnections() {
        try {
            _config = LoadConfiguration();
            return _config.Connections;
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to load connections: {ex.Message}");
            return new List<DatabaseConnection>();
        }
    }

    // Save connections to configuration file
    public void SaveConnections() {
        try {
            SaveConfiguration(_config);
            ConnectionsChanged?.Invoke(_config.Connections);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to save connections: {ex.Message}");
            throw;
        }
    }

    // Get a specific connection by ID
    public DatabaseConnection? GetConnection(string id) {
        return _config.GetConnection(id);
    }

    // Add a new connection
    public void AddConnection(DatabaseConnection connection, string plainPassword) {
        try {
            // Validate the connection
            var validationErrors = connection.GetValidationErrors();
            if (validationErrors.Count > 0) {
                throw new ArgumentException($"Invalid connection: {string.Join(", ", validationErrors)}");
            }

            // Check for duplicates
            if (_config.HasDuplicateConnection(connection)) {
                throw new InvalidOperationException($"A connection with the same details already exists: {connection.UniqueKey}");
            }

            // Encrypt the password
            connection.EncryptedPassword = _encryptionService.Encrypt(plainPassword);

            // Add to configuration
            _config.AddConnection(connection);

            // Save to file
            SaveConnections();
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to add connection: {ex.Message}");
            throw;
        }
    }

    // Remove a connection
    public bool RemoveConnection(string id) {
        try {
            var removed = _config.RemoveConnection(id);
            if (removed) {
                SaveConnections();
                // Fire the ConnectionDeleted event
                ConnectionDeleted?.Invoke(id);
            }
            return removed;
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to remove connection: {ex.Message}");
            return false;
        }
    }

    // Set default connection
    public void SetDefaultConnection(string id) {
        try {
            _config.SetDefaultConnection(id);
            SaveConnections();
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to set default connection: {ex.Message}");
            throw;
        }
    }

    // Test a connection
    public bool TestConnection(DatabaseConnection connection, string plainPassword) {
        try {
            var connectionString = BuildConnectionString(connection, plainPassword);
            using var npgsqlConnection = new NpgsqlConnection(connectionString);
            npgsqlConnection.Open();

            // Test with a simple query
            using var command = new NpgsqlCommand("SELECT 1", npgsqlConnection);
            var result = command.ExecuteScalar();

            return result != null && result.ToString() == "1";
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    // Test an existing connection using stored encrypted password
    public bool TestConnection(DatabaseConnection connection) {
        try {
            var plainPassword = _encryptionService.Decrypt(connection.EncryptedPassword);
            return TestConnection(connection, plainPassword);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Connection test failed: {ex.Message}");
            return false;
        }
    }

    // Get decrypted password for a connection with fallback
    public string GetDecryptedPassword(DatabaseConnection connection) {
        try {
            if (string.IsNullOrEmpty(connection.EncryptedPassword)) {
                return string.Empty;
            }
            return _encryptionService.Decrypt(connection.EncryptedPassword);
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to decrypt password for {connection.Name}: {ex.Message}");

            // If decryption fails, return empty string - user will need to re-enter password
            return string.Empty;
        }
    }

    // Update connection last used timestamp
    public void UpdateConnectionLastUsed(string id) {
        try {
            _config.UpdateConnectionLastUsed(id);
            SaveConnections();
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to update connection usage: {ex.Message}");
        }
    }

    // Get connections sorted by usage
    public List<DatabaseConnection> GetConnectionsSortedByUsage() {
        return _config.GetConnectionsSortedByUsage();
    }

    // Build connection string for PostgreSQL
    public string BuildConnectionString(DatabaseConnection connection, string plainPassword) {
        var builder = new NpgsqlConnectionStringBuilder {
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = plainPassword,
            Timeout = 30,
            CommandTimeout = 30
        };

        return builder.ToString();
    }

    // Create a PostgreSQL service for a connection
    public SyncPostgresService CreatePostgresService(DatabaseConnection connection) {
        var plainPassword = GetDecryptedPassword(connection);

        // If password decryption failed, we'll need to prompt for password
        if (string.IsNullOrEmpty(plainPassword)) {
            throw new InvalidOperationException($"Password decryption failed for connection '{connection.Name}'. " +
                "This may be due to corrupted data or running on a different machine. " +
                "Please edit the connection and re-enter the password.");
        }

        return new SyncPostgresService(
            connection.Host,
            connection.Port,
            connection.Database,
            connection.Username,
            plainPassword
        );
    }

    // Private methods
    private ConnectionConfig LoadConfiguration() {
        try {
            if (!File.Exists(_configFilePath)) {
                // Create default configuration if file doesn't exist
                var defaultConfig = ConnectionConfig.CreateEmpty();
                SaveConfiguration(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<ConnectionConfig>(json);

            if (config == null) {
                throw new InvalidOperationException("Failed to deserialize configuration");
            }

            // Validate configuration
            var validationErrors = config.GetValidationErrors();
            if (validationErrors.Count > 0) {
                throw new InvalidOperationException($"Invalid configuration: {string.Join(", ", validationErrors)}");
            }

            // Test encryption service with existing data
            if (config.Connections.Any() && !_encryptionService.TestEncryption()) {
                throw new InvalidOperationException("Encryption service test failed");
            }

            return config;
        } catch (Exception ex) {
            ErrorOccurred?.Invoke($"Failed to load configuration: {ex.Message}");

            // Return empty configuration as fallback
            return ConnectionConfig.CreateEmpty();
        }
    }

    private void SaveConfiguration(ConnectionConfig config) {
        try {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            // Update last modified timestamp
            config.LastModified = DateTime.UtcNow;

            // Serialize to JSON with indentation for readability
            var options = new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(config, options);

            // Write to temporary file first, then move to avoid corruption
            var tempFilePath = _configFilePath + ".tmp";
            File.WriteAllText(tempFilePath, json);

            // Backup existing file if it exists
            if (File.Exists(_configFilePath)) {
                var backupPath = _configFilePath + ".bak";
                File.Copy(_configFilePath, backupPath, true);
            }

            // Move temp file to final location
            File.Move(tempFilePath, _configFilePath, true);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    private static string GetConfigFilePath() {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "DbOps");
        return Path.Combine(configDirectory, "connections.json");
    }

    // Utility methods
    public bool HasConnections => _config.Connections.Count > 0;

    public int ConnectionCount => _config.Connections.Count;

    public string ConfigFilePath => _configFilePath;

    public bool ConfigFileExists => File.Exists(_configFilePath);

    // Import/Export functionality
    public void ExportConnections(string filePath) {
        try {
            var options = new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(filePath, json);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to export connections: {ex.Message}", ex);
        }
    }

    public void ImportConnections(string filePath, bool replaceExisting = false) {
        try {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Import file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            var importedConfig = JsonSerializer.Deserialize<ConnectionConfig>(json);

            if (importedConfig == null) {
                throw new InvalidOperationException("Failed to deserialize imported configuration");
            }

            if (replaceExisting) {
                _config = importedConfig;
            } else {
                // Merge connections, avoiding duplicates
                foreach (var connection in importedConfig.Connections) {
                    if (!_config.HasDuplicateConnection(connection)) {
                        connection.Id = DatabaseConnection.GenerateId(); // Generate new ID
                        _config.AddConnection(connection);
                    }
                }
            }

            SaveConnections();
        } catch (Exception ex) {
            throw new InvalidOperationException($"Failed to import connections: {ex.Message}", ex);
        }
    }
}