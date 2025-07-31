using System.Text;
using ConsoleAppFramework;
using DbfSharp.ConsoleAot.Commands;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch
{
    Console.OutputEncoding = Encoding.Default;
}

var app = ConsoleApp.Create();

app.Add<DbfCommands>();

app.Run(args);
