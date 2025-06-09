# DbOps Troubleshooting Guide

## Password Decryption Error

### Problem
```
TUI Application error: Decryption failed: Padding is invalid and cannot be removed.
```

### Cause
This error occurs when the application cannot decrypt stored passwords. Common causes:
1. **Machine Change** - Running on a different computer than where passwords were encrypted
2. **User Account Change** - Running under a different user account
3. **Corrupted Configuration** - The connections.json file has been corrupted
4. **OS Reinstall** - Machine-specific encryption keys have changed

### Solution
The application now includes automatic recovery options:

#### Option 1: Automatic Recovery (Recommended)
When you run the application and encounter this error, you'll see a recovery menu:

```
╔══════════════════════════════════════════════════════════════╗
║                    PASSWORD RECOVERY                        ║
╚══════════════════════════════════════════════════════════════╝

Connection: Default Local Connection
Details: postgres@127.0.0.1:5433/postgres

Options:
1. Re-enter password for this connection
2. Delete this connection and create a new one
3. Skip and create a new connection

Choose option (1-3):
```

**Choose Option 1** to keep your connection settings and just update the password.

#### Option 2: Manual Recovery
If you need to manually fix the issue:

1. **Delete the configuration file:**
   - Windows: Delete `%APPDATA%\DbOps\connections.json`
   - Linux/Mac: Delete `~/.config/DbOps/connections.json`

2. **Restart the application** - it will prompt you to set up connections again

#### Option 3: Edit Connection in TUI
If the application starts successfully but some connections fail:

1. **Press 'N'** to open connection management
2. **Select the problematic connection**
3. **Press F2** to edit (or delete and recreate)
4. **Re-enter the password**

### Prevention
To avoid this issue in the future:
- **Export connections** before major system changes
- **Document connection details** separately
- **Use consistent user accounts** when possible

### Technical Details
The application uses machine-specific encryption that combines:
- Machine name
- Username  
- OS version
- Hardware characteristics

When any of these change significantly, stored passwords cannot be decrypted.

## Other Common Issues

### Connection Test Failures
**Problem:** "Connection test failed" when adding connections

**Solutions:**
1. Verify PostgreSQL server is running
2. Check host/port/database name spelling
3. Confirm username/password are correct
4. Test network connectivity
5. Check firewall settings

### Terminal.Gui Compatibility Issues
**Problem:** TUI doesn't display correctly or crashes

**Solutions:**
1. Try the console version: `DbOps.exe --console`
2. Update your terminal/console application
3. Check terminal size (minimum 80x24 recommended)
4. Try running in different terminal applications

### Performance Issues
**Problem:** Slow session loading or UI responsiveness

**Solutions:**
1. Check database server performance
2. Verify network latency to database
3. Reduce number of active sessions if possible
4. Consider connection pooling settings

## Getting Help

If you continue to experience issues:

1. **Check the error message** - most errors include helpful details
2. **Try the console version** - run with `--console` flag for simpler interface
3. **Review connection settings** - verify all connection parameters
4. **Test database connectivity** - use other tools to verify database access
5. **Check logs** - look for additional error details in console output

## Configuration File Location

The application stores connections in:
- **Windows:** `%APPDATA%\DbOps\connections.json`
- **Linux/Mac:** `~/.config/DbOps/connections.json`

This file contains encrypted passwords and connection metadata. You can safely delete it to reset all connections.