namespace DbOps.Models;

public class BlockingRelationship {
    public int BlockedPid { get; set; }
    public string BlockedUser { get; set; } = string.Empty;
    public string BlockedApp { get; set; } = string.Empty;
    public int BlockingPid { get; set; }
    public string BlockingUser { get; set; } = string.Empty;
    public string BlockingApp { get; set; } = string.Empty;
    public string LockType { get; set; } = string.Empty;
    public string RequestedMode { get; set; } = string.Empty;
    public string HeldMode { get; set; } = string.Empty;
    public string RelationName { get; set; } = string.Empty;

    public string GetBlockingDescription() {
        return $"PID {BlockingPid} ({BlockingUser}@{BlockingApp}) holding {HeldMode} on {RelationName}";
    }

    public string GetBlockedDescription() {
        return $"PID {BlockedPid} ({BlockedUser}@{BlockingApp}) waiting for {RequestedMode} on {RelationName}";
    }
}