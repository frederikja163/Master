using Csv;
using Microsoft.VisualBasic;
using TapResult.Encodings;

namespace TapResult.CLI.Converters;

internal static class Csv
{
    internal static void Convert(Constants.FileType fileType, Encoder encoder, FileStream input, FileStream output)
    {
        if (Program.Verbose)
        {
            Console.WriteLine($"Encodings: {string.Join(", ", encoder.EncodingsById.Select(encoding => $"({encoding.Key}: {encoding.Value})"))}");
        }
        
        Console.WriteLine($"Converting from {Constants.FileType.Csv} to {fileType.ToDisplayString()}");
        switch (fileType)
        {
            case Constants.FileType.TapResult:
                ConvertToTapResult(encoder, input, output);
                break;
            case Constants.FileType.Csv:
                throw new NotImplementedException();
            case Constants.FileType.Parquet:
                throw new NotImplementedException();
            case Constants.FileType.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
        }
    }
    
    private static void ConvertToTapResult(Encoder encoder, FileStream input, FileStream output)
    {
        var csv = CsvReader.ReadFromStream(input); //TODO: consider ReadFromStreamAsSpan

        using var enumerator = csv.GetEnumerator();

        if (!enumerator.MoveNext())
            return;

        var headers = enumerator.Current.Headers;
        // TODO: we likely want to convert to other logicaltypes. A specific encoder is likely the best solution
        var datacolumns = headers.Select(header => new ColumnBuilder(LogicalType.String, 512)).ToArray(); 

        do
        {
            for (int i = 0; i < enumerator.Current.ColumnCount; i++)
            {
                datacolumns[i].WriteString(enumerator.Current[i]);
            }

            for (int i = 0; i < headers.Length - enumerator.Current.ColumnCount; i++)
            {
                datacolumns[i].WriteString("");
            }
        } while (enumerator.MoveNext());

        Table table = new Table(datacolumns.Select(datacolumn => datacolumn.BuildDataColumn()), headers, "CSVTable");
        table.Compress(encoder);
        using Writer writer = new Writer(output);
        writer.Write(table);
    }
}