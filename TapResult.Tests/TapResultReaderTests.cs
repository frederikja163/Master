using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests;

internal sealed class MyColumn : IColumnParent
{
    public required byte[] Blob { get; set; }
    public required List<IColumn> Columns { get; set; }

    public required EncodingType EncodingType { get; set; } = EncodingType.Binary;
    public required LogicalType LogicalType { get; set; } = LogicalType.UInt8;
    public int LogicalLength { get; } = 0;

    public IEnumerable<DataColumn> GetDataColumns()
    {
        yield break;
    }
    public int CalculateTotalLength()
    {
        return 0;
    }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        blobBuilder.WriteRaw(Blob);
    }

    public IColumnReader OpenReader()
    {
        throw new NotSupportedException();
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        return Columns;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        int index = Columns.IndexOf(existingColumn);
        if (index != -1)
        {
            Columns[index] = newColumn;
            return true;
        }
        else
        {
            Columns.Add(newColumn);
            return false;
        }
    }
}

public sealed class TapResultReaderTests
{
    [Test]
    public async Task MetadataRoundtripTest()
    {
        MyColumn column1 = new MyColumn()
        {
            Blob = [0, 1, 2, 3],
            EncodingType = EncodingType.BitPacking,
            LogicalType = LogicalType.UInt8,
            Columns =
            [
                new MyColumn()
                {
                    Blob = [],
                    EncodingType = EncodingType.Binary,
                    LogicalType = LogicalType.UInt8,
                    Columns = [],
                }
            ],
        };
        MyColumn column2 = new MyColumn()
        {
            Blob = [],
            EncodingType = EncodingType.Binary,
            LogicalType = LogicalType.Blob,
            Columns = [],
        };
        using MemoryStream stream = new MemoryStream();
        TapResultWriter tapResultWriter = new TapResultWriter(stream, true);
        Table tab1 = new Table([column1, column2], ["col1", "col2"], "table1");
        tapResultWriter.Write(tab1);
        Table tab2 = new Table([column2], ["col3"], "table2");
        tapResultWriter.Write(tab2);
        tapResultWriter.Dispose();

        stream.Seek(0, SeekOrigin.Begin);
        TapResultReader tapResultReader = await TapResultReader.CreateReaderAsync(stream);
        Assert.That(tapResultReader.GetTables().Count(), Is.EqualTo(2));
        
        Assert.That(tapResultReader.TryGetTable("table1", out TableInfo? table1), Is.True);
        Assert.That(table1!.GetColumns().Count(), Is.EqualTo(2));
        
        Assert.That(table1.TryGetColumn("col1", out ColumnInfo? col1), Is.True);
        EncodingInfo enc1 = col1!.Encoding;
        Assert.That(enc1.Blob.ToArray(), Is.EqualTo(new byte[]{0, 1, 2, 3}));
        Assert.That(enc1.Encoding, Is.EqualTo(EncodingType.BitPacking));
        Assert.That(enc1.Type, Is.EqualTo(LogicalType.UInt8));
        Assert.That(enc1.GetSubEncodings().Count(), Is.EqualTo(1));
        
        EncodingInfo subCol = enc1.GetSubEncodings().First();
        Assert.That(subCol.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(subCol.Encoding, Is.EqualTo(EncodingType.Binary));
        Assert.That(subCol.Type, Is.EqualTo(LogicalType.UInt8));
        Assert.That(subCol.GetSubEncodings().Count(), Is.EqualTo(0));
        
        Assert.That(table1.TryGetColumn("col2", out ColumnInfo? col2), Is.True);
        EncodingInfo enc2 = col2!.Encoding;
        Assert.That(enc2.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(enc2.Encoding, Is.EqualTo(EncodingType.Binary));
        Assert.That(enc2.Type, Is.EqualTo(LogicalType.Blob));
        Assert.That(enc2.GetSubEncodings().Count(), Is.EqualTo(0));
        
        
        Assert.That(tapResultReader.TryGetTable("table2", out TableInfo? table2), Is.True);
        
        Assert.That(table2!.TryGetColumn("col3", out ColumnInfo? col3), Is.True);
        EncodingInfo enc3 = col3!.Encoding;
        Assert.That(enc3.Blob.ToArray(), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(enc3.Encoding, Is.EqualTo(EncodingType.Binary));
        Assert.That(enc3.Type, Is.EqualTo(LogicalType.Blob));
        Assert.That(enc3.GetSubEncodings().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task IntegerRoundtripTest()
    {
        int[] data = Enumerable.Range(0, 1000).Select(t => Random.Shared.Next(0, 255)).ToArray();
        Table table = new Table([ColumnBuilder.Create(data)], ["integers"], "table");
        using Stream stream = new MemoryStream();
        using (TapResultWriter tapResultWriter = new TapResultWriter(stream, leaveOpen: true))
        {
            tapResultWriter.Write(table);
        }

        stream.Seek(0, SeekOrigin.Begin);
        TapResultReader tapResultReader = await TapResultReader.CreateReaderAsync(stream);
        Assert.That(tapResultReader.TryGetTable("table", out var tableInfo), Is.True);
        Assert.That(tableInfo!.TryGetColumn("integers", out var columnInfo), Is.True);
        IColumnReader<int> colReader = tapResultReader.OpenColumnReader<int>(columnInfo!);
        Assert.That(colReader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
}