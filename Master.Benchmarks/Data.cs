namespace Master.Benchmarks;

public sealed class Data
{
    private List<Array> _columns = [];
    private List<string> _columnNames = [];

    public int Count { get; }

    public Data(int count)
    {
        Count = count;
    }

    public IEnumerable<Array> Columns => _columns;
    public IEnumerable<string> ColumnNames => _columnNames;
    
    public Data PopulateRandomInts()
    {
        UniqueColumnName("RandomInt");
        _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.Next()).ToArray());
        return this;
    }
    
    public Data PopulateOrderedInts()
    {
        UniqueColumnName("OrderedInt");
        _columns.Add(Enumerable.Range(0, Count).ToArray());
        return this;
    }
    
    public Data PopulateRandomFloats()
    {
        UniqueColumnName("RandomFloat");
        _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextSingle()).ToArray());
        return this;
    }

    private string UniqueColumnName(string name)
    {
        if (!_columnNames.Contains(name))
        {
            _columnNames.Add(name);
            return name;
        }

        int index = 0;
        while (_columnNames.Contains($"{name} ({index})"))
        {
            index += 1;
        }

        _columnNames.Add($"{name} ({index})");
        return $"{name} ({index})";
    }

    public override string ToString()
    {
        return $"{Count} {string.Join(", ", _columnNames)}";
    }
}