using Master.Benchmarks.Data;
using SqlParser;
using SqlParser.Ast;

namespace Master.Benchmarks.Raw;

public interface IRawBenchmark
{
    public void Write(string path, ICustomData data);

    public void Read(string path, Statement sql);
}