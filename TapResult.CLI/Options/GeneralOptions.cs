using CommandLine;

namespace TapResult.CLI.Options;

internal class GeneralOptions
{
    [Option('v', "verbose", Required = false, HelpText = "WIP Set output to verbose messages.")]
    public bool Verbose { get; set; }
        
    [Value(1, MetaName = "inputfile", Required = true, HelpText = "WIP Specify the input file. Currently supports ")] // TODO: add parquet, CSV, our fileformat
    public string InputFile { get; set; } = null!;
    
    [Value(3, MetaName = "inputfiletype", Required = false, HelpText = "WIP Override the assumed filetype based off file extension")]
    public string InputFileType { get; set; } = Constants.Auto;
}