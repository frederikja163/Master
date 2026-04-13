using TapResult.CLI.Options;
using TapResult.Encodings;

namespace TapResult.CLI;

public class Convert
{
    internal static async Task<int> RunConvertOptions(ConvertOptions opts)
    {
        if (opts.Verbose)
        {
            Console.WriteLine("Verbose output enabled. Current Arguments:");
            Console.WriteLine($"Input file: {opts.InputFile} filetype: {opts.InputFileType}");
            Console.WriteLine($"output file: {opts.OutputFile} filetype: {opts.OutputFileType}");
            Console.WriteLine("--------");
            Program.Verbose = true;
        }
        var encoder = ParseEncodings(opts);
        
        var inputPath = new FileInfo(opts.InputFile);
        var outputPath = new FileInfo(opts.OutputFile);
        using var inputStream = inputPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var outputStream = outputPath.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        
        var outputFileType = opts.OutputFileType.ToLower() switch
        {
            Constants.CsvFile => Constants.FileType.Csv,
            Constants.ParquetFile => Constants.FileType.Parquet,
            Constants.Auto => GetFileTypeFromPath(outputPath.FullName),
            _ => throw new Exception() // TODO how do we handle unknown filetype?
        };
        
        switch (opts.InputFileType.ToLower())
        {
            case Constants.Csv:
                Converters.Csv.Convert(outputFileType, encoder, inputStream, outputStream);
                break;
            case Constants.Parquet:
                throw new NotImplementedException();
            case Constants.TapResult:
                await Converters.TapResult.Convert(outputFileType, encoder, inputStream, outputStream);
                break;
            case Constants.Auto:
                switch (GetFileTypeFromPath(inputPath.FullName))
                {
                    case Constants.FileType.Csv:
                        Converters.Csv.Convert(outputFileType, encoder, inputStream, outputStream);
                        break;
                    case Constants.FileType.Parquet:
                        throw new NotImplementedException();
                    case Constants.FileType.TapResult:
                        await Converters.TapResult.Convert(outputFileType, encoder, inputStream, outputStream);
                        break;
                    case Constants.FileType.Unknown:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                throw new Exception(); // TODO how do we handle unknown filetype?
        }
        

        return 0;
    }
    
    private static Constants.FileType GetFileTypeFromPath(string inputFile)
    {
        var extension = Path.GetExtension(inputFile).TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            Constants.CsvFile => Constants.FileType.Csv,
            Constants.ParquetFile => Constants.FileType.Parquet,
            Constants.TapResultFile => Constants.FileType.TapResult,
            _ => Constants.FileType.Unknown
        };
    }

    private static Encoder ParseEncodings(ConvertOptions o)
    {
        if (o is { BitPacking: false, Split: false, RunLength: false })
        {
            return new Encoder();
        }
        List<IEncoding> encodings = [];
        
        if (o.BitPacking)
        {
            encodings.Add(new BitPacking());
        }
        if (o.Split)
        {
            encodings.Add(new SplitEncoding());
        }
        if (o.RunLength)
        {
            //encodings.Add(new RunLength());
        }

        return new Encoder(encodings);
    }
}