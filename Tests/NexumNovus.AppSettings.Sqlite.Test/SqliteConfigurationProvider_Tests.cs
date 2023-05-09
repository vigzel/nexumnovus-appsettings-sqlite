namespace NexumNovus.AppSettings.Json.Test;

using Microsoft.Data.Sqlite;
using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Sqlite;

public class SqliteConfigurationProvider_Tests : IDisposable
{
  private readonly SqliteConfigurationProvider _sut;
  private readonly SqliteConfigurationSource _source;

  public SqliteConfigurationProvider_Tests()
  {
    var mockProtector = new Mock<ISecretProtector>();
    mockProtector.Setup(x => x.Protect(It.IsAny<string>())).Returns("***");

    _source = new SqliteConfigurationSource
    {
      ConnectionString = "Data Source=NexumNovus.AppSettings.Sqlite.SqliteConfigurationProvider_Tests.db",
      Protector = mockProtector.Object,
      ReloadOnChange = false,
    };

    _sut = new SqliteConfigurationProvider(_source);

    CreateDb();
    CleanDb();
  }

  [Fact]
  public void Should_Load_Keys_From_Database()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age", "36" },
      };
    SeedDb(initialSettings);

    // Act
    _sut.Load();
    var result = _sut.GetChildKeys(Enumerable.Empty<string>(), null).ToList();

    // Assert
    result.Count.Should().Be(2);
    _sut.TryGet("Name", out var tmpStr);
    tmpStr.Should().Be("test");
    _sut.TryGet("Age", out tmpStr);
    tmpStr.Should().Be("36");
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

  public void Dispose()
  {
    _sut?.Dispose();
    GC.SuppressFinalize(this);
  }
}
