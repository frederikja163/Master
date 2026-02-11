using Parquet;
using SqlParser.Ast;

namespace Master.Benchmarks.Raw.Visitors;

public class ParquetWhereVisitor(IParquetRowGroupReader parquetRowGroupReader) : Visitor
{
    public override ControlFlow PreVisitExpression(Expression expression)
    {
        Console.WriteLine(expression);
        return base.PreVisitExpression(expression);
    }
}