using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Master.Benchmarks.BenchmarkDotnetConfig;

internal sealed class FileSizeDiagnoser : IDiagnoser
{
    private readonly Dictionary<BenchmarkCase, long> _fileSizes = new();
    
    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;
    
    public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
    {
        return;
    }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(results.BuildResult.ArtifactsPaths.ExecutablePath);
        string folderPath = dirInfo.Parent!.FullName;
        string filePath = Path.Combine(folderPath, Config.FilePath);
        FileInfo fileInfo = new FileInfo(filePath);
        yield return new Metric(new FileSizeMetric(), fileInfo.Length);
    }

    public void DisplayResults(ILogger logger)
    {
    }

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public IEnumerable<string> Ids { get; } = [nameof(FileSizeDiagnoser)];
    public IEnumerable<IExporter> Exporters { get; } = [];
    public IEnumerable<IAnalyser> Analysers { get; } = [];
}

internal sealed class FileSizeMetric : IMetricDescriptor {
    public bool GetIsAvailable(Metric metric) => metric.Value > 0;

    public string Id { get; } = nameof(FileSizeMetric);
    public string DisplayName { get; } = "File Size";
    public string Legend { get; } = "File size in bytes.";
    public string NumberFormat { get; } = "#000";
    public UnitType UnitType { get; } = UnitType.Size;
    public string Unit { get; } = "B";
    public bool TheGreaterTheBetter { get; } = false;
    public int PriorityInCategory { get; } = 0;
}
