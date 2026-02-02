using System.Runtime.InteropServices;

namespace Master.Serializing;

internal sealed class MetadataColumn
{
    public MetadataColumn(DataColumn data)
    {
        Id = EncodingId.Binary;
        Metadata = data;
        Columns = [];
    }

    public MetadataColumn(EncodingId id, DataColumn metadata, MetadataColumn[] childColumns)
    {
        Id = id;
        Metadata = metadata;
        Columns = childColumns;
    }

    public EncodingId Id { get; }
    public DataColumn Metadata { get; }
    public MetadataColumn[] Columns { get; }

    public int CalculateTotalLength()
    {
        if (Id == EncodingId.Binary)
        {
            return Metadata.LogicalLength;
        }

        int length = 0;
        foreach (MetadataColumn column in Columns)
        {
            length += column.CalculateTotalLength();
        }
        return length;
    }

    public IEnumerable<DataColumn> GetDataColumns()
    {
        if (Id == EncodingId.Binary)
        {
            yield return Metadata;
        }
        
        foreach (MetadataColumn column in Columns)
        {
            foreach (DataColumn data in column.GetDataColumns())
            {
                yield return data;
            }
        }
    }
}