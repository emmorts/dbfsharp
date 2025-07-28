using System.Reflection;
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
        config.SetApplicationVersion(
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
        );

        config
            .AddCommand<ReadCommand>("read")
            .WithDescription("Read and display DBF file contents")
            .WithExample("read", "data.dbf")
            .WithExample("read", "data.dbf", "--format", "csv")
            .WithExample("read", "data.dbf", "--limit", "100", "--fields", "NAME,SALARY")
            .WithExample("read");

        config
            .AddCommand<InfoCommand>("info")
            .WithDescription("Display DBF file information and structure")
            .WithExample("info", "data.dbf")
            .WithExample("info", "data.dbf", "--verbose")
            .WithExample("info");

        config.UseStrictParsing();
        config.ValidateExamples();
    });

    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}
