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
    private ColumnBuilder _idBuilder = new (LogicalType.SInt32, 200, true);
    private ColumnBuilder _parentIdBuilder = new (LogicalType.SInt32, 200, true);
    private ColumnBuilder _encodingIdBuilder = new (LogicalType.UInt8, 200, true);
    private ColumnBuilder _logicalTypeBuilder = new(LogicalType.UInt8, 200, true);
    private ColumnBuilder _blobBuilder = new (LogicalType.Blob, 200, true);
    
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

        DataColumn idColumn = _idBuilder.Build();
        idColumn.Write(_outStream);
        _parentIdBuilder.Build().Write(_outStream);
        _encodingIdBuilder.Build().Write(_outStream);
        _logicalTypeBuilder.Build().Write(_outStream);
        _blobBuilder.Build() .Write(_outStream);
        
        // Postscript
        long metadataLength = _outStream.Position - metadataStart;
        long metadataLogicalLength = idColumn.LogicalLength;
        
        ColumnBuilder postScript = new(LogicalType.UInt64, 24);
        postScript.Write(metadataStart);
        postScript.Write(metadataLength);
        postScript.Write(metadataLogicalLength);
        
        _outStream.Write(postScript.Build().Data.Span);
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
        _idBuilder.Write(id);
        _parentIdBuilder.Write(parentId);
        _encodingIdBuilder.Write((byte) column.EncodingType);
        _logicalTypeBuilder.Write((byte) column.LogicalType);
        column.WriteMetadata(_blobBuilder);
    }
    
    internal Table GetMetadata()
    {
        return new Table([_idBuilder.Build(), _parentIdBuilder.Build(), _encodingIdBuilder.Build(), _logicalTypeBuilder.Build(), _blobBuilder.Build()],
            ["Id", "ParentId", "Encoding", "LogicalType", "Blob"],
            "schema");
    }
}

