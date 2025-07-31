namespace DbfSharp.ConsoleAot.Validation;

/// <summary>
/// Result of argument validation containing error information
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; private init; }

    public string? ErrorMessage { get; private init; }
    public string? Suggestion { get; private init; }

    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    public static ValidationResult Failure(string errorMessage, string? suggestion = null)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = errorMessage, Suggestion = suggestion };
    }
}
