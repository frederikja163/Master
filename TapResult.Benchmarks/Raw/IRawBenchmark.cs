using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

public interface IRawBenchmark
{
    public void Write(string path, ICustomData data);
}