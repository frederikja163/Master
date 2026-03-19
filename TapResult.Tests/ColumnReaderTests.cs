using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Tests;

internal sealed class ColumnReaderTests
{
    [TestCase(0, 100)]
    public void PrimitiveReaderTest(int start, int count)
    {
        int[] arr = Enumerable.Range(start, count).ToArray();
        DataColumn column = ColumnBuilder.Create(arr);
        IColumnReader<int> reader = column.OpenReader<int>();
        
        Assert.That(reader.Peek(), Is.EqualTo(start));
        Assert.That(reader.Peek(1), Is.EqualTo(start + 1));
        reader.Advance(1);
        Assert.That(reader.Peek(), Is.EqualTo(start + 1));
        Assert.That(reader.Peek(0, arr.Length - 1), Is.EqualTo(arr.Skip(1)));
        Assert.That(reader.IsAtEnd, Is.False);
        Assert.That(reader.Read(arr.Length - 1), Is.EqualTo(arr.Skip(1)));
        Assert.That(reader.IsAtEnd, Is.True);
    }
    
    [Test]
    public void VarLengthReaderTest()
    {
        string[] arr = ["This", "is", "a", "test"];
        DataColumn column = ColumnBuilder.Create(arr);
        IColumnReader<string> reader = column.OpenReader<string>();
        
        Assert.That(reader.Peek(), Is.EqualTo("This"));
        Assert.That(reader.Peek(1), Is.EqualTo("is"));
        reader.Advance(1);
        Assert.That(reader.Peek(), Is.EqualTo("is"));
        Assert.That(reader.Peek(0, arr.Length - 1), Is.EqualTo(arr.Skip(1)));
        Assert.That(reader.IsAtEnd, Is.False);
        Assert.That(reader.Read(arr.Length - 1), Is.EqualTo(arr.Skip(1)));
        Assert.That(reader.IsAtEnd, Is.True);
    }

    [Test]
    public void ThrowsExceptionOnWrongType()
    {
        DataColumn column = ColumnBuilder.Create([0, 1, 2, 3]);
        Assert.Throws<ArgumentException>(() => column.OpenReader<string>());
        Assert.DoesNotThrow(() => column.OpenReader());
        Assert.DoesNotThrow(() => column.OpenGenericReader());
    }
}