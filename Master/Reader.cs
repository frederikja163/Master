using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;
using Master.Serializing.Readers;

namespace Master;

public sealed class TableInfo
{
    private readonly Dictionary<string, ColumnInfo> _columns;
    public string Name { get; }
    public EncodingInfo Encoding { get; }

    internal TableInfo(EncodingInfo encoding)
    {
        Encoding = encoding;
        GenericReader reader = new GenericReader(encoding.Blob.Span);
        Name = reader.ReadString();
        _columns = new Dictionary<string, ColumnInfo>();
        int i = 0;
        foreach (EncodingInfo subEncoding in encoding.GetSubEncodings())
        {
            string name = reader.ReadString();
            ColumnInfo columnInfo = new ColumnInfo(name, subEncoding, this);
            _columns.Add(name, columnInfo);
        }
    }

    public IEnumerable<ColumnInfo> GetColumns()
    {
        return _columns.Values;
    }

    public bool TryGetColumn(string name, [NotNullWhen(true)] out ColumnInfo? columnInfo)
    {
        return _columns.TryGetValue(name, out columnInfo);
    }
}

public sealed class ColumnInfo
{
    public string Name { get; }
    public EncodingInfo Encoding { get; }
    public TableInfo TableInfo { get; }
    
    internal ColumnInfo(string name, EncodingInfo encoding, TableInfo tableInfo)
    {
        Name = name;
        Encoding = encoding;
        TableInfo = tableInfo;
    }
}

public sealed class EncodingInfo
{
    private readonly List<EncodingInfo> _subEncodings = new();
    public int Id { get; }
    public int ParentId { get; }
    public EncodingId Encoding { get; }
    public LogicalType Type { get; }
    public ReadOnlyMemory<byte> Blob { get; }
    public EncodingInfo? ParentEncoding { get; private set; } = null;

    public EncodingInfo(int id, int parentId, EncodingId encoding, LogicalType type, ReadOnlyMemory<byte> blob)
    {
        Id = id;
        ParentId = parentId;
        Encoding = encoding;
        Type = type;
        Blob = blob;
    }

    public IEnumerable<EncodingInfo> GetSubEncodings()
    {
        return _subEncodings;
    }

    internal void AddSubEncoding(EncodingInfo subEncoding)
    {
        _subEncodings.Add(subEncoding);
        subEncoding.ParentEncoding = this;
    }
}

public sealed class Reader
{
    private readonly Stream _stream;
    private readonly Dictionary<string, TableInfo> _tables;
    private readonly ILookup<EncodingId, IEncoding> _encodingsById;
    
    private Reader(Stream stream, IEnumerable<TableInfo> tables, IEnumerable<IEncoding> encodings)
    {
        _stream = stream;
        _tables = tables.ToDictionary(t => t.Name, t => t);
        _encodingsById = encodings.ToLookup(e => e.Id);
    }

    public IEnumerable<TableInfo> GetTables()
    {
        return _tables.Values;
    }

    public bool TryGetTable(string name, [NotNullWhen(true)] out TableInfo? table)
    {
        return _tables.TryGetValue(name, out table);
    }

    public static async Task<Reader> CreateReaderAsync(Stream stream) =>
        await CreateReaderAsync(stream, new BitPacking(), new SplitEncoding());

    public static async Task<Reader> CreateReaderAsync(Stream stream, params IEnumerable<IEncoding> encodings)
    {
        int postfixSize = Unsafe.SizeOf<long>() * 4;
        stream.Seek(-postfixSize, SeekOrigin.End);
        Span<byte> postfix = stackalloc byte[postfixSize];
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

        Dictionary<int, EncodingInfo> encodingsById = new Dictionary<int, EncodingInfo>();
        for (int i = 0; i < logicalLength; i++)
        {
            int blobLength = schemaReader.Read<int>();
            ReadOnlyMemory<byte> blob = new ReadOnlyMemory<byte>(schema, schemaReader.ByteIndex, blobLength);
            schemaReader.Advance(blobLength);
            encodingsById.Add(ids[i], new EncodingInfo(ids[i], parentIds[i], (EncodingId)encodingIds[i], (LogicalType)types[i], blob));
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
        
        return new Reader(stream, tableEncodings.Select(e => new TableInfo(e)), encodings);
    }

    public IColumnReader<T> OpenColumnReader<T>(ColumnInfo column)
    {
        return (OpenColumnReader(column) as IColumnReader<T>) ??
               throw new Exception($"Column {column.Name} cannot be read as {typeof(T).ToLogicalType()}, must be read as {column.Encoding.Type}");
    }

    public IColumnReader OpenColumnReader(ColumnInfo column)
    {
        return CreateReader(column.Encoding);
    }

    private IColumnReader CreateReader(EncodingInfo encodingInfo)
    {
        GenericReader reader = new GenericReader(encodingInfo.Blob.Span);
        if (encodingInfo.Encoding == EncodingId.Binary)
        {
            int physicalSize = reader.Read<int>();
            int logicalLength = reader.Read<int>();
            long offset = reader.Read<long>();
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[physicalSize];
            _stream.ReadExactly(data);
            DataColumn col = new DataColumn(encodingInfo.Type, data, logicalLength);
            return col.OpenReader();
        }
        
        IEnumerable<IColumnReader> childReaders = encodingInfo.GetSubEncodings().Select(CreateReader);
        IEncoding encoding = _encodingsById[encodingInfo.Encoding]
            .FirstOrDefault(e => e.GetSupportedTypes().Any(t => t == encodingInfo.Type)) ??
            throw new NullReferenceException($"Could not find encoding of type {encodingInfo.Id} with logical type {encodingInfo.Type}");
        return encoding.CreateDecoder(encodingInfo.Type, ref reader, childReaders);
    }
}