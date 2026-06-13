using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using OpenTap;
using OpenTap.Hdf5;
using OpenTap.Plugins.Parquet;
using TapResult.Benchmarks.Data;
using TapResult.Benchmarks.OpenTAP;
using TapResult.OpenTAP;

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
            RawData data = (RawData)Data;
            TestPlan plan = new();
            RepeatStep repeatStep = new RepeatStep()
            {
                Repeat = data.Repeats,
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

    public static IEnumerable<ResultListener> GetResultListeners()
    {
        yield return new BinaryResultListener()
        {
            FilePath = Config.FilePath,
        };
        yield return new ParquetResultListener()
        {
            FilePath = new MacroString() { Text = Config.FilePath },
        };
        yield return new TapResultListener()
        {
            FilePath = new MacroString() {Text = Config.FilePath},
            WriterCreator = s => new TapDataWriter(File.Create(s), File.Create(s + ".TapSchema")),
            Name = "TapData",
        };
        yield return new TapResultListener()
        {
            FilePath = new MacroString() {Text = Config.FilePath},
            Name = "TapResult",
        };
        // yield return new Hdf5ResultListener()
        // {
        //     FilePath = Config.FilePath
        // };
        yield return new SQLiteDatabase()
        {
            FilePath = Config.FilePath
        };
        // yield return new CsvResultListener()
        // {
        //     FilePath = new MacroString() { Text = Config.FilePath }
        // };
    }
}