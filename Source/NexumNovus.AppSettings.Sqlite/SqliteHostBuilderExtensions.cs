namespace NexumNovus.AppSettings.Sqlite;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexumNovus.AppSettings.Common;

/// <summary>
/// Extension methods for adding <see cref="SqliteConfigurationProvider"/>.
/// </summary>
public static class SqliteHostBuilderExtensions
{
  /// <summary>
  /// Adds the database configuration provider at <paramref name="connectionString"/> to <paramref name="builder"/>.
  /// </summary>
  /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
  /// <param name="connectionString">Connection string (relative to the base path stored in
  /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>).</param>
  /// <param name="reloadOnChange">Whether the configuration should be reloaded if the database settings change.</param>
  /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
  public static IHostBuilder AddSQLiteConfig(this IHostBuilder builder, string connectionString, bool reloadOnChange = true)
  {
    if (string.IsNullOrEmpty(connectionString))
    {
      throw new ArgumentNullException(nameof(connectionString));
    }

    return builder.AddSQLiteConfig(s =>
    {
      s.ConnectionString = connectionString;
      s.ReloadOnChange = reloadOnChange;
    });
  }

  /// <summary>
  /// Adds a database configuration source to <paramref name="builder"/>.
  /// </summary>
  /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
  /// <param name="configureSource">Configures the source.</param>
  /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
  public static IHostBuilder AddSQLiteConfig(this IHostBuilder builder, Action<SqliteConfigurationSource> configureSource)
  {
    if (builder == null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    var source = new SqliteConfigurationSource();
    configureSource?.Invoke(source);

    builder.ConfigureAppConfiguration((HostBuilderContext _, IConfigurationBuilder cfg) => cfg.Add(source));

    builder.ConfigureServices((HostBuilderContext _, IServiceCollection services) =>
    {
      services.AddSingleton(source);
      services.AddScoped<ISettingsRepository, SqliteSettingsRepository>();
    });

    return builder;
  }
}
