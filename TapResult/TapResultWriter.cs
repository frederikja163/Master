using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult;

/// <summary>
/// Provides a common base for tap result writers.
/// Most likely you want to use a class that derives from this class instead of the base class.
/// </summary>
public abstract class WriterBase
{
    private int _currentId = 0;
    private ColumnBuilder<int> _idBuilder = null!;
    private ColumnBuilder<int> _parentIdBuilder = null!;
    private ColumnBuilder<byte> _encodingIdBuilder = null!;
    private ColumnBuilder<byte> _logicalTypeBuilder = null!;
    private ColumnBuilder<int> _lengthBuilder = null!;
    private ColumnBuilder<byte[]> _blobBuilder = null!;

    protected int Length => _idBuilder.LogicalLength;
    
    protected WriterBase()
    {
        Clear();
    }

    internal Table GetMetadata()
    {
        return new Table([_idBuilder.Build(), _parentIdBuilder.Build(), _encodingIdBuilder.Build(), _logicalTypeBuilder.Build(), _lengthBuilder.Build(), _blobBuilder.Build()],
        ["Id", "ParentId", "Encoding", "LogicalType", "Length", "Blob"],
        "schema");
    }

    protected void Clear()
    {
        _idBuilder = new (200);
        _parentIdBuilder = new (200);
        _encodingIdBuilder = new (200);
        _logicalTypeBuilder = new (200);
        _lengthBuilder = new (200);
        _blobBuilder = new (200);
    }
    
    /// <summary>
    /// Write a table to this writer.
    /// </summary>
    public void Write(Table table)
    {
        Write(table, false);

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
    
    protected virtual void Write(IColumn column, bool isSchema)
    {
        if (column is IColumnParent parent)
        {
            foreach (DataColumn col in parent.GetChildColumnsRecursive().OfType<DataColumn>())
            {
                Write(col, isSchema);
            }
        }

        if (column is DataColumn)
        {
            throw new UnreachableException($"Didn't find an implementation for writing {nameof(DataColumn)}. Most likely you forgot to handle it in {nameof(Write)}");
        }
    }
}

/// <summary>
/// Writer for a TapResult file,
/// encodes columns and writes them out.
/// </summary>
public sealed class TapResultWriter : WriterBase, IDisposable, IAsyncDisposable
{
    private readonly Stream _outStream;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Create a new TapResultWriter from a path.
    /// </summary>
    public TapResultWriter(string path) : this(File.Create(path))
    { }
    
    /// <summary>
    /// Create a new TapResultWriter. optionally leaving the stream open.
    /// </summary>
    public TapResultWriter(Stream output, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (!output.CanWrite)
            throw new ArgumentException("Stream not writeable");

        _outStream = output;
        _leaveOpen = leaveOpen;

        Span<byte> data = stackalloc byte[Unsafe.SizeOf<ulong>()];
        ulong magicNumber = Bootstrap.SerializeMagicNumber(FileType.TapResult, 1, 0, 0);
        BinaryPrimitives.WriteUInt64LittleEndian(data, magicNumber);
        _outStream.Write(data);
    }

    public void Dispose()
    {
        // Metadata
        long metadataStart = _outStream.Position; // for Postscript
        
        Write(GetMetadata(), true);
        
        // Postscript
        long metadataLength = _outStream.Position - metadataStart;
        long metadataLogicalLength = Length;
        
        ulong magicNumber = Bootstrap.SerializeMagicNumber(FileType.TapResult, 1, 0, 0);
        byte[] data = Bootstrap.SerializeTapResultPostfix(metadataStart, metadataLength, metadataLogicalLength, magicNumber);
        _outStream.Write(data);
        
        _outStream.Flush();
        if (_leaveOpen)
            return;
        _outStream.Dispose();
    }
    

    public async ValueTask DisposeAsync()
    {
        await _outStream.FlushAsync();
        if (_leaveOpen)
            return;
        await _outStream.DisposeAsync();
    }

    protected override void Write(IColumn column, bool isSchema)
    {
        if (column is DataColumn dataColumn)
        {
            dataColumn.Write(_outStream);
            return;
        }
        
        base.Write(column, isSchema);
    }
}

