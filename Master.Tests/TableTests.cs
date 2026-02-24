using System.Collections;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Tests;

public class TableTests
{
    [TestCase(1, 10)]
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
        Assert.That(table.GetDataColumns(), Is.EquivalentTo(dataColumns));
        Assert.That(table.Columns.Select(item => item.name), Is.EquivalentTo(names));
    }
}