using System.Text.Json.Serialization;

namespace DbOps.Domain.Models;

public class DatabaseConnection {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("database")]
    public string Database { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; } = 5432;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("encryptedPassword")]
    public string EncryptedPassword { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastUsed")]
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;

    // Helper properties for display and validation
    [JsonIgnore]
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : $"{Host}:{Port}/{Database}";

    [JsonIgnore]
    public string ConnectionSummary => $"{Username}@{Host}:{Port}/{Database}";

    // Create a unique identifier for duplicate detection
    [JsonIgnore]
    public string UniqueKey => $"{Host.ToLowerInvariant()}:{Port}:{Database.ToLowerInvariant()}:{Username.ToLowerInvariant()}";

    // Validation methods
    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(Host) &&
               !string.IsNullOrWhiteSpace(Database) &&
               !string.IsNullOrWhiteSpace(Username) &&
               Port > 0 && Port <= 65535;
    }

    public List<string> GetValidationErrors() {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Host)) {
            errors.Add("Host is required");
        }

        if (string.IsNullOrWhiteSpace(Database)) {
            errors.Add("Database name is required");
        }

        if (string.IsNullOrWhiteSpace(Username)) {
            errors.Add("Username is required");
        }

        if (Port <= 0 || Port > 65535) {
            errors.Add("Port must be between 1 and 65535");
        }

        if (string.IsNullOrWhiteSpace(Name)) {
            errors.Add("Connection name is required");
        }

        return errors;
    }

    // Create a copy for editing without affecting the original
    public DatabaseConnection Clone() {
        return new DatabaseConnection {
            Id = Id,
            Name = Name,
            Host = Host,
            Database = Database,
            Port = Port,
            Username = Username,
            EncryptedPassword = EncryptedPassword,
            CreatedAt = CreatedAt,
            LastUsed = LastUsed,
            IsDefault = IsDefault
        };
    }

    // Update last used timestamp
    public void UpdateLastUsed() {
        LastUsed = DateTime.UtcNow;
    }

    // Generate a new unique ID
    public static string GenerateId() {
        return $"conn-{Guid.NewGuid():N}";
    }
}