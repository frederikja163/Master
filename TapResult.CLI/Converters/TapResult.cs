using System.Collections;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.CLI.Converters;

internal static class TapResult
{
    internal static void Convert(Constants.FileType fileType, FileStream input, FileStream output)
    {
        switch (fileType)
        {
            case Constants.FileType.Csv:
                ConvertToCsv(input, output);
                break;
            case Constants.FileType.Parquet:
                throw new NotImplementedException();
            case Constants.FileType.TapResult:
                throw new NotImplementedException();
            case Constants.FileType.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
        }
    }

    private static async void ConvertToCsv(FileStream input, FileStream output)
    {
        try
        {
            Reader reader = await Reader.CreateReaderAsync(input);
            await using StreamWriter writer = new StreamWriter(output);

            var columns = CombineColumns(reader.GetTables()).ToList();
        
            // write columnNames
            await writer.WriteLineAsync(string.Join(",", columns.Select(combinedColumn => combinedColumn[0].Name)));
        
            // Input is in columns so we need to flip columns to rows
            for (int i = 0; i < columns[0].Count; i++)
            {
                var readers = columns.Select(combinedColumn => reader.OpenColumnReader<string>(combinedColumn[i])).ToArray();
                while (!readers[0].IsAtEnd)
                {
                    var line = string.Join(",", readers.Select(columnReader => columnReader.Read())) + "\n";
                    if (Program.verbose)
                        Console.Write(line);
                    await writer.WriteAsync(line);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to read TapResult file: " + e);
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
}