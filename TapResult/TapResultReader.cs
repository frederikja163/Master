using System.Buffers.Binary;
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
    private readonly Dictionary<string, List<TableInfo>> _tables;
    private readonly Encoder _encoder;

    protected ReaderBase(Encoder encoder)
    {
        _tables = new Dictionary<string, List<TableInfo>>();
        _encoder = encoder;
    }

    protected void AddEncodings(ReadOnlyMemory<byte> schema, int logicalLength)
    {
        var encodingsById = GetEncodings(schema, logicalLength);

        var tableEncodings = PopulateSubEncodings(encodingsById);

        CreateTableEncodings(tableEncodings);
    }

    private void CreateTableEncodings(List<EncodingInfo> tableEncodings)
    {
        foreach (EncodingInfo encodingInfo in tableEncodings)
        {
            TableInfo tableInfo = new TableInfo(encodingInfo);
            if (!_tables.TryGetValue(tableInfo.Name, out List<TableInfo>? tables))
            {
                tables = new List<TableInfo>();
                _tables[tableInfo.Name] = tables;
            }

            tables.Add(tableInfo);
        }
    }

    private static List<EncodingInfo> PopulateSubEncodings(Dictionary<int, EncodingInfo> encodingsById)
    {
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

        return tableEncodings;
    }

    private static Dictionary<int, EncodingInfo> GetEncodings(ReadOnlyMemory<byte> schema, int logicalLength)
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

        return encodingsById;
    }

    /// <summary>
    /// Gets all tables that are part of this file.
    /// </summary>
    public IEnumerable<TableInfo> GetTables()
    {
        return _tables.SelectMany(t => t.Value);
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
public class TapResultReader : ReaderBase, IDisposable, IAsyncDisposable
{
    private readonly bool _leaveOpen;
    private readonly Stream _stream;
    
    private TapResultReader(Encoder encoder, Stream stream, bool leaveOpen) : base(encoder)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Creates a new reader asynchronously.
    /// </summary>
    public static async Task<TapResultReader> CreateReaderAsync(Stream stream, Encoder? encoder = null, bool leaveOpen = true)
    {
        TapResultReader reader = new TapResultReader(encoder ?? Encoder.Default, stream, leaveOpen);
        
        stream.Seek(-Bootstrap.PostfixSize, SeekOrigin.End);
        Span<byte> postfix = stackalloc byte[Bootstrap.PostfixSize];
        stream.ReadExactly(postfix);
        Bootstrap.ParsePostfix(postfix, out long start, out long length, out long logicalLength, out ulong magicNumber);
        
        if (!Bootstrap.TryParseMagicNumber(magicNumber, out FileType type, out byte major, out _, out _) ||
            type != FileType.TapResult || major != 1)
        {
            throw new Exception("Magic number is either malformed or this file is of another type.");
        }
        
        stream.Seek(start, SeekOrigin.Begin);
        byte[] schema = new byte[length];
        await stream.ReadExactlyAsync(schema);
        
        reader.AddEncodings(schema, (int)logicalLength);
        return reader;
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