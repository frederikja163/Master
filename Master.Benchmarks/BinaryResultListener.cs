using OpenTap;

namespace Master.Benchmarks;

internal sealed class BinaryResultListener : ResultListener
{
    public override void OnResultPublished(Guid stepRunId, ResultTable resultTable)
    {
        using Stream stream = File.OpenWrite(Config.FilePath);
        using BinaryWriter writer = new(stream);
        foreach (ResultColumn column in resultTable.Columns)
        {
            Array data = column.Data;
            byte[] result = new byte[data.Length * sizeof(int)];
            Buffer.BlockCopy(data, 0, result, 0, result.Length);
            writer.Write(result);
        }
    }

    public override string ToString()
    {
        return "Binary";
    }
}