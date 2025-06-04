using DbOps.Models;
using DbOps.Services;

namespace DbOps;

public static class SimpleConsoleVersion {
    public static void Run() {
        Console.Clear();
        Console.WriteLine("=== PostgreSQL Database Monitor (Console Version) ===");
        Console.WriteLine();

        // Connection details
        const string host = "127.0.0.1";
        const int port = 5433;
        const string database = "postgres";
        const string username = "postgres";
        const string password = "cenozon";

        Console.WriteLine($"Connecting to: {host}:{port}/{database}");
        Console.WriteLine();

        try {
            var postgresService = new SyncPostgresService(host, port, database, username, password);

            // Test connection
            Console.Write("Testing connection... ");
            bool connected = postgresService.TestConnection();

            if (!connected) {
                Console.WriteLine("FAILED!");
                Console.WriteLine("Could not connect to PostgreSQL database.");
                Console.WriteLine("Please check your connection settings and ensure PostgreSQL is running.");
                return;
            }

            Console.WriteLine("SUCCESS!");
            Console.WriteLine();

            while (true) {
                try {
                    Console.WriteLine("Fetching active sessions...");
                    var sessions = postgresService.GetActiveSessions();

                    Console.Clear();
                    Console.WriteLine("=== PostgreSQL Database Monitor ===");
                    Console.WriteLine($"Connected to: {host}:{port}/{database}");
                    Console.WriteLine($"Active Sessions: {sessions.Count}");
                    Console.WriteLine();

                    if (sessions.Count == 0) {
                        Console.WriteLine("No active sessions found.");
                    } else {
                        Console.WriteLine("Active Sessions:");
                        Console.WriteLine("================");

                        for (int i = 0; i < sessions.Count; i++) {
                            var session = sessions[i];
                            Console.WriteLine($"{i + 1}. PID: {session.Pid}");
                            Console.WriteLine($"   Database: {session.DatabaseName}");
                            Console.WriteLine($"   Application: {session.ApplicationName}");
                            Console.WriteLine($"   State: {session.State}");
                            Console.WriteLine($"   Query: {session.TruncatedQuery}");
                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine("Commands: [R]efresh, [Q]uit");
                    Console.Write("Enter command: ");

                    var key = Console.ReadKey();
                    Console.WriteLine();

                    if (key.Key == ConsoleKey.Q) {
                        break;
                    } else if (key.Key == ConsoleKey.R) {
                        continue;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error fetching sessions: {ex.Message}");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey();
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"Connection error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        Console.WriteLine("Goodbye!");
    }
}
