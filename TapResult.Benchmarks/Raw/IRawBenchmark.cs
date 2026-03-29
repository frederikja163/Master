using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

public interface IRawBenchmark : IDisposable
{
    public void Write(ICustomData data);
}