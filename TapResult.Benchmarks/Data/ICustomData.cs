namespace Master.Benchmarks.Data;

public interface ICustomData
{
    public IEnumerable<string> ColumnNames { get; }
    public IEnumerable<Array> Columns { get; }
    public int Count { get; }
    public int Repeats { get; }
    public IEnumerable<Array> Rows { get; }
}