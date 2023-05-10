![Banner](Images/Banner.png)

# NexumNovus.AppSettings.Sqlite

[![NexumNovus.AppSettings.Sqlite NuGet Package](https://img.shields.io/nuget/v/NexumNovus.AppSettings.Sqlite.svg)](https://www.nuget.org/packages/NexumNovus.AppSettings.Sqlite/) [![NexumNovus.AppSettings.Sqlite NuGet Package Downloads](https://img.shields.io/nuget/dt/NexumNovus.AppSettings.Sqlite)](https://www.nuget.org/packages/NexumNovus.AppSettings.Sqlite) [![GitHub Actions Status](https://github.com/vigzel/nexumnovus-appsettings-sqlite/workflows/Build/badge.svg?branch=main)](https://github.com/vigzel/nexumnovus-appsettings-sqlite/actions)

[![GitHub Actions Build History](https://buildstats.info/github/chart/vigzel/nexumnovus-appsettings-sqlite?branch=main&includeBuildsFromPullRequest=false)](https://github.com/vigzel/nexumnovus-appsettings-sqlite/actions)


This package enables you to load and persist settings into sqlite database, and encrypt/decrypt settings marked with SecretSetting attribute.
### About

Sqlite configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). This package enables you to 
 - read application settings from Sqlite database. 
 - update application settings and save changes to Sqlite database. 
 - cryptographically protect selected application settings

Use `SqliteHostBuilderExtensions.AddSQLiteConfig` extension method on `IHostBuilder` to add the Sqlite configuration provider to the configuration builder and register `SqliteSettingsRepository` with service collection.

Use `ISettingsRepository.UpdateSettingsAsync` to update Sqlite settings.

Mark properties with `SecretSetting` attribute to cryptographically protect them.

### Example

```cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexumNovus.AppSettings.Common;
using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Load settings from sqlite database and register settings repository with service collection
builder.Host.AddSQLiteConfig("Data Source=demo.data.db");

// Use of options pattern to register configuration elements is optional.
builder.Services.AddOptions<EmailSettings>().BindConfiguration(EmailSettings.ConfigElement);

var app = builder.Build();

// Api's to get and update EmailSettings
app.MapGet("/emailSettings", (IOptionsMonitor<EmailSettings> emailSettings) => emailSettings.CurrentValue);
app.MapPost("/emailSettings", async (EmailSettings emailSettings, ISettingsRepository settingsRepo)
  => await settingsRepo.UpdateSettingsAsync(EmailSettings.ConfigElement, emailSettings)
);

// Api's to get and update a setting
app.MapGet("/settings", (string section, IConfiguration settings) => settings.GetSection(section));
app.MapPost("/settings", async (string section, string value, ISettingsRepository settingsRepo)
  => await settingsRepo.UpdateSettingsAsync(section, value)
);

app.Run();

public record EmailSettings
{
  public static string ConfigElement = "Email";
  public string Host { get; set; }
  public string Username { get; set; }
  [SecretSetting]
  public string Password { get; set; } //this setting will be cryptographically protected
} 
```

If a setting is not found in the database, then it's created.

Note that password is marked with `[SecretSetting]` and it will be protected. After update Sqlite database will have a table `__AppSettings` with data: 

| Key               | Value                                                                             |
| ----------------- | --------------------------------------------------------------------------------- |
| Email:Host        | "example.com"                                                                     |
| Email:Username    | "my_username"                                                                     |
| Email:Password    | "CfDJ8IBGRtcA2S1Ji7VPVwaKmLYnTN6skE_2RQqvNZ8_CN5y3Xvk3LkFC6GXCe8EY7AicxH5...."    |


Default implementation for `ISecretProtector` uses [Microsoft.AspNetCore.DataProtection](https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection/). 
You can also provide your own implementation.

By default settings are reloaded if updated using `ISettingsRepository`. If you need to reload them on external database changes set `CheckForChangesPeriod` to a timespan value larger that zero.

```c#
builder.Host.AddSQLiteConfig(x =>
{
  x.ConnectionString = "Data Source=demo.data.db",
  x.TableName = "MyCustomNameForAppSettingsTable",
  x.CheckForChangesPeriod = TimeSpan.FromSeconds(60),
  x.Protector = <your implementation of ISecretProtector>
});
```
