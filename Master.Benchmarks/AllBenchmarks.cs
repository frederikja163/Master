using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using Master.Benchmarks.BenchmarkDotnetConfig;
using Master.Benchmarks.RawBenchmarks;
using OpenTap;
using OpenTap.Plugins.Parquet;
using Spreadsheet;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob()]
public class AllBenchmarks
{
    [GlobalSetup]
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
        yield return new Data(1_000).PopulateOrderedInts();
        yield return new Data(10_000).PopulateOrderedInts();
        yield return new Data(100_000).PopulateOrderedInts();
        yield return new Data(1_000_000).PopulateOrderedInts();
        yield return new Data(1_000).PopulateRandomInts();
        yield return new Data(10_000).PopulateRandomInts();
        yield return new Data(100_000).PopulateRandomInts();
        yield return new Data(1_000_000).PopulateRandomInts();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(IRawBenchmark implementation)
    {
        implementation.Data = Data;
        implementation?.Write();
    }

    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new RawBinaryStream();
        yield return new RawParquet();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetResultListeners))]
    public void WriteOpenTAP(ResultListener implementation)
    {
        TestPlan plan = new();
        plan.ChildTestSteps.Add(new ResultStep()
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
        yield return new BinaryResultListener();
        yield return new ParquetResultListener()
        {
            FilePath = new MacroString() { Text = Config.FilePath }
        };
        yield return new SQLiteDatabase()
        {
            FilePath = Config.FilePath
        };
        // yield return new CsvResultListener()
        // {
        //     FilePath = new MacroString() { Text = Config.FilePath }
        // };
        // yield return new SpreadsheetResultListener()
        // {
        //     Path = new MacroString() { Text = Config.FilePath },
        //     OpenFile = false,
        // };
    }
}