using Master.Benchmarks.Extensions;

namespace Master.Benchmarks;

public sealed class Data
{
    private static readonly string[] NatoAlphabet = [
        "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima",
        "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey",
        "X-ray", "Yankee", "Zebra"
    ];
    private List<Array> _columns = [];
    private List<string> _columnNames = [];

    public int Count { get; }
    public int Repeats { get; }
    public float Sparsity { get; }
    
    public Data(int count, int repeats = 1, float sparsity = 1.0f)
    {
        Count = count;
        Repeats = repeats;
        Sparsity = sparsity;
    }

    public IEnumerable<Array> Columns => _columns;
    public IEnumerable<string> ColumnNames => _columnNames;

    public IEnumerable<IEnumerable<object?>> RowMajor()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return GetRow(i);
        }

        IEnumerable<object?> GetRow(int i)
        {
            for (int j = 0; j < _columns.Count; j++)
            {
                yield return _columns[j].GetValue(i);
            }
        }
    }
    
    public Data PopulateRandomInts(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName("RandomInt");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.Next()).WithNullsStruct(Sparsity).ToArray());
        }
        
        return this;
    }
    
    public Data PopulateOrderedInts(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName("OrderedInt");
            _columns.Add(Enumerable.Range(0, Count).WithNullsStruct(Sparsity).ToArray());
        }

        return this;
    }
    
    public Data PopulateRandomFloats(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName("RandomFloat");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextSingle()).WithNullsStruct(Sparsity)
                .ToArray());
        }

        return this;
    }
    
    public Data PopulateRandomDoubles(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName("RandomDouble");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextDouble()).WithNullsStruct(Sparsity)
                .ToArray());
        }

        return this;
    }

    public Data PopulateRandomGuidStrings(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName("GuidStrings");
            _columns.Add(
                Enumerable.Range(0, Count).Select(_ => Guid.NewGuid().ToString()).WithNullsClass(Sparsity).ToArray());
        }

        return this;
    }

    public Data PopulateRandomNatoAlphabetStrings(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            string[] natoAlphabet = NatoAlphabet
                .Select<string, char[]>(s => s.ToCharArray())
                .ForEach(Random.Shared.Shuffle)
                .Select(ca => new string(ca)).ToArray();

            UniqueColumnName("NatoAlphabet");
            _columns.Add(Random.Shared.GetItems(natoAlphabet, Count).WithNullsClass(Sparsity).ToArray());
        }

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