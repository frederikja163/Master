using OpenTap;

namespace Master.Benchmarks;

public sealed class ResultStep : TestStep
{
    public Data Data { get; set; }
    
    public override void Run()
    {
        Results.PublishTable("Results", Data.ColumnNames.ToList(), Data.Columns.ToArray());
    }
}