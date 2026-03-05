using System.Text;
using Master.Serializing.Columns;

namespace Master.Serializing;

// Based on System.IO.BinaryWriter
public sealed class TableWriter : IDisposable, IAsyncDisposable
{
    private readonly Stream _outStream;
    private readonly Encoding _encoding;
    private readonly bool _leaveOpen;
    
    private int _currentId = 0;
    private DataColumnBuilder _idBuilder = new (LogicalType.SInt32, 50, true);
    private DataColumnBuilder _parentIdBuilder = new (LogicalType.SInt32, 50, true);
    private DataColumnBuilder _encodingIdBuilder = new (LogicalType.SInt16, 50, true);
    private DataColumnBuilder _logicalTypeBuilder = new(LogicalType.SInt8, 50, true);
    private DataColumnBuilder _blobBuilder = new (LogicalType.Blob, 50, true);
    private int _columnCount = 0;
    
    private const string Identifier = "OTAP";
    private const int FileVersion = 001;
    internal static readonly string MagicNumber = $"{Identifier}R{FileVersion:D3}"; // OTAP R001

    public TableWriter(Stream output) : this(output, Encoding.UTF8)
    {
    }

    public TableWriter(Stream output, Encoding encoding, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(encoding);

        if (!output.CanWrite)
            throw new ArgumentException("Stream not writeable");

        _outStream = output;
        _encoding = encoding;
        _leaveOpen = leaveOpen;
        
        _outStream.Write(Encoding.UTF8.GetBytes(MagicNumber));
    }

    // Closes this writer and releases any system resources associated with the
    // writer. Following a call to Close, any operations on the writer
    // may raise exceptions.
    public void Close()
    {
        Dispose(true);
    }

    internal void Dispose(bool disposing)
    {
        // Metadata
        long metadataStart = _outStream.Position; // for Postscript
        
        _idBuilder.Build().Write(_outStream);
        _parentIdBuilder.Build().Write(_outStream);
        _encodingIdBuilder.Build().Write(_outStream);
        _logicalTypeBuilder.Build().Write(_outStream);
        _blobBuilder.Build().Write(_outStream);
        
        // Postscript
        long metadataLength = _outStream.Position - metadataStart;
        long metadataLogicalLength = _columnCount;
        
        DataColumnBuilder postScript = new(LogicalType.SInt64, 24);
        postScript.Write(metadataStart);
        postScript.Write(metadataLength);
        postScript.Write(metadataLogicalLength);
        
        _outStream.Write(postScript.Build().Data.Span);
        _outStream.Write(Encoding.UTF8.GetBytes(MagicNumber));
        
        if (!disposing) 
            return;
        if (_leaveOpen)
            _outStream.Flush();
        else
            _outStream.Close();
    }

    public void Dispose()
    {
        Dispose(true);
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (_leaveOpen)
            {
                return new ValueTask(_outStream.FlushAsync());
            }
            _outStream.Close();

            return default;
        }
        catch (Exception exc)
        {
            return ValueTask.FromException(exc);
        }
    }

    // Returns the stream associated with the writer. It flushes all pending
    // writes before returning.
    public Stream BaseStream
    {
        get
        {
            Flush();
            return _outStream;
        }
    }

    // Clears all buffers for this writer and causes any buffered data to be
    // written to the underlying device.
    public void Flush()
    {
        _outStream.Flush();
    }

    public long Seek(int offset, SeekOrigin origin)
    {
        return _outStream.Seek(offset, origin);
    }
    
    public void Write(Table table)
    {
        foreach (DataColumn dataColumn in table.GetDataColumns())
        {
            dataColumn.Write(_outStream);
        }

        _columnCount += table.ColumnCount + 1;
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
        _encodingIdBuilder.Write((byte) column.EncodingId);
        _logicalTypeBuilder.Write((byte) column.LogicalType);
        column.WriteMetadata(ref _blobBuilder);
    }
    
    internal Table GetMetadata()
    {
        return new Table([_idBuilder.Build(), _parentIdBuilder.Build(), _encodingIdBuilder.Build(), _logicalTypeBuilder.Build(), _blobBuilder.Build()], ["Id", "ParentId", "Encoding", "LogicalType", "Blob"]);
    }
}

