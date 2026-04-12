using System.Runtime.CompilerServices;
using System.Text;
using TapResult;
using TapResult.Columns;
using TapResult.Extensions;
using TapResult.Readers;

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

    public static IEnumerable<(Array, LogicalType type)> CreatePrimitivesTestSource()
    {
        foreach ((Array array, LogicalType type) in CreateArrays())
        {
            yield return (array, type);
        }

        static IEnumerable<(Array, LogicalType)> CreateArrays()
        {
            yield return (Enumerable.Range(0, 10).Select(i => (sbyte)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 10).Select(i => (short)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 10).Select(i => (int)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 10).Select(i => (long)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 10).Select(i => (byte)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 10).Select(i => (ushort)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 10).Select(i => (uint)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 10).Select(i => (ulong)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 10).Select(i => (Half)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 10).Select(i => (float)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 10).Select(i => (double)i).ToArray(), LogicalType.Float64);
            
            yield return (Enumerable.Range(0, 10).Select(i => (sbyte?)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 10).Select(i => (short?)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 10).Select(i => (int?)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 10).Select(i => (long?)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 10).Select(i => (byte?)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 10).Select(i => (ushort?)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 10).Select(i => (uint?)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 10).Select(i => (ulong?)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 10).Select(i => (Half?)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 10).Select(i => (float?)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 10).Select(i => (double?)i).ToArray(), LogicalType.Float64);
            
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (sbyte?)i : null).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (short?)i : null).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (int?)i : null).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (long?)i : null).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (byte?)i : null).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (ushort?)i : null).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (uint?)i : null).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (ulong?)i : null).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (Half?)i : null).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (float?)i : null).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 10).Select(i => Random.Shared.Next() > 0.5 ? (double?)i : null).ToArray(), LogicalType.Float64);
        }
    }
    
    [Test]
    [TestCaseSource(nameof(CreatePrimitivesTestSource))]
    public void CreatePrimitivesTest((Array array, LogicalType type) tuple)
    {
        (Array array, LogicalType type) = tuple;
        IColumn column = ColumnBuilder.Create(array, out _);
        IColumnReader reader = column.OpenReader();
        Assert.That(column.LogicalType, Is.EqualTo(type));
        Assert.That(reader.Read(reader.Length), Is.EqualTo(array));
        Assert.That(reader.Length, Is.EqualTo(array.Length));
    }

    [Test]
    public void DataColumnCreateFailsOnInvalidType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColumnBuilder.Create(Array.Empty<object>(), out _));
    }
}