using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult;

/// <summary>
/// Provides a common base for tap result readers.
/// Most likely you want to use a class that derives from this class instead of the base class.
/// </summary>
public abstract class ReaderBase
{
    private readonly ILookup<string, TableInfo> _tables;
    private readonly Encoder _encoder;

    protected ReaderBase(Encoder encoder, ReadOnlyMemory<byte> schema, int logicalLength)
    {
        GenericReader schemaReader = new GenericReader(schema);
        ReadOnlySpan<int> ids = schemaReader.Read<int>(logicalLength);
        ReadOnlySpan<int> parentIds = schemaReader.Read<int>(logicalLength);
        ReadOnlySpan<byte> encodingIds = schemaReader.Read<byte>(logicalLength);
        ReadOnlySpan<byte> types = schemaReader.Read<byte>(logicalLength);
        ReadOnlySpan<int> lengths = schemaReader.Read<int>(logicalLength);
        ReadOnlySpan<int> blobLengths = schemaReader.Read<int>(logicalLength);

        Dictionary<int, EncodingInfo> encodingsById = new Dictionary<int, EncodingInfo>();
        for (int i = 0; i < logicalLength; i++)
        {
            int blobLength = blobLengths[i];
            ReadOnlyMemory<byte> blob = schema.Slice(schemaReader.ByteIndex, blobLength);
            schemaReader.Advance(blobLength);
            encodingsById.Add(ids[i], new EncodingInfo(ids[i], parentIds[i], (EncodingType)encodingIds[i], (LogicalType)types[i], lengths[i], blob));
        }
        
        List<EncodingInfo> tableEncodings = new List<EncodingInfo>();
        foreach (EncodingInfo value in encodingsById.Values)
        {
            if (value.ParentId == -1)
            {
                // We need to populate all child columns before we create the TableInfo.
                tableEncodings.Add(value);
                continue;
            }

            if (!encodingsById.TryGetValue(value.ParentId, out EncodingInfo? info))
                throw new Exception($"No parent column (id {value.ParentId}) found for column {value.Id}");
            EncodingInfo parent = info;
            parent.AddSubEncoding(value);
        }
        _tables = tableEncodings.Select(e => new TableInfo(e)).ToLookup(t => t.Name);
        _encoder = encoder;
    }

    /// <summary>
    /// Gets all tables that are part of this file.
    /// </summary>
    public IEnumerable<TableInfo> GetTables()
    {
        foreach (IGrouping<string, TableInfo> table in _tables)
        {
            foreach (TableInfo info in table)
            {
                yield return info;
            }
        }
    }

    /// <summary>
    /// Gets the amount of tables in the file.
    /// </summary>
    public int TableCount => _tables.Count;

    /// <summary>
    /// Tries to get a table by name, returning true and the table if any is found. Otherwise returns false and null.
    /// </summary>
    public bool TryGetTable(string name, [NotNullWhen(true)] out TableInfo? table)
    {
        table = _tables[name].FirstOrDefault();
        return table is not null;
    }

    /// <summary>
    /// Opens a new column reader for a specific column with a type.
    /// Throws an exception if the column type is not the same as T.
    /// </summary>
    public IColumnReader<T> OpenColumnReader<T>(ColumnInfo column)
    {
        return (OpenColumnReader(column) as IColumnReader<T>) ??
               throw new Exception($"Column {column.Name} cannot be read as {typeof(T).ToLogicalType()}, must be read as {column.Encoding.Type}");
    }

    /// <summary>
    /// Opens a new column reader for a specific column with a type.
    /// </summary>
    public IColumnReader OpenColumnReader(ColumnInfo column)
    {
        return CreateReader(column.Encoding);
    }

    /// <summary>
    /// Creates a reader of a specific encoding type.
    /// Subclasses should handle binary encodings depending on how they read the data.
    /// </summary>
    protected virtual IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        GenericReader reader = new GenericReader(encodingInfo.Blob);
        IEnumerable<IColumnReader> childReaders = encodingInfo.GetSubEncodings().Select(CreateReader);
        return _encoder.Decode(encodingInfo.Encoding, encodingInfo.Type, encodingInfo.Length, ref reader, childReaders);
    }
}

/// <summary>
/// Reader for a TapResult file,
/// can open new columns to read and read the metadata to figure out what columns and types exist.
/// </summary>
public sealed class TapResultReader : ReaderBase, IDisposable, IAsyncDisposable
{
    private readonly bool _leaveOpen;
    private readonly Stream _stream;
    
    private TapResultReader(Encoder encoder, ReadOnlyMemory<byte> schema, int length, Stream stream, bool leaveOpen) : base(encoder, schema, length)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Creates a new reader asynchronously.
    /// </summary>
    public static async Task<TapResultReader> CreateReaderAsync(Stream stream, Encoder? encoder = null, bool leaveOpen = true)
    {
        int postfixSize = Unsafe.SizeOf<long>() * 4;
        stream.Seek(-postfixSize, SeekOrigin.End);
        byte[] postfix = new byte[postfixSize];
        stream.ReadExactly(postfix);
        var start = Bootstrap.ParseTapResultPostfix(postfix, out long length, out long logicalLength, out ulong magicNumber);

        if (!Bootstrap.TryParseMagicNumber(magicNumber, out FileType type, out byte major, out _, out _) &&
            type != FileType.TapResult && major != 1)
        {
            throw new Exception("Magic number is either malformed or this file is of another type.");
        }
        
        stream.Seek(start, SeekOrigin.Begin);
        byte[] schema = new byte[length];
        await stream.ReadExactlyAsync(schema);
        
        return new TapResultReader(encoder ?? Encoder.Default, schema, (int)logicalLength, stream, leaveOpen);
    }

    protected override IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        if (encodingInfo.Encoding == EncodingType.Binary)
        {
            GenericReader reader = new GenericReader(encodingInfo.Blob);
            int physicalSize = reader.Read<int>();
            long offset = reader.Read<long>();
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[physicalSize];
            _stream.ReadExactly(data);
            DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
            return col.OpenReader();
        }
        
        return base.CreateReader(encodingInfo);
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _stream.DisposeAsync();
        }
    }
}