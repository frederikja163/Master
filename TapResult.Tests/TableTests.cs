using System.Runtime.CompilerServices;
using System.Text;
using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests;

public class TableTests
{
    [Test]
    public void SwapColumns()
    {
        Table table = new Table([DataColumn.Empty], ["test"], "table");
        table.Swap(DataColumn.Empty, new DataColumn(LogicalType.UInt8, Array.Empty<byte>(), 0));
        Assert.That(table.Columns, Does.Not.Contain(DataColumn.Empty));
    }
    
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        IColumn[] dataColumns = [
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names, "table");
        Assert.That(table.Columns, Is.EqualTo(dataColumns));
        Assert.That(table.GetChildColumns(), Is.EqualTo(dataColumns));
        Assert.That(table.Names, Is.EqualTo(names));
    }
    
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateEncodedTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        IColumn[] dataColumns = [
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names, "table");
        table.Compress();
        // TODO: Read
        // serializer.Decode(ref table);
        // Assert.That(table.Columns, Is.EqualTo(dataColumns));
        // Assert.That(table.GetDataColumns(), Is.EqualTo(dataColumns));
        // Assert.That(table.Names, Is.EqualTo(names));
    }

    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateMetaDataTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        IColumn[] dataColumns = [
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names, "table");
        // Encoding is skipped
        Writer writer = new Writer(Stream.Null);
        writer.Write(table);

        Table metadata = writer.GetMetadata();
        DataColumn[] metadataColumns = metadata.Columns.OfType<DataColumn>().ToArray();
        DataColumn idColumn = metadataColumns[0];
        DataColumn parentIdColumn = metadataColumns[1];
        DataColumn encodingIdColumn = metadataColumns[2];
        DataColumn logicalTypeColumn = metadataColumns[3];
        DataColumn lengthColumn = metadataColumns[4];
        DataColumn blobColumn = metadataColumns[5];
        
        /* Table should look the following:
         * | Id | ParentId | Encoding | LogicalType | Blob
         * | 1  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 2  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 3  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 0  | -1       | Table    | SInt32      | { (id, name)[] }
         */
        Assert.That(idColumn.OpenReader<int>().Read(4), Is.EqualTo(new [] { 1, 2, 3, 0 }));
        Assert.That(parentIdColumn.OpenReader<int>().Read(4), Is.EqualTo(new [] { 0, 0, 0, -1 }));
        Assert.That(encodingIdColumn.OpenReader<byte>().Read(4), Is.EqualTo( new byte[] { (byte)EncodingType.Binary, (byte)EncodingType.Binary, (byte)EncodingType.Binary, (byte)EncodingType.Table }));
        Assert.That(logicalTypeColumn.OpenReader<byte>().Read(4), Is.EqualTo(new byte[] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        Assert.That(lengthColumn.OpenReader<int>().Read(4), Is.EqualTo(new []{length, length, length, length}));
        
        var blobReader = blobColumn.OpenGenericReader();
        for (int i = 0; i < 3; i++)
        {
            Assert.That(blobReader.Read<int>(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<long>())); // Size of blob
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(blobReader.Read<long>(), Is.EqualTo(0)); // Offset, although the Column is not written out
        }
        Assert.That(blobReader.Read<int>(), Is.EqualTo(42));
        Assert.That(blobReader.ReadString(), Is.EqualTo("table"));
        Assert.That(blobReader.ReadString(), Is.EqualTo("columnA"));
        Assert.That(blobReader.ReadString(), Is.EqualTo("columnB"));
        Assert.That(blobReader.ReadString(), Is.EqualTo("columnC"));
    }

    [Test]
    public void ParentIdTest()
    {
        IColumnParent column = new SplitColumn(
            new BitPackingColumn(
                new BitPackingColumn(
                    new BitPackingColumn(
                        DataColumn.Empty, 0, 0, 0
                        ), 0, 0, 0
                    ), 0, 0, 0
                ), 
            new BitPackingColumn(DataColumn.Empty, 0, 0, 0), 
            LogicalType.Blob
        );
        
        Writer writer = new Writer(Stream.Null);
        writer.SaveMetaDataForColumn(column, -1);

        Table metadata = writer.GetMetadata();
        DataColumn[] metadataColumns = metadata.Columns.OfType<DataColumn>().ToArray();
        DataColumn idColumn = metadataColumns[0];
        DataColumn parentIdColumn = metadataColumns[1];
        DataColumn encodingIdColumn = metadataColumns[2];
        DataColumn logicalTypeColumn = metadataColumns[3];
        DataColumn blobColumn = metadataColumns[4];
        
        Assume.That(idColumn.OpenReader<int>().Read(7).ToArray(), Is.EqualTo(new [] { 4, 3, 2, 1, 6, 5, 0 }));
        Assume.That(encodingIdColumn.OpenReader<byte>().Read(7).ToArray(), Is.EqualTo( new []
        {
            (byte)EncodingType.Binary, (byte)EncodingType.BitPacking, (byte)EncodingType.BitPacking, (byte)EncodingType.BitPacking, (byte)EncodingType.Binary, (byte)EncodingType.BitPacking, (byte)EncodingType.Split
        }));
        
        // Only Tables write their own parentId, therefore this it is intended that the columns are not evenly long in this unittest
        Assert.That(parentIdColumn.OpenReader<int>().Read(6), Is.EqualTo(new [] { 3, 2, 1, 0, 5, 0 }));
    }

    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void WriteTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        IColumn[] dataColumns =
        [
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan()),
            ColumnBuilder.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names, "table");

        Stream stream = new MemoryStream();
        Writer writer = new(stream, leaveOpen: true);
        writer.Write(table);
        writer.Dispose();

        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader reader = new BinaryReader(stream);
        int expectedPosition = 0;
        
        // Magic Number
        Assert.That(reader.ReadBytes(8), Is.EqualTo(Writer.MagicNumber.ToArray()));
        expectedPosition += 8;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        
        // Data
        for (int i = 0; i < 3; i++)
        {
            foreach (var num in data)
            {
                Assert.That(reader.ReadInt32(), Is.EqualTo(num));
            }
        }
        expectedPosition += 12 * length;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));

        // Metadata
        int metadataPos = expectedPosition;
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(reader.ReadInt32(), Is.EqualTo(2));
            Assert.That(reader.ReadInt32(), Is.EqualTo(3));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
        });
        expectedPosition += 16;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(-1));
        });
        expectedPosition += 16;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));

        Assert.That(reader.ReadBytes(4), Is.EqualTo( new [] { (byte)EncodingType.Binary, (byte)EncodingType.Binary, (byte)EncodingType.Binary, (byte)EncodingType.Table }));
        Assert.That(reader.ReadBytes(4), Is.EqualTo( new [] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        expectedPosition += 8;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(length));
            Assert.That(reader.ReadInt32(), Is.EqualTo(length));
            Assert.That(reader.ReadInt32(), Is.EqualTo(length));
            Assert.That(reader.ReadInt32(), Is.EqualTo(length));
        });
        expectedPosition += 16;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));

        for (int i = 0; i < 3; i++)
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<long>())); // Size of blob
            Assert.That(reader.ReadInt32(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(reader.ReadInt64(), Is.Not.EqualTo(0)); // Offset, although the Column is not written out
        }
        expectedPosition += (4 + 4 + 8) * 3;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        
        
        Assert.That(reader.ReadInt32(), Is.EqualTo(42));
        expectedPosition += 4;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        Assert.That(reader.ReadInt32(), Is.EqualTo(5));
        Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(5)), Is.EqualTo("table"));
        Assert.That(reader.ReadInt32(), Is.EqualTo(7));
        Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(7)), Is.EqualTo("columnA"));
        Assert.That(reader.ReadInt32(), Is.EqualTo(7));
        Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(7)), Is.EqualTo("columnB"));
        Assert.That(reader.ReadInt32(), Is.EqualTo(7));
        Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(7)), Is.EqualTo("columnC"));
        expectedPosition += 42;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        int metadataLength = expectedPosition - metadataPos;
        
        // Postscript
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt64(), Is.EqualTo(metadataPos));
            Assert.That(reader.ReadInt64(), Is.EqualTo(metadataLength));
            Assert.That(reader.ReadInt64(), Is.EqualTo(4));
        });
        expectedPosition += 24;
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
        
        // Magic Number
        Assert.That(reader.ReadChars(8), Is.EqualTo(Writer.MagicNumber.ToArray()));
        expectedPosition += 8;
        
        Assert.That(reader.BaseStream.Position, Is.EqualTo(expectedPosition));
    }
}