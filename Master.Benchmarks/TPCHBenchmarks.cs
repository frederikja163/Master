using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Master.Benchmarks.BenchmarkDotnetConfig;
using Master.Benchmarks.Data;
using Master.Benchmarks.Raw;
using Parquet;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class TPCHBenchmarks
{
    protected TimeSpan Timeout = TimeSpan.FromMinutes(2);
    
    [IterationSetup]
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
    }
    
    // Types: System.Int32, System.String, System.Decimal, System.Char, System.DateTime
    [ParamsSource(nameof(GetData))] public required TpchData Data { get; set; }

    public static IEnumerable<TpchData> GetData()
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
        foreach (string table in ddlFile.Substring(ddlFile.IndexOf('\n')).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            /*
             * CREATE TABLE XXX  ( ColumnName  Type,
                            ColumnName       Type,
                            ColumnName  Type NOT NULL,
                            ColumnName    Type);
             */
            int startIndex = table.IndexOf("TABLE", StringComparison.Ordinal) + 6;
            int length = table.IndexOf('(') - table.IndexOf("TABLE", StringComparison.Ordinal) - 6;
            string tableName = table.Substring(startIndex, length).Trim();
            //Console.WriteLine(tableName);
            startIndex = table.IndexOf("(", StringComparison.Ordinal) + 1;
            length = table.LastIndexOf(')') - table.IndexOf("(", StringComparison.Ordinal) - 1;
            string tableColumns = table.Substring(startIndex, length);
            //Console.WriteLine(tableColumns);
            List<(string columnName, Type type)> columns = [];
            foreach (string tableColumn in tableColumns.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string[] values = tableColumn.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var column = (columnName: values[0], type: StringToType(values[1]));
                columns.Add(column);
            }
            yield return new TpchData(columns, tableName, $"{path}/{tableName}.tbl");
        }
    }

    private static Type StringToType(string stringName)
    {
        // TODO: Consider fixing types - Check dss.ddl 
        return stringName switch
        {
            "INTEGER" => typeof(int),
            "CHAR(1)" => typeof(string), // typeof(char)
            "DATE" => typeof(string), // typeof(DateTime)
            var decimalValue when Regex.IsMatch(decimalValue, "DECIMAL*") => typeof(float), // typeof(decimal)
            var stringValue when Regex.IsMatch(stringValue, "(CHAR|VARCHAR)*") => typeof(string),
            _ => typeof(object)
        };
    }
    
    protected static void RunWithTimeout(Action action, TimeSpan timeout)
    {
        Task task = Task.Run(action);
        if (!task.Wait(timeout))
        {
            throw new TimeoutException();
        }
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetImplementations))]
    public void WriteRaw(IRawBenchmark implementation)
    {
        RunWithTimeout(() =>
        {
            implementation?.Write(Config.FilePath, Data);
        }, Timeout);
    }

    public IEnumerable<IRawBenchmark> GetImplementations()
    {
        yield return new RawBinaryStream();
        yield return new EncodingBenchmark();
        yield return new CascadingBenchmark();
        yield return new CascadingAsyncBenchmark();
        yield return new RawParquet(CompressionMethod.Snappy);
        yield return new RawParquet(CompressionMethod.Zstd);
        yield return new RawParquet(CompressionMethod.Gzip);
        yield return new RawParquet(CompressionMethod.None);
        yield return new RawParquet(CompressionMethod.LZ4);
        yield return new RawParquet(CompressionMethod.Lz4Raw);
        yield return new RawParquet(CompressionMethod.Brotli);
        yield return new RawCsv();
        yield return new RawSqlite();
        yield return new RawHdf5Benchmark();
    }
}