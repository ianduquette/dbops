# DbOps User Guide

## Connection Management

### Adding a New Connection

1. **Open Connection Management**
   - Press **'N'** while in the main application
   - This opens the connection selection dialog

2. **Add New Connection**
   - Press **'+'** in the connection selection dialog
   - This opens the "Add New Connection" dialog

3. **Fill in Connection Details**
   - **Connection Name**: A friendly name for this connection (e.g., "Production DB", "Local Dev")
   - **Host**: The PostgreSQL server address (e.g., localhost, 192.168.1.100)
   - **Port**: The PostgreSQL port (default: 5432)
   - **Database**: The database name to connect to
   - **Username**: Your PostgreSQL username
   - **Password**: Your PostgreSQL password
   - **Set as default**: Check this to make it your default connection

4. **Test the Connection** (Recommended)
   - Press **F5** or click **"Test Connection"**
   - This verifies your settings before saving
   - You'll see a success or error message

5. **Save the Connection**
   - Press **Ctrl+S** or click **"Save"**
   - The connection is encrypted and stored securely
   - You'll return to the connection selection dialog

### Form Validation

The add connection dialog now uses **lenient validation**:

- **Buttons are enabled by default** - you can always test or save
- **Real-time feedback** - status updates as you type
- **Clear error messages** - tells you exactly what's missing
- **Test without saving** - verify connection settings first

### Example Connection Settings

#### Local Development
- **Name**: Local Development
- **Host**: localhost (or 127.0.0.1)
- **Port**: 5432
- **Database**: postgres
- **Username**: postgres
- **Password**: your_password

#### Remote Server
- **Name**: Production Database
- **Host**: db.company.com
- **Port**: 5432
- **Database**: production_db
- **Username**: db_user
- **Password**: secure_password

## Using Connections

### Switching Connections
1. **Press 'N'** to open connection management
2. **Select a connection** using ↑↓ arrow keys
3. **Press Enter** to connect immediately
4. **No restart required** - data refreshes automatically

### Managing Existing Connections
- **Edit**: Select connection and press **F2**
- **Delete**: Select connection and press **Delete**
- **Test**: Select connection and press **F5**
- **Set as Default**: Edit connection and check "Set as default"

## Keyboard Shortcuts

### Main Application
- **N** - Open connection management
- **Q** - Quit application
- **S** - Show session details
- **W** - Show wait information
- **L** - Show locking information
- **Enter/F5** - Refresh data

### Connection Selection Dialog
- **↑↓** - Navigate connections
- **Enter** - Connect to selected
- **+** - Add new connection
- **F2** - Edit selected connection
- **Delete** - Remove selected connection
- **F5** - Test selected connection
- **Q** - Close dialog

### Add/Edit Connection Dialog
- **Tab** - Navigate between fields
- **F5** - Test connection
- **Ctrl+S** - Save connection
- **Q** - Cancel and close

## Troubleshooting

### "Username is required" Error
If you see this error even after entering a username:
1. **Click in the username field** and re-type the username
2. **Press Tab** to move to the next field
3. **The validation should update** and enable the buttons

### Connection Test Fails
1. **Verify server is running** - check PostgreSQL service
2. **Check network connectivity** - ping the host
3. **Verify credentials** - test with another tool like pgAdmin
4. **Check firewall settings** - ensure port is accessible

### Buttons Stay Disabled
1. **Fill in all required fields**: name, host, database, username
2. **Press Tab** between fields to trigger validation
3. **Try typing in each field** to refresh validation
4. **Buttons should enable** once basic fields are filled

### Password Decryption Issues
If you get decryption errors:
1. **Choose Option 1** from the recovery menu
2. **Re-enter your password** when prompted
3. **Connection will be re-encrypted** with current machine keys

## Security Notes

- **Passwords are encrypted** using machine-specific keys
- **Configuration stored** in user-specific directory
- **No plain text passwords** are ever saved
- **Machine binding** prevents unauthorized access

## Configuration File Location

Your connections are stored at:
- **Windows**: `%APPDATA%\DbOps\connections.json`
- **Linux/Mac**: `~/.config/DbOps/connections.json`

You can safely delete this file to reset all connections.