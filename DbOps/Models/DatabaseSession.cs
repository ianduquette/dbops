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

    // Locking information properties
    public List<DatabaseLock> Locks { get; set; } = new();
    public List<BlockingRelationship> BlockingRelationships { get; set; } = new();

    public string DisplayText {
        get {
            // Truncate long application names and database names to prevent scroll bar shifting
            var dbName = DatabaseName.Length > 15 ? DatabaseName[..12] + "..." : DatabaseName;
            var appName = ApplicationName.Length > 25 ? ApplicationName[..22] + "..." : ApplicationName;
            var stateText = State.Length > 30 ? State[..27] + "..." : State;

            return $"[{dbName,-15}] {appName,-25} - PID: {Pid,6} - State: {stateText,-30}";
        }
    }

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

    public string GetLockingInformation() {
        var lockInfo = "Locking Information:\n";
        lockInfo += $"Session PID: {Pid} | Database: {DatabaseName} | Application: {ApplicationName}\n";
        lockInfo += new string('═', 50) + "\n\n";

        // Check for blocking relationships FIRST - show at top
        var blockingOthers = BlockingRelationships.Where(br => br.BlockingPid == Pid).ToList();
        var blockedByOthers = BlockingRelationships.Where(br => br.BlockedPid == Pid).ToList();

        // Show blocking status prominently at the top
        if (blockingOthers.Count > 0) {
            lockInfo += $"[!] BLOCKING STATUS - CRITICAL:\n";
            lockInfo += $"[X] This session is BLOCKING {blockingOthers.Count} other session(s):\n";
            foreach (var blocking in blockingOthers) {
                lockInfo += $"   * {blocking.GetBlockedDescription()}\n";
            }
            lockInfo += "\n";
        }

        if (blockedByOthers.Count > 0) {
            lockInfo += $"[~] BLOCKED STATUS - WAITING:\n";
            lockInfo += $"[X] This session is BLOCKED by {blockedByOthers.Count} other session(s):\n";
            foreach (var blocked in blockedByOthers) {
                lockInfo += $"   * {blocked.GetBlockingDescription()}\n";
            }
            lockInfo += "\n";
        }

        if (blockingOthers.Count == 0 && blockedByOthers.Count == 0) {
            lockInfo += "[OK] BLOCKING STATUS: No blocking relationships detected.\n\n";
        }

        // Add separator before detailed lock information
        lockInfo += new string('─', 40) + "\n";
        lockInfo += "DETAILED LOCK INFORMATION:\n";
        lockInfo += new string('─', 40) + "\n";

        if (Locks.Count == 0) {
            lockInfo += "No locks held by this session.\n";
        } else {
            lockInfo += $"Locks Held/Requested ({Locks.Count}):\n\n";

            foreach (var lockItem in Locks) {
                lockInfo += $"{lockItem.GetDisplayText()}\n";
            }
        }

        return lockInfo;
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
