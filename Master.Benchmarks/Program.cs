using BenchmarkDotNet.Running;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
<<<<<<< HEAD
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.Write(123);
        writer.Dispose();
        stream.GetBuffer();
        
        var b = new RawBenchmarks()
        {
            Data = AllBenchmarks.GetData().First(),
        };
        b.WriteRaw(new RawBinaryStream());
        // BenchmarkRunner.Run<OpenTAPBenchmarks>();
        BenchmarkRunner.Run<RawBenchmarks>();
        // BenchmarkRunner.Run<SparkBenchmarks>();
=======
        BenchmarkRunner.Run<OpenTAPBenchmarks>();
        BenchmarkRunner.Run<RawBenchmarks>();
        BenchmarkRunner.Run<SparkBenchmarks>();
        BenchmarkRunner.Run<TPCHBenchmarks>();
>>>>>>> origin/main
    }
}