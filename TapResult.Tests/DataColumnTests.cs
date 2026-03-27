using System.Runtime.CompilerServices;
using System.Text;
using TapResult;
using TapResult.Columns;
using TapResult.Extensions;

namespace TapResult.Tests;

internal sealed class DataColumnTests
{
    [Test]
    public void CreateStringsTest()
    {
        string[] strings = ["Abcd", "1234", "Hello world!", "CSharp"];
        DataColumn column = ColumnBuilder.Create(strings);
        int physicalLength = strings.Select(str => Unsafe.SizeOf<int>() + Encoding.UTF8.GetByteCount(str)).Sum();
        Assert.That(column.PhysicalSize, Is.EqualTo(physicalLength));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.String));
        Assert.That(column.LogicalLength, Is.EqualTo(strings.Length));
        
        byte[] data = strings.SelectMany(DataHelper.GetBytes).ToArray();
        Assert.That(column.Data.ToArray(), Is.EqualTo(data));
    }
    
    [Test]
    public void CreateBlobsTest()
    {
        byte[][] blobs = [[0, 1, 2], [1,2,3], [2,3,4]];
        DataColumn column = ColumnBuilder.Create(blobs);
        int physicalLength = blobs.Select(blob => Unsafe.SizeOf<int>() + blob.Length).Sum();
        Assert.That(column.PhysicalSize, Is.EqualTo(physicalLength));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.Blob));
        Assert.That(column.LogicalLength, Is.EqualTo(blobs.Length));

        byte[] data = blobs.SelectMany(DataHelper.GetBytes).ToArray();
        Assert.That(column.Data.ToArray(), Is.EqualTo(data));
    }

    public static IEnumerable<(Array, byte[], LogicalType type)> CreatePrimitivesTestSource()
    {
        foreach ((Array array, LogicalType type) in CreateArrays())
        {
            yield return (array, array.Cast<object>().SelectMany(DataHelper.GetBytes).ToArray(), type);
        }

        static IEnumerable<(Array, LogicalType)> CreateArrays()
        {
            yield return (Enumerable.Range(0, 8).Select(i => (sbyte)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (short)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (int)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (long)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (ushort)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (uint)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (ulong)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (Half)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 8).Select(i => (float)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 8).Select(i => (double)i).ToArray(), LogicalType.Float64);
            
            yield return (Enumerable.Range(0, 8).Select(i => (sbyte?)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (short?)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (int?)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (long?)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (byte?)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (ushort?)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (uint?)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (ulong?)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (Half?)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 8).Select(i => (float?)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 8).Select(i => (double?)i).ToArray(), LogicalType.Float64);
        }
    }
    
    [Test]
    [TestCaseSource(nameof(CreatePrimitivesTestSource))]
    public void CreatePrimitivesTest((Array array, byte[] bytes, LogicalType type) tuple)
    {
        (Array array, byte[] bytes, LogicalType type) = tuple;
        DataColumn column = ColumnBuilder.Create(array, out DataColumn? nulls);
        Assert.That(column.LogicalType, Is.EqualTo(type));
        Assert.That(column.Data.ToArray(), Is.EqualTo(bytes));
        Assert.That(column.LogicalLength, Is.EqualTo(array.Length));

        if (!array.GetType().GetElementType()!.IsNullable())
        {
            Assert.That(nulls, Is.Null);
        }
        else
        {
            
            Assert.That(nulls, Is.Not.Null);
            DataColumn nullsCol = nulls;
            Assert.That(nullsCol.LogicalType, Is.EqualTo(LogicalType.UInt8));
            Assert.That(nullsCol.LogicalLength, Is.EqualTo(array.Length / 8 + 1));
            Assert.That(nullsCol.PhysicalSize, Is.EqualTo(array.Length / 8 + 1));
            Assert.That(nullsCol.Data.ToArray(), Is.All.EqualTo(0));
        }
    }

    [Test]
    public void DataColumnCreateFailsOnInvalidType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColumnBuilder.Create(Array.Empty<object>(), out _));
    }
}