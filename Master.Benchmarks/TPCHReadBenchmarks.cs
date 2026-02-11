using BenchmarkDotNet.Attributes;
using Master.Benchmarks.BenchmarkDotnetConfig;
using Master.Benchmarks.Data;
using Master.Benchmarks.Raw;
using Parquet;
using SqlParser;
using SqlParser.Ast;
using Action = System.Action;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class TPCHReadBenchmarks
{
    protected TimeSpan Timeout = TimeSpan.FromMinutes(2);
    
    [ParamsSource(nameof(GetQueries))] public required Statement Query { get; set; }
    
    public IEnumerable<Statement> GetQueries()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TPC-H V3.0.1", "dbgen");
        string queriesFile;
        try
        {
            queriesFile = new StreamReader(path + "/queries.sql").ReadToEnd();
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Couldn't find TPCH data. Please go to https://www.tpc.org/ and ensure that ddl and .tbl exist in Master.Benchmarks/TPC-H V3.0.1/dbgen");
            Console.WriteLine(e);
            return [];
        }

        return new SqlQueryParser().Parse(queriesFile).Where(statement => statement.GetType() != typeof(Statement.Comment))
            .Take(1); //TODO: remove
        
    }

    [GlobalSetup]
    public void Setup()
    {
        if (File.Exists(Config.FilePath))
        {
            File.Delete(Config.FilePath);
        }

        if (Directory.Exists(Config.FilePath))
        {
            Directory.Delete(Config.FilePath, true);
        }

        foreach (IRawBenchmark implementation in GetImplementations())
        {
            foreach (TpchData tpchData in TPCHBenchmarks.GetData())
            {
                Directory.CreateDirectory(Config.FilePath);
                implementation.Write(Path.Combine(Config.FilePath, tpchData.ToString()), tpchData);
            }
        }
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void ReadRaw(IRawBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            implementation?.Read(Config.FilePath, Query);
        }, Timeout);
    }
    
    protected static void RunWithTimeout(Action action, TimeSpan timeout)
    {
        Task task = Task.Run(action);
        if (!task.Wait(timeout))
        {
            throw new TimeoutException();
        }
    }
    
    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new RawParquet(CompressionMethod.Snappy);
        //TODO: add others
    }
}