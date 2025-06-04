# PostgreSQL Database Monitor

A Terminal User Interface (TUI) application for monitoring PostgreSQL database sessions.

## Features

- **Real-time Session Monitoring**: View active PostgreSQL sessions with key information
- **Session Details**: Display current SQL statements, connection info, and session state
- **Simple Navigation**: Easy-to-use keyboard controls
- **Read-only Interface**: Safe monitoring without modification capabilities

## Requirements

- .NET 8.0 or later
- PostgreSQL database access
- Terminal/Console environment

## Usage

### Running the Application

The application offers two interfaces:
1. **Terminal User Interface (TUI)** - Full-featured graphical interface
2. **Simple Console Version** - Basic text-based interface

#### Option 1: VSCode F5 Debug (Recommended)

1. Open the project in VSCode
2. Press **F5** or go to **Run > Start Debugging**
3. The application will build automatically and open in an external terminal window
4. Choose your preferred interface (TUI or Simple Console)

#### Option 2: Using the Launcher Scripts

**PowerShell:**
```powershell
.\run-dbops.ps1
```

**Batch File:**
```cmd
run-dbops.bat
```

These scripts will automatically open the TUI application in a new terminal window, which is required for proper Terminal.Gui functionality.

#### Option 3: Manual Command Line

**In a separate terminal window (not VSCode integrated terminal):**
```bash
dotnet run --project DbOps
```

**Note:** The TUI requires a proper terminal environment. VSCode's integrated terminal may not display the TUI correctly. Use Windows Terminal, Command Prompt, or PowerShell in a separate window.

#### Option 4: Testing Connection

Before running the main application, you can test the database connection:
```powershell
.\test-connection-fast.ps1
```

### Interface Selection

When you run the application, you'll be prompted to choose:
1. **TUI Interface** - Full Terminal.Gui interface with navigation
2. **Simple Console** - Basic console output (works in any terminal)

### Controls

- **↑↓ Arrow Keys**: Navigate through the session list
- **Enter**: Refresh the session data
- **F5**: Refresh the session data (alternative)
- **Q**: Quit the application

### Display Information

The application shows:

- **Connection Info**: Database host, port, and database name
- **Session List**: Active sessions with database, application name, PID, and state
- **Session Details**: Detailed information for the selected session including:
  - Process ID (PID)
  - Database name
  - Application name
  - Username
  - Client address
  - Session state
  - Query start time
  - Current SQL statement

## Configuration

Currently configured for:
- **Host**: localhost
- **Port**: 5433
- **Database**: postgres
- **Username**: postgres
- **Password**: cenozon

## Architecture

The application is built with:

- **Terminal.Gui**: For the terminal user interface
- **Npgsql**: For PostgreSQL connectivity
- **Modular Design**: Separated concerns for services, models, and UI

### Project Structure

```
DbOps/
├── .vscode/
│   ├── launch.json             # VSCode debug configuration
│   ├── tasks.json              # VSCode build tasks
│   └── settings.json           # VSCode workspace settings
├── Models/
│   └── DatabaseSession.cs      # Data model for session information
├── Services/
│   └── SyncPostgresService.cs  # Database connectivity and queries
├── UI/
│   └── MainWindow.cs           # Main TUI window
├── Queries/
│   └── PostgresQueries.cs      # SQL queries
├── Program.cs                  # Application entry point
├── SimpleConsoleVersion.cs     # Simple console interface
├── run-dbops.ps1              # PowerShell launcher
├── run-dbops.bat              # Batch launcher
└── test-connection-fast.ps1    # Connection testing script
```

## Troubleshooting

### Application Not Displaying Properly

**Problem**: The TUI interface doesn't appear or looks corrupted in VSCode's integrated terminal.

**Solution**: Terminal.Gui applications require a proper terminal environment. Use one of these alternatives:

1. **Use the launcher scripts**: Run `.\run-dbops.ps1` or `run-dbops.bat` to open in a new terminal window
2. **Use Simple Console**: Choose option 2 when prompted - works in any terminal including VSCode integrated terminal
3. **Open Windows Terminal**: Press `Win + R`, type `wt`, press Enter, then navigate to the project folder and run `dotnet run --project DbOps`
4. **Open Command Prompt**: Press `Win + R`, type `cmd`, press Enter, then navigate to the project folder and run the application

### Database Connection Issues

**Problem**: "Failed to connect to database" error.

**Solutions**:
1. Verify PostgreSQL is running on localhost:5433
2. Check that the database "postgres" exists
3. Verify the username "postgres" and password "cenozon" are correct
4. Run the connection test: `.\test-connection-fast.ps1`

### Application Crashes or Freezes

**Problem**: Application stops responding or crashes.

**Solutions**:
1. Press `Ctrl + C` to force quit if needed
2. Ensure you're running in a compatible terminal (not VSCode integrated terminal)
3. Check that .NET 8.0 is properly installed: `dotnet --version`

## Future Enhancements

- Configuration file support with encrypted passwords
- Multiple database connection management
- Auto-refresh functionality
- Session filtering and search
- Wait event and locking information
- Export capabilities