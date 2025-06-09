using DbOps.Models;
using DbOps.Services;
using DbOps.UI;
using DbOps.UI.Dialogs;
using Terminal.Gui;

namespace DbOps;

class Program {
    static void Main(string[] args) {
        // Default to TUI version
        RunTuiVersion();
    }

    static void RunTuiVersion() {
        try {
            Console.WriteLine("Starting DbOps - PostgreSQL Database Monitor...");

            // Initialize connection manager
            var connectionManager = new ConnectionManager();

            // Initialize Terminal.Gui immediately - no blocking connection logic
            Application.Init();

            // Create main window - it will handle connection logic internally
            var mainWindow = new MainWindow(connectionManager.CreatePostgresService(connectionManager.DefaultConnection ?? new DatabaseConnection()), connectionManager, connectionManager.DefaultConnection);

            // Set as top-level window
            Application.Top.Add(mainWindow);

            // Initialize the window
            mainWindow.Initialize();

            // Run the application
            Application.Run();
        } catch (Exception ex) {
            // Ensure Terminal.Gui is shut down before showing console output
            try { Application.Shutdown(); } catch { }

            Console.WriteLine($"TUI Application error: {ex.Message}");
            Console.WriteLine("This might be a Terminal.Gui compatibility issue.");
            Console.WriteLine("\nWould you like to try the simple console version instead? (y/n): ");
            Console.WriteLine($"Full error details: {ex.StackTrace}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        } finally {
            // Cleanup Terminal.Gui
            try { Application.Shutdown(); } catch { }
        }
    }

    static DatabaseConnection? GetOrSelectConnection(ConnectionManager connectionManager) {
        try {
            // Check if there's a default connection
            var defaultConnection = connectionManager.DefaultConnection;
            if (defaultConnection != null) {
                Console.WriteLine($"Found default connection: {defaultConnection.DisplayName}");

                // Try to decrypt the password to see if it's valid
                try {
                    var testPassword = connectionManager.GetDecryptedPassword(defaultConnection);
                    if (string.IsNullOrEmpty(testPassword)) {
                        Console.WriteLine("⚠️  Warning: Password decryption failed for default connection.");
                        Console.WriteLine("This may be due to corrupted data or running on a different machine.");
                        Console.WriteLine("You'll need to re-enter the password using the connection manager.");
                        return ShowConnectionManagerTui(connectionManager);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"⚠️  Warning: Password decryption failed: {ex.Message}");
                    return ShowConnectionManagerTui(connectionManager);
                }

                return defaultConnection;
            }

            // Check if there are any connections at all
            if (!connectionManager.HasConnections) {
                Console.WriteLine("No connections configured. Opening connection manager...");
                return ShowConnectionManagerTui(connectionManager);
            }

            // Multiple connections available, let user choose via TUI
            Console.WriteLine("Multiple connections available. Opening connection manager...");
            return ShowConnectionManagerTui(connectionManager);
        } catch (Exception ex) {
            Console.WriteLine($"Error managing connections: {ex.Message}");
            Console.WriteLine("Opening connection manager...");
            return ShowConnectionManagerTui(connectionManager);
        }
    }

    static DatabaseConnection? ShowConnectionManagerTui(ConnectionManager connectionManager) {
        try {
            // Initialize Terminal.Gui for the connection manager
            Application.Init();

            DatabaseConnection? selectedConnection = null;

            // Show connection selection dialog
            var connectionDialog = new ConnectionSelectionDialog(connectionManager);
            Application.Run(connectionDialog);

            if (connectionDialog.ConnectionSelected && connectionDialog.SelectedConnection != null) {
                selectedConnection = connectionDialog.SelectedConnection;
            }

            // Cleanup Terminal.Gui
            Application.Shutdown();

            return selectedConnection;
        } catch (Exception ex) {
            // Cleanup Terminal.Gui on error
            try { Application.Shutdown(); } catch { }

            Console.WriteLine($"Error showing connection manager: {ex.Message}");
            Console.WriteLine("Please check your terminal compatibility.");
            return null;
        }
    }
}
