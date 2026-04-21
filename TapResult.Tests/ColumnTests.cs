using System.Diagnostics;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

namespace TapResult.Tests;

public class ColumnParent : IColumnParent
{
    private readonly List<IColumn> _columns;

    public ColumnParent(params IEnumerable<IColumn> columns)
    {
        _columns = columns.ToList();
    }
    
    public EncodingType EncodingType { get; } = EncodingType.Table;
    public LogicalType LogicalType { get; } = LogicalType.UInt8;
    public int LogicalLength { get; } = 0;

    public IColumnReader OpenReader()
    {
        throw new NotSupportedException();
    }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        throw new NotSupportedException();
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        return _columns;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        int i = _columns.IndexOf(existingColumn);
        if (i == -1)
            return false;
        _columns[i] = newColumn;
        return true;
    }
}

internal sealed class ColumnTests
{
    private IEnumerable<IColumn> CreateRecursiveColumns(int depth, int count = 1)
    {
        if (depth == 0)
        {
            return Enumerable.Repeat(DataColumn.Empty, count);
        }

        return Enumerable.Repeat(1, count).Select(_ => new ColumnParent(CreateRecursiveColumns(depth - 1, count + 1)));
    }
    
    [Test]
    public void RecursiveGetChildrenWorks()
    {
        IColumnParent column = CreateRecursiveColumns(3).First().Expect<IColumnParent>();
        AssertChildren(column, 3);


        static void AssertChildren(IColumnParent parent, int depth, int count = 2)
        {
            IColumn[] columns = parent.GetChildColumns().ToArray();
            if (depth == 0)
            {
                Assert.That(columns, Is.EqualTo(Enumerable.Repeat(DataColumn.Empty, count)));
                return;
            }

            Assert.That(columns.Length, Is.EqualTo(count));
            foreach (IColumnParent column in columns.OfType<IColumnParent>())
            {
                AssertChildren(column, depth - 1, count + 1);
            }
        }
    }
    
    [Test]
    public void RecursiveSwapWorks()
    {
        IColumnParent column = CreateRecursiveColumns(3).First().Expect<IColumnParent>();
        ColumnParent existingColumn = GetColumn();
        
        Assert.That(column.Swap(existingColumn, new ColumnParent()), Is.False);
        Assert.That(column.SwapRecursive(existingColumn, new ColumnParent()), Is.True);
        
        Assert.That(GetColumn().GetChildColumns().Count(),
            Is.EqualTo(0));

        ColumnParent GetColumn()
        {
            return column.GetChildColumns().Last().Expect<IColumnParent>().GetChildColumns().Last()
                .Expect<ColumnParent>();
        }
    }
}