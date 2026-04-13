using TapResult.CLI.Options;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.CLI;

public class Describe
{
    const int nameWidth = 10;
    const int encodingWidth = 11;
    const int typeWidth = 7;
    const int idWidth = 3;
    const int parentIdWidth = 8;
    const int blobWidth = 50;
    
    internal static async Task<int> RunDescribeOptions(DescribeOptions opts)
    {
        if (opts.Verbose)
        {
            Console.WriteLine("Verbose output enabled. Current Arguments:");
            Console.WriteLine($"Input file: {opts.InputFile} filetype: {opts.InputFileType}");
            Console.WriteLine("--------");
            Program.Verbose = true;
        }
        var inputPath = new FileInfo(opts.InputFile);
        await using var inputStream = inputPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

        

        Reader reader = await Reader.CreateReaderAsync(inputStream);
        
        if (opts.DescribeHeader)
        {
            Console.WriteLine($"| {Center("Name",nameWidth)} | {Center("Encoding",encodingWidth)} | {Center("Type",typeWidth)} | {Center("Id",idWidth)} | {Center("ParentId",parentIdWidth)} | {"Blob Values",-blobWidth} |");
            Console.WriteLine($"|{new string('-', nameWidth + 2)}|{new string('-', encodingWidth + 2)}|{new string('-', typeWidth + 2)}|{new string('-', idWidth + 2)}|{new string('-', parentIdWidth + 2)}|{new string('-', blobWidth + 2)}|" );
            foreach (TableInfo tableInfo in reader.GetTables())
            {
                Console.WriteLine($"| {tableInfo.Name,-nameWidth} | {EncodingToString(tableInfo.Encoding)}");
                foreach (ColumnInfo columnInfo in tableInfo.GetColumns())
                {
                    Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Gray ? ConsoleColor.White : ConsoleColor.Gray;
                    Console.WriteLine($"| {columnInfo.Name,-nameWidth} | {EncodingToString(columnInfo.Encoding)}");
                    RecursiveWriteOutMetadata(columnInfo.Encoding);
                }
            }
        }

        if (opts.DescribeFile)
        {
            foreach (EncodingInfo columnInfo in getDataColumns(reader.GetTables()))
            {
                var a = reader.OpenColumnReader(columnInfo);
                for (int i = 0; i < a.Length; i++)
                {
                    Console.Write(a.Read());
                }
                Console.WriteLine();
            }
        }
        
        
        return 0;
    }
    
    static string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        int left = (width - text.Length) / 2 + text.Length;
        return text.PadLeft(left).PadRight(width);
    }

    private static string EncodingToString(EncodingInfo info)
    {
        GenericReader reader = new GenericReader(info.Blob);
        string blob = "";
        switch (info.Encoding)
        {
            case EncodingType.Table:
                while (!reader.IsAtEnd)
                {
                    blob += reader.ReadString() + ", ";
                }
                break;
            case EncodingType.BitPacking:
                byte prefixLength = reader.Read<byte>();
                ulong prefix = reader.Read<ulong>();
                int logicalLength = reader.Read<int>();
                blob = $"{prefixLength}, {prefix}, {logicalLength}";
                break;
            case EncodingType.Split:
            case EncodingType.Null:
            case EncodingType.Binary:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return $"{info.Encoding, -encodingWidth} | {info.Type, -typeWidth} | {info.Id, -idWidth} | {info.ParentId, -parentIdWidth} | {blob, -blobWidth} |";
    }

    private static void RecursiveWriteOutMetadata(EncodingInfo encoding)
    {
        foreach (EncodingInfo child in encoding.GetSubEncodings())
        {
            Console.WriteLine($"| {"", -nameWidth} | {EncodingToString(encoding)}");
            RecursiveWriteOutMetadata(child);
        }
    }

    private static IEnumerable<EncodingInfo> getDataColumns(IEnumerable<TableInfo> tables)
    {
        foreach (TableInfo tableInfo in tables)
        {
            foreach (EncodingInfo encoding in getRecursiveSubEncodings(tableInfo.Encoding.GetSubEncodings()))
            {
                if (encoding is ColumnInfo columnInfo)
                {
                    yield return 
                }
                if (encoding.Encoding == EncodingType.Binary)
                    yield return encoding;
            }
        }
        IEnumerable<EncodingInfo> getRecursiveSubEncodings(IEnumerable<EncodingInfo> encodings)
        {
            foreach (EncodingInfo encodingInfo in encodings)
            {
                foreach (EncodingInfo recursiveSubEncoding in getRecursiveSubEncodings(encodingInfo.GetSubEncodings()))
                {
                    yield return recursiveSubEncoding;
                }

                yield return encodingInfo;
            }
        }
    }

}