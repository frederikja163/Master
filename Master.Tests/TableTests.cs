using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;
using Master.Serializing.Readers;

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
        DataColumn[] dataColumns = [
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan()),
            DataColumn.Create<int>(data.AsSpan())
        ];
        string[] names = ["columnA", "columnB", "columnC"];
        Table table = new Table(dataColumns, names);
        Serializer serializer = new();
        // Encoding is skipped
        serializer.WriteMetadata(table);

        Table metadata = serializer.GetMetadata();
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
        Assert.That(idColumn.OpenReader<int>().Read(4), Is.EqualTo(new [] { 1, 2, 3, 0 }));
        Assert.That(parentIdColumn.OpenReader<int>().Read(4), Is.EqualTo(new [] { 0, 0, 0, -1 }));
        Assert.That(encodingIdColumn.OpenReader<byte>().Read(4), Is.EqualTo( new byte[] { (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Table }));
        Assert.That(logicalTypeColumn.OpenReader<byte>().Read(4), Is.EqualTo(new byte[] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        var blobReader = blobColumn.OpenGenericReader();
        for (int i = 0; i < 3; i++)
        {
            Assert.That(blobReader.Read<int>(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>())); // Size of blob
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length)); // LogicalLength
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
                        DataColumn.Empty, 0, 0
                        ), 0, 0, 0
                    ), 0, 0, 0
                ), 
            new BitPackingColumn(new DataColumn(), 0, 0), 
            LogicalType.Blob
        );
        
        Serializer serializer = new();
        serializer.WriteMetaDataForColumn(column, -1);

        Table metadata = serializer.GetMetadata();
        DataColumn[] metadataColumns = metadata.Columns.OfType<DataColumn>().ToArray();
        DataColumn idColumn = metadataColumns[0];
        DataColumn parentIdColumn = metadataColumns[1];
        DataColumn encodingIdColumn = metadataColumns[2];
        DataColumn logicalTypeColumn = metadataColumns[3];
        DataColumn blobColumn = metadataColumns[4];
        
        Assume.That(idColumn.OpenReader<int>().Read(7), Is.EqualTo(new [] { 4, 3, 2, 1, 6, 5, 0 }));
        Assume.That(encodingIdColumn.OpenReader<byte>().Read(7), Is.EqualTo( new byte[]
        {
            (byte)EncodingId.Binary, (byte)EncodingId.BitPacking, (byte)EncodingId.BitPacking, (byte)EncodingId.BitPacking, (byte)EncodingId.Binary, (byte)EncodingId.BitPacking, (byte)EncodingId.Split
        }));
        
        // Only Tables write their own parentId, therefore this it is intended that the columns are not evenly long in this unittest
        Assert.That(parentIdColumn.OpenReader<int>().Read(6), Is.EqualTo(new [] { 3, 2, 1, 0, 5, 0 }));
    }
}