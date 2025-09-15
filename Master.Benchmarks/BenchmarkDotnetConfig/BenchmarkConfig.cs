using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Perfolizer.Horology;

namespace Master.Benchmarks.BenchmarkDotnetConfig;

internal sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(new Job(Job.MediumRun).WithIterationTime(TimeInterval.FromSeconds(1))
            .WithMinInvokeCount(1)
            .WithMinIterationCount(1)
            .WithMinWarmupCount(1));
        AddDiagnoser(new FileSizeDiagnoser());
        AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()));
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Declared));
    }
}