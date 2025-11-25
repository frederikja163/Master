using Master.Benchmarks.Raw;
using OpenTap;

namespace Master.Benchmarks.OpenTAP;

internal sealed class BinaryResultListener : ResultListener
{
    [Display("File path")]
    public string FilePath { get; set; } = "Results";
    
    public override void OnResultPublished(Guid stepRunId, ResultTable resultTable)
    {
        using ExtendedBinaryWriter writer = new ExtendedBinaryWriter(FilePath);
        foreach (ResultColumn column in resultTable.Columns)
        {
            writer.Write(column.Data);
        }
    }

    public override string ToString()
    {
        return "Binary";
    }
}