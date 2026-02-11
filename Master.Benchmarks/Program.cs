using BenchmarkDotNet.Running;
using CommandLine;
using Master.Benchmarks.Data;
using Master.Benchmarks.Raw;
using Parquet;
using Parquet.Meta;
using Parquet.Schema;
using SqlParser;
using SqlParser.Ast;

namespace Master.Benchmarks;

internal static class Program
{
    internal static void Main(string[] args)
    {
        //BenchmarkRunner.Run<OpenTAPBenchmarks>();
        //BenchmarkRunner.Run<RawBenchmarks>();
        //BenchmarkRunner.Run<SparkBenchmarks>();
        //BenchmarkRunner.Run<TPCHBenchmarks>();
        //BenchmarkRunner.Run<TPCHReadBenchmarks>();
        _ = sqlDebugging();
    }

    private static async Task sqlDebugging()
    {
        Directory.CreateDirectory("File");
        /*foreach (TpchData tpchData in TPCHBenchmarks.GetData(specificTables: "LINEITEM"))
        {
            new RawParquet(CompressionMethod.Snappy).Write(Path.Combine("File", "lineitem.parquet"), tpchData);
        }*/
        
        string path = Path.Combine(AppContext.BaseDirectory, "TPC-H V3.0.1", "dbgen");
        string queriesFile;
        try
        {
            queriesFile = new StreamReader(path + "/queries.sql").ReadToEnd();
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Couldn't find TPCH data. Please go to https://www.tpc.org/ and ensure that ddl and .tbl exist in Master.Benchmarks/TPC-H V3.0.1/dbgen");
            Console.WriteLine(e);
            return;
        }

        var statements = new SqlQueryParser().Parse(queriesFile)
            .Where(statement => statement.GetType() != typeof(Statement.Comment));
        
        var statement = statements.First();
        Console.WriteLine(statement.AsSelect().Query.Body.AsSelectExpression().Select.Projection[0].AsUnnamed().Expression.AsIdentifier().Ident);
        Console.WriteLine(statement.AsSelect().Query.Body.AsSelectExpression().Select.From[0].Relation.AsTable().Name);


        foreach (var stm in statements)
        {
            //Console.WriteLine("/////");
            List<string> filesToOpen = [];

            foreach (var from in statement.AsSelect().Query.Body.AsSelectExpression().Select.From)
            {
                TableFactor? t = from.Relation;
                //Console.Write(t?.GetType() + " ");
                switch (t)
                {
                    case null:
                        break;
                    case TableFactor.Derived derived:
                        //Console.WriteLine(derived);
                        break;
                    case TableFactor.Function function:
                        break;
                    case TableFactor.JsonTable jsonTable:
                        break;
                    case TableFactor.MatchRecognize matchRecognize:
                        break;
                    case TableFactor.NestedJoin nestedJoin:
                        break;
                    case TableFactor.Pivot pivot:
                        break;
                    case TableFactor.Table table:
                        filesToOpen.Add(table.Name);
                        //Console.WriteLine(table.Name);
                        break;
                    case TableFactor.TableFunction tableFunction:
                        break;
                    case TableFactor.UnNest unNest:
                        break;
                    case TableFactor.Unpivot unpivot:
                        break;
                }
            }

            foreach (string file in filesToOpen)
            {
                Console.WriteLine(file);
                var a = await ParquetReader.CreateAsync(Path.Combine("File", file + ".parquet"));
                
                foreach (var field in a.Schema.DataFields)
                {
                    Console.WriteLine(field.Name);
                    a.OpenRowGroupReader(0).ReadColumnAsync(field);
                }
            }
            
            //Console.WriteLine("/////");
            break;
        }

    }
}