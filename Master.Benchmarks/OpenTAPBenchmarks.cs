using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using Master.Benchmarks.OpenTAP;
using OpenTap;
using OpenTap.Plugins.Parquet;

namespace Master.Benchmarks;

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
                Repeat = Data.Repeats,
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
        yield return new ParquetResultListener()
        {
            FilePath = new MacroString() { Text = Config.FilePath }
        };
        yield return new SQLiteDatabase()
        {
            FilePath = Config.FilePath
        };
        yield return new CsvResultListener()
        {
            FilePath = new MacroString() { Text = Config.FilePath }
        };
        // yield return new SpreadsheetResultListener()
        // {
        //     Path = new MacroString() { Text = Config.FilePath },
        //     OpenFile = false,
        // };
    }
}