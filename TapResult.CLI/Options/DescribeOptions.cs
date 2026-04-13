using CommandLine;

namespace TapResult.CLI.Options;

[Verb("describe", HelpText = "Describes a TapResult file")]
internal sealed class DescribeOptions : GeneralOptions
{
    [Option('f', "file", Required = false, HelpText = "Describes the file. By default disabled")]
    public bool DescribeFile { get; set; } = false;

    [Option('h', "header", Required = false, HelpText = "Describes the header. By default enabled")]
    public bool DescribeHeader { get; set; } = true;
}