using CommandLine;

namespace TapResult.CLI.Options;

[Verb("describe", HelpText = "Describes a TapResult file")]
internal sealed class DescribeOptions : GeneralOptions
{
    [Option('f', "file", Required = false, HelpText = "Describes the file. By default disabled")]
    public bool DescribeFile { get; set; } = false;

    [Option('h', "header", Required = false, HelpText = "Describes the header. By default enabled")]
    public bool DescribeHeader { get; set; } = true;
    
    [Value(2, MetaName = "namewidth", Required = false, HelpText = "width of name column ")]
    public int NameWidth { get; set; } = 10;
    [Value(3, MetaName = "encodingwidth", Required = false, HelpText = "width of encoding column ")]
    public int EncodingWidth { get; set; } = 11;
    [Value(4, MetaName = "typewidth", Required = false, HelpText = "width of type column ")]
    public int TypeWidth { get; set; } = 7;
    [Value(5, MetaName = "idwidth", Required = false, HelpText = "width of id column ")]

    public int IdWidth { get; set; } = 3;
    [Value(6, MetaName = "parentidwidth", Required = false, HelpText = "width of parent id column ")]
    public int ParentIdWidth { get; set; } = 8;
    [Value(7, MetaName = "blobwidth", Required = false, HelpText = "width of blob column ")]
    public int BlobWidth { get; set; } = 50;

    [Value(8, MetaName = "maxcoldescribelength", Required = false, HelpText = "max amount of values in file description")]
    public int MaxColDescribeLength { get; set; } = 15;
    [Value(9, MetaName = "maxcoldescribecharlength", Required = false, HelpText = "max amount of characters in file description")]
    public int MaxColDescribeCharLength { get; set; } = 12;
    
    [Option('l', "limittablelength", Required = false, HelpText = "limits the length of the file table to the size of the console window. Disable to show all columns")]
    public bool LimitTableLength { get; set; } = true;
}