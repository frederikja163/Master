using CommandLine;

namespace TapResult.CLI.Options;

[Verb("describe", HelpText = "Describes a TapResult file")]
internal sealed class DescribeOptions : GeneralOptions
{
    [Option('f', "file", Required = false, HelpText = "Describes the file. By default disabled")]
    public bool DescribeFile { get; set; } = false;

    [Option('h', "header", Required = false, HelpText = "Describes the header. By default enabled")]
    public bool DescribeHeader { get; set; } = true;
    
    [Option('n', "namewidth", Required = false, HelpText = "width of name column")]
    public int NameWidth { get; set; } = 10;
    [Option('e', "encodingwidth", Required = false, HelpText = "width of encoding column ")]
    public int EncodingWidth { get; set; } = 11;
    [Option('t', "typewidth", Required = false, HelpText = "width of type column ")]
    public int TypeWidth { get; set; } = 7;
    [Option('i', "idwidth", Required = false, HelpText = "width of id column ")]

    public int IdWidth { get; set; } = 3;
    [Option('p', "parentidwidth", Required = false, HelpText = "width of parent id column ")]
    public int ParentIdWidth { get; set; } = 8;
    [Option('b', "blobwidth", Required = false, HelpText = "width of blob column ")]
    public int BlobWidth { get; set; } = 50;

    [Option('c', "maxcoldescribelength", Required = false, HelpText = "max amount of values in file description")]
    public int MaxColDescribeLength { get; set; } = 15;
    [Option('d', "maxcoldescribecharlength", Required = false, HelpText = "max amount of characters in file description")]
    public int MaxColDescribeCharLength { get; set; } = 12;
    
    [Option('l', "limittablelength", Required = false, HelpText = "limits the length of the file table to the size of the console window. Disable to show all columns")]
    public bool LimitTableLength { get; set; } = true;
}