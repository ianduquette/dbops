using DbOps.Models;
using DbOps.Services;
using DbOps.UI;
using DbOps.UI.Dialogs;
using Terminal.Gui;

namespace DbOps;

class Program {
    static void Main(string[] args) {
        // Check for command line argument to use simple console version
        if (args.Length > 0 && args[0].ToLower() == "--console") {
            SimpleConsoleVersion.Run();
            return;
        }

        // Default to TUI version
        RunTuiVersion();
    }

    static void RunTuiVersion() {
        try {
            Console.WriteLine("Starting DbOps - PostgreSQL Database Monitor...");
            Console.WriteLine("Initializing connection manager...");

            // Initialize connection manager
            var connectionManager = new ConnectionManager();

            // Get or select a connection
            var selectedConnection = GetOrSelectConnection(connectionManager);
            if (selectedConnection == null) {
                Console.WriteLine("No connection selected. Exiting...");
                return;
            }

            Console.WriteLine($"Using connection: {selectedConnection.DisplayName}");
            Console.WriteLine("Creating PostgreSQL service...");

            // Create PostgreSQL service from selected connection
            var postgresService = connectionManager.CreatePostgresService(selectedConnection);

            Console.WriteLine("Testing database connection...");

            // Test connection BEFORE initializing Terminal.Gui
            bool connectionTest = false;
            string connectionError = "";

            try {
                connectionTest = postgresService.TestConnection();
            } catch (Exception ex) {
                connectionError = ex.Message;
            }

            if (!connectionTest) {
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    CONNECTION ERROR                         ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"❌ Could not connect to PostgreSQL database!");
                Console.WriteLine($"📍 Connection: {selectedConnection.ConnectionSummary}");

                if (!string.IsNullOrEmpty(connectionError)) {
                    Console.WriteLine($"💥 Error: {connectionError}");
                }

                Console.WriteLine();
                Console.WriteLine("🔍 Please verify:");
                Console.WriteLine("   1. PostgreSQL server is running");
                Console.WriteLine("   2. Database exists and is accessible");
                Console.WriteLine("   3. Username and password are correct");
                Console.WriteLine("   4. Network connectivity is available");
                Console.WriteLine("   5. No firewall blocking the connection");
                Console.WriteLine();
                Console.WriteLine("Would you like to try the simple console version instead? (y/n): ");
                var fallback = Console.ReadKey();
                Console.WriteLine();
                if (fallback.Key == ConsoleKey.Y) {
                    SimpleConsoleVersion.Run();
                }
                return;
            }

            Console.WriteLine("Connection successful! Initializing Terminal.Gui...");

            // Update connection usage
            connectionManager.UpdateConnectionLastUsed(selectedConnection.Id);

            // Initialize Terminal.Gui AFTER successful connection test
            Application.Init();

            Console.WriteLine("Creating UI...");

            // Create and setup main window with connection management
            var mainWindow = new MainWindow(postgresService, connectionManager, selectedConnection);

            // Set as top-level window
            Application.Top.Add(mainWindow);

            Console.WriteLine("Initializing window...");

            // Initialize the window
            mainWindow.Initialize();

            Console.WriteLine("Starting application...");

            // Run the application
            Application.Run();
        } catch (Exception ex) {
            // Ensure Terminal.Gui is shut down before showing console output
            try { Application.Shutdown(); } catch { }

            Console.WriteLine($"TUI Application error: {ex.Message}");
            Console.WriteLine("This might be a Terminal.Gui compatibility issue.");
            Console.WriteLine("\nWould you like to try the simple console version instead? (y/n): ");
            var fallback = Console.ReadKey();
            Console.WriteLine();
            if (fallback.Key == ConsoleKey.Y) {
                SimpleConsoleVersion.Run();
            } else {
                Console.WriteLine($"Full error details: {ex.StackTrace}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
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
