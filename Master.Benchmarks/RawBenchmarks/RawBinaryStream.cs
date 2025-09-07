namespace Master.Benchmarks.RawBenchmarks;

internal sealed class RawBinaryStream : IRawBenchmark
{
    public Data Data { get; set; }

    public void Write()
    {
        using Stream stream = File.OpenWrite(Config.FilePath);
        using BinaryWriter writer = new(stream);
        foreach (Array column in Data.Columns)
        {
            byte[] result = new byte[column.Length * sizeof(int)];
            Buffer.BlockCopy(column, 0, result, 0, result.Length);
            writer.Write(result);
        }
    }

    public override string ToString()
    {
        return "Binary";
    }
}