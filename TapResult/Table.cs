using System.Diagnostics;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult;

public sealed class Table : IColumnParent
{
    private readonly IColumn[] _columns;
    public int ColumnCount => _columns.Length;
    internal IEnumerable<IColumn> Columns => _columns;
    private readonly string[] _names;
    public IEnumerable<string> Names => _names;
    private string Name { get; }


    public EncodingType EncodingType => EncodingType.Table;
    public LogicalType LogicalType => LogicalType.UInt8;
    public int LogicalLength { get; }

    public IEnumerable<IColumn> GetChildColumns()
    {
        foreach (IColumn column in _columns)
        {
            yield return column;
        }
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            IColumn column = _columns[i];
            if (!existingColumn.Equals(column)) 
                continue;
            _columns[i] = newColumn;
            return true;
        }

        return false;
    }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        blobBuilder.WriteValue(Name);
        foreach (string name in _names)
        {
            blobBuilder.WriteValue(name);
        }
    }

    IColumnReader IColumn.OpenReader()
    {
        throw new Exception("Cannot open a reader for tables.");
    }

    public void Compress(Encoder? encoder = null)
    {
        encoder ??= Encoder.Default;
        foreach (DataColumn column in this.GetChildColumnsRecursive().OfType<DataColumn>())
        {
            IColumn encodedColumn = encoder.Encode(column);
            this.SwapRecursive(column, encodedColumn);
        }
    }

    public async Task CompressAsync(Encoder? encoder = null)
    {
        encoder ??= Encoder.Default;
        List<Task> tasks = new();
        foreach (DataColumn column in this.GetChildColumnsRecursive().OfType<DataColumn>())
        {
            tasks.Add(Task.Run(() =>
            {
                IColumn encodedColumn = encoder.Encode(column);
                this.SwapRecursive(column, encodedColumn);
            }));
        }

        await Task.WhenAll(tasks);
    }

    public Table(IEnumerable<IColumn> columns, IEnumerable<string> names, string name)
    {
        _columns = columns.ToArray();
        _names = names.ToArray();
        Name = name;
        Debug.Assert(_columns.Any());
        Debug.Assert(_columns.Count() == _names.Count());
        LogicalLength = _columns.First().LogicalLength;
        Debug.Assert(_columns.All(c => c.LogicalLength == LogicalLength));
    }
}