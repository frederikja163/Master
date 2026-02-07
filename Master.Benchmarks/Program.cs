using BenchmarkDotNet.Running;
using Master.Benchmarks.Raw;
using Parquet;
using Vortex.Net;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
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
    }
}