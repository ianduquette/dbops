namespace DbOps.Models;

public class DatabaseLock {
    public string LockType { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public string? RelationName { get; set; }
    public int? Page { get; set; }
    public short? Tuple { get; set; }
    public string? VirtualXid { get; set; }
    public uint? TransactionId { get; set; }
    public uint? ClassId { get; set; }
    public uint? ObjId { get; set; }
    public short? ObjSubId { get; set; }

    public string GetLockIdentifier() {
        if (!string.IsNullOrEmpty(RelationName)) {
            return $"Relation: {RelationName}";
        }
        if (TransactionId.HasValue) {
            return $"Transaction: {TransactionId}";
        }
        if (!string.IsNullOrEmpty(VirtualXid)) {
            return $"Virtual Transaction: {VirtualXid}";
        }
        if (Page.HasValue) {
            return $"Page: {Page}" + (Tuple.HasValue ? $", Tuple: {Tuple}" : "");
        }
        if (ClassId.HasValue && ObjId.HasValue) {
            return $"Object: {ClassId}.{ObjId}" + (ObjSubId.HasValue ? $".{ObjSubId}" : "");
        }
        return "System Lock";
    }

    public string GetDisplayText() {
        // Use simple ASCII text without color codes
        var status = Granted ? "[HELD]" : "[WAIT]";
        var identifier = GetLockIdentifier();
        return $"{status} | {LockType} | {Mode} | {identifier}";
    }
}