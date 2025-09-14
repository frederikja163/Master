using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using Master.Benchmarks.BenchmarkDotnetConfig;
using Master.Benchmarks.OpenTAP;
using Master.Benchmarks.RawBenchmarks;
using OpenTap;
using OpenTap.Plugins.Parquet;
using Spreadsheet;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class AllBenchmarks
{
    [IterationSetup]
    public void Setup()
    {
        if (File.Exists(Config.FilePath))
        {
            File.Delete(Config.FilePath);
        }
    }
    
    [ParamsSource(nameof(GetData))] public Data Data { get; set; }

    public IEnumerable<Data> GetData()
    {
        yield return new Data(1000, 1).PopulateOrderedInts();
        // yield return new Data(10_000, 10).PopulateOrderedInts();
        // yield return new Data(10_000, 1).PopulateOrderedInts();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(IRawBenchmark implementation)
    {
        implementation?.Write(Config.FilePath, Data);
    }

    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new RawBinaryStream();
        yield return new RawParquet();
        yield return new RawCsv();
        yield return new RawAvro();
        yield return new RawSqlite();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetResultListeners))]
    public void WriteOpenTAP(ResultListener implementation)
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
        while (plan.IsRunning)
        {
            
        }
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