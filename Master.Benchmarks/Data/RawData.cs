using Master.Benchmarks.Data;
using OpenTap;

namespace Master.Benchmarks;

public sealed class RawData : ICustomData
{
    private static readonly string[] NatoAlphabet = [
        "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima",
        "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey",
        "X-ray", "Yankee", "Zebra"
    ];
    private List<Array> _columns = [];
    private List<string> _columnNames = [];
    
    public RawData(int count, int repeats = 1)
    {
        Count = count;
        Repeats = repeats;
    }
    
    public int Count { get; }
    public int Repeats { get; }

    public IEnumerable<Array> Columns => _columns;
    public IEnumerable<string> ColumnNames => _columnNames;

    public IEnumerable<Array> Rows { 
        get {
            for (int i = 0; i < Count; i++)
            {
                yield return GetRow(i).ToArray();
            }

            IEnumerable<object> GetRow(int i)
            {
                for (int j = 0; j < _columns.Count; j++)
                {
                    yield return _columns[j].GetValue(i) ?? throw new IndexOutOfRangeException();
                }
            }
        } 
    }
    
    public RawData PopulateRandomInts()
    {
        UniqueColumnName("RandomInt");
        _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.Next()).ToArray());
        return this;
    }
    
    public RawData PopulateOrderedInts()
    {
        UniqueColumnName("OrderedInt");
        _columns.Add(Enumerable.Range(0, Count).ToArray());
        return this;
    }
    
    public RawData PopulateRandomFloats()
    {
        UniqueColumnName("RandomFloat");
        _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextSingle()).ToArray());
        return this;
    }

    public RawData PopulateRandomGuidStrings()
    {
        UniqueColumnName("GuidStrings");
        _columns.Add(Enumerable.Range(0, Count).Select(_ => Guid.NewGuid().ToString()).ToArray());
        return this;
    }

    public RawData PopulateRandomNatoAlphabetStrings()
    {
        UniqueColumnName("NatoAlphabet");
        _columns.Add(Random.Shared.GetItems(NatoAlphabet, Count));
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
        return $"{Repeats}x({string.Join(", ", _columnNames.Select(s => Count + s))})";
    }
}