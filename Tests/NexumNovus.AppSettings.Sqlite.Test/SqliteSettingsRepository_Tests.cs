namespace NexumNovus.AppSettings.Json.Test;

using Microsoft.Data.Sqlite;
using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Sqlite;

public class SqliteSettingsRepository_Tests
{
  private readonly SqliteSettingsRepository _sut;
  private readonly SqliteConfigurationSource _source;

  public SqliteSettingsRepository_Tests()
  {
    var mockProtector = new Mock<ISecretProtector>();
    mockProtector.Setup(x => x.Protect(It.IsAny<string>())).Returns("***");

    _source = new SqliteConfigurationSource
    {
      ConnectionString = "Data Source=NexumNovus.AppSettings.Sqlite.Test.db",
      Protector = mockProtector.Object,
      ReloadOnChange = false,
    };

    _sut = new SqliteSettingsRepository(_source);

    CreateDb();
    CleanDb();
  }

  [Fact]
  public async Task Update_Key_Should_Be_Case_Insensitive_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("name", "New Name").ConfigureAwait(false); // key should be case-insensitive

    // Assert
    var result = GetAllDbSettings();
    result.Count.Should().Be(2);
    result["name"].Should().Be("New Name");
  }

  [Fact]
  public async Task Should_Add_New_Setting_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Surname", "New Surname").ConfigureAwait(false);

    // Assert
    var result = GetAllDbSettings();
    result.Count.Should().Be(3);
    result["surname"].Should().Be("New Surname");
  }

  [Fact]
  public async Task Should_Protect_Settings_With_Secret_Attribute_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Account", new TestSetting
    {
      Name = "demo",
      Password = "demo",
    }).ConfigureAwait(false);

    // Assert
    var result = GetAllDbSettings();
    result.Count.Should().Be(6);
    result["Account:Name"].Should().Be("demo");
    result["Account:Password*"].Should().Be("***");
  }

  [Fact]
  public async Task Should_Update_Complex_Objects_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Account", new TestSetting
    {
      Name = "demo",
      Password = "demo",
      Types = new[] { "A", "B", "C" },
      Data = new Dictionary<string, int>
      {
        { "A", 1 },
        { "B", 2 },
      },
    }).ConfigureAwait(false);

    // Assert
    var result = GetAllDbSettings();
    result.Count.Should().Be(9);
    result["Account:Types:0"].Should().Be("A");
    result["Account:Types:1"].Should().Be("B");
    result["Account:Types:2"].Should().Be("C");
    result["Account:Data:A"].Should().Be("1");
    result["Account:Data:B"].Should().Be("2");
  }

  private void CreateDb()
  {
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open(); // this will create the database file, if it does not exist

      var command = connection.CreateCommand();
      command.CommandText = _source.CreateTableCommand;
      command.ExecuteNonQuery();
    }
  }

  private Dictionary<string, string?> GetAllDbSettings()
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

  private void SeedDb(Dictionary<string, string?> seedData)
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

  private void CleanDb()
  {
    using (var connection = new SqliteConnection(_source.ConnectionString))
    {
      connection.Open(); // this will create the database file, if it does not exist

      var command = connection.CreateCommand();
      command.CommandText = $"DELETE FROM {_source.TableName}";
      command.ExecuteNonQuery();
    }
  }

  private sealed class TestSetting
  {
    public string Name { get; set; } = string.Empty;

    [SecretSetting]
    public string Password { get; set; } = string.Empty;

    public IList<string>? Types { get; set; }

    public IDictionary<string, int>? Data { get; set; }
  }
}
