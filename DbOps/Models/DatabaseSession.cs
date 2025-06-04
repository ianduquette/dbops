namespace DbOps.Models;

public class DatabaseSession {
    public int Pid { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CurrentQuery { get; set; } = string.Empty;
    public DateTime? QueryStart { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ClientAddress { get; set; } = string.Empty;

    // Wait information properties
    public string? WaitEventType { get; set; }
    public string? WaitEvent { get; set; }
    public DateTime? StateChange { get; set; }
    public DateTime? BackendStart { get; set; }
    public DateTime? TransactionStart { get; set; }

    public string DisplayText => $"[{DatabaseName}] {ApplicationName} - PID: {Pid} - State: {State}";

    public string TruncatedQuery => CurrentQuery.Length > 80
        ? CurrentQuery[..77] + "..."
        : CurrentQuery;

    public string GetSessionDetails() {
        return $"Session Information:\n" +
               $"PID: {Pid}\n" +
               $"Database: {DatabaseName}\n" +
               $"Application: {ApplicationName}\n" +
               $"Username: {Username}\n" +
               $"Client Address: {ClientAddress}\n" +
               $"State: {State}\n" +
               $"Query Start: {QueryStart?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\n\n" +
               $"Session Timing:\n" +
               (BackendStart.HasValue ? $"Session Started: {BackendStart.Value:yyyy-MM-dd HH:mm:ss}\n" : "") +
               (TransactionStart.HasValue ? $"Transaction Started: {TransactionStart.Value:yyyy-MM-dd HH:mm:ss}\n" : "") +
               (BackendStart.HasValue ? $"Session Duration: {FormatDuration(DateTime.Now - BackendStart.Value)}" : "");
    }

    public string GetWaitInformation() {
        var waitInfo = "Wait Information:\n";
        waitInfo += $"Event Type: {WaitEventType ?? "None"}\n";
        waitInfo += $"Event Name: {WaitEvent ?? "Not Waiting"}\n";
        waitInfo += $"Current State: {State}\n";

        if (StateChange.HasValue) {
            var timeInState = DateTime.Now - StateChange.Value;
            waitInfo += $"Time in Current State: {FormatDuration(timeInState)}\n";
        } else {
            waitInfo += "Time in Current State: N/A\n";
        }

        if (BackendStart.HasValue) {
            var sessionDuration = DateTime.Now - BackendStart.Value;
            waitInfo += $"Session Duration: {FormatDuration(sessionDuration)}\n";
        } else {
            waitInfo += "Session Duration: N/A\n";
        }

        if (TransactionStart.HasValue) {
            var transactionDuration = DateTime.Now - TransactionStart.Value;
            waitInfo += $"Transaction Duration: {FormatDuration(transactionDuration)}\n";
        } else {
            waitInfo += "Transaction Duration: N/A\n";
        }

        return waitInfo;
    }

    private static string FormatDuration(TimeSpan duration) {
        if (duration.TotalDays >= 1) {

            return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
        } else if (duration.TotalHours >= 1) {
            return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
        } else if (duration.TotalMinutes >= 1) {
            return $"{duration.Minutes}m {duration.Seconds}s";
        } else {
            return $"{duration.TotalSeconds:F1}s";
        }
    }
}
