using DbfSharp.Core;

namespace DbfSharp.ConsoleAot.Text;

/// <summary>
/// Utilities for file size calculations and human-readable formatting
/// </summary>
public static class FileSize
{
    private const double BytesPerKilobyte = 1024;
    private const double BytesPerMegabyte = BytesPerKilobyte * 1024;
    private const double BytesPerGigabyte = BytesPerMegabyte * 1024;
    private const double BytesPerTerabyte = BytesPerGigabyte * 1024;

    /// <summary>
    /// Calculates the expected file size based on DBF header information
    /// </summary>
    /// <param name="reader">The DBF reader containing header information</param>
    /// <returns>The expected file size in bytes</returns>
    public static long CalculateExpectedSize(DbfReader reader)
    {
        var header = reader.Header;
        return header.HeaderLength + (long)header.RecordLength * header.NumberOfRecords;
    }

    /// <summary>
    /// Formats a byte count into a human-readable string with appropriate units
    /// </summary>
    /// <param name="bytes">The number of bytes to format</param>
    /// <param name="decimalPlaces">Number of decimal places to show (default: 2)</param>
    /// <returns>A formatted file size string (e.g., "1.25 MB")</returns>
    public static string Format(long bytes, int decimalPlaces = 2)
    {
        if (bytes < 0)
        {
            return "0 bytes";
        }

        var format = $"F{Math.Max(0, Math.Min(decimalPlaces, 3))}";

        if (bytes >= BytesPerTerabyte)
        {
            return $"{(bytes / BytesPerTerabyte).ToString(format)} TB";
        }

        if (bytes >= BytesPerGigabyte)
        {
            return $"{(bytes / BytesPerGigabyte).ToString(format)} GB";
        }

        if (bytes >= BytesPerMegabyte)
        {
            return $"{(bytes / BytesPerMegabyte).ToString(format)} MB";
        }

        if (bytes >= BytesPerKilobyte)
        {
            return $"{(bytes / BytesPerKilobyte).ToString(format)} KB";
        }

        return $"{bytes} {(bytes == 1 ? "byte" : "bytes")}";
    }

    /// <summary>
    /// Formats a size difference between actual and expected file sizes
    /// </summary>
    /// <param name="actualSize">The actual file size</param>
    /// <param name="expectedSize">The expected file size</param>
    /// <returns>A formatted difference description</returns>
    public static string FormatSizeDifference(long actualSize, long expectedSize)
    {
        var difference = actualSize - expectedSize;

        return difference switch
        {
            0 => "Exact match",
            > 0 => $"+{Format(difference)} larger than expected",
            < 0 => $"{Format(-difference)} smaller than expected",
        };
    }

    /// <summary>
    /// Calculates the percentage difference between actual and expected sizes
    /// </summary>
    /// <param name="actualSize">The actual file size</param>
    /// <param name="expectedSize">The expected file size</param>
    /// <returns>The percentage difference (positive if larger, negative if smaller)</returns>
    public static double CalculatePercentageDifference(long actualSize, long expectedSize)
    {
        if (expectedSize == 0)
        {
            return actualSize == 0 ? 0 : double.PositiveInfinity;
        }

        return (double)(actualSize - expectedSize) / expectedSize * 100;
    }

    /// <summary>
    /// Determines if a file size is within acceptable bounds for DBF processing
    /// </summary>
    /// <param name="fileSize">The file size to check</param>
    /// <param name="maxRecommendedSize">Maximum recommended size for memory processing</param>
    /// <returns>True if the file size is within acceptable bounds</returns>
    public static bool IsWithinProcessingBounds(
        long fileSize,
        long maxRecommendedSize = 1024 * 1024 * 1024
    )
    {
        return fileSize > 0 && fileSize <= maxRecommendedSize;
    }

    /// <summary>
    /// Creates a progress reporter that logs file transfer progress at reasonable intervals
    /// </summary>
    /// <param name="totalSize">Total expected size (if known)</param>
    /// <param name="reportIntervalBytes">How often to report progress in bytes</param>
    /// <returns>A progress reporter for file operations</returns>
    public static IProgress<long> CreateProgressReporter(
        long? totalSize = null,
        long reportIntervalBytes = 10 * 1024 * 1024
    )
    {
        var lastReported = 0L;

        return new Progress<long>(bytesProcessed =>
        {
            if (bytesProcessed - lastReported >= reportIntervalBytes)
            {
                var message = totalSize.HasValue
                    ? $"Processed {Format(bytesProcessed)} of {Format(totalSize.Value)} ({(double)bytesProcessed / totalSize.Value * 100:F1}%)"
                    : $"Processed {Format(bytesProcessed)}";

                Console.WriteLine(message);
                lastReported = bytesProcessed;
            }
        });
    }

    /// <summary>
    /// Estimates memory usage for processing a file of the given size
    /// </summary>
    /// <param name="fileSize">The file size to estimate memory usage for</param>
    /// <returns>Estimated memory usage in bytes</returns>
    public static long EstimateMemoryUsage(long fileSize)
    {
        return fileSize switch
        {
            <= 1024 * 1024 => fileSize * 3,
            <= 10 * 1024 * 1024 => fileSize * 2,
            <= 100 * 1024 * 1024 => (long)(fileSize * 1.5),
            _ => fileSize + 100 * 1024 * 1024,
        };
    }

    /// <summary>
    /// Creates a warning message if a file size might cause performance issues
    /// </summary>
    /// <param name="fileSize">The file size to check</param>
    /// <returns>Warning message if applicable, null otherwise</returns>
    public static string? CreatePerformanceWarning(long fileSize)
    {
        if (fileSize > 1024 * 1024 * 1024)
        {
            return $"Very large file ({Format(fileSize)}). Processing may take significant time and memory.";
        }

        if (fileSize > 500 * 1024 * 1024)
        {
            return $"Large file detected ({Format(fileSize)}). Consider using --limit for faster processing.";
        }

        return null;
    }
}
