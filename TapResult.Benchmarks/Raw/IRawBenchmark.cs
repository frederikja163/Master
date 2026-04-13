using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

public interface IRawBenchmark
{
    public void Open(string filePath);
    public void Write(ICustomData data);
    public void Close();
}