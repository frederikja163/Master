using Master.Benchmarks.Data;
using OpenTap;

namespace Master.Benchmarks.OpenTAP;

public sealed class ResultStep : TestStep
{
    public required ICustomData Data { get; set; }
    
    public override void Run()
    {
        Results.PublishTable("Results", Data.ColumnNames.ToList(), Data.Columns.ToArray());
    }
}