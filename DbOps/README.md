# DbOps - PostgreSQL Database Operations Monitor

A Terminal User Interface (TUI) application for monitoring and managing PostgreSQL database sessions with advanced connection management and real-time monitoring capabilities.

## Features

- **Dynamic Connection Management**: Add, edit, and switch between multiple PostgreSQL connections
- **Encrypted Connection Storage**: Secure password storage using machine-specific encryption
- **Real-time Session Monitoring**: View active PostgreSQL sessions with comprehensive details
- **Multiple Display Modes**: 
  - Session details view
  - Wait information display
  - Database locking information
- **Interactive Navigation**: Intuitive keyboard controls for all operations
- **Safe Read-only Operations**: Monitor without modification capabilities
- **Auto-refresh**: Real-time data updates

## Requirements

- .NET 8.0 or later
- PostgreSQL database access
- Terminal/Console environment (Windows Terminal, Command Prompt, or PowerShell)

## Installation & Usage

### Running the Application

```bash
dotnet run --project DbOps
```

**Important**: Use a proper terminal environment (Windows Terminal, Command Prompt, or PowerShell). VSCode's integrated terminal may not display the TUI correctly.

### First Time Setup

1. Launch the application
2. Press **'C'** to open connection management
3. Press **'+'** to add a new connection
4. Fill in your PostgreSQL connection details
5. Press **F5** to test the connection
6. Press **Ctrl+S** to save

## Building and Deployment

### Cross-Platform Release Builds

You can build release versions for multiple platforms from your Windows machine:

#### Windows (x64)
```bash
dotnet publish DbOps -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

#### Linux (x64)
```bash
dotnet publish DbOps -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

#### macOS (x64)
```bash
dotnet publish DbOps -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### Build Options

- **`-c Release`**: Optimized release configuration
- **`-r [runtime]`**: Target runtime identifier
- **`--self-contained true`**: Includes .NET runtime (no installation required on target)
- **`-p:PublishSingleFile=true`**: Creates single executable file

### Output Locations

Built executables will be located at:
- **Windows**: `DbOps/bin/Release/net8.0/win-x64/publish/DbOps.exe`
- **Linux**: `DbOps/bin/Release/net8.0/linux-x64/publish/DbOps`
- **macOS**: `DbOps/bin/Release/net8.0/osx-x64/publish/DbOps`

### Framework-Dependent Builds (Smaller Size)

For smaller executables that require .NET runtime on target machine:

```bash
# Windows
dotnet publish DbOps -c Release -r win-x64 --self-contained false

# Linux
dotnet publish DbOps -c Release -r linux-x64 --self-contained false
```

## Keyboard Controls

### Main Application
- **C** - Open connection management dialog
- **S** - Switch to session details view
- **W** - Switch to wait information view
- **L** - Switch to locking information view
- **↑↓** - Navigate through session list
- **Enter/F5** - Refresh data
- **Q** - Quit application

### Connection Management Dialog
- **↑↓** - Navigate connections
- **Enter** - Connect to selected database
- **+** - Add new connection
- **F2** - Edit selected connection
- **Delete** - Remove selected connection
- **F5** - Test selected connection
- **Q** - Close dialog

### Add/Edit Connection Dialog
- **Tab** - Navigate between fields
- **F5** - Test connection before saving
- **Ctrl+S** - Save connection
- **Q** - Cancel and close

## Connection Configuration

### Adding a Connection

1. **Connection Name**: Friendly identifier (e.g., "Production DB", "Local Dev")
2. **Host**: PostgreSQL server address
3. **Port**: PostgreSQL port (default: 5432)
4. **Database**: Target database name
5. **Username**: PostgreSQL username
6. **Password**: PostgreSQL password (encrypted when saved)
7. **Set as Default**: Make this the default connection

### Example Configurations

#### Local Development
```
Name: Local Development
Host: localhost
Port: 5432
Database: postgres
Username: postgres
Password: your_password
```

#### Remote Server
```
Name: Production Server
Host: db.company.com
Port: 5432
Database: production_db
Username: db_user
Password: secure_password
```

## Display Information

### Session Details View
- Process ID (PID)
- Database name
- Application name
- Username
- Client address
- Session state
- Query start time
- Current SQL statement
- Connection duration

### Wait Information View
- Wait events and types
- Wait duration
- Blocking processes
- Resource contention details

### Locking Information View
- Lock types and modes
- Blocked and blocking sessions
- Lock duration
- Resource being locked

## Architecture

The application follows a clean Domain-Driven Design architecture:

### Technology Stack

- **Terminal.Gui**: Terminal user interface framework
- **Npgsql**: PostgreSQL .NET data provider
- **.NET 8**: Runtime platform
- **System.Security.Cryptography**: Password encryption

## Security

- **Encrypted Storage**: All passwords are encrypted using machine-specific keys
- **Secure Configuration**: Connection details stored in user-specific directories
- **No Plain Text**: Passwords are never stored in plain text
- **Machine Binding**: Encrypted data is tied to the specific machine

### Configuration File Location

Connection configurations are stored at:
- **Windows**: `%APPDATA%\DbOps\connections.json`
- **Linux/Mac**: `~/.config/DbOps/connections.json`

## Troubleshooting

### Display Issues

**Problem**: TUI interface doesn't appear correctly or looks corrupted.

**Solution**: 
- Use Windows Terminal, Command Prompt, or PowerShell
- Avoid VSCode's integrated terminal
- Ensure terminal supports ANSI escape sequences

### Connection Issues

**Problem**: "Failed to connect to database" error.

**Solutions**:
1. Verify PostgreSQL server is running
2. Check host and port accessibility
3. Verify database name exists
4. Confirm username and password are correct
5. Use the connection test feature (F5) before saving

### Password Decryption Errors

**Problem**: Cannot decrypt saved passwords.

**Solution**:
1. Delete the configuration file to reset all connections
2. Re-add connections with current credentials
3. Ensure you're on the same machine where connections were created

### Application Performance

**Problem**: Slow response or high CPU usage.

**Solutions**:
1. Reduce refresh frequency if auto-refresh is implemented
2. Check PostgreSQL server performance
3. Verify network connectivity to database server

### WSL Keyboard Issues (Fixed)

**Problem**: In WSL environments, keyboard commands like 'q', 'n', 's', 'w', 'l' don't work when the session list has focus, but work fine in text views.

**Root Cause**: Terminal.Gui handles key events differently between Windows and Linux. In Windows, unhandled keys bubble up to parent controls, but in Linux/WSL, ListView controls consume all key events by default.

**Solution**: The application now uses a global `OnKeyDown` handler that captures key events before they reach focused controls, ensuring consistent behavior across all platforms.

## Development Notes

This application was developed iteratively with AI assistance. Key architectural decisions:

- **Domain-Driven Design**: Clear separation of concerns
- **Modular UI Components**: Reusable interface elements  
- **Secure Configuration**: Encrypted credential storage
- **Extensible Display Modes**: Easy to add new monitoring views

## Future Enhancements

- Auto-refresh configuration
- Session filtering and search capabilities
- Export functionality for session data
- Multiple database type support
- Performance metrics dashboard
- Query execution history