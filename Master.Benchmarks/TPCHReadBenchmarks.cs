using BenchmarkDotNet.Attributes;
using Master.Benchmarks.BenchmarkDotnetConfig;
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
    
    public TPCHReadBenchmarks()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TPC-H V3.0.1", "dbgen");
        
        
    }
    
    
    [ParamsSource(nameof(GetQueries))] public required Sequence<Statement> Query { get; set; }
    
    public IEnumerable<Sequence<Statement>> GetQueries()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TPC-H V3.0.1", "dbgen");
        string ddlFile;
        try
        {
            ddlFile = new StreamReader(path + "/dss.ddl").ReadToEnd();
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Couldn't find TPCH data. Please go to https://www.tpc.org/ and ensure that ddl and .tbl exist in Master.Benchmarks/TPC-H V3.0.1/dbgen");
            Console.WriteLine(e);
            yield break;
        }

        var sqlParser = new SqlQueryParser();
        foreach (var queryPath in Directory.EnumerateFiles(Path.Combine(path, "queries")))
        {
            yield return sqlParser.Parse(new StreamReader(queryPath).ReadToEnd());
        }
        
    }

    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void ReadRaw(IRawBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            Console.WriteLine(Query);
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
    }
}