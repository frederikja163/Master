using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.CodeAnalysis;
using TapResult.Benchmarks.BenchmarkDotnetConfig;
using OpenTap;
using OpenTap.Plugins.Parquet;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Extensions;
using TapResult.OpenTAP;
using TapResult.Readers;

namespace TapResult.Benchmarks;

public class CustomDiagnoser : IDiagnoser
{
    private readonly Dictionary<BenchmarkCase, long> _stats = new();

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
    {
    }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
    {
        long stats = Server.GetFileStatsAsync(Server.DefaultUrl).GetAwaiter().GetResult();
        yield return new Metric(new AvgBytesPerOpMetric(), stats);
    }

    public void DisplayResults(ILogger logger)
    {
    }

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public IEnumerable<string> Ids => ["CustomDiagnoser"];
    public IEnumerable<IExporter> Exporters { get; } = [];
    public IEnumerable<IAnalyser> Analysers { get; } = [];
}

internal sealed class AvgBytesPerOpMetric : IMetricDescriptor
{
    public bool GetIsAvailable(Metric metric) => metric.Value > 0;

    public string Id { get; } = nameof(AvgBytesPerOpMetric);
    public string DisplayName { get; } = "Bytes sent";
    public string Legend { get; } = "Bytes sent per read.";
    public string NumberFormat { get; } = "##,##0.0";
    public UnitType UnitType { get; } = UnitType.Size;
    public string Unit { get; } = "B";
    public bool TheGreaterTheBetter { get; } = false;
    public int PriorityInCategory { get; } = 0;
}

public class CustomBenchmarkConfig : ManualConfig
{
    public CustomBenchmarkConfig()
    {
        AddDiagnoser(new CustomDiagnoser());
        AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()));
    }
}

[Config(typeof(CustomBenchmarkConfig))]
public class HttpReadBenchmarks
{
    private const string ServerUrl = Server.DefaultUrl;

    [GlobalSetup]
    public async Task Setup() => await Server.ResetStatsAsync(ServerUrl);

    [IterationSetup]
    public void SetupIter() => Server.IncrementFileAsync(ServerUrl).GetAwaiter().GetResult();
    
    [Benchmark]
    public async Task<object?> ReadSingleTapData()
    {
        byte[] tapDataBytes = await Server.ReadFileAsync(ServerUrl, "Results.TapData");
        byte[] tapSchemaBytes = await Server.ReadFileAsync(ServerUrl, "Results.TapSchema");
        
        await using TapDataReader tapResultReader = new TapDataReader(Encoder.Default,
            new MemoryStream(tapDataBytes), new MemoryStream(tapSchemaBytes), leaveOpen: false);
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleTapResult()
    {
        byte[] tapResultBytes = await Server.ReadFileAsync(ServerUrl, "Results.TapResult");

        using TapResultReader tapResultReader = await TapResultReader.CreateReaderAsync(new MemoryStream(tapResultBytes), leaveOpen: false);
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }
    
    [Benchmark]
    public async Task<object?> ReadSingleHttpTapData()
    {
        await using TapDataHttpReader tapResultReader = new TapDataHttpReader(Encoder.Default,
            ServerUrl, "Results.TapData", "Results.TapSchema");
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }

    [Benchmark]
    public async Task<object?> ReadSingleHttpTapResult()
    {
        using TapResultHttpReader tapResultReader =
            await TapResultHttpReader.CreateReaderAsync(ServerUrl, "Results.TapResult", leaveOpen: false);
        TableInfo table = tapResultReader.GetTables().PickRandom();
        ColumnInfo column = table.GetColumns().PickRandom();
        IColumnReader colReader = tapResultReader.OpenColumnReader(column);
        int index = Random.Shared.Next(0, colReader.Length);
        return colReader.Peek(index);
    }


    [Benchmark]
    public async Task<object?> ReadSingleParquet()
    {
        byte[] parquetBytes = await Server.ReadFileAsync(ServerUrl, "Results.Parquet");

        using ParquetReader reader = await ParquetReader.CreateAsync(new MemoryStream(parquetBytes));
        int rowgroup = Random.Shared.Next(0, reader.RowGroupCount);
        DataField field = reader.Schema.DataFields.PickRandom();
        using ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(rowgroup);
        DataColumn column = await groupReader.ReadColumnAsync(field);
        return column.Data.Cast<object>().PickRandom();
    }
}
