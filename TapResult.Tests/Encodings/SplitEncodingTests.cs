using System.Text;
using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

namespace TapResult.Tests.Encodings;

internal sealed class SplitEncodingTests
{
    [Test]
    public void SplitStringEncodingTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitColumn columns = Assert.InstanceOf<SplitColumn>(ColumnBuilder.Create(strs));
        Assert.That(columns.GetChildColumns().Count(), Is.EqualTo(2));
        DataColumn lengthColumn = Assert.InstanceOf<DataColumn>(columns.LengthColumn);
        IColumnReader<int> lengthReader = lengthColumn.OpenReader<int>();
        DataColumn strColumn = Assert.InstanceOf<DataColumn>(columns.ByteColumn);
        IColumnReader<byte> strReader = strColumn.OpenReader<byte>();
        Assert.That(lengthColumn.LogicalType, Is.EqualTo(LogicalType.SInt32));
        Assert.That(lengthColumn.LogicalLength, Is.EqualTo(2));
        Assert.That(lengthReader.Read(2).ToArray(), Is.EqualTo(new int[]{str1.Length, str2.Length}));
        Assert.That(strColumn.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(strColumn.LogicalLength, Is.EqualTo(str1.Length + str2.Length));
        Assert.That(strReader.Read(str1.Length + str2.Length).ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes(str1).Concat(Encoding.UTF8.GetBytes(str2))));
    }

    [Test]
    public void SplitStringEncodingRoundRobinTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoding = new SplitEncoding();
        SplitColumn column = Assert.InstanceOf<SplitColumn>(ColumnBuilder.Create(strs));
        IColumnReader<string> reader = column.OpenReader<string>();
        Assert.That(reader.Read(2), Is.EqualTo(strs));
        Assert.That(reader.IsAtEnd, Is.True);
    }
}