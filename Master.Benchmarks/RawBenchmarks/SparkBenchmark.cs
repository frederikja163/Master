using Microsoft.Spark;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;

namespace Master.Benchmarks.RawBenchmarks;

public class SparkBenchmark : IRawBenchmark
{
    private readonly string _format;
    private readonly SparkSession _spark;

    public SparkBenchmark(string format)
    {
        _format = format;
        //https://spark.apache.org/docs/latest/configuration.html#compression-and-serialization
        _spark = SparkSession.Builder()
            .Config(new SparkConf()
                .Set("spark.sql.orc.compression.codec", "none")
                .Set("spark.ui.enabled", "false"))
            .GetOrCreate();
        
    }
    
    public void Write(string path, Data data)
    {
        Task.Run(() =>
        {
            StructType schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new StructField(tuple.First, ReadDataType(tuple.Second.GetType().GetElementType()!)))
            );
            var dataFrame = _spark.CreateDataFrame(data.RowMajor().Select(row => new GenericRow(row.ToArray())), schema);
            dataFrame.Write()
                .Mode(SaveMode.Append)
                .Format(_format)
                .Save(Path.Combine(Directory.GetCurrentDirectory(), path));
            return Task.CompletedTask;
        }).Wait();
    }

    private DataType ReadDataType(Type dataType)
    {
        return dataType == typeof(int) ? new IntegerType() :
            dataType == typeof(string) ? new StringType() :
            dataType == typeof(float) ? new FloatType() : throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Spark " + _format;
    }
}