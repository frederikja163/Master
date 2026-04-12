using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Parquet;
using TapResult.Benchmarks.BenchmarkDotnetConfig;
using TapResult.Benchmarks.Data;
using TapResult.Benchmarks.Raw;

namespace TapResult.Benchmarks;

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
            Console.WriteLine("Couldn't find TPCH data. Please go to https://www.tpc.org/ and ensure that ddl and .tbl exist in TapResult.Benchmarks/TPC-H V3.0.1/dbgen");
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

            if (tableName != "LINEITEM")
            {
                yield return new TpchData(columns, tableName, $"{path}/{tableName}.tbl");
            }
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
    public void WriteRaw(Implementation implementation)
    {
        (IRawBenchmark impl, TpchData[] data) = implementation;
        RunWithTimeout(() =>
        {
            impl.Open(Config.FilePath);
            foreach (TpchData tpchData in data)
            {
                impl?.Write(tpchData);
            }
            impl?.Close();
        }, Timeout);
    }

    public record Implementation(IRawBenchmark Impl, TpchData[] Data)
    {
        public override string ToString()
        {
            return Impl?.ToString() ?? "";
        }
    }

    public static IEnumerable<Implementation> GetImplementations()
    {
        TpchData[] data = GetData().ToArray();
        // TpchData[] data = [GetData().First()];
        yield return new Implementation(new RawBinaryStream(), data);
        yield return new Implementation(new EncodingBenchmark(), data);
        yield return new Implementation(new CascadingBenchmark(), data);
        yield return new Implementation(new CascadingAsyncBenchmark(), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.Snappy), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.Zstd), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.Gzip), data);
        yield return new Implementation(new RawParquet(CompressionMethod.None), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.LZ4), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.Lz4Raw), data);
        // yield return new Implementation(new RawParquet(CompressionMethod.Brotli), data);
        yield return new Implementation(new RawCsv(), data);
        yield return new Implementation(new RawSqlite(), data);
        yield return new Implementation(new RawHdf5Benchmark(), data);
    }
}