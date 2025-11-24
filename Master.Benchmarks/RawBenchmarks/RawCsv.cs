namespace Master.Benchmarks.RawBenchmarks;

internal sealed class RawCsv : IRawBenchmark
{
    public void Write(string filePath, Data data)
    {
        using Stream stream = File.OpenWrite(filePath);
        using StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine(string.Join(",", data.ColumnNames));
        
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (IEnumerable<object> row in data.RowMajor())
            {
                writer.WriteLine(string.Join(",", row));
            }
        }
    }

    public override string ToString()
    {
        return "CSV";
    }
}