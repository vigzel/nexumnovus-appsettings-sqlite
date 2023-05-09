[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NexumNovus.AppSettings.Sqlite.Test")]

namespace NexumNovus.AppSettings.Sqlite;

using Microsoft.Extensions.Configuration;
using NexumNovus.AppSettings.Common;

/// <summary>
/// Represents Sqlite database as an <see cref="IConfigurationSource"/>.
/// </summary>
public class SqliteConfigurationSource : NexumDbConfigurationSource
{
  /// <inheritdoc/>
  protected override IConfigurationProvider CreateProvider(IConfigurationBuilder builder)
    => new SqliteConfigurationProvider(this);

  /// <inheritdoc/>
  protected override void EnsureDefaults()
  {
    base.EnsureDefaults();
    if (string.IsNullOrWhiteSpace(ConnectionString))
    {
      throw new ArgumentException("ConnectionString is required for SqliteConfigurationSource");
    }
  }

  /// <summary>
  /// Gets SQL Command for table creation.
  /// </summary>
  internal string CreateTableCommand => @$"CREATE TABLE IF NOT EXISTS {TableName} (
                                    Key NTEXT NOT NULL COLLATE NOCASE CONSTRAINT PK_{TableName} PRIMARY KEY,
                                    Value NTEXT NULL,
                                    LastUpdateDt TEXT NOT NULL
                                  );";

#pragma warning disable SA1600, SA1516 // Elements should be documented and separated by a line
  internal string GetAllQuery => $"SELECT Key, Value FROM {TableName}";
  internal string GetByKeyQuery => $"SELECT Key, Value FROM {TableName} WHERE Key = @key or Key LIKE @keyLike COLLATE NOCASE;";
  internal string InsertCommand => $"INSERT INTO {TableName} (Key, Value, LastUpdateDt) values(@key, @value, @lastUpdateDt)";
  internal string UpdateCommand => $"UPDATE {TableName} SET Value = @value, LastUpdateDt = @lastUpdateDt WHERE Key = @key COLLATE NOCASE";
  internal string DeleteCommand => $"DELETE FROM {TableName} WHERE Key = @key COLLATE NOCASE";
  internal string LastUpdateDtQuery => $"SELECT max(LastUpdateDt) FROM {TableName}";
#pragma warning restore SA1600, SA1516 // Elements should be documented and separated by a line
}
