using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace Master.Benchmarks.BenchmarkDotnetConfig;

internal sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(new Exporter());
        AddJob(new Job(Job.ShortRun));
        AddDiagnoser(new FileSizeDiagnoser());
        AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()));
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Declared));
    }
}