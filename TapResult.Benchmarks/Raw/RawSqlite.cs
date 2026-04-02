using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using TapResult.Extensions;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawSqlite : IRawBenchmark, IAsyncDisposable
{
    private SQLiteConnection connection;

    public void Open(string filePath)
    {
        string connectionString = new SQLiteConnectionStringBuilder()
        {
            DataSource = filePath
        }.ToString();

        connection = new SQLiteConnection(connectionString);
        connection.Open();
    }

    public void Write(ICustomData data)
    {

        using (SQLiteCommand command = new SQLiteCommand(
                   $"CREATE TABLE {data.Name} ({string.Join(",", data.ColumnNames.Zip(data.Columns.Select(GetColumnType)).Select(CreateField))});",
                   connection))
        {
            command.ExecuteNonQuery();
        }

        string names = string.Join(",", data.ColumnNames);
        string values = string.Join(",", data.ColumnNames.Select(n => $"${n}"));
        
        using SQLiteTransaction transaction = connection.BeginTransaction();
        using SQLiteCommand command1 = new SQLiteCommand($"INSERT INTO results ({names}) VALUES ({values})", connection);

        SQLiteParameter[] parameters = data.ColumnNames.Zip(data.Columns.Select(GetParamType))
            .Select(t => command1.Parameters.Add($"${t.First}", t.Second)).ToArray();
        
        foreach (Array row in data.Rows)
        {
            for (int j = 0; j < row.Length; j++)
            {
                parameters[j].Value = row.GetValue(j);
            }
            command1.ExecuteNonQuery();
        }
        transaction.Commit();
        static string GetColumnType(Array array)
        {
            Type type = array.GetType().GetElementType()?.GetUnderlyingNullableType() ?? throw new ArgumentException(null, nameof(array));
            return type == typeof(int) ? "INT" :
                type == typeof(string) ? "TEXT" :
                type == typeof(double) ? "REAL" :
                type == typeof(float) ? "REAL" :
                throw new UnreachableException();
        }

        static DbType GetParamType(Array array)
        {
            Type type = array.GetType().GetElementType()?.GetUnderlyingNullableType() ?? throw new ArgumentNullException(null, nameof(array));
            return type == typeof(int) ? DbType.Int32 :
                type == typeof(string) ? DbType.String :
                type == typeof(float) ? DbType.Single :
                type == typeof(double) ? DbType.Double :
                throw new UnreachableException();
        }

        static string CreateField((string name, string type) tuple)
        {
            (string name, string type) = tuple;
            return $"{name} {type}";
        }
    }

    public override string ToString()
    {
        return "Sql";
    }

    public void Close()
    {
        connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
    }
}