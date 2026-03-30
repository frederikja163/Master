using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

public interface IRawBenchmark : IDisposable
{
    public void Open(string filePath);
    public void Write(ICustomData data);
}