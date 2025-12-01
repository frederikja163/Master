using BenchmarkDotNet.Running;
using Master.Benchmarks.Raw;
using Parquet;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        // File.Delete("file");
        // new RawVortexWriter().Write("file", new Data(1000, 1, 0.5f).PopulateRandomDoubles(2).PopulateRandomFloats().PopulateRandomInts().PopulateRandomNatoAlphabetStrings());
        BenchmarkRunner.Run<OpenTAPBenchmarks>();
        // BenchmarkRunner.Run<RawBenchmarks>();
        // BenchmarkRunner.Run<SparkBenchmarks>();
    }
}