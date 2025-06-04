using DbOps.Services;
using DbOps.UI;
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
        // Connection details (hardcoded for MVP)
        const string host = "127.0.0.1";
        const int port = 5433;
        const string database = "postgres";
        const string username = "postgres";
        const string password = "cenozon";

        try {
            Console.WriteLine("Starting TUI version...");
            Console.WriteLine("Creating PostgreSQL service...");

            // Create PostgreSQL service
            var postgresService = new SyncPostgresService(host, port, database, username, password);

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
                Console.WriteLine($"📍 Connection: {host}:{port}/{database}");
                Console.WriteLine($"👤 Username: {username}");

                if (!string.IsNullOrEmpty(connectionError)) {
                    Console.WriteLine($"💥 Error: {connectionError}");
                }

                Console.WriteLine();
                Console.WriteLine("🔍 Please verify:");
                Console.WriteLine("   1. PostgreSQL server is running");
                Console.WriteLine("   2. Database 'postgres' exists");
                Console.WriteLine("   3. Username and password are correct");
                Console.WriteLine("   4. Port 5433 is accessible");
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

            // Initialize Terminal.Gui AFTER successful connection test
            Application.Init();

            Console.WriteLine("Creating UI...");

            // Create and setup main window
            var mainWindow = new MainWindow(postgresService);

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
}
