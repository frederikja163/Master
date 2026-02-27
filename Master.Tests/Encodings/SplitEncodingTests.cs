using System.Text;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;
using Master.Serializing.Readers;

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
        SplitColumn columns = (SplitColumn) encoder.Encode(DataColumn.Create(strs));
        Assert.That(columns.GetDataColumns().Count(), Is.EqualTo(2));
        DataColumn lengthColumn = (DataColumn) columns.LengthColumn;
        IColumnReader<int> lengthReader = lengthColumn.OpenReader<int>();
        DataColumn strColumn = (DataColumn) columns.ByteColumn;
        IColumnReader<byte> strReader = strColumn.OpenReader<byte>();
        Assert.That(lengthColumn.LogicalType, Is.EqualTo(LogicalType.SInt32));
        Assert.That(lengthColumn.LogicalLength, Is.EqualTo(2));
        Assert.That(lengthReader.Read(2).ToArray(), Is.EquivalentTo(new int[]{str1.Length, str2.Length}));
        Assert.That(strColumn.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(strColumn.LogicalLength, Is.EqualTo(str1.Length + str2.Length));
        Assert.That(strReader.Read(str1.Length + str2.Length).ToArray(), Is.EquivalentTo(Encoding.UTF8.GetBytes(str1).Concat(Encoding.UTF8.GetBytes(str2))));
    }

    [Test]
    public void SplitStringEncodingRoundRobinTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoding = new SplitEncoding();
        IColumn columns = encoding.Encode(DataColumn.Create(strs));
        // DataColumnReader decoded = encoding.Decode(columns).OpenReader();
        // Assert.That(decoded.ReadString(2), Is.EquivalentTo(strs));
    }
}