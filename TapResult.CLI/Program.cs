using CommandLine;
using TapResult.CLI.Options;

namespace TapResult.CLI;

public static class Program
{
    private static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<ConvertOptions, DescribeOptions>(args).MapResult(
                (ConvertOptions opts) => Convert.RunConvertOptions(opts),
                (DescribeOptions opts) => Describe.RunDescribeOptions(opts),
                HandleParseError
            );
    }

    static Task<int> HandleParseError(IEnumerable<Error> errs)
    {
        return Task.FromResult(1);
        //handle errors
    }
}