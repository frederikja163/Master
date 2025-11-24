using BenchmarkDotNet.Attributes;
using Keysight.OpenTap.Plugins.Csv;
using Keysight.OpenTap.Plugins.ResultListeners;
using Master.Benchmarks.BenchmarkDotnetConfig;
using Master.Benchmarks.OpenTAP;
using Master.Benchmarks.RawBenchmarks;
using OpenTap;
using OpenTap.Plugins.Parquet;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class AllBenchmarks
{
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);
    
    [IterationSetup]
    public void Setup()
    {
        if (File.Exists(Config.FilePath))
        {
            File.Delete(Config.FilePath);
        }
    }
    
    [ParamsSource(nameof(GetData))] public required Data Data { get; set; }

    public IEnumerable<Data> GetData()
    {
        yield return new Data(10_000, 1_000).PopulateOrderedInts().PopulateRandomInts();
        yield return new Data(10_000, 100).PopulateOrderedInts().PopulateRandomInts();
        yield return new Data(10_000, 10).PopulateOrderedInts().PopulateRandomInts();
        yield return new Data(10_000, 1).PopulateOrderedInts().PopulateRandomInts();
        // yield return new Data(10_000, 1_000).PopulateOrderedInts();
        // yield return new Data(10_000, 100).PopulateOrderedInts();
        // yield return new Data(10_000, 10).PopulateOrderedInts();
        // yield return new Data(10_000, 1).PopulateOrderedInts();
        // yield return new Data(10_000, 1_000).PopulateRandomInts();
        // yield return new Data(10_000, 100).PopulateRandomInts();
        // yield return new Data(10_000, 10).PopulateRandomInts();
        // yield return new Data(10_000, 1).PopulateRandomInts();
        // yield return new Data(10_000, 1_000).PopulateRandomFloats();
        // yield return new Data(10_000, 100).PopulateRandomFloats();
        // yield return new Data(10_000, 10).PopulateRandomFloats();
        // yield return new Data(10_000, 1).PopulateRandomFloats();
        // yield return new Data(10_000, 1_000).PopulateRandomGuidStrings();
        // yield return new Data(10_000, 100).PopulateRandomGuidStrings();
        // yield return new Data(10_000, 10).PopulateRandomGuidStrings();
        // yield return new Data(10_000, 1).PopulateRandomGuidStrings();
        // yield return new Data(10_000, 1_000).PopulateRandomNatoAlphabetStrings();
        // yield return new Data(10_000, 100).PopulateRandomNatoAlphabetStrings();
        // yield return new Data(10_000, 10).PopulateRandomNatoAlphabetStrings();
        // yield return new Data(10_000, 1).PopulateRandomNatoAlphabetStrings();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(IRawBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            implementation?.Write(Config.FilePath, Data);
        }, _timeout);
    }

    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new RawBinaryStream();
        yield return new RawParquet();
        // yield return new RawCsv();
        // yield return new RawAvro();
        // yield return new RawSqlite();
        // yield return new RawHdf5Benchmark();
        yield return new RawVortexWriter();
    }
    
    // [Benchmark]
    // [ArgumentsSource(nameof(GetResultListeners))]
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
        }, _timeout);
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

    private static void RunWithTimeout(Action action, TimeSpan timeout)
    {
        Task task = Task.Run(action);
        if (!task.Wait(timeout))
        {
            throw new TimeoutException();
        }
    }
}