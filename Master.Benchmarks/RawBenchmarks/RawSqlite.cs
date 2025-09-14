using System.Data.SQLite;

namespace Master.Benchmarks.RawBenchmarks;

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
                   $"CREATE TABLE results({string.Join(",", data.ColumnNames.Zip(data.Columns.Select(GetArrayType)).Select(CreateField))});",
                   connection))
        {
            command.ExecuteNonQuery();
        }

        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (IEnumerable<object> row in data.RowMayor())
            {
                using SQLiteCommand command = new SQLiteCommand(
                    $"INSERT INTO results ({string.Join(",", data.ColumnNames)}) VALUES ({string.Join(",",row)})",
                    connection);
                command.ExecuteNonQuery();
            }
        }
        static string GetArrayType(Array array)
        {
            Type type = array.GetType().GetElementType() ?? throw new ArgumentException(null, nameof(array));
            return type == typeof(int) ? "INT" :
                type == typeof(string) ? "TEXT" :
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