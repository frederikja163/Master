namespace Master.Benchmarks.RawBenchmarks;

public interface IRawBenchmark
{
    public Data Data { get; set; }
    public void Write();
}