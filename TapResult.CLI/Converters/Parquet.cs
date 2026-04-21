using Csv;
using Parquet;
using Parquet.Schema;
using TapResult.Columns;
using DataColumn = Parquet.Data.DataColumn;

namespace TapResult.CLI.Converters;

public sealed class Parquets
{
    internal static void Convert(Constants.FileType fileType, Encoder encoder, FileStream input, FileStream output, bool verbose)
    {
        if (verbose)
        {
            Console.WriteLine($"Encodings: {string.Join(", ", encoder.EncodingsById.Select(encoding => $"({encoding.Key}: {encoding.Value})"))}");
        }
        
        Console.WriteLine($"Converting from {Constants.FileType.Parquet} to {fileType.ToDisplayString()}");
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
    
    private static async void ConvertToTapResult(Encoder encoder, FileStream input, FileStream output)
    {
        try
        {
            using ParquetReader parquetReader = await ParquetReader.CreateAsync(input);

            await using Writer writer = new Writer(output);

            for (int i = 0; i < parquetReader.RowGroupCount; i++)
            {
                using ParquetRowGroupReader rowGroupReader = parquetReader.OpenRowGroupReader(i);
                IColumn[] columns = new IColumn[parquetReader.Schema.DataFields.Length];
                for (var j = 0; j < parquetReader.Schema.DataFields.Length; j++)
                {
                    var df = parquetReader.Schema.DataFields[j];
                    DataColumn columnData = await rowGroupReader.ReadColumnAsync(df);
                    columns[j] = ColumnBuilder.Create(columnData.Data);
                }

                Table table = new Table(columns, parquetReader.Schema.DataFields.Select(field => field.Name),
                    "Parquetfile");
                table.Compress(encoder);
                writer.Write(table); 
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to convert Parquet file to TapResult: " + e);
        }
    }
}