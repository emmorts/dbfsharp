using System.Runtime.CompilerServices;
using System.Text;

namespace DbfSharp.Tests;

/// <summary>
/// Module initializer to set up test environment
/// </summary>
internal static class TestInitializer
{
    /// <summary>
    /// Initializes the test environment by registering encoding providers
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // Register code pages encoding provider to enable legacy encodings like Windows-1252
        // This matches the setup in Program.cs for the console application
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
