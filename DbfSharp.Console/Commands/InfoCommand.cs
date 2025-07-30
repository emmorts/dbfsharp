using System.ComponentModel;
using System.Text;
using DbfSharp.Console.Utils;
using DbfSharp.Core;
using DbfSharp.Core.Enums;
using DbfSharp.Core.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbfSharp.Console.Commands;

/// <summary>
/// Settings for the info command
/// </summary>
public sealed class InfoSettings : CommandSettings
{
    [CommandArgument(0, "[FILE]")]
    [Description("Path to the DBF file to analyze (omit to read from stdin)")]
    public string? FilePath { get; set; }

    [CommandOption("--fields")]
    [Description("Show field definitions")]
    [DefaultValue(true)]
    public bool ShowFields { get; set; } = true;

    [CommandOption("--header")]
    [Description("Show header information")]
    [DefaultValue(true)]
    public bool ShowHeader { get; set; } = true;

    [CommandOption("--stats")]
    [Description("Show record statistics")]
    [DefaultValue(true)]
    public bool ShowStats { get; set; } = true;

    [CommandOption("--memo")]
    [Description("Show memo file information")]
    [DefaultValue(true)]
    public bool ShowMemo { get; set; } = true;

    [CommandOption("-v|--verbose")]
    [Description("Show additional detailed information")]
    [DefaultValue(false)]
    public bool Verbose { get; set; } = false;

    [CommandOption("-q|--quiet")]
    [Description("Suppress all output except errors")]
    [DefaultValue(false)]
    public bool Quiet { get; set; } = false;

    [CommandOption("--encoding")]
    [Description("Override character encoding (e.g., UTF-8, Windows-1252)")]
    public string? Encoding { get; set; }

    [CommandOption("--ignore-missing-memo")]
    [Description("Don't fail if memo file is missing")]
    [DefaultValue(true)]
    public bool IgnoreMissingMemo { get; set; } = true;
}

/// <summary>
/// Command for displaying DBF file information and metadata
/// </summary>
public sealed class InfoCommand : AsyncCommand<InfoSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InfoSettings settings)
    {
        string? tempFilePath = null;
        try
        {
            using var inputSource = await StdinHelper.ResolveInputSourceAsync(settings.FilePath);

            if (!settings.Quiet)
            {
                var source = inputSource.IsStdin ? "stdin" : inputSource.OriginalPath;
                AnsiConsole.MarkupLine($"[blue]Analyzing DBF file:[/] {source}");
                AnsiConsole.WriteLine();
            }

            var readerOptions = CreateDbfReaderOptions(settings);

            // Determine a meaningful table name for display
            var tableName = inputSource.IsStdin ? "stdin" : Path.GetFileNameWithoutExtension(inputSource.OriginalPath);

            using var reader = await DbfReader.OpenAsync(inputSource.Stream, tableName, readerOptions);
            var stats = reader.GetStatistics();

            // Calculate file size information
            var fileSize = GetFileSize(inputSource);
            var calculatedSize = CalculateExpectedFileSize(reader);

            if (settings.ShowHeader)
            {
                AnsiConsole.WriteLine();
                DisplayHeaderInformation(reader, fileSize, calculatedSize);
            }

            if (settings.ShowFields)
            {
                AnsiConsole.WriteLine();
                DisplayFieldInformation(reader, settings.Verbose);
            }

            if (settings.ShowStats)
            {
                AnsiConsole.WriteLine();
                DisplayRecordStatistics(reader, settings.Verbose);
            }

            if (settings.ShowMemo && stats.HasMemoFile)
            {
                AnsiConsole.WriteLine();
                DisplayMemoFileInformation(reader);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
        finally
        {
            if (tempFilePath != null)
            {
                StdinHelper.CleanupTemporaryFile(tempFilePath);
            }
        }
    }

    /// <summary>
    /// Creates DBF reader options from command settings
    /// </summary>
    private static DbfReaderOptions CreateDbfReaderOptions(InfoSettings settings)
    {
        var options = new DbfReaderOptions
        {
            IgnoreMissingMemoFile = settings.IgnoreMissingMemo,
            ValidateFields = true
        };

        if (!string.IsNullOrEmpty(settings.Encoding))
        {
            var encoding = TryGetEncoding(settings.Encoding);
            if (encoding != null)
            {
                options = options with { Encoding = encoding };
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning:[/] Unknown encoding '{settings.Encoding}', using auto-detection"
                );
            }
        }

        return options;
    }

    /// <summary>
    /// Tries to get an encoding by name, with special handling for Japanese encodings
    /// </summary>
    private static Encoding? TryGetEncoding(string encodingName)
    {
        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }


    /// <summary>
    /// Displays detailed header information
    /// </summary>
    private static void DisplayHeaderInformation(DbfReader reader, long? actualFileSize, long expectedFileSize)
    {
        var header = reader.Header;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Header Information[/]");

        table.AddColumn("Property", col => col.Width(20));
        table.AddColumn("Value", col => col.Width(40));
        table.AddColumn("Details");

        table.AddRow(
            "DBF Version",
            EscapeMarkup(header.DbfVersion.GetDescription()),
            $"Byte: 0x{header.DbVersionByte:X2}"
        );

        if (header.LastUpdateDate.HasValue)
        {
            table.AddRow(
                "Last Update",
                header.LastUpdateDate.Value.ToString("yyyy-MM-dd"),
                $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}"
            );
        }
        else
        {
            table.AddRow(
                "Last Update",
                "Unknown",
                $"Raw: {header.Year:00}/{header.Month:00}/{header.Day:00}"
            );
        }

        table.AddRow(
            "Header Length",
            $"{header.HeaderLength} bytes",
            $"Fields area: {header.HeaderLength - DbfHeader.Size - 1} bytes"
        );

        table.AddRow("Record Length", $"{header.RecordLength} bytes", "Including deletion flag");

        table.AddRow(
            "Total Records",
            header.NumberOfRecords.ToString("N0"),
            "Including deleted records"
        );

        table.AddRow(
            "MDX Index",
            header.MdxFlag != 0 ? "Present" : "None",
            $"Flag: 0x{header.MdxFlag:X2}"
        );

        table.AddRow(
            "Encryption",
            header.EncryptionFlag != 0 ? "Encrypted" : "None",
            $"Flag: 0x{header.EncryptionFlag:X2}"
        );

        table.AddRow(
            "Language Driver",
            EscapeMarkup(header.EncodingDescription),
            $"Code: 0x{header.LanguageDriver:X2}"
        );

        if (actualFileSize.HasValue)
        {
            table.AddRow(
                "File Size",
                FormatFileSize(actualFileSize.Value),
                $"{actualFileSize.Value:N0} bytes"
            );

            var sizeDifference = actualFileSize.Value - expectedFileSize;
            var status = sizeDifference == 0 ? "Exact match" :
                        sizeDifference > 0 ? $"+{FormatFileSize(sizeDifference)} larger" :
                        $"{FormatFileSize(-sizeDifference)} smaller";

            table.AddRow(
                "Size vs Expected",
                status,
                $"Expected: {FormatFileSize(expectedFileSize)}"
            );
        }
        else
        {
            table.AddRow(
                "Expected File Size",
                FormatFileSize(expectedFileSize),
                $"{expectedFileSize:N0} bytes"
            );
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Displays field definitions and information
    /// </summary>
    private static void DisplayFieldInformation(DbfReader reader, bool verbose)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Field Definitions[/]");

        table.AddColumn("#", col => col.Width(3).RightAligned());
        table.AddColumn("Name", col => col.Width(15));
        table.AddColumn("Type", col => col.Width(15));
        table.AddColumn("Length", col => col.Width(8).RightAligned());
        table.AddColumn("Decimals", col => col.Width(8).RightAligned());

        if (verbose)
        {
            table.AddColumn("Flags", col => col.Width(8));
        }

        var fieldIndex = 1;
        foreach (var field in reader.Fields)
        {
            var row = new List<string>
            {
                fieldIndex.ToString(),
                EscapeMarkup(field.Name),
                EscapeMarkup($"{field.Type} ({(char)field.Type})"),
                field.ActualLength.ToString(),
                field.Type is FieldType.Numeric or FieldType.Float
                    ? field.ActualDecimalCount.ToString()
                    : "-"
            };

            if (verbose)
            {
                var flags = new List<string>();
                if (field.UsesMemoFile)
                {
                    flags.Add("Memo");
                }

                if (field.IndexFieldFlag != 0)
                {
                    flags.Add("Index");
                }

                if (field.SetFieldsFlag != 0)
                {
                    flags.Add("Set");
                }

                row.Add(string.Join(", ", flags));
            }

            table.AddRow(row.ToArray());
            fieldIndex++;
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Displays record statistics
    /// </summary>
    private static void DisplayRecordStatistics(DbfReader reader, bool verbose)
    {
        var stats = reader.GetStatistics();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Record Statistics[/]");

        table.AddColumn("Metric");
        table.AddColumn("Count", col => col.RightAligned());
        table.AddColumn("Percentage", col => col.RightAligned());

        table.AddRow("Total Records", stats.TotalRecords.ToString("N0"), "100.0%");
        table.AddRow(
            "Active Records",
            stats.ActiveRecords.ToString("N0"),
            $"{(stats.TotalRecords > 0 ? stats.ActiveRecords * 100.0 / stats.TotalRecords : 0):F1}%"
        );
        table.AddRow(
            "Deleted Records",
            stats.DeletedRecords.ToString("N0"),
            $"{(stats.TotalRecords > 0 ? stats.DeletedRecords * 100.0 / stats.TotalRecords : 0):F1}%"
        );

        AnsiConsole.Write(table);

        if (verbose && stats.ActiveRecords > 0)
        {
            AnsiConsole.WriteLine();

            DisplaySampleData(reader);
        }
    }

    /// <summary>
    /// Displays sample data from the file
    /// </summary>
    private static void DisplaySampleData(DbfReader reader)
    {
        AnsiConsole.MarkupLine("[bold blue]Sample Data (first 3 records):[/]");

        var sampleRecords = reader.Records.Take(3).ToList();
        if (sampleRecords.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No records to display[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);

        var fieldsToShow = reader.FieldNames.Take(5).ToArray();
        foreach (var fieldName in fieldsToShow)
        {
            table.AddColumn(EscapeMarkup(fieldName));
        }

        if (reader.FieldNames.Count > 5)
        {
            table.AddColumn("[dim]...[/]");
        }

        foreach (var record in sampleRecords)
        {
            var values = new List<string>();

            foreach (var fieldName in fieldsToShow)
            {
                var value = record[fieldName];
                values.Add(FormatSampleValue(value));
            }

            if (reader.FieldNames.Count > 5)
            {
                values.Add($"[dim]+{reader.FieldNames.Count - 5} more[/]");
            }

            table.AddRow(values.ToArray());
        }

        AnsiConsole.Write(table);

        if (reader.Count > 3)
        {
            AnsiConsole.MarkupLine($"[dim]... and {reader.Count - 3:N0} more records[/]");
        }
    }

    /// <summary>
    /// Displays memo file information
    /// </summary>
    private static void DisplayMemoFileInformation(DbfReader reader)
    {
        var stats = reader.GetStatistics();

        var panel = new Panel(
            Align.Left(
                new Markup(
                    $"[bold]Memo File:[/] {stats.MemoFilePath ?? "Present"}\n"
                    + $"[bold]Status:[/] {(stats.HasMemoFile ? "Available" : "Missing")}"
                )
            )
        )
        {
            Header = new PanelHeader("[bold blue]Memo File Information[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Gets the file size in bytes, handling both file streams and stdin
    /// </summary>
    private static long? GetFileSize(InputSourceResult inputSource)
    {
        try
        {
            switch (inputSource)
            {
                case { IsStdin: true, TempFilePath: not null }:
                    return new FileInfo(inputSource.TempFilePath).Length;
                case { IsStdin: false, Stream.CanSeek: true }:
                    return inputSource.Stream.Length;
            }
        }
        catch
        {
            // Ignore errors getting file size
        }
        return null;
    }

    /// <summary>
    /// Calculates the expected file size based on header and record information
    /// </summary>
    private static long CalculateExpectedFileSize(DbfReader reader)
    {
        var header = reader.Header;
        return header.HeaderLength + (header.RecordLength * header.NumberOfRecords);
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        const double gb = mb * 1024;

        if (bytes >= gb)
        {
            return $"{bytes / gb:F2} GB";
        }

        if (bytes >= mb)
        {
            return $"{bytes / mb:F2} MB";
        }

        if (bytes >= kb)
        {
            return $"{bytes / kb:F2} KB";
        }

        return $"{bytes} bytes";
    }

    /// <summary>
    /// Formats a value for sample data display
    /// </summary>
    private static string FormatSampleValue(object? value)
    {
        return value switch
        {
            null => "[dim]NULL[/]",
            string s when string.IsNullOrWhiteSpace(s) => "[dim]<empty>[/]",
            string { Length: > 20 } s => string.Concat(s.AsSpan(0, 17), "..."),
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            bool b => b ? "True" : "False",
            InvalidValue => "[red]<invalid>[/]",
            byte[] bytes => $"[dim]<{bytes.Length} bytes>[/]",
            _ => value.ToString()?.Replace("[", "[[").Replace("]", "]]") ?? "[dim]NULL[/]"
        };
    }

    /// <summary>
    /// Escapes markup characters in strings (to prevent Spectre.Console parsing errors)
    /// </summary>
    private static string EscapeMarkup(string? text)
    {
        return text?.Replace("[", "[[").Replace("]", "]]") ?? string.Empty;
    }
}
