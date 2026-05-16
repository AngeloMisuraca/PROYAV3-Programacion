using Microsoft.Data.Sqlite;

namespace TodoMVC.Data;

public static class Database
{
    private const string ConnectionString = "Data Source=instrumentos.db";

    public static void Inicializar()
    {
        using var connection = AbrirConexion();
        var sql = @"
            CREATE TABLE IF NOT EXISTS Instrumentos (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                Titulo     TEXT    NOT NULL,
                Completada INTEGER NOT NULL DEFAULT 0,
                Cantidad   INTEGER NOT NULL DEFAULT 1
            )";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.ExecuteNonQuery();

        var columnas = new List<string>();
        using var pragma = new SqliteCommand("PRAGMA table_info(Instrumentos)", connection);
        using var reader = pragma.ExecuteReader();
        while (reader.Read())
        {
            columnas.Add(reader.GetString(1));
        }

        if (!columnas.Contains("Cantidad"))
        {
            using var alter = new SqliteCommand(
                "ALTER TABLE Instrumentos ADD COLUMN Cantidad INTEGER NOT NULL DEFAULT 1",
                connection);
            alter.ExecuteNonQuery();
        }
    }

    public static SqliteConnection AbrirConexion()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
