namespace DbOps.Models;

public class ColumnWidths
{
    public int Database { get; set; }
    public int Application { get; set; }
    public int Machine { get; set; }
    public int Pid { get; set; } = 10; // "PID: 12345" - fixed width
    public int Status { get; set; }
    public int Type { get; set; }

    public int TotalWidth => Database + Application + Machine + Pid + Status + Type + FixedCharacters;

    // Fixed characters: "[", "] ", " | ", " | PID: ", " | ", " | " = 11 chars
    private const int FixedCharacters = 11;

    public static class Breakpoints
    {
        public const int MINIMUM_WIDTH = 80;   // Below this, show simplified view
        public const int COMPACT_WIDTH = 100;  // Reduced field widths
        public const int NORMAL_WIDTH = 120;   // Standard widths
        public const int WIDE_WIDTH = 150;     // Expanded widths
    }

    public static class MinimumWidths
    {
        public const int Database = 8;
        public const int Application = 12;
        public const int Machine = 10;
        public const int Pid = 10;
        public const int Status = 12;
        public const int Type = 8;
    }

    public static ColumnWidths? CalculateWidths(int terminalWidth)
    {
        // Account for ListView borders and scrollbar
        int availableWidth = terminalWidth - 3;

        // If terminal is too small, return null
        if (availableWidth < Breakpoints.MINIMUM_WIDTH)
            return null;

        var widths = new ColumnWidths();

        if (availableWidth < Breakpoints.COMPACT_WIDTH)
        {
            // Very compact - minimal widths
            widths.Database = MinimumWidths.Database;
            widths.Application = MinimumWidths.Application;
            widths.Machine = MinimumWidths.Machine;
            widths.Status = MinimumWidths.Status;
            widths.Type = MinimumWidths.Type;
        }
        else if (availableWidth < Breakpoints.NORMAL_WIDTH)
        {
            // Compact - use more of available space
            int usableWidth = availableWidth - FixedCharacters - widths.Pid;
            widths.Database = Math.Max(MinimumWidths.Database, usableWidth * 15 / 100);
            widths.Application = Math.Max(MinimumWidths.Application, usableWidth * 22 / 100);
            widths.Machine = Math.Max(MinimumWidths.Machine, usableWidth * 13 / 100);
            widths.Status = Math.Max(MinimumWidths.Status, usableWidth * 27 / 100);
            widths.Type = Math.Max(MinimumWidths.Type, usableWidth * 23 / 100);
        }
        else if (availableWidth < Breakpoints.WIDE_WIDTH)
        {
            // Normal - use most of available space
            int usableWidth = availableWidth - FixedCharacters - widths.Pid;
            widths.Database = Math.Max(12, usableWidth * 15 / 100);
            widths.Application = Math.Max(20, usableWidth * 25 / 100);
            widths.Machine = Math.Max(15, usableWidth * 13 / 100);
            widths.Status = Math.Max(18, usableWidth * 22 / 100);
            widths.Type = Math.Max(15, usableWidth * 25 / 100);
        }
        else
        {
            // Wide - use ALL available space aggressively
            int usableWidth = availableWidth - FixedCharacters - widths.Pid;

            widths.Database = Math.Max(15, usableWidth * 15 / 100);
            widths.Application = Math.Max(30, usableWidth * 28 / 100); // Further reduced to 28%
            widths.Machine = Math.Max(20, usableWidth * 12 / 100); // Further reduced to 12%
            widths.Status = Math.Max(20, usableWidth * 18 / 100); // Increased to 18%
            widths.Type = Math.Max(20, usableWidth * 27 / 100); // Increased to 27%
        }

        return widths;
    }
}