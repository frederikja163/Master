using BenchmarkDotNet.Attributes;
using OpenTap;
using OpenTap.Plugins.Parquet;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Extensions;
using TapResult.OpenTAP;
using TapResult.Readers;

namespace TapResult.Benchmarks;

[MemoryDiagnoser]
public class ReadBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        var benchmarks = new OpenTAPBenchmarks() { Data = AllBenchmarks.GetData().Last() };
        benchmarks.WriteOpenTAP(new TapResultListener()
        {
            FilePath = new MacroString(){Text = "Results.TapResult"}
        });
        benchmarks.WriteOpenTAP(new TapResultListener()
        {
            FilePath = new MacroString(){Text = "Results"},
            WriterCreator = s => new TapDataWriter(File.Create(s + ".TapData"), File.Create(s + ".TapSchema"))
        });
        benchmarks.WriteOpenTAP(new ParquetResultListener()
        {
            FilePath = new MacroString(){Text = "Results.Parquet"}
        });
    }

    [Benchmark]
    public async Task<object?> ReadSingleTapData()
    {
        await using TapDataReader tapResultReader = new TapDataReader(Encoder.Default, File.OpenRead("Results.TapData"), File.OpenRead("Results.TapSchema"));
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleTapResult()
    {
        using TapResultReader tapResultReader = await TapResultReader.CreateReaderAsync(File.OpenRead("Results.TapResult"), leaveOpen: false);
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleParquet()
    {
        using ParquetReader reader = await ParquetReader.CreateAsync("Results.Parquet");
        int rowgroup = Random.Shared.Next(0, reader.RowGroupCount);
        DataField field = reader.Schema.DataFields.PickRandom();
        using ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(rowgroup);
        DataColumn column = await groupReader.ReadColumnAsync(field);
        return column.Data.Cast<object>().PickRandom();
    }
}