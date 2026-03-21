using System.Text;
using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests.Encodings;

internal sealed class SplitEncodingTests
{
    [Test]
    public void SplitStringEncodingTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoder = new SplitEncoding();
        DataColumn dataColumn = ColumnBuilder.Create(strs);
        SplitColumn columns = (SplitColumn) encoder.Encode(dataColumn);
        Assert.That(columns.GetChildColumns().Count(), Is.EqualTo(2));
        DataColumn lengthColumn = (DataColumn) columns.LengthColumn;
        IColumnReader<int> lengthReader = lengthColumn.OpenReader<int>();
        DataColumn strColumn = (DataColumn) columns.ByteColumn;
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
        DataColumn dataColumn = ColumnBuilder.Create(strs);
        SplitColumn columns = (SplitColumn) encoding.Encode(dataColumn);
        ColumnBuilder builder = new ColumnBuilder(LogicalType.String, 100);
        columns.WriteMetadata(builder);
        DataColumn metadataColumn = builder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<string> reader = (IColumnReader<string>)encoding.CreateDecoder(LogicalType.String, genericReader,
            columns.GetChildColumns().OfType<DataColumn>().Select(c => c.OpenReader()));
        Assert.That(reader.Read(2), Is.EqualTo(strs));
        Assert.That(reader.IsAtEnd, Is.True);
    }
}