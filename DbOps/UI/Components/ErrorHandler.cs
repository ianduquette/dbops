using Terminal.Gui;

namespace DbOps.UI.Components;

/// <summary>
/// Centralized error handling and user feedback component.
/// Provides consistent error display, confirmation dialogs, and notification methods.
/// </summary>
public class ErrorHandler {
    /// <summary>
    /// Defines the severity level of errors for appropriate display handling.
    /// </summary>
    public enum ErrorSeverity {
        /// <summary>Informational messages that don't indicate problems</summary>
        Info,
        /// <summary>Warning messages that indicate potential issues</summary>
        Warning,
        /// <summary>Error messages that indicate failures</summary>
        Error,
        /// <summary>Critical errors that may affect application stability</summary>
        Critical
    }

    /// <summary>
    /// Displays an error message to the user with appropriate severity handling.
    /// </summary>
    /// <param name="title">The title of the error dialog</param>
    /// <param name="message">The error message to display</param>
    /// <param name="severity">The severity level of the error</param>
    public void ShowError(string title, string message, ErrorSeverity severity = ErrorSeverity.Error) {
        try {
            // Map severity to appropriate MessageBox method
            switch (severity) {
                case ErrorSeverity.Info:
                    MessageBox.Query(title, message, "OK");
                    break;
                case ErrorSeverity.Warning:
                case ErrorSeverity.Error:
                case ErrorSeverity.Critical:
                    MessageBox.ErrorQuery(title, message, "OK");
                    break;
            }

            // Log the error for debugging/monitoring
            LogError($"{title}: {message}", context: severity.ToString());
        } catch (Exception ex) {
            // Fallback error handling - should never happen but prevents crashes
            Console.WriteLine($"ErrorHandler failed to display error: {ex.Message}");
            Console.WriteLine($"Original error - {title}: {message}");
        }
    }

    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="title">The title of the confirmation dialog</param>
    /// <param name="message">The confirmation message</param>
    /// <param name="confirmText">Text for the confirm button (default: "Yes")</param>
    /// <param name="cancelText">Text for the cancel button (default: "No")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    public bool ShowConfirmationDialog(string title, string message, string confirmText = "Yes", string cancelText = "No") {
        try {
            var result = MessageBox.Query(title, message, confirmText, cancelText);
            LogError($"Confirmation dialog: {title} - Result: {(result == 0 ? confirmText : cancelText)}",
                context: "Confirmation");
            return result == 0;
        } catch (Exception ex) {
            // Fallback - log error and return false (safer default)
            Console.WriteLine($"ErrorHandler failed to show confirmation dialog: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows a brief notification message to the user.
    /// Currently uses MessageBox but can be enhanced with toast notifications.
    /// </summary>
    /// <param name="message">The notification message</param>
    public void ShowBriefNotification(string message) {
        try {
            // For now, use a simple message box
            // In future, this could be enhanced with toast notifications or status bar messages
            MessageBox.Query("Notification", message, "OK");
            LogError($"Notification: {message}", context: "Notification");
        } catch (Exception ex) {
            Console.WriteLine($"ErrorHandler failed to show notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles exceptions by formatting them appropriately and displaying to the user.
    /// </summary>
    /// <param name="ex">The exception to handle</param>
    /// <param name="context">Context information about where the exception occurred</param>
    /// <param name="severity">The severity level to display the exception as</param>
    public void HandleException(Exception ex, string context, ErrorSeverity severity = ErrorSeverity.Error) {
        try {
            var formattedMessage = FormatErrorMessage(ex);
            var title = $"{context} Error";

            ShowError(title, formattedMessage, severity);
            LogError($"Exception in {context}: {ex.Message}", ex, context);
        } catch (Exception handlerEx) {
            // Fallback error handling
            Console.WriteLine($"ErrorHandler failed to handle exception: {handlerEx.Message}");
            Console.WriteLine($"Original exception in {context}: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats exception messages for user-friendly display.
    /// </summary>
    /// <param name="ex">The exception to format</param>
    /// <returns>A formatted error message</returns>
    private string FormatErrorMessage(Exception ex) {
        if (ex == null) return "An unknown error occurred.";

        // For common exception types, provide more user-friendly messages
        return ex switch {
            ArgumentException => $"Invalid input: {ex.Message}",
            InvalidOperationException => $"Operation failed: {ex.Message}",
            UnauthorizedAccessException => "Access denied. Please check your permissions.",
            TimeoutException => "The operation timed out. Please try again.",
            _ => ex.Message
        };
    }

    /// <summary>
    /// Logs error information for debugging and monitoring.
    /// Currently writes to console but can be enhanced with file logging.
    /// </summary>
    /// <param name="message">The error message to log</param>
    /// <param name="ex">Optional exception details</param>
    /// <param name="context">Optional context information</param>
    private void LogError(string message, Exception? ex = null, string? context = null) {
        try {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";

            if (!string.IsNullOrEmpty(context)) {
                logMessage += $" (Context: {context})";
            }

            if (ex != null) {
                logMessage += $" | Exception: {ex.GetType().Name} - {ex.Message}";
                if (ex.StackTrace != null) {
                    logMessage += $" | StackTrace: {ex.StackTrace}";
                }
            }

            // For now, log to console. In future, this could write to a file or external logging service
            Console.WriteLine($"[ErrorHandler] {logMessage}");
        } catch {
            // Silently fail logging to prevent recursive errors
        }
    }
}