using Parquet;
using Parquet.Schema;
using TapResult.CLI.Options;
using TapResult.Readers;
using DataColumn = Parquet.Data.DataColumn;

namespace TapResult.CLI.Converters;

internal static class TapResult
{
    internal static async Task Convert(Constants.FileType fileType, Encoder encoder, FileStream input, FileInfo output, ConvertOptions opts)
    {
        Console.WriteLine($"Converting from {Constants.FileType.TapResult} to {fileType.ToDisplayString()}");
        
        switch (fileType)
        {
            case Constants.FileType.Csv:
                await ConvertToCsv(input, output, opts);
                break;
            case Constants.FileType.Parquet:
                await ConvertToParquet(input, output);
                break;
            case Constants.FileType.TapResult:
                if (opts.Verbose)
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

    private static async Task ConvertToCsv(FileStream input, FileInfo output, ConvertOptions opts)
    {
        try
        {
            Reader reader = await Reader.CreateReaderAsync(input);

            if (opts.MultipleFiles)
            {
                foreach (TableInfo tableInfo in reader.GetTables())
                {
                    FileInfo fileInfo;
                    if (tableInfo.Name.ContainsAny(['/', '\\']))
                    {
                        if (opts.Verbose)
                        {
                            Console.WriteLine($"Table {tableInfo.Name} contains a path, creating directory");
                        }
                        var directory = Directory.CreateDirectory(Path.Combine(output.DirectoryName ?? string.Empty, Path.GetDirectoryName(tableInfo.Name) ?? string.Empty));
                        fileInfo = new FileInfo(Path.Combine(directory.FullName, $"{Path.GetFileName(tableInfo.Name)}{output.Extension}"));
                    }
                    else
                    {
                        fileInfo = new FileInfo(
                            Path.Combine(
                                output.DirectoryName ?? string.Empty,
                                $"{Path.GetFileNameWithoutExtension(output.Name)}_{tableInfo.Name}{output.Extension}"
                            ));
                    }
                    await using var outputStream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);
                    await using StreamWriter writer = new StreamWriter(outputStream);
                    await WriteTableToCsv(tableInfo, writer, reader, opts.Verbose, includeTableName: false);
                }
            }
            else
            {
                await using var outputStream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await using StreamWriter writer = new StreamWriter(outputStream);
                foreach (TableInfo tableInfo in reader.GetTables())
                {
                    await WriteTableToCsv(tableInfo, writer, reader, opts.Verbose);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to Convert TapResult file to CSV: " + e);
        }
    }

    private static async Task WriteTableToCsv(TableInfo tableInfo, StreamWriter writer, Reader reader, bool verbose, bool includeTableName = true)
    {
        if (verbose)
        {
            Console.WriteLine($"Starting write of table {tableInfo.Name}");
        }
        if (includeTableName)
            await writer.WriteLineAsync(tableInfo.Name);
        await writer.WriteLineAsync(string.Join(",", tableInfo.GetColumns().Select(column => column.Name)));
        IColumnReader[] readers = tableInfo.GetColumns().Select(column => reader.OpenColumnReader(column)).ToArray();
        while (!readers.All(read => read.IsAtEnd))
        {
            var line = string.Join(",", readers.Select(columnReader => !columnReader.IsAtEnd ? columnReader.Read()?.ToString() ?? "" : "")
                .Select(str => str.Contains(',') ? $"\"{str}\"" : str)) + "\n";
            if (verbose)
            {
                Console.WriteLine($"reader at {readers[0].Index} out of {readers[0].Length}");
                Console.Write(line);
            }
            await writer.WriteAsync(line);
        }
        if (verbose)
        {
            Console.WriteLine($"Finished writing columns from table {tableInfo.Name}");
        }
    }
    
    private static async Task ConvertToParquet(FileStream input, FileInfo output)
    {
        try
        {
            // TODO: currently 1 parquet file with n rowgroups => 1 tapresult file with n tables => n parquet files with 1 rowgroup
            Reader reader = await Reader.CreateReaderAsync(input);
            foreach (TableInfo tableInfo in reader.GetTables())
            {
                var schema = new ParquetSchema(tableInfo.GetColumns().Select(column => new DataField(column.Name, column.Encoding.Type.ToCsType())));
                
                var fileInfo = new FileInfo(
                    Path.Combine(
                        output.DirectoryName ?? string.Empty,
                        $"{Path.GetFileNameWithoutExtension(output.Name)}_{tableInfo.Name}{output.Extension}"
                    ));

                await using var outputStream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, outputStream);
                using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
                foreach ((ColumnInfo columnInfo, DataField field) in tableInfo.GetColumns().Zip(schema.DataFields))
                {
                    var columnReader = reader.OpenColumnReader(columnInfo);
                    var values = columnReader.Read(columnReader.Length).ToArray();

                    Array typedValues = Array.CreateInstance(field.ClrType, values.Length);

                    for (int i = 0; i < values.Length; i++)
                    {
                        typedValues.SetValue(System.Convert.ChangeType(values[i], field.ClrType), i);
                    }
                    await groupWriter.WriteColumnAsync(new DataColumn(field, typedValues));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to Convert TapResult file to CSV: " + e);
        }
    }
}