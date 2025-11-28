using System.Data;
using System.Data.SQLite;
using Master.Benchmarks.Extensions;

namespace Master.Benchmarks.Raw;

internal sealed class RawSqlite : IRawBenchmark
{
    public void Write(string path, Data data)
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
            
            foreach (IEnumerable<object> row in data.RowMajor())
            {
                foreach ((object value, SQLiteParameter parameter)  in row.Zip(parameters))
                {
                    parameter.Value = value;
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
                type == typeof(float) ? "REAL" :
                type == typeof(float) ? "REAL" :
                throw new NotImplementedException();
        }

        static DbType GetParamType(Array array)
        {
            Type type = array.GetType().GetElementType().GetUnderlyingNullableType() ?? throw new ArgumentNullException(null, nameof(array));
            return type == typeof(int) ? DbType.Int32 :
                type == typeof(string) ? DbType.String :
                type == typeof(float) ? DbType.Single :
                type == typeof(float) ? DbType.Double :
                throw new NotImplementedException();
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