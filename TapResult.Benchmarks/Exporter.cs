using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Master.Benchmarks.Extensions;

namespace Master.Benchmarks;

internal sealed class Exporter : ExporterBase
{
    public override void ExportToLog(Summary summary, ILogger logger)
    {
        using FileStream stream = File.Open(Path.Combine(summary.ResultsDirectoryPath, summary.Title + ".csv"), FileMode.Create);
        using StreamWriter writer = new StreamWriter(stream);

        string[] headerRow = summary.Table.FullHeader;
        Dictionary<int, string> parameterNames =
            summary.BenchmarksCases
                .SelectMany(b => b.Parameters.Items.Select((p, index) => (p.Name, index)))
                .TryToDictionary(t => Array.IndexOf(headerRow, t.Name), t => t.Name);
        for (var row = 0; row < summary.Table.FullContentWithHeader.Length; row++)
        {
            string[] rows = summary.Table.FullContentWithHeader[row];
            writer.WriteLine(string.Join(", ", rows.Select((value, column) =>
            {
                if (row != 0 && parameterNames.TryGetValue(column, out string? paramName))
                {
                    object parameter = summary.BenchmarksCases[row - 1].Parameters[paramName];
                    return parameter.ToString();
                }
                return value;
            })));
        }

    }
}