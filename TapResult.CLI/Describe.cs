using TapResult.CLI.Options;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.CLI;

public class Describe
{
    
    internal static async Task<int> RunDescribeOptions(DescribeOptions opts)
    {
        if (opts.Verbose)
        {
            Console.WriteLine("Verbose output enabled. Current Arguments:");
            Console.WriteLine($"Input file: {opts.InputFile} filetype: {opts.InputFileType}");
            Console.WriteLine("--------");
        }
        var inputPath = new FileInfo(opts.InputFile);
        await using var inputStream = inputPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

        

        Reader reader = await Reader.CreateReaderAsync(inputStream);
        
        if (opts.DescribeHeader)
        {
            Console.WriteLine($"| {Center("Name",opts.NameWidth)} | {Center("Encoding",opts.EncodingWidth)} | {Center("Type",opts.TypeWidth)} | {Center("Id",opts.IdWidth)} | {Center("ParentId",opts.ParentIdWidth)} | {Center("Blob Values",opts.BlobWidth)} |");
            Console.WriteLine($"|{new string('-', opts.NameWidth + 2)}|{new string('-', opts.EncodingWidth + 2)}|{new string('-', opts.TypeWidth + 2)}|{new string('-', opts.IdWidth + 2)}|{new string('-', opts.ParentIdWidth + 2)}|{new string('-', opts.BlobWidth + 2)}|" );
            foreach (TableInfo tableInfo in reader.GetTables())
            {
                Console.WriteLine($"| {tableInfo.Name.PadRight(opts.NameWidth)} | {EncodingToString(tableInfo.Encoding, opts)}");
                foreach (ColumnInfo columnInfo in tableInfo.GetColumns())
                {
                    Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Gray ? ConsoleColor.White : ConsoleColor.Gray;
                    Console.WriteLine($"| {columnInfo.Name.PadRight(opts.NameWidth)} | {EncodingToString(columnInfo.Encoding, opts)}");
                    foreach (EncodingInfo subEncoding in columnInfo.Encoding.GetSubEncodings())
                    {
                        RecursiveWriteOutMetadata(subEncoding, opts);
                    }
                }
            }
        }

        if (!opts.DescribeFile) 
            return 0;
        foreach (TableInfo tableInfo in reader.GetTables())
        {
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"| {Center(tableInfo.Name, opts.MaxColDescribeCharLength)} |");

            EncodingInfo[] dataColumnInfos = opts.LimitTableLength ? 
                GetDataColumnInfos(tableInfo.Encoding.GetSubEncodings())
                    .Take(Console.BufferWidth / (opts.MaxColDescribeCharLength + 4)).ToArray() 
                : GetDataColumnInfos(tableInfo.Encoding.GetSubEncodings()).ToArray();
            
            Console.Write("| ");
            foreach (EncodingInfo info in dataColumnInfos)
            {
                Console.Write(Center($"{info.Id} ({info.Type})", opts.MaxColDescribeCharLength) + " | ");
            }
            Console.WriteLine();
            Console.Write("|");
            for (var i = 0; i < dataColumnInfos.Length; i++)
            {
                Console.Write(new string('-', opts.MaxColDescribeCharLength+2) + "|");
            }
            Console.WriteLine();

            string[] rows = new string[opts.MaxColDescribeLength + 2];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = "| ";
            }
            
            foreach (EncodingInfo encodingInfo in dataColumnInfos)
            {
                var col = GetDataColumn(encodingInfo, inputStream);
                var columnReader = col.OpenReader();
            
                int length = Math.Min(col.LogicalLength, opts.MaxColDescribeLength);
                for (int i = 0; i < length; i++)
                {
                    var str = columnReader.Read()?.ToString();
                    if (str is null)
                        rows[i] += new string(' ', opts.MaxColDescribeCharLength) + " | ";
                    else 
                        rows[i] += $"{str.Substring(0, Math.Min(str.Length, opts.MaxColDescribeCharLength)).PadRight(opts.MaxColDescribeCharLength)} | ";
                }

                if (col.LogicalLength > opts.MaxColDescribeLength)
                {
                    rows[^2] += " ...".PadRight(opts.MaxColDescribeCharLength) + " | ";
                    rows[^1] += $"{$"[ln: {col.LogicalLength}]".PadRight(opts.MaxColDescribeCharLength)} | ";
                }
                else
                {
                    rows[^2] += new string(' ', opts.MaxColDescribeCharLength) + " | ";
                    rows[^1] += new string(' ', opts.MaxColDescribeCharLength) + " | ";
                }
            }

            foreach (string row in rows)
            {
                Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Gray ? ConsoleColor.White : ConsoleColor.Gray;
                Console.WriteLine(row);
            }
        }

        return 0;
    }

    private static DataColumn GetDataColumn(EncodingInfo encodingInfo, FileStream inputStream)
    {
        GenericReader genericReader = new GenericReader(encodingInfo.Blob);
        int physicalSize = genericReader.Read<int>();
        long offset = genericReader.Read<long>();
        inputStream.Seek(offset, SeekOrigin.Begin);
        byte[] data = new byte[physicalSize];
        inputStream.ReadExactly(data);
        DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
        return col;
    }

    static string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        int left = (width - text.Length) / 2 + text.Length;
        return text.PadLeft(left).PadRight(width);
    }

    private static string EncodingToString(EncodingInfo info, DescribeOptions opts)
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
                blob = $"{{ prefixLn: {prefixLength}, prefix: {prefix} }}";
                break;
            case EncodingType.Binary:
                int physicalSize = reader.Read<int>();
                long offset = reader.Read<long>();
                blob = $"{{ physicalLn: {physicalSize}, offset: {offset} }}";
                break;
            case EncodingType.RunLength:
            case EncodingType.Split:
            case EncodingType.Null:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return $"{info.Encoding.ToString().PadRight(opts.EncodingWidth)} | {info.Type.ToString().PadRight(opts.TypeWidth)} | {info.Id.ToString().PadRight(opts.IdWidth)} | {info.ParentId.ToString().PadRight(opts.ParentIdWidth)} | {blob.PadRight(opts.BlobWidth)} |";
    }

    private static void RecursiveWriteOutMetadata(EncodingInfo encoding, DescribeOptions opts)
    {
        Console.WriteLine($"| {new string(' ', opts.NameWidth)} | {EncodingToString(encoding, opts)}");
        foreach (EncodingInfo child in encoding.GetSubEncodings())
        {
            RecursiveWriteOutMetadata(child, opts);
        }
    }

    private static IEnumerable<EncodingInfo> GetDataColumnInfos(IEnumerable<EncodingInfo> encodings)
    {
        foreach (EncodingInfo encodingInfo in encodings)
        {
            foreach (EncodingInfo recursiveSubEncoding in GetDataColumnInfos(encodingInfo.GetSubEncodings()))
            {
                yield return recursiveSubEncoding;
            }
            if (encodingInfo.Encoding == EncodingType.Binary)
                yield return encodingInfo;
        }
    }

}