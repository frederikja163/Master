using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        Assert.That(table.Columns, Is.EquivalentTo(dataColumns));
        Assert.That(table.GetDataColumns(), Is.EquivalentTo(dataColumns));
        Assert.That(table.Names, Is.EquivalentTo(names));
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
        serializer.Encode(ref table);
        
        DataColumnBuilder idBuilder = new (LogicalType.SInt32, 50, true);
        DataColumnBuilder parentIdBuilder = new (LogicalType.SInt32, 50, true);
        DataColumnBuilder encodingIdBuilder = new (LogicalType.SInt16, 50, true);
        DataColumnBuilder logicalTypeBuilder = new(LogicalType.SInt8, 50, true);
        DataColumnBuilder blobBuilder = new (LogicalType.Blob, 50, true);
        int idCounter = 0;
        
        Serializer.CreateSchema(table, ref idCounter, ref idBuilder, ref parentIdBuilder, ref encodingIdBuilder, ref logicalTypeBuilder, ref blobBuilder);
        
        var metaDataTable = new Table([idBuilder.Build(), parentIdBuilder.Build(), encodingIdBuilder.Build(), logicalTypeBuilder.Build(), blobBuilder.Build()], ["Id", "ParentId", "Encoding", "LogicalType", "Blob"]);
        /* Table should look the following:
         * | Id | ParentId | Encoding | LogicalType | Blob
         * | 1 | 0 | Binary | SInt32 | { PhysicalSize, LogicalLength }
         * | 2 | 0 | Binary | SInt32 | { PhysicalSize, LogicalLength }
         * | 3 | 0 | Binary | SInt32 | { PhysicalSize, LogicalLength }
         * | 0 | -1 | Binary | SInt32 | { (id, name)[] }
         */
        Assert.That(idBuilder.Build().OpenReader().Read<int>(4).ToArray(), Is.EquivalentTo(new [] { 0, 1, 2, 3 }));
        Assert.That(parentIdBuilder.Build().OpenReader().Read<int>(4).ToArray(), Is.EquivalentTo(new [] { 3, 3, 3, -1 }));
        Assert.That(encodingIdBuilder.Build().OpenReader().Read<byte>(4).ToArray(), Is.EquivalentTo( new byte[] { (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Binary, (byte)EncodingId.Table }));
        Assert.That(logicalTypeBuilder.Build().OpenReader().Read<byte>(4).ToArray(), Is.EquivalentTo(new byte[] { (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.SInt32, (byte)LogicalType.UInt8 }));
        var blobReader = blobBuilder.Build().OpenReader();
        for (int i = 0; i < 3; i++)
        {
            Assert.That(blobReader.Read<int>(), Is.EqualTo(Unsafe.SizeOf<int>() + Unsafe.SizeOf<int>())); // Size of blob
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length * Unsafe.SizeOf<int>())); // PhysicalSize
            Assert.That(blobReader.Read<int>(), Is.EqualTo(length)); // LogicalLength
        }
        // length of names + 2 commas + integer string length = 30
        Assert.That(blobReader.Read<int>(), Is.EqualTo(names.Sum(name => name.Length + 2) + (names.Length / 10 + 1) * names.Length)); 
        Assert.That(Encoding.UTF8.GetString(blobReader.Read<byte>(30).ToArray()), Is.EquivalentTo("0,columnA,1,columnB,2,columnC,"));
    }
}