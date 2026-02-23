using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Master.Benchmarks.Data;

public class TpchData : ICustomData
{
    public TpchData(List<(string columnName, Type type)> columns, string tableName, string path)
    {
        _tableName = tableName;
        _columnNames = columns.Select(c => c.columnName).ToList();
        _columnTypes = columns.Select(c => c.type).ToArray();
        var tempColumns = Enumerable.Range(0,_columnTypes.Length).Select(_ => new List<object>()).ToArray();
        foreach (string line in File.ReadLines(path))
        { 
            var values = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); 
            for (int i = 0; i < tempColumns.Length; i++)
            {
                tempColumns[i].Add(Parse(values[i], _columnTypes[i]));
            }
        }

        // We can't insert tempColumns directly because the type of the array is lost
        for (var i = 0; i < tempColumns.Length; i++)
        {
            var column = tempColumns[i];
            var newColumn = Array.CreateInstance(_columnTypes[i], column.Count);
            for (var index = 0; index < column.Count; index++)
            {
                var val = column[index];
                newColumn.SetValue(val, index);
            }
            _columns.Add(newColumn);
        }

        Count = _columns[0].Length;
    }

    private readonly string _tableName;
    private List<Array> _columns = [];
    private List<string> _columnNames = [];
    private Type[] _columnTypes;
    
    public IEnumerable<Array> Columns => _columns;
    public IEnumerable<string> ColumnNames => _columnNames;
    
    public int Count { get; }
    public int Repeats { get; } = 1;

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
    
    private static object Parse(string input, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        return converter.ConvertFrom(null, CultureInfo.InvariantCulture, input) ??
               throw new InvalidOperationException();
    }

    public override string ToString()
    {
        return _tableName;
    }
}