namespace TapResult.CLI;

internal static class Constants
{
    internal const string Parquet = "parquet";
    internal const string ParquetFile = "par";
    internal const string TapResult = "tapresult";
    internal const string TapResultFile = "otap";
    internal const string Csv = "csv";
    internal const string CsvFile = "csv";
    internal const string Auto = "auto"; // used by filetype to have the CLI guess the filetype by file extension
        
    internal enum FileType
    {
        Csv,
        Parquet,
        TapResult,
        Unknown
    }
    public static string ToDisplayString(this FileType filetype)
    {
        return filetype switch
        {
            FileType.Csv => "CSV",
            FileType.Parquet => "Parquet",
            FileType.TapResult => "TapResult",
            FileType.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(filetype), filetype, null)
        };
    }
}