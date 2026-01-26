using System.Runtime.InteropServices;

namespace Master.Serializing;

internal sealed class EncodedColumn
{
    public EncodedColumn(ReadOnlyMemory<byte> data)
    {
        Id = EncodingId.Binary;
        Parameters = data;
        Columns = [];
    }

    public EncodedColumn(EncodingId id, ReadOnlyMemory<byte> parameters, EncodedColumn[] childColumns)
    {
        Id = id;
        Parameters = parameters;
        Columns = childColumns;
    }

    public EncodingId Id { get; }
    public ReadOnlyMemory<byte> Parameters { get; }
    public EncodedColumn[] Columns { get; }

    public int CalculateTotalLength()
    {
        int length = sizeof(EncodingId);
        length += Marshal.SizeOf(Parameters);
        foreach (EncodedColumn column in Columns)
        {
            length += column.CalculateTotalLength();
        }
        return length;
    }

    public IEnumerable<ReadOnlyMemory<byte>> GetDataColumns()
    {
        if (Id == EncodingId.Binary)
        {
            yield return Parameters;
        }
        
        foreach (EncodedColumn column in Columns)
        {
            foreach (ReadOnlyMemory<byte> data in column.GetDataColumns())
            {
                yield return data;
            }
        }
    }
}