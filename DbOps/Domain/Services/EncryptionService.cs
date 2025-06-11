using System.Security.Cryptography;
using System.Text;

namespace DbOps.Domain.Services;

public class EncryptionService {
    private const int KeySize = 256;
    private const int IvSize = 128;
    private const int SaltSize = 32;
    private const int Iterations = 10000;

    // Machine-specific entropy for key derivation
    private static readonly byte[] MachineEntropy = GetMachineEntropy();

    public string Encrypt(string plaintext) {
        if (string.IsNullOrEmpty(plaintext)) {
            return string.Empty;
        }

        try {
            // Generate random salt
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(salt);
            }

            // Derive key from machine entropy and salt
            var key = DeriveKey(salt);

            // Encrypt the data
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = IvSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            using var encryptor = aes.CreateEncryptor();
            var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            // Combine salt + IV + encrypted data
            var result = new byte[SaltSize + aes.IV.Length + encryptedBytes.Length];
            Array.Copy(salt, 0, result, 0, SaltSize);
            Array.Copy(aes.IV, 0, result, SaltSize, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, SaltSize + aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }

    public string Decrypt(string ciphertext) {
        if (string.IsNullOrEmpty(ciphertext)) {
            return string.Empty;
        }

        try {
            var ciphertextBytes = Convert.FromBase64String(ciphertext);

            if (ciphertextBytes.Length < SaltSize + IvSize / 8) {
                throw new ArgumentException("Invalid ciphertext format");
            }

            // Extract salt, IV, and encrypted data
            var salt = new byte[SaltSize];
            var iv = new byte[IvSize / 8];
            var encryptedData = new byte[ciphertextBytes.Length - SaltSize - IvSize / 8];

            Array.Copy(ciphertextBytes, 0, salt, 0, SaltSize);
            Array.Copy(ciphertextBytes, SaltSize, iv, 0, IvSize / 8);
            Array.Copy(ciphertextBytes, SaltSize + IvSize / 8, encryptedData, 0, encryptedData.Length);

            // Derive key from machine entropy and salt
            var key = DeriveKey(salt);

            // Decrypt the data
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = IvSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }

    public bool ValidatePassword(string password) {
        if (string.IsNullOrEmpty(password)) {
            return false;
        }

        // Basic password validation
        return password.Length >= 1; // Allow any non-empty password for database connections
    }

    public bool CanDecrypt(string ciphertext) {
        if (string.IsNullOrEmpty(ciphertext)) {
            return true; // Empty string is valid
        }

        try {
            Decrypt(ciphertext);
            return true;
        } catch {
            return false;
        }
    }

    private byte[] DeriveKey(byte[] salt) {
        // Combine machine entropy with salt for key derivation
        var combinedEntropy = new byte[MachineEntropy.Length + salt.Length];
        Array.Copy(MachineEntropy, 0, combinedEntropy, 0, MachineEntropy.Length);
        Array.Copy(salt, 0, combinedEntropy, MachineEntropy.Length, salt.Length);

        using var pbkdf2 = new Rfc2898DeriveBytes(combinedEntropy, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize / 8);
    }

    private static byte[] GetMachineEntropy() {
        try {
            // Create machine-specific entropy from stable system properties
            var entropy = new List<byte>();

            // Add machine name
            var machineName = Environment.MachineName;
            entropy.AddRange(Encoding.UTF8.GetBytes(machineName));

            // Add user name
            var userName = Environment.UserName;
            entropy.AddRange(Encoding.UTF8.GetBytes(userName));

            // Add OS version
            var osVersion = Environment.OSVersion.ToString();
            entropy.AddRange(Encoding.UTF8.GetBytes(osVersion));

            // Add processor count
            var processorCount = Environment.ProcessorCount;
            entropy.AddRange(BitConverter.GetBytes(processorCount));

            // Add a fixed application identifier
            var appId = "DbOps-v1.0";
            entropy.AddRange(Encoding.UTF8.GetBytes(appId));

            // Hash the combined entropy to get a fixed-size key
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(entropy.ToArray());
        } catch {
            // Fallback to a default entropy if system properties are not available
            var fallbackEntropy = "DbOps-Default-Entropy-" + Environment.MachineName;
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackEntropy));
        }
    }

    // Test method to verify encryption/decryption works
    public bool TestEncryption() {
        try {
            const string testData = "Test encryption data 123!@#";
            var encrypted = Encrypt(testData);
            var decrypted = Decrypt(encrypted);
            return testData == decrypted;
        } catch {
            return false;
        }
    }
}