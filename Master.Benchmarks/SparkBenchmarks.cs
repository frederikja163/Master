using BenchmarkDotNet.Attributes;
using Master.Benchmarks.Raw;
using Master.Benchmarks.Spark;
using Microsoft.Spark;
using Microsoft.Spark.Sql;

namespace Master.Benchmarks;

public class SparkBenchmarks : AllBenchmarks
{
    private new readonly TimeSpan Timeout = TimeSpan.FromMinutes(60);
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(SparkBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            implementation?.Write(Config.FilePath, Data);
        }, Timeout);
    }
    
    public IEnumerable<SparkBenchmark> GetImplementations()
    {
        SparkSession spark;
        try
        {
            //https://spark.apache.org/docs/latest/configuration.html#compression-and-serialization
            spark = SparkSession.Builder()
                .Config(new SparkConf(loadDefaults: false)
                    .Set("spark.sql.orc.compression.codec", "none")
                    .Set("spark.ui.enabled", "false")
                    .Set("spark.log.level", "OFF")
                    .Set("spark.driver.maxResultSize", "0")
                    .Set("spark.sql.orc.filterPushdown", "false")
                    .Set("spark.sql.parquet.filterPushdown", "false")
                    .Set("spark.sql.csv.filterPushdown", "false")
                    .Set("spark.sql.json.filterPushdown", "false")
                    .Set("spark.sql.codegen.wholeStage", "false")
                    .Set("log4j2.logger.org.apache.spark.util.ShutdownHookManager", "OFF"))
                .GetOrCreate();
        }
        catch (Exception)
        {
            Console.WriteLine("Couldn't find a running spark instance.");
            yield break;
        }
        yield return new SparkBenchmark("ORC", spark);
        yield return new SparkBenchmark("Csv", spark);
        yield return new SparkBenchmark("Json", spark);
        yield return new SparkBenchmark("Parquet", spark);
    }
}