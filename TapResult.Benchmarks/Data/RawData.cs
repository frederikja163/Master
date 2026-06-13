using TapResult.Benchmarks.Extensions;

namespace TapResult.Benchmarks.Data;

public sealed class RawData : ICustomData
{
    private static readonly string[] NatoAlphabet = [
        "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima",
        "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey",
        "X-ray", "Yankee", "Zebra"
    ];
    private List<Array> _columns = [];
    private List<string> _columnNames = [];
    
    public RawData(int count, int repeats = 1, float sparsity = 1.0f)
    {
        Count = (int)(count / sparsity);
        Repeats = repeats;
        Sparsity = sparsity;
    }
    
    public int Count { get; }
    public int Repeats { get; }
    public float Sparsity { get; }
    public string Name => "My Table";

    public IEnumerable<Array> Columns => _columns;
    public IEnumerable<string> ColumnNames => _columnNames;

    public IEnumerable<Array> Rows { 
        get {
            for (int i = 0; i < Count; i++)
            {
                yield return GetRow(i).ToArray();
            }

            IEnumerable<object?> GetRow(int i)
            {
                for (int j = 0; j < _columns.Count; j++)
                {
                    yield return _columns[j].GetValue(i);
                }
            }
        } 
    }
    
    public RawData PopulateRandomInts(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName($"RandomInt_{i}");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.Next()).WithNullsStruct(Sparsity).ToArray());
        }
        
        return this;
    }
    
    public RawData PopulateOrderedInts(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName($"OrderedInt_{i}");
            _columns.Add(Enumerable.Range(0, Count).WithNullsStruct(Sparsity).ToArray());
        }

        return this;
    }
    
    public RawData PopulateRandomFloats(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName($"RandomFloat_{i}");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextSingle()).WithNullsStruct(Sparsity)
                .ToArray());
        }

        return this;
    }
    
    public RawData PopulateRandomDoubles(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName($"RandomDouble_{i}");
            _columns.Add(Enumerable.Range(0, Count).Select(_ => Random.Shared.NextDouble()).WithNullsStruct(Sparsity)
                .ToArray());
        }

        return this;
    }

    public RawData PopulateRandomGuidStrings(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            UniqueColumnName($"GuidStrings_{i}");
            _columns.Add(
                Enumerable.Range(0, Count).Select(_ => Guid.NewGuid().ToString()).WithNullsClass(Sparsity).ToArray());
        }

        return this;
    }

    public RawData PopulateRandomNatoAlphabetStrings(int columns = 1)
    {
        for (int i = 0; i < columns; i++)
        {
            string[] natoAlphabet = NatoAlphabet
                .Select<string, char[]>(s => s.ToCharArray())
                .ForEach(Random.Shared.Shuffle)
                .Select(ca => new string(ca)).ToArray();

            UniqueColumnName($"NatoAlphabet_{i}");
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
        while (_columnNames.Contains($"{name}_{index}"))
        {
            index += 1;
        }

        _columnNames.Add($"{name}_{index}");
        return $"{name}_{index}";
    }

    public override string ToString()
    {
        return $"{Count*Repeats}";
    }
}