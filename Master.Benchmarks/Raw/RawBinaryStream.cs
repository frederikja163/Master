namespace Master.Benchmarks.Raw;

internal sealed class RawBinaryStream : IRawBenchmark
{
    public void Write(string filePath, Data data)
    {
        using ExtendedBinaryWriter writer = new ExtendedBinaryWriter(filePath);
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                writer.Write(array);
            }
        }
    }

    public override string ToString()
    {
        return "Binary";
    }
}