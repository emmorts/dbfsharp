using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbfSharp.Console.Commands;

/// <summary>
/// Settings for the export command
/// </summary>
public sealed class ExportSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Path to the DBF file to export")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Output file path")]
    public string OutputPath { get; set; } = string.Empty;

    [CommandOption("-f|--format")]
    [Description("Output format (csv, tsv, json)")]
    [DefaultValue(OutputFormat.Csv)]
    public OutputFormat Format { get; set; } = OutputFormat.Csv;
}

/// <summary>
/// Command for exporting DBF files to various formats
/// </summary>
public sealed class ExportCommand : AsyncCommand<ExportSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExportSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow]Export command not yet implemented[/]");
        return 0;
    }
}