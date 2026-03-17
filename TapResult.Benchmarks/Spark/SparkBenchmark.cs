using TapResult.Extensions;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Spark;

public class SparkBenchmark
{
    private readonly string _format;
    private readonly SparkSession _spark;

    public SparkBenchmark(string format, SparkSession spark)
    {
        _format = format;
        _spark = spark;
    }
    
    public void Write(string path, ICustomData data)
    {
        Task.Run(() =>
        {
            StructType schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new StructField(tuple.First, ReadDataType(tuple.Second.GetType().GetElementType()!.GetUnderlyingNullableType())))
            );
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), path);
        
            for (int i = 0; i < data.Repeats; i++)
            {
                var dataFrame = _spark.CreateDataFrame(data.Rows.Select(row => new GenericRow(row.OfType<object>().Select(ConvertValue).ToArray())), schema);
                dataFrame.Write()
                    .Mode(i == 0 ? SaveMode.Overwrite : SaveMode.Append)
                    .Format(_format)
                    .Save(outputPath);
            }
            return Task.CompletedTask;
        }).Wait();
    }

    private DataType ReadDataType(Type dataType)
    {
        return dataType == typeof(int) ? new IntegerType() :
            dataType == typeof(string) ? new StringType() :
            dataType == typeof(double) ? new DoubleType() :
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