using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using OpenTap;
using OpenTap.Hdf5;
using TapResult.Benchmarks.OpenTAP;

// using OpenTap.Plugins.Parquet;

namespace TapResult.Benchmarks;

public class OpenTAPBenchmarks : AllBenchmarks
{
    
    [Benchmark]
    [ArgumentsSource(nameof(GetResultListeners))]
    public void WriteOpenTAP(ResultListener implementation)
    {
        RunWithTimeout(() =>
        {
            TestPlan plan = new();
            RepeatStep repeatStep = new RepeatStep()
            {
                Repeat = 1,
            };
            plan.ChildTestSteps.Add(repeatStep);
            repeatStep.ChildTestSteps.Add(new ResultStep()
            {
                Data = Data
            });
            TestPlanRun planRun = plan.Execute([implementation]);
            planRun.WaitForResults();
        }, Timeout);
    }

    public IEnumerable<ResultListener> GetResultListeners()
    {
        yield return new BinaryResultListener()
        {
            FilePath = Config.FilePath,
        };
        // TODO: For some reason the parquet dependency is a bit messed up right now, we should try to resolve this.
        // yield return new ParquetResultListener()
        // {
        //     FilePath = new MacroString() { Text = Config.FilePath }
        // };
        yield return new Hdf5ResultListener()
        {
            FilePath = Config.FilePath
        };
        yield return new SQLiteDatabase()
        {
            FilePath = Config.FilePath
        };
        yield return new CsvResultListener()
        {
            FilePath = new MacroString() { Text = Config.FilePath }
        };
    }
}