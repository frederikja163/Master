using TapResult.Columns;

namespace TapResult;

/// <summary>
/// Writer for a TapResult file,
/// encodes columns and writes them out.
/// </summary>
public sealed class Writer : IDisposable, IAsyncDisposable
{
    private readonly Stream _outStream;
    private readonly bool _leaveOpen;
    
    private int _currentId = 0;
    private ColumnBuilder _idBuilder = new (LogicalType.SInt32, 200);
    private ColumnBuilder _parentIdBuilder = new (LogicalType.SInt32, 200);
    private ColumnBuilder _encodingIdBuilder = new (LogicalType.UInt8, 200);
    private ColumnBuilder _logicalTypeBuilder = new(LogicalType.UInt8, 200);
    private ColumnBuilder _blobBuilder = new (LogicalType.Blob, 200);
    
    private const byte MajorVersion = 1;
    private const byte MinorVersion = 0;
    private const byte PatchVersion = 0;
    internal static ReadOnlySpan<byte> MagicNumber =>
    [
        (byte)'O',
        (byte)'T',
        (byte)'A',
        (byte)'P',
        (byte)'R',
        MajorVersion,
        MinorVersion,
        PatchVersion
    ]; // OTAP R100

    /// <summary>
    /// Create a new TableWriter. optionally leaving the stream open.
    /// </summary>
    public Writer(Stream output, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (!output.CanWrite)
            throw new ArgumentException("Stream not writeable");

        _outStream = output;
        _leaveOpen = leaveOpen;
        
        _outStream.Write(MagicNumber);
    }

    public void Dispose()
    {
        // Metadata
        long metadataStart = _outStream.Position; // for Postscript

        DataColumn idColumn = _idBuilder.BuildDataColumn();
        idColumn.Write(_outStream);
        _parentIdBuilder.BuildDataColumn().Write(_outStream);
        _encodingIdBuilder.BuildDataColumn().Write(_outStream);
        _logicalTypeBuilder.BuildDataColumn().Write(_outStream);
        _blobBuilder.BuildDataColumn() .Write(_outStream);
        
        // Postscript
        long metadataLength = _outStream.Position - metadataStart;
        long metadataLogicalLength = idColumn.LogicalLength;
        
        ColumnBuilder postScript = new(LogicalType.UInt64, 24);
        postScript.WriteValue(metadataStart);
        postScript.WriteValue(metadataLength);
        postScript.WriteValue(metadataLogicalLength);
        
        _outStream.Write(postScript.BuildDataColumn().Data.Span);
        _outStream.Write(MagicNumber);
        
        if (_leaveOpen)
            _outStream.Flush();
        else
            _outStream.Close();
    }
    

    public async ValueTask DisposeAsync()
    {
        if (_leaveOpen)
        {
            await _outStream.FlushAsync();
        }
        _outStream.Close();
    }

    /// <summary>
    /// Clears all buffers for this writer and causes any buffered data to be
    /// written to the underlying device.
    /// </summary>
    public void Flush()
    {
        _outStream.Flush();
    }
    
    /// <summary>
    /// Write a table to this writer.
    /// </summary>
    public void Write(Table table)
    {
        foreach (DataColumn dataColumn in table.GetChildColumnsRecursive().OfType<DataColumn>())
        {
            dataColumn.Write(_outStream);
        }

        SaveMetaDataForColumn(table, -1);
    }
    
    internal void SaveMetaDataForColumn(IColumn column, int parentId)
    {
        int id = _currentId++;
        if (column is IColumnParent parent)
        {
            foreach (IColumn childColumn in parent.GetChildColumns()) 
                SaveMetaDataForColumn(childColumn, id);
        }
        _idBuilder.WriteValue(id);
        _parentIdBuilder.WriteValue(parentId);
        _encodingIdBuilder.WriteValue((byte) column.EncodingType);
        _logicalTypeBuilder.WriteValue((byte) column.LogicalType);
        column.WriteMetadata(_blobBuilder);
    }
    
    internal Table GetMetadata()
    {
        return new Table([_idBuilder.BuildDataColumn(), _parentIdBuilder.BuildDataColumn(), _encodingIdBuilder.BuildDataColumn(), _logicalTypeBuilder.BuildDataColumn(), _blobBuilder.BuildDataColumn()],
            ["Id", "ParentId", "Encoding", "LogicalType", "Blob"],
            "schema");
    }
}

