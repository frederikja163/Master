namespace Master.Benchmarks.Raw;

public interface IRawBenchmark
{
    public void Write(string path, Data data);
}