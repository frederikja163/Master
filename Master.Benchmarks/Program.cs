using BenchmarkDotNet.Running;
using Keysight.OpenTap.Plugins.Csv;
using Master.Benchmarks.RawBenchmarks;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        // new RawHdf5Benchmark().Write("Test.hdf5", new Data(1000, 10).PopulateRandomNatoAlphabetStrings());
        // new RawHdf5Benchmark().Write("Test.hdf5", new Data(1000, 10).PopulateOrderedInts());
        BenchmarkRunner.Run<AllBenchmarks>();
    }
}