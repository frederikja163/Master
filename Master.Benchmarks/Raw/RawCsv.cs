using Master.Benchmarks.Data;

namespace Master.Benchmarks.Raw;

internal sealed class RawCsv : IRawBenchmark
{
    public void Write(string filePath, ICustomData data)
    {
        using Stream stream = File.Create(filePath);
        using StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine(string.Join(",", data.ColumnNames));
        
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array row in data.Rows)
            {
                writer.WriteLine(string.Join(",", row.OfType<object>().Select(o => o.ToString() ?? "")));
            }
        }
    }

    public override string ToString()
    {
        return "CSV";
    }
}