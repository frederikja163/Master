using Microsoft.Spark;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;

namespace Master.Benchmarks.RawBenchmarks;

public class SparkBenchmark : IRawBenchmark
{
    private readonly string _format;
    private readonly SparkSession _spark;

    public SparkBenchmark(string format, SparkSession spark)
    {
        _format = format;
        _spark = spark;
    }
    
    public void Write(string path, Data data)
    {
        Task.Run(() =>
        {
            StructType schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new StructField(tuple.First, ReadDataType(tuple.Second.GetType().GetElementType()!)))
            );
            var dataFrame = _spark.CreateDataFrame(data.RowMajor().Select(row => new GenericRow(row.Select(ConvertValue).ToArray())), schema);
            DataFrameWriter dataFrameWriter = dataFrame.Write();
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            {
                dataFrameWriter = dataFrameWriter.Mode(SaveMode.Append);
            }
            dataFrameWriter.Format(_format) 
                .Save(Path.Combine(Directory.GetCurrentDirectory(), path));
            return Task.CompletedTask;
        }).Wait();
    }

    private DataType ReadDataType(Type dataType)
    {
        return dataType == typeof(int) ? new IntegerType() :
            dataType == typeof(string) ? new StringType() :
            dataType == typeof(float) ? new DoubleType() : throw new NotImplementedException("Couldn't read data type");
    }
    private static object ConvertValue(object v)
    {
        return v switch
        {
            float f => (double)f,
            _ => v
        };
    }

    public override string ToString()
    {
        return "Spark " + _format;
    }
}