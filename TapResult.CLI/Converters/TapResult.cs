using Parquet;
using Parquet.Schema;
using TapResult.Readers;
using DataColumn = Parquet.Data.DataColumn;

namespace TapResult.CLI.Converters;

internal static class TapResult
{
    internal static async Task Convert(Constants.FileType fileType, Encoder encoder, FileStream input, FileStream output)
    {
        Console.WriteLine($"Converting from {Constants.FileType.TapResult} to {fileType.ToDisplayString()}");
        switch (fileType)
        {
            case Constants.FileType.Csv:
                await ConvertToCsv(input, output);
                break;
            case Constants.FileType.Parquet:
                throw new NotImplementedException();
            case Constants.FileType.TapResult:
                if (Program.Verbose)
                {
                    Console.WriteLine($"Encodings: {string.Join(", ", encoder.EncodingsById.Select(encoding => $"({encoding.Key}: {encoding.Value})"))}");
                }
                throw new NotImplementedException();
            case Constants.FileType.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
        }
    }

    private static async Task ConvertToCsv(FileStream input, FileStream output)
    {
        try
        {
            Reader reader = await Reader.CreateReaderAsync(input);
            await using StreamWriter writer = new StreamWriter(output);

            var columns = CombineColumns(reader.GetTables()).ToList();
        
            // write columnNames
            await writer.WriteLineAsync(string.Join(",", columns.Select(combinedColumn => combinedColumn[0].Name)));
        
            // Input is in columns so we need to flip columns to rows
            for (int i = 0; i < columns[0].Count; i++) // Amount of combined columns across tables
            {
                var readers = columns.Select(combinedColumn => reader.OpenColumnReader<string>(combinedColumn[i])).ToArray();
                while (!readers[0].IsAtEnd)
                {
                    var line = string.Join(",", readers.Select(columnReader => columnReader.Read())) + "\n";
                    if (Program.Verbose)
                    {
                        Console.WriteLine($"reader at {readers[0].Index} out of {readers[0].Length}");
                        Console.Write(line);
                    }
                    await writer.WriteAsync(line);
                }

                if (Program.Verbose)
                {
                    Console.WriteLine($"Finished writing columns from table {i}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to Convert TapResult file to CSV: " + e);
        }
    }
    
    private static IEnumerable<List<ColumnInfo>> CombineColumns(IEnumerable<TableInfo> tables) //TODO: make this optional
    {
        Dictionary<(LogicalType, string), List<ColumnInfo>> dict = new();
        foreach (TableInfo tableInfo in tables)
        {
            foreach (ColumnInfo columnInfo in tableInfo.GetColumns())
            {
                if (dict.TryGetValue((columnInfo.Encoding.Type, columnInfo.Name), out var list))
                {
                    list.Add(columnInfo);
                }
                else
                {
                    List<ColumnInfo> newList = new List<ColumnInfo>();
                    newList.Add(columnInfo);
                    dict.Add((columnInfo.Encoding.Type, columnInfo.Name), newList);
                }
            }
        }

        return dict.Values.AsEnumerable<List<ColumnInfo>>();
    }
    
    private static async void ConvertToParquet(FileStream input, FileInfo output)
    {
        try
        {
            // TODO: currently 1 parquet file with n rowgroups => 1 tapresult file with n tables => n parquet files with 1 rowgroup
            Reader reader = await Reader.CreateReaderAsync(input);
            foreach (TableInfo tableInfo in reader.GetTables())
            {
                var schema = new ParquetSchema(tableInfo.GetColumns().Select(column => new DataField(column.Name, column.GetType())));
                FileInfo fileInfo = new FileInfo(output.FullName + "_" + tableInfo.Name);

                await using var outputStream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, outputStream);
                using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
                foreach ((ColumnInfo columnInfo, DataField field) in tableInfo.GetColumns().Zip(schema.DataFields))
                {
                    var columnReader = reader.OpenColumnReader(columnInfo);
                    await groupWriter.WriteColumnAsync(new DataColumn(field, columnReader.Read(columnReader.Length).ToArray()));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to Convert TapResult file to CSV: " + e);
        }
    }
}