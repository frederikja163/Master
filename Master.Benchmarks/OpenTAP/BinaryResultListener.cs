using Master.Benchmarks.Raw;
using OpenTap;

namespace Master.Benchmarks.OpenTAP;

internal sealed class BinaryResultListener : ResultListener
{
    [Display("File path")]
    public string FilePath { get; set; } = "Results";
     
    private ExtendedBinaryWriter? _writer;

    public override void Close()
    {
        _writer?.Dispose();
        base.Close();
    }

    public override void Open()
    {
        _writer = new ExtendedBinaryWriter(FilePath);
        base.Open();
    }

    public override void OnResultPublished(Guid stepRunId, ResultTable resultTable)
    {
        foreach (ResultColumn column in resultTable.Columns)
        {
            _writer.Write(column.Data);
        }
    }

    public override string ToString()
    {
        return "Binary";
    }
}