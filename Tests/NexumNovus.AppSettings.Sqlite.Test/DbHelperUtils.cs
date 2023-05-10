namespace NexumNovus.AppSettings.Json.Test;
using Microsoft.Data.Sqlite;
using NexumNovus.AppSettings.Sqlite;

public class DbHelperUtils
{
  private readonly SqliteConfigurationSource _source;

  public DbHelperUtils(SqliteConfigurationSource source) => _source = source;

  public void CleanDb()
  {
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open(); // this will create the database file, if it does not exist

      var command = connection.CreateCommand();
      command.CommandText = $"DELETE FROM {_source.TableName}";
      command.ExecuteNonQuery();
    }
  }

  public void CreateDb()
  {
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open(); // this will create the database file, if it does not exist

      var command = connection.CreateCommand();
      command.CommandText = _source.CreateTableCommand;
      command.ExecuteNonQuery();
    }
  }

  public Dictionary<string, string?> GetAllDbSettings()
  {
    var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open();

      var command = connection.CreateCommand();
      command.CommandText = _source.GetAllQuery;

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          var key = reader.GetString(0);
          var value = reader.IsDBNull(1) ? null : reader.GetString(1);
          settings.Add(key, value);
        }
      }
    }

    return settings;
  }

  public void SeedDb(Dictionary<string, string?> seedData)
  {
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open(); // this will create the database file, if it does not exist

      using (var command = connection.CreateCommand())
      {
        command.CommandText = _source.InsertCommand;
        command.Parameters.Add("@key", SqliteType.Text);
        command.Parameters.Add("@value", SqliteType.Text);
        command.Parameters.Add("@lastUpdateDt", SqliteType.Text);

        foreach (var item in seedData)
        {
          command.Parameters["@key"].Value = item.Key;
          command.Parameters["@value"].Value = item.Value as object ?? DBNull.Value;
          command.Parameters["@lastUpdateDt"].Value = DateTime.UtcNow;

          command.ExecuteNonQuery();
        }
      }
    }
  }
}
