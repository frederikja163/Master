using CommandLine;
using TapResult.Encodings;

namespace TapResult.CLI;

public static class Program
{
    internal class GeneralOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "WIP Set output to verbose messages.")]
        public bool Verbose { get; set; }
        
        [Value(1, MetaName = "inputfile", Required = true, HelpText = "WIP Specify the input file. Currently supports ")] // TODO: add parquet, CSV, our fileformat
        public string InputFile { get; set; } = null!;
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    [Verb("convert", HelpText = "Converts the file from one format to another")]
    internal sealed class ConvertOptions : GeneralOptions
    {
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
    
    public static bool Verbose = false;
    
    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<ConvertOptions, DescribeOptions>(args).MapResult(
                (ConvertOptions opts) => Converter.RunConvertOptions(opts),
                (DescribeOptions opts) => Task.FromResult(0), //TODO
                HandleParseError
            );
    }

    static Task<int> HandleParseError(IEnumerable<Error> errs)
    {
        return Task.FromResult(1);
        //handle errors
    }
}