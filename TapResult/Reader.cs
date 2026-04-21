using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult;

/// <summary>
/// Reader for a TapResult file,
/// can open new columns to read and read the metadata to figure out what columns and types exist.
/// </summary>
public sealed class Reader
{
    private readonly Stream _stream;
    private readonly Dictionary<string, TableInfo> _tables;
    private readonly Encoder _encoder;
    
    private Reader(Stream stream, IEnumerable<TableInfo> tables, Encoder encoder)
    {
        _stream = stream;
        _tables = tables.ToDictionary(t => t.Name, t => t);
        _encoder = encoder;
    }

    /// <summary>
    /// Gets all tables that are part of this file.
    /// </summary>
    public IEnumerable<TableInfo> GetTables()
    {
        return _tables.Values;
    }

    /// <summary>
    /// Tries to get a table by name, returning true and the table if any is found. Otherwise returns false and null.
    /// </summary>
    public bool TryGetTable(string name, [NotNullWhen(true)] out TableInfo? table)
    {
        return _tables.TryGetValue(name, out table);
    }

    /// <summary>
    /// Creates a new reader asynchronously.
    /// </summary>
    public static async Task<Reader> CreateReaderAsync(Stream stream, Encoder? encoder = null)
    {
        int postfixSize = Unsafe.SizeOf<long>() * 4;
        stream.Seek(-postfixSize, SeekOrigin.End);
        byte[] postfix = new byte[postfixSize];
        stream.ReadExactly(postfix);
        GenericReader postfixReader = new GenericReader(postfix);
        long start = postfixReader.Read<long>();
        long length = postfixReader.Read<long>();
        int logicalLength = (int)postfixReader.Read<long>();
        ulong magicNumber = postfixReader.Read<ulong>();
        // TODO: Check magic number;
        
        stream.Seek(start, SeekOrigin.Begin);
        byte[] schema = new byte[length];
        await stream.ReadExactlyAsync(schema);
        GenericReader schemaReader = new GenericReader(schema);
        ReadOnlySpan<int> ids = schemaReader.Read<int>(logicalLength);
        ReadOnlySpan<int> parentIds = schemaReader.Read<int>(logicalLength);
        ReadOnlySpan<byte> encodingIds = schemaReader.Read<byte>(logicalLength);
        ReadOnlySpan<byte> types = schemaReader.Read<byte>(logicalLength);
        ReadOnlySpan<int> lengths = schemaReader.Read<int>(logicalLength);

        Dictionary<int, EncodingInfo> encodingsById = new Dictionary<int, EncodingInfo>();
        for (int i = 0; i < logicalLength; i++)
        {
            int blobLength = schemaReader.Read<int>();
            ReadOnlyMemory<byte> blob = new ReadOnlyMemory<byte>(schema, schemaReader.ByteIndex, blobLength);
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
        
        return new Reader(stream, tableEncodings.Select(e => new TableInfo(e)), encoder ?? Encoder.Default);
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

    private IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        GenericReader reader = new GenericReader(encodingInfo.Blob);
        if (encodingInfo.Encoding == EncodingType.Binary)
        {
            int physicalSize = reader.Read<int>();
            long offset = reader.Read<long>();
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[physicalSize];
            _stream.ReadExactly(data);
            DataColumn col = new DataColumn(encodingInfo.Type, data, encodingInfo.Length);
            return col.OpenReader();
        }
        
        IEnumerable<IColumnReader> childReaders = encodingInfo.GetSubEncodings().Select(CreateReader);
        return _encoder.Decode(encodingInfo.Encoding, encodingInfo.Type, encodingInfo.Length, ref reader, childReaders);
    }
}