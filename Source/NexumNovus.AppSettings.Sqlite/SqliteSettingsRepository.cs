namespace NexumNovus.AppSettings.Sqlite;

using System.Linq;
using Microsoft.Data.Sqlite;
using NexumNovus.AppSettings.Common;
using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Common.Utils;

/// <summary>
/// Used to update database settings.
/// </summary>
public class SqliteSettingsRepository : ISettingsRepository
{
  private readonly SqliteConfigurationSource _source;

  /// <summary>
  /// Initializes a new instance of the <see cref="SqliteSettingsRepository"/> class.
  /// </summary>
  /// <param name="source">The source settings.</param>
  public SqliteSettingsRepository(SqliteConfigurationSource source) => _source = source;

  /// <summary>
  /// Update settings in Sqlite database.
  /// </summary>
  /// <param name="name">Setting name (with the colon separator).</param>
  /// <param name="settings">Value of the setting.</param>
  /// <returns>Task.</returns>
  public async Task UpdateSettingsAsync(string name, object settings)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return;
    }

    var flatSettings = AppSettingsParser.Flatten(settings, name, SecretAttributeAction.MarkWithStar);

    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      await connection.OpenAsync().ConfigureAwait(false);

      var keysToUpdate = new List<string>();
      var keysToDelete = new List<string>();

      var dbSettings = await GetAsync(connection, name).ConfigureAwait(false);
      foreach (var key in dbSettings.Keys)
      {
        if (flatSettings.TryGetValue(key, out var flatSettingsValue))
        {
          if (dbSettings[key] != flatSettingsValue)
          {
            keysToUpdate.Add(key);
          }
        }
        else
        {
          keysToDelete.Add(key);
        }
      }

      var keysToAdd = flatSettings.Keys
        .Where(x => !dbSettings.ContainsKey(x));

      if (!keysToAdd.Any() && keysToUpdate.Count == 0 && keysToDelete.Count == 0)
      {
        return;
      }

      using (var transaction = connection.BeginTransaction())
      {
        try
        {
          var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

          // INSERT
          await UpsertAsync(connection, _source.InsertCommand, keysToAdd, flatSettings, now).ConfigureAwait(false);

          // UPDATE
          await UpsertAsync(connection, _source.UpdateCommand, keysToUpdate, flatSettings, now).ConfigureAwait(false);

          // DELETE
          await DeleteAsync(connection, keysToDelete).ConfigureAwait(false);

          transaction.Commit();
          _source.ChangeWatcher?.TriggerChange(now);
        }
        catch (Exception)
        {
          transaction.Rollback();
          throw;
        }
      }
    }
  }

  private async Task<Dictionary<string, string?>> GetAsync(SqliteConnection connection, string settingKey)
  {
    var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    using (var command = connection.CreateCommand())
    {
      command.CommandText = _source.GetByKeyQuery;
      command.Parameters.AddWithValue("@key", $"{settingKey}");
      command.Parameters.AddWithValue("@keyLike", $"{settingKey}:%");

      using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
      {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
          var key = reader.GetString(0);
          var value = await reader.IsDBNullAsync(1).ConfigureAwait(false)
            ? null
            : reader.GetString(1);
          value = key.EndsWith('*') ? Unprotect(value) : value;
          settings.Add(key, value);
        }
      }
    }

    return settings;
  }

  private async Task UpsertAsync(SqliteConnection connection, string commandText, IEnumerable<string> keys, IDictionary<string, string?> settings, string date)
  {
    if (!keys.Any())
    {
      return;
    }

    using (var command = connection.CreateCommand())
    {
      command.CommandText = commandText;
      command.Parameters.Add("@key", SqliteType.Text);
      command.Parameters.Add("@value", SqliteType.Text);
      command.Parameters.Add("@lastUpdateDt", SqliteType.Text);

      foreach (var key in keys)
      {
        object? value = key.EndsWith('*') ? Protect(settings[key]) : settings[key];

        command.Parameters["@key"].Value = key;
        command.Parameters["@value"].Value = value ?? DBNull.Value;
        command.Parameters["@lastUpdateDt"].Value = date;

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
      }
    }
  }

  private async Task DeleteAsync(SqliteConnection connection, IEnumerable<string> keys)
  {
    if (!keys.Any())
    {
      return;
    }

    using (var command = connection.CreateCommand())
    {
      command.CommandText = _source.DeleteCommand;
      command.Parameters.Add("@key", SqliteType.Text);

      foreach (var key in keys)
      {
        command.Parameters["@key"].Value = key;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
      }
    }
  }

  private string? Unprotect(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return value;
    }

    return _source.Protector.Unprotect(value) ?? value;
  }

  private string? Protect(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return value;
    }

    return _source.Protector.Protect(value) ?? value;
  }
}
