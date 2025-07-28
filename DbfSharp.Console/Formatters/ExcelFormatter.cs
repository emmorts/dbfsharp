using ClosedXML.Excel;
using DbfSharp.Core;
using DbfSharp.Core.Parsing;

namespace DbfSharp.Console.Formatters;

/// <summary>
/// Formats DBF records as Excel (XLSX) files using ClosedXML
/// </summary>
public sealed class ExcelFormatter : IFileOnlyFormatter
{
    private readonly FormatterOptions _options;

    /// <summary>
    /// Initializes a new instance of the ExcelFormatter
    /// </summary>
    /// <param name="options">Formatting options</param>
    public ExcelFormatter(FormatterOptions? options = null)
    {
        _options = options ?? new FormatterOptions();
    }

    /// <summary>
    /// Writes DBF records as Excel file content to the specified TextWriter
    /// For Excel formatter, this throws an exception since Excel files must be saved to disk
    /// </summary>
    public Task WriteAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        TextWriter writer,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException(
            "Excel format can only be written to files. Use the -o/--output option to specify an output file path."
        );
    }

    /// <summary>
    /// Writes DBF records as Excel file directly to the specified file path
    /// </summary>
    public Task WriteToFileAsync(
        IEnumerable<DbfRecord> records,
        string[] fields,
        DbfReader reader,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("DBF Data");

        // Write headers
        for (var i = 0; i < fields.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = fields[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Write data rows
        var row = 2;
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var col = 0; col < fields.Length; col++)
            {
                var value = record[fields[col]];
                SetCellValue(worksheet.Cell(row, col + 1), value);
            }

            row++;

            // Check if we've hit the limit
            if (_options.MaxDisplayRecords > 0 && row - 2 >= _options.MaxDisplayRecords)
            {
                break;
            }
        }

        // Auto-fit columns for better readability
        worksheet.Columns().AdjustToContents();

        // Save directly to file
        workbook.SaveAs(filePath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the appropriate cell value and format based on the data type
    /// </summary>
    private void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = "";
                break;

            case string stringValue:
                cell.Value = stringValue;
                break;

            case DateTime dateTimeValue:
                cell.Value = dateTimeValue;
                cell.Style.DateFormat.Format = _options.DateFormat ?? "yyyy-mm-dd";
                break;

            case decimal decimalValue:
                cell.Value = (double)decimalValue;
                cell.Style.NumberFormat.Format = "0.00";
                break;

            case double doubleValue:
                if (double.IsFinite(doubleValue))
                {
                    cell.Value = doubleValue;
                    cell.Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    cell.Value = doubleValue.ToString();
                }
                break;

            case float floatValue:
                if (float.IsFinite(floatValue))
                {
                    cell.Value = floatValue;
                    cell.Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    cell.Value = floatValue.ToString();
                }
                break;

            case bool boolValue:
                cell.Value = boolValue;
                break;

            case InvalidValue:
                cell.Value = "#ERROR";
                cell.Style.Font.FontColor = XLColor.Red;
                break;

            case byte[] byteArray:
                cell.Value = Convert.ToBase64String(byteArray);
                break;

            // Integer types
            case int intValue:
                cell.Value = intValue;
                break;

            case long longValue:
                cell.Value = longValue;
                break;

            case short shortValue:
                cell.Value = shortValue;
                break;

            case byte byteValue:
                cell.Value = byteValue;
                break;

            case sbyte sbyteValue:
                cell.Value = sbyteValue;
                break;

            case uint uintValue:
                cell.Value = uintValue;
                break;

            case ulong ulongValue:
                cell.Value = (double)ulongValue;
                break;

            case ushort ushortValue:
                cell.Value = ushortValue;
                break;

            default:
                cell.Value = value.ToString() ?? "";
                break;
        }
    }
}
