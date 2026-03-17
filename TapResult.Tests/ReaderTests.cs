using Master;
using Master.Columns;
using Master.Encodings;
using Master.Readers;

namespace TapResult.Tests;

internal sealed class MyColumn : IColumnParent
{
    public required byte[] Blob { get; set; }
    public required List<IColumn> Columns { get; set; }

    public required EncodingId EncodingId { get; set; } = EncodingId.Binary;
    public required LogicalType LogicalType { get; set; } = LogicalType.UInt8;
    public IEnumerable<DataColumn> GetDataColumns()
    {
        yield break;
    }
    public int CalculateTotalLength()
    {
        return 0;
    }

    public void WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        blobBuilder.WriteBlob(Blob);
    }

    public IEnumerable<IColumn> GetChildColumns(bool recursive = false)
    {
        return Columns;
    }

    public void Swap(in IColumn existingColumn, in IColumn newColumn)
    {
        int index = Columns.IndexOf(existingColumn);
        if (index != -1)
        {
            Columns[index] = newColumn;
        }
        else
        {
            Columns.Add(newColumn);
        }
    }
}

public sealed class ReaderTests
{
    [Test]
    public async Task MetadataRoundtripTest()
    {
        MyColumn column1 = new MyColumn()
        {
            Blob = [0, 1, 2, 3],
            EncodingId = EncodingId.BitPacking,
            LogicalType = LogicalType.UInt8,
            Columns =
            [
                new MyColumn()
                {
                    Blob = [],
                    EncodingId = EncodingId.Binary,
                    LogicalType = LogicalType.UInt8,
                    Columns = [],
                }
            ],
        };
        MyColumn column2 = new MyColumn()
        {
            Blob = [],
            EncodingId = EncodingId.Binary,
            LogicalType = LogicalType.Blob,
            Columns = [],
        };
        using MemoryStream stream = new MemoryStream();
        TableWriter writer = new TableWriter(stream, true);
        Table tab1 = new Table([column1, column2], ["col1", "col2"], "table1");
        writer.Write(tab1);
        Table tab2 = new Table([column2], ["col3"], "table2");
        writer.Write(tab2);
        writer.Dispose();

        stream.Seek(0, SeekOrigin.Begin);
        Reader reader = await Reader.CreateReaderAsync(stream);
        Assert.That(reader.GetTables().Count(), Is.EqualTo(2));
        
        Assert.That(reader.TryGetTable("table1", out TableInfo? table1), Is.True);
        Assert.That(table1!.GetColumns().Count(), Is.EqualTo(2));
        
        Assert.That(table1.TryGetColumn("col1", out ColumnInfo? col1), Is.True);
        EncodingInfo enc1 = col1!.Encoding;
        Assert.That(enc1.Blob.ToArray(), Is.EqualTo(new byte[]{0, 1, 2, 3}));
        Assert.That(enc1.Encoding, Is.EqualTo(EncodingId.BitPacking));
        Assert.That(enc1.Type, Is.EqualTo(LogicalType.UInt8));
        Assert.That(enc1.GetSubEncodings().Count(), Is.EqualTo(1));
        
        EncodingInfo subCol = enc1.GetSubEncodings().First();
        Assert.That(subCol.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(subCol.Encoding, Is.EqualTo(EncodingId.Binary));
        Assert.That(subCol.Type, Is.EqualTo(LogicalType.UInt8));
        Assert.That(subCol.GetSubEncodings().Count(), Is.EqualTo(0));
        
        Assert.That(table1.TryGetColumn("col2", out ColumnInfo? col2), Is.True);
        EncodingInfo enc2 = col2!.Encoding;
        Assert.That(enc2.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(enc2.Encoding, Is.EqualTo(EncodingId.Binary));
        Assert.That(enc2.Type, Is.EqualTo(LogicalType.Blob));
        Assert.That(enc2.GetSubEncodings().Count(), Is.EqualTo(0));
        
        
        Assert.That(reader.TryGetTable("table2", out TableInfo? table2), Is.True);
        
        Assert.That(table2!.TryGetColumn("col3", out ColumnInfo? col3), Is.True);
        EncodingInfo enc3 = col3!.Encoding;
        Assert.That(enc3.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(enc3.Encoding, Is.EqualTo(EncodingId.Binary));
        Assert.That(enc3.Type, Is.EqualTo(LogicalType.Blob));
        Assert.That(enc3.GetSubEncodings().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task IntegerRoundtripTest()
    {
        int[] data = Enumerable.Range(0, 1000).Select(t => Random.Shared.Next(0, 255)).ToArray();
        Table table = new Table([DataColumn.Create(data)], ["integers"], "table");
        using Stream stream = new MemoryStream();
        using (TableWriter writer = new TableWriter(stream, leaveOpen: true))
        {
            writer.Write(table);
        }

        stream.Seek(0, SeekOrigin.Begin);
        Reader reader = await Reader.CreateReaderAsync(stream);
        Assert.That(reader.TryGetTable("table", out var tableInfo), Is.True);
        Assert.That(tableInfo!.TryGetColumn("integers", out var columnInfo), Is.True);
        IColumnReader<int> colReader = reader.OpenColumnReader<int>(columnInfo!);
        Assert.That(colReader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
}