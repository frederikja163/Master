namespace Master.Benchmarks.RawBenchmarks;

internal sealed class RawCsv : IRawBenchmark
{
    public void Write(string filePath, Data data)
    {
        using Stream stream = File.OpenWrite(filePath);
        using StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine(string.Join(",", data.ColumnNames));
        Array[] values = data.Columns.ToArray();
        for (int k = 0; k < data.Repeats; k++)
        {
            for (int i = 0; i < values.Length; i++)
            {
                writer.Write(values[i].GetValue(0));
                for (int j = 1; j < values[i].Length; j++)
                {
                    writer.Write(",");
                    writer.Write(values[i].GetValue(j));
                }
                writer.WriteLine();
            }
        }
    }

    public override string ToString()
    {
        return "CSV";
    }
}