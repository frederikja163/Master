using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Master.Benchmarks.BenchmarkDotnetConfig;

internal sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(new FileSizeDiagnoser());
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Declared));
    }
}