using DbOps.UI.Views;
using Terminal.Gui;

namespace DbOps;

class Program {
    static void Main(string[] args) {
        MainWindowView? mainWindow = null;

        try {
            Console.WriteLine("Starting DbOps - PostgreSQL Database Monitor...");

            // Initialize Terminal.Gui
            Application.Init();

            // Create main window with MVP pattern
            mainWindow = new MainWindowView();

            // Add to Terminal.Gui
            Application.Top.Add(mainWindow);

            // Initialize the window (this will create the presenter)
            mainWindow.Initialize();

            // Run the application
            Application.Run();
        } catch (Exception ex) {
            HandleCatastrophicError(ex);
        } finally {
            // Cleanup
            try {
                mainWindow?.Cleanup();
                Application.Shutdown();
            } catch (Exception cleanupEx) {
                Console.WriteLine($"Error during cleanup: {cleanupEx.Message}");
            }
        }
    }

    private static void HandleCatastrophicError(Exception ex) {
        try {
            Application.Shutdown();
        } catch {
            // Ignore shutdown errors
        }

        Console.WriteLine($"Fatal error: {ex.Message}");
        Console.WriteLine("This might be a Terminal.Gui compatibility issue.");
        Console.WriteLine($"Full error details: {ex}");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
