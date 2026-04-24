using CommandLine;

namespace TapResult.CLI.Options;

// ReSharper disable once ClassNeverInstantiated.Local
[Verb("convert", HelpText = "Converts the file from one format to another")]
internal sealed class ConvertOptions : GeneralOptions
{
    [Value(2, MetaName = "outputfile", Required = false, HelpText = "WIP Specify the output file. Currently supports ")]
    public string OutputFile { get; set; } = "out.csv";
        
    [Value(4, MetaName = "outputfiletype", Required = false, HelpText = "WIP Override the assumed filetype based off file extension")]
    public string OutputFileType { get; set; } = Constants.Auto;
        
        
    [Option('b', "bitpacking", Required = false, HelpText = "WIP Enable BitPacking Encoding. Specifying one enables only that encoding; otherwise all encodings are enabled.")]
    public bool BitPacking { get; set; }
        
    [Option('s', "split", Required = false, HelpText = "WIP Enable Split Encoding. Specifying one enables only that encoding; otherwise all encodings are enabled.")]
    public bool Split { get; set; }
        
    [Option('r', "runlength", Required = false, HelpText = "WIP Enable Run Length Encoding(RLE). Specifying one enables only that encoding; otherwise all encodings are enabled.")]
    public bool RunLength { get; set; }
    
    [Option('m', "multipleFiles", Required = false, HelpText = "Makes it so that TapResult Files are converted to multiple files")]
    public bool MultipleFiles { get; set; }

    //[Option("noextension", Required = false, HelpText = "WIP Disables the automation addition of file extensions if not specified in outputfile")]
    //public bool NoOutputFileTypeName { get; set; }
        
    // [Option('i',"stdin", HelpText = "WIP Read from stdin")]
    // public bool Stdin { get; set; }
}