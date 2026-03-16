using Master.Benchmarks.Data;

namespace Master.Benchmarks.Raw;

public interface IRawBenchmark
{
    public void Write(string path, ICustomData data);
}