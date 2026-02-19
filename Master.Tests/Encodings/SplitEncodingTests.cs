using System.Text;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Tests.Encodings;

internal sealed class SplitEncodingTests
{
    [Test]
    public void SplitStringEncodingTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoder = new SplitEncoding();
        var columns = (SplitColumn) encoder.Encode(DataColumn.Create(strs.AsSpan()));
        Assert.That(columns.GetDataColumns().Count(), Is.EqualTo(2));
        DataColumn lengthColumn = columns._lengthColumn;
        DataColumnReader lengthReader = lengthColumn.OpenReader();
        DataColumn strColumn = columns._byteColumn;
        DataColumnReader strReader = strColumn.OpenReader();
        Assert.That(lengthColumn.LogicalType, Is.EqualTo(LogicalType.SInt32));
        Assert.That(lengthColumn.LogicalLength, Is.EqualTo(2));
        Assert.That(lengthReader.Read<int>(2).ToArray(), Is.EquivalentTo(new int[]{str1.Length, str2.Length}));
        Assert.That(strColumn.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(strColumn.LogicalLength, Is.EqualTo(str1.Length + str2.Length));
        Assert.That(strReader.Read<byte>(str1.Length + str2.Length).ToArray(), Is.EquivalentTo(Encoding.UTF8.GetBytes(str1).Concat(Encoding.UTF8.GetBytes(str2))));
    }

    [Test]
    public void SplitStringEncodingRoundRobinTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoding = new SplitEncoding();
        IColumn columns = encoding.Encode(DataColumn.Create(strs.AsSpan()));
        DataColumnReader decoded = encoding.Decode(columns).OpenReader();
        Assert.That(decoded.ReadString(2), Is.EquivalentTo(strs));
    }
}