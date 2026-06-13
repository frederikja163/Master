using BenchmarkDotNet.Attributes;
using OpenTap;
using OpenTap.Plugins.Parquet;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Benchmarks.Raw;
using TapResult.Extensions;
using TapResult.OpenTAP;
using TapResult.Readers;

namespace TapResult.Benchmarks;

[MemoryDiagnoser]
public class ReadBenchmarks
{
    [GlobalSetup]
    public static void SetupTemp() => Setup(Path.GetTempPath());

    public static void Setup(string path)
    {
        // var data = TPCHBenchmarks.GetData().ToArray();
        // var benchmarks = new TPCHBenchmarks();
        // Config.FilePath = Path.Combine(path,  "Results.Parquet");
        // benchmarks.WriteRaw(new TPCHBenchmarks.Implementation(new RawParquet(CompressionMethod.Snappy), data));
        // Config.FilePath = Path.Combine(path, "Results");
        // benchmarks.WriteRaw(new TPCHBenchmarks.Implementation(
        //     new AsyncTapResultBenchmark(s =>
        //         new TapDataWriter(File.Create(s + ".TapData"), File.Create(s + ".TapSchema"))), data));
        // benchmarks.WriteRaw(new TPCHBenchmarks.Implementation(
        //     new AsyncTapResultBenchmark(s =>
        //         new TapResultWriter(s + ".TapResult")), data));

        var benchmarks = new OpenTAPBenchmarks() { Data = AllBenchmarks.GetData().Last() };
        benchmarks.WriteOpenTAP(new TapResultListener()
        {
            FilePath = new MacroString() { Text = Path.Combine(path, "Results.TapResult") }
        });
        benchmarks.WriteOpenTAP(new TapResultListener()
        {
            FilePath = new MacroString() { Text = Path.Combine(path, "Results") },
            WriterCreator = s => new TapDataWriter(File.Create(s + ".TapData"), File.Create(s + ".TapSchema"))
        });
        benchmarks.WriteOpenTAP(new ParquetResultListener()
        {
            FilePath = new MacroString() { Text = Path.Combine(path, "Results.Parquet") }
        });
    }

    [Benchmark]
    public object? ReadSingleTapData()
    {
        using TapDataReader tapResultReader = new TapDataReader(Encoder.Default,
            File.OpenRead(Path.Combine(Path.GetTempPath(), "Results.TapData")),
            File.OpenRead(Path.Combine(Path.GetTempPath(), "Results.TapSchema")));
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleTapResult()
    {
        using TapResultReader tapResultReader =
            await TapResultReader.CreateReaderAsync(
                File.OpenRead(Path.Combine(Path.GetTempPath(), "Results.TapResult")), leaveOpen: false);
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleParquet()
    {
        using ParquetReader reader = await ParquetReader.CreateAsync(Path.Combine(Path.GetTempPath(), "Results.Parquet"));
        int rowgroup = Random.Shared.Next(0, reader.RowGroupCount);
        DataField field = reader.Schema.DataFields.PickRandom();
        using ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(rowgroup);
        DataColumn column = await groupReader.ReadColumnAsync(field);
        return column.Data.Cast<object>().PickRandom();
    }
}