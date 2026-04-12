namespace TapResult.Benchmarks.Data;

public interface ICustomData
{
    public IEnumerable<string> ColumnNames { get; }
    public IEnumerable<Array> Columns { get; }
    public int Count { get; }
    public IEnumerable<Array> Rows { get; }
    public string Name { get; }
}