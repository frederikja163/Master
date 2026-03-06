using System.Runtime.CompilerServices;
using System.Text;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Tests;

public class TableTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn[] dataColumns = [
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names);
        Assert.That(table.Columns, Is.EqualTo(dataColumns));
        Assert.That(table.GetDataColumns(), Is.EqualTo(dataColumns));
        Assert.That(table.Names, Is.EqualTo(names));
    }
    
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateEncodedTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn[] dataColumns = [
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names);
        Serializer serializer = new();
        serializer.Encode(ref table);
        serializer.Decode(ref table);
        Assert.That(table.Columns, Is.EqualTo(dataColumns));
        Assert.That(table.GetDataColumns(), Is.EqualTo(dataColumns));
        Assert.That(table.Names, Is.EqualTo(names));
    }

    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void CreateMetaDataTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn[] dataColumns = [
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names);
        // Encoding is skipped
        TableWriter tableWriter = new TableWriter(Stream.Null);
        tableWriter.Write(table);

        Table metadata = tableWriter.GetMetadata();
        DataColumn[] metadataColumns = metadata.Columns.OfType<DataColumn>().ToArray();
        DataColumn idColumn = metadataColumns[0];
        DataColumn parentIdColumn = metadataColumns[1];
        DataColumn encodingIdColumn = metadataColumns[2];
        DataColumn logicalTypeColumn = metadataColumns[3];
        DataColumn blobColumn = metadataColumns[4];
        
        /* Table should look the following:
         * | Id | ParentId | Encoding | LogicalType | Blob
         * | 1  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 2  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 3  | 0        | Binary   | SInt32      | { PhysicalSize, LogicalLength }
         * | 0  | -1       | Table    | SInt32      | { (id, name)[] }
         */
        Assert.That(idColumn.OpenReader().Read<int>(4).ToArray(), Is.EqualTo(new [] { 1, 2, 3, 0 }));
        Assert.That(parentIdColumn.OpenReader().Read<int>(4).ToArray(), Is.EqualTo(new [] { 0, 0, 0, -1 }));
        Assert.That(encodingIdColumn.OpenReader().Read<byte>(4).ToArray(), Is.EqualTo( new [] { (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Table }));
        Assert.That(logicalTypeColumn.OpenReader().Read<byte>(4).ToArray(), Is.EqualTo(new [] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        var blobReader = blobColumn.OpenReader();
        for (int i = 0; i < 3; i++)
        {
            Assert.That(blobReader.Read<int>(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>() + Unsafe.SizeOf<long>())); // Size of blob
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length)); // LogicalLength
            Assert.That(blobReader.Read<long>(), Is.EqualTo(0)); // Offset, although the Column is not written out
        }
        // length of names + 2 commas + integer string length = 30
        Assert.That(blobReader.Read<int>(), Is.EqualTo(names.Sum(name => name.Length + 2) + (names.Length / 10 + 1) * names.Length)); 
        Assert.That(Encoding.UTF8.GetString(blobReader.Read<byte>(30).ToArray()), Is.EqualTo("0,columnA,1,columnB,2,columnC,"));
    }

    [Test]
    public void ParentIdTest()
    {
        IColumnParent column = new SplitColumn(
            new BitPackingColumn(
                new BitPackingColumn(
                    new BitPackingColumn(
                        new DataColumn()
                        )
                    )
                ), 
            new BitPackingColumn(new DataColumn()), 
            LogicalType.Blob
        );
        
        TableWriter tableWriter = new TableWriter(Stream.Null);
        tableWriter.SaveMetaDataForColumn(column, -1);

        Table metadata = tableWriter.GetMetadata();
        DataColumn[] metadataColumns = metadata.Columns.OfType<DataColumn>().ToArray();
        DataColumn idColumn = metadataColumns[0];
        DataColumn parentIdColumn = metadataColumns[1];
        DataColumn encodingIdColumn = metadataColumns[2];
        DataColumn logicalTypeColumn = metadataColumns[3];
        DataColumn blobColumn = metadataColumns[4];
        
        Assume.That(idColumn.OpenReader().Read<int>(7).ToArray(), Is.EqualTo(new [] { 4, 3, 2, 1, 6, 5, 0 }));
        Assume.That(encodingIdColumn.OpenReader().Read<byte>(7).ToArray(), Is.EqualTo( new []
        {
            (byte)EncodingId.Binary, (byte)EncodingId.BitPacking, (byte)EncodingId.BitPacking, (byte)EncodingId.BitPacking, (byte)EncodingId.Binary, (byte)EncodingId.BitPacking, (byte)EncodingId.Split
        }));
        
        // Only Tables write their own parentId, therefore this it is intended that the columns are not evenly long in this unittest
        Assert.That(parentIdColumn.OpenReader().Read<int>(6).ToArray(), Is.EqualTo(new [] { 3, 2, 1, 0, 5, 0 }));
    }

    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void WriteTableTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn[] dataColumns =
        [
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names);
        Serializer serializer = new();
        serializer.Encode(ref table);

        Stream stream = new MemoryStream();
        TableWriter writer = new(stream);
        writer.Write(table);
        writer.Dispose(disposing: false);

        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader reader = new BinaryReader(stream);
        
        // Magic Number
        Assert.That(reader.ReadBytes(8), Is.EqualTo(TableWriter.MagicNumber.ToArray()));
        
        // Data
        for (int i = 0; i < 3; i++)
        {
            foreach (var num in data)
            {
                Assert.That(reader.ReadInt32(), Is.EqualTo(num));
            } // 4 (<- notes for calculating size of data. Used for reader.BaseStream.Position assertions)
        } // 4 x 3 = 12 

        Assert.That(reader.BaseStream.Position, Is.EqualTo(12 * length + 8)); // data length + magicnumber
        // Metadata

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(reader.ReadInt32(), Is.EqualTo(2));
            Assert.That(reader.ReadInt32(), Is.EqualTo(3));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
        }); // 12 (<- notes for calculating size of metadata)
        
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(0));
            Assert.That(reader.ReadInt32(), Is.EqualTo(-1));
        }); // 24

        Assert.That(reader.ReadBytes(4), Is.EqualTo( new [] { (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Table }));
        Assert.That(reader.ReadBytes(4), Is.EqualTo( new [] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        // 32
        for (int i = 0; i < 3; i++)
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>() + Unsafe.SizeOf<long>())); // Size of blob
            Assert.That(reader.ReadInt32(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(reader.ReadInt32(), Is.EqualTo(length)); // LogicalLength
            Assert.That(reader.ReadInt64(), Is.EqualTo(0)); // Offset, although the Column is not written out
            // 20
        } // 92
        // length of names + 2 commas + integer string length = 30
        Assert.That(reader.ReadInt32(), Is.EqualTo(names.Sum(name => name.Length + 2) + (names.Length / 10 + 1) * names.Length)); 
        Assert.That(Encoding.UTF8.GetString(reader.ReadBytes(30).ToArray()), Is.EqualTo("0,columnA,1,columnB,2,columnC,"));
        // 134
        Assert.That(reader.BaseStream.Position, Is.EqualTo(4 * 3 * length + 8 + 134)); // data length + magicnumber + metadata length
        
        // Postscript
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt64(), Is.EqualTo(4 * 3 * length + 8));
            Assert.That(reader.ReadInt64(), Is.EqualTo(134));
            Assert.That(reader.ReadInt64(), Is.EqualTo(4));
        });
        
        // Magic Number
        Assert.That(reader.ReadChars(8), Is.EqualTo(TableWriter.MagicNumber.ToArray()));
        
        Assert.That(reader.BaseStream.Position, Is.EqualTo(12 * length // data length
                                                           + 134 // metadata size (
                                                           + 8 * 2 // Magicnumber x 2
                                                           + 24)); // postscript (3 * 8)
    }
}