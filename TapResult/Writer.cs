using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
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
    private ColumnBuilder<int> _idBuilder = new (200);
    private ColumnBuilder<int> _parentIdBuilder = new (200);
    private ColumnBuilder<byte> _encodingIdBuilder = new (200);
    private ColumnBuilder<byte> _logicalTypeBuilder = new (200);
    private ColumnBuilder<int> _lengthBuilder = new (200);
    private ColumnBuilder<byte[]> _blobBuilder = new (200);
    
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

        Write(_idBuilder.Build());
        Write(_parentIdBuilder.Build());
        Write(_encodingIdBuilder.Build());
        Write(_logicalTypeBuilder.Build());
        Write(_lengthBuilder.Build());
        Write(_blobBuilder.Build());
        
        // Postscript
        long metadataLength = _outStream.Position - metadataStart;
        long metadataLogicalLength = _idBuilder.LogicalLength;
        
        Span<byte> data = stackalloc byte[Unsafe.SizeOf<long>()];
        BinaryPrimitives.WriteInt64LittleEndian(data, metadataStart);
        _outStream.Write(data);
        BinaryPrimitives.WriteInt64LittleEndian(data, metadataLength);
        _outStream.Write(data);
        BinaryPrimitives.WriteInt64LittleEndian(data, metadataLogicalLength);
        _outStream.Write(data);
        _outStream.Write(MagicNumber);
        
        if (_leaveOpen)
            _outStream.Flush();
        else
            _outStream.Close();
    }

    private void Write(IColumn column)
    {
        if (column is IColumnParent parent)
        {
            foreach (DataColumn col in parent.GetChildColumnsRecursive().OfType<DataColumn>())
            {
                Write(col);
            }
        }
        else if (column is DataColumn dataColumn)
        {
            Write(dataColumn);
        }
    }

    private void Write(DataColumn column)
    {
        column.Write(_outStream);
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
        Write((IColumn)table);

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
        _lengthBuilder.WriteValue(column.LogicalLength);
        using BlobBuilder blobBuilder = _blobBuilder.OpenBlob();
        column.WriteMetadata(blobBuilder);
    }
    
    internal Table GetMetadata()
    {
        return new Table([_idBuilder.Build(), _parentIdBuilder.Build(), _encodingIdBuilder.Build(), _logicalTypeBuilder.Build(), _lengthBuilder.Build(), _blobBuilder.Build()],
            ["Id", "ParentId", "Encoding", "LogicalType", "Length", "Blob"],
            "schema");
    }
}

