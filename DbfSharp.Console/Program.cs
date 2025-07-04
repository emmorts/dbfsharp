using System.Text;
using DbfSharp.Console.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

// register code page encodings for legacy character sets
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch
{
    Console.OutputEncoding = Encoding.Default;
}

try
{
    var app = new CommandApp();
    app.Configure(config =>
    {
        config.SetApplicationName("dbfsharp");
        config.SetApplicationVersion("1.0.0");
                
        // Add commands
        config.AddCommand<ReadCommand>("read")
            .WithDescription("Read and display DBF file contents")
            .WithExample("read", "data.dbf")
            .WithExample("read", "data.dbf", "--format", "csv")
            .WithExample("read", "data.dbf", "--limit", "100", "--fields", "NAME,SALARY");
                
        config.AddCommand<InfoCommand>("info")
            .WithDescription("Display DBF file information and structure")
            .WithExample("info", "data.dbf")
            .WithExample("info", "data.dbf", "--verbose");
                
        config.AddCommand<ExportCommand>("export")
            .WithDescription("Export DBF file to various formats")
            .WithExample("export", "data.dbf", "--output", "data.csv")
            .WithExample("export", "data.dbf", "--format", "json", "--output", "data.json");
                
        config.AddCommand<ValidateCommand>("validate")
            .WithDescription("Validate DBF file structure and integrity")
            .WithExample("validate", "data.dbf");

        // Use Spectre.Console for help
        config.UseStrictParsing();
        config.ValidateExamples();
    });

    // Run the application
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}