# Storing Database Credentials in Encrypted Format in JSON Files

When storing database credentials in an encrypted format within a plain text JSON file, you have several options depending on your security requirements and implementation preferences:

## 1. Field-Level Encryption

Encrypt only the sensitive credential fields while keeping the JSON structure readable:

```json
{
  "database": {
    "host": "localhost",
    "port": 5432,
    "database": "myapp",
    "username": "encrypted:AES256:dGVzdHVzZXI=",
    "password": "encrypted:AES256:cGFzc3dvcmQxMjM="
  }
}
```

**Pros:**
- JSON remains partially readable
- Easy to identify which fields are encrypted
- Can use different encryption for different fields

**Cons:**
- More complex parsing logic required
- Metadata about encryption is visible

## 2. Full Credential Block Encryption

Encrypt the entire credentials object as a single encrypted string:

```json
{
  "database": {
    "host": "localhost",
    "port": 5432,
    "database": "myapp",
    "encrypted_credentials": "AES256:eyJ1c2VybmFtZSI6InRlc3R1c2VyIiwicGFzc3dvcmQiOiJwYXNzd29yZDEyMyJ9"
  }
}
```

**Pros:**
- Simpler encryption/decryption logic
- No credential structure visible
- Single encryption key management

**Cons:**
- Must decrypt entire block to access any credential
- Less granular access control

## 3. Base64 + Symmetric Encryption

Use AES encryption with Base64 encoding for storage:

```json
{
  "database": {
    "connection": {
      "encrypted_data": "U2FsdGVkX1+vupppZksvRf5pq5g5XjFRIipRkwB0K1Y96Qsv2Lm+31cmzaAILwyt",
      "algorithm": "AES-256-CBC",
      "encoding": "base64"
    }
  }
}
```

## 4. Envelope Encryption Pattern

Use a master key to encrypt data encryption keys (DEKs):

```json
{
  "database": {
    "encrypted_dek": "RSA:LS0tLS1CRUdJTi...",
    "encrypted_credentials": "AES256:eyJkYXRhYmFzZSI6...",
    "key_id": "master-key-001"
  }
}
```

## Implementation Approaches

### Option A: AES-256 with PBKDF2
```
Password/Key → PBKDF2 Key Derivation → AES-256 Encryption → Base64 Encoding → JSON Storage
```

**Best for:** Simple applications with password-based encryption

### Option B: Key Management Service (KMS)
```
Application → KMS Service → Data Encryption Key → Encrypt Credentials → Store in JSON
```

**Best for:** Production environments with proper key management

### Option C: Hardware Security Module (HSM)
```
Application → HSM/TPM → Hardware-backed Key → Encrypt Credentials → JSON Storage
```

**Best for:** High-security requirements

## Security Considerations

1. **Key Storage**: Never store encryption keys alongside encrypted data
2. **Salt/IV**: Always use unique salts/initialization vectors
3. **Key Rotation**: Implement periodic key rotation
4. **Access Control**: Restrict file system permissions
5. **Audit Logging**: Log access to encrypted credentials

## Recommended Libraries by Language

- **Node.js**: `crypto` (built-in), `node-forge`, `crypto-js`
- **Python**: `cryptography`, `pycryptodome`
- **Java**: `javax.crypto`, `Bouncy Castle`
- **C#**: `System.Security.Cryptography`, `Bouncy Castle`
- **Go**: `crypto/aes`, `golang.org/x/crypto`

## Example Implementation Pattern

```json
{
  "metadata": {
    "version": "1.0",
    "encryption": {
      "algorithm": "AES-256-GCM",
      "key_derivation": "PBKDF2-SHA256",
      "iterations": 100000
    }
  },
  "databases": {
    "primary": {
      "host": "localhost",
      "port": 5432,
      "credentials": {
        "salt": "random_salt_here",
        "iv": "initialization_vector",
        "encrypted": "encrypted_username_password_json"
      }
    }
  }
}
```

## Conclusion

The best approach depends on your specific security requirements, infrastructure, and compliance needs. For most applications, **AES-256 with proper key management** provides a good balance of security and implementation complexity.