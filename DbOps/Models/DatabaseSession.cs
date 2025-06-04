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

    // Enhanced session information properties
    public string ClientHostname { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Locking information properties
    public List<DatabaseLock> Locks { get; set; } = new();
    public List<BlockingRelationship> BlockingRelationships { get; set; } = new();

    // Computed properties for requirements mapping
    public string Machine => !string.IsNullOrEmpty(ClientHostname) ? ClientHostname : ClientAddress;
    public string OSUser => "N/A"; // PostgreSQL doesn't provide OS user information
    public string Server => ServerName;
    public int SID => Pid; // Use PID as Session ID equivalent
    public string Terminal => "N/A"; // PostgreSQL doesn't provide terminal information
    public string Type => DetermineSessionType();

    public string DisplayText {
        get {
            // Enhanced display format with new fields
            var dbName = DatabaseName.Length > 12 ? DatabaseName[..9] + "..." : DatabaseName;
            var appName = ApplicationName.Length > 20 ? ApplicationName[..17] + "..." : ApplicationName;
            var machine = Machine.Length > 15 ? Machine[..12] + "..." : Machine;
            var stateText = State.Length > 18 ? State[..15] + "..." : State;
            var typeText = Type.Length > 20 ? Type[..17] + "..." : Type;

            return $"[{dbName,-12}] {appName,-20} | {machine,-15} | PID: {Pid,6} | {stateText,-18} | {typeText,-20}";
        }
    }

    public string TruncatedQuery => CurrentQuery.Length > 80
        ? CurrentQuery[..77] + "..."
        : CurrentQuery;

    public string GetSessionDetails() {
        return $"Session Information:\n" +
               $"PID (SID): {Pid}\n" +
               $"Database: {DatabaseName}\n" +
               $"Program: {ApplicationName}\n" +
               $"Machine: {Machine}\n" +
               $"OS User: {OSUser}\n" +
               $"Server: {Server}\n" +
               $"Status: {State}\n" +
               $"Terminal: {Terminal}\n" +
               $"Type: {Type}\n" +
               $"Username: {Username}\n" +
               $"Query Start: {QueryStart?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\n" +
               $"Active: {(IsActive ? "Yes" : "No")}\n\n" +
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

    private string DetermineSessionType() {
        if (string.IsNullOrEmpty(ApplicationName)) {
            return "Unknown";
        }

        // Determine session type based on application name and state
        var appLower = ApplicationName.ToLowerInvariant();

        if (appLower.Contains("psql")) return "Interactive";
        if (appLower.Contains("pgadmin")) return "Admin Tool";
        if (appLower.Contains("jdbc") || appLower.Contains("npgsql") || appLower.Contains("psycopg")) return "Application";
        if (appLower.Contains("backup") || appLower.Contains("restore")) return "Maintenance";
        if (appLower.Contains("replication")) return "Replication";
        if (State == "active") return "Active Query";
        if (State.Contains("idle")) return "Idle Connection";

        return "Application";
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
