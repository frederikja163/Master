using CommandLine;
using TapResult.Encodings;

namespace TapResult.CLI;

public static class Program
{
    private class GeneralOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "WIP Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    [Verb("convert", HelpText = "Converts the file from one format to another")]
    private sealed class ConvertOptions : GeneralOptions
    {
        [Value(1, MetaName = "inputfile", Required = true, HelpText = "WIP Specify the input file. Currently supports ")] // TODO: add parquet, CSV, our fileformat
        public string InputFile { get; set; } = null!;
        
        [Value(2, MetaName = "outputfile", Required = false, HelpText = "WIP Specify the output file. Currently supports ")] // TODO: add parquet, CSV, our fileformat
        public string OutputFile { get; set; } = "out.csv";
        
        [Value(3, MetaName = "inputfiletype", Required = false, HelpText = "WIP Override the assumed filetype based off file extension")]
        public string InputFileType { get; set; } = Constants.Auto;
        
        [Value(4, MetaName = "outputfiletype", Required = false, HelpText = "WIP Override the assumed filetype based off file extension")]
        public string OutputFileType { get; set; } = Constants.Auto;
        
        
        [Option('b', "bitpacking", Required = false, HelpText = "WIP Enable BitPacking Encoding. Specifying one enables only that encoding; otherwise all encodings are enabled.")]
        public bool BitPacking { get; set; }
        
        [Option('s', "split", Required = false, HelpText = "WIP Enable Split Encoding. Specifying one enables only that encoding; otherwise all encodings are enabled.")]
        public bool Split { get; set; }
        
        [Option('r', "runlength", Required = false, HelpText = "WIP Enable Run Length Encoding(RLE). Specifying one enables only that encoding; otherwise all encodings are enabled.")]
        public bool RunLength { get; set; }

        //[Option("noextension", Required = false, HelpText = "WIP Disables the automation addition of file extensions if not specified in outputfile")]
        //public bool NoOutputFileTypeName { get; set; }
        
        // [Option('i',"stdin", HelpText = "WIP Read from stdin")]
        // public bool Stdin { get; set; }
    }

    [Verb("describe", HelpText = "Describes a TapResult file")]
    private sealed class DescribeOptions
    {
        
    }
    
    public static bool verbose = false;
    
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<ConvertOptions, DescribeOptions>(args).MapResult(
                (ConvertOptions opts) => RunConvertOptions(opts),
                (DescribeOptions opts) => 0, //TODO
                HandleParseError
            );
    }
    static int RunConvertOptions(ConvertOptions opts)
    {
        if (opts.Verbose)
        {
            Console.WriteLine("Verbose output enabled. Current Arguments:");
            Console.WriteLine($"-v {opts.Verbose}");
            Console.WriteLine($"Encodings: -s {opts.Split}, -");
            Console.WriteLine("Commandline is in Verbose mode!");
            verbose = true;
        }
        else
        {
            Console.WriteLine("Arguments: ");
            Console.WriteLine($"Input file: {opts.InputFile} filetype: {opts.InputFileType}");
            Console.WriteLine($"output file: {opts.OutputFile} filetype: {opts.OutputFileType}");
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
                break;
            case Constants.TapResult:
                Converters.TapResult.Convert(outputFileType, inputStream, outputStream);
                break;
            case Constants.Auto:
                switch (GetFileTypeFromPath(inputPath.FullName))
                {
                    case Constants.FileType.Csv:
                        Converters.Csv.Convert(outputFileType, encoder, inputStream, outputStream);
                        break;
                    case Constants.FileType.Parquet:
                        throw new NotImplementedException();
                        break;
                    case Constants.FileType.TapResult:
                        Converters.TapResult.Convert(outputFileType, inputStream, outputStream);
                        break;
                    case Constants.FileType.Unknown:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                throw new Exception(); // TODO how do we handle unknown filetype?
        }
        

        return 0;
    }
    static int HandleParseError(IEnumerable<Error> errs)
    {
        return 1;
        //handle errors
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