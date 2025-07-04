using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbfSharp.Console.Commands;

public sealed class ValidateSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Path to the DBF file to validate")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("-v|--verbose")]
    [Description("Show detailed validation information")]
    [DefaultValue(false)]
    public bool Verbose { get; set; } = false;
}

public sealed class ValidateCommand : AsyncCommand<ValidateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ValidateSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow]Validate command not yet implemented[/]");
        return 0;
    }
}