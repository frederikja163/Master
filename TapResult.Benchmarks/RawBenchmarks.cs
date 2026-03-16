using BenchmarkDotNet.Attributes;
using Parquet;
using TapResult.Benchmarks.Raw;

namespace TapResult.Benchmarks;

public class RawBenchmarks : AllBenchmarks
{
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(IRawBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            implementation?.Write(Config.FilePath, Data);
        }, Timeout);
    }

    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new CascadingBenchmark();
        yield return new EncodingBenchmark();
        yield return new RawBinaryStream();
        yield return new RawParquet(CompressionMethod.Snappy);
        // yield return new RawParquet(CompressionMethod.Zstd);
        // yield return new RawParquet(CompressionMethod.Gzip);
        yield return new RawParquet(CompressionMethod.None);
        // yield return new RawParquet(CompressionMethod.LZ4);
        // yield return new RawParquet(CompressionMethod.Lz4Raw);
        // yield return new RawParquet(CompressionMethod.Brotli);
        // yield return new RawCsv();
        // yield return new RawSqlite();
        // yield return new RawHdf5Benchmark();
        if (OperatingSystem.IsLinux())
        {
            // yield return new RawVortexWriter();
        }
        else
        {
            Console.WriteLine("Skipping Vortex benchmark since they are only enabled on linux.");
        }
    }
}