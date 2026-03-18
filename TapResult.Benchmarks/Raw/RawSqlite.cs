using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using TapResult.Extensions;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawSqlite : IRawBenchmark
{
    public void Write(string path, ICustomData data)
    {
        string connectionString = new SQLiteConnectionStringBuilder()
        {
            DataSource = path
        }.ToString();

        using SQLiteConnection connection = new SQLiteConnection(connectionString);
        connection.Open();

        using (SQLiteCommand command = new SQLiteCommand(
                   $"CREATE TABLE results({string.Join(",", data.ColumnNames.Zip(data.Columns.Select(GetColumnType)).Select(CreateField))});",
                   connection))
        {
            command.ExecuteNonQuery();
        }

        for (int i = 0; i < data.Repeats; i++)
        {
            string names = string.Join(",", data.ColumnNames);
            string values = string.Join(",", data.ColumnNames.Select(n => $"${n}"));
            
            using SQLiteTransaction transaction = connection.BeginTransaction();
            using SQLiteCommand command = new SQLiteCommand($"INSERT INTO results ({names}) VALUES ({values})", connection);

            SQLiteParameter[] parameters = data.ColumnNames.Zip(data.Columns.Select(GetParamType))
                .Select(t => command.Parameters.Add($"${t.First}", t.Second)).ToArray();
            
            foreach (Array row in data.Rows)
            {
                for (int j = 0; j < row.Length; j++)
                {
                    parameters[j].Value = row.GetValue(j);
                }
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
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
}