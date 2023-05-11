namespace NexumNovus.AppSettings.Json.Test;

using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Sqlite;

[Collection("Sequential")]
public class SqliteSettingsRepository_Tests
{
  private readonly SqliteSettingsRepository _sut;
  private readonly SqliteConfigurationSource _source;
  private readonly DbHelperUtils _dbHelperUtils;

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
    _dbHelperUtils = new DbHelperUtils(_source);

    _sut = new SqliteSettingsRepository(_source);

    _dbHelperUtils.CreateDb();
    _dbHelperUtils.CleanDb();
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
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("name", "New Name").ConfigureAwait(false); // key should be case-insensitive

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
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
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Surname", "New Surname").ConfigureAwait(false);

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
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
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Account", new TestSetting
    {
      Name = "demo",
      Password = "demo",
    }).ConfigureAwait(false);

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
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
    _dbHelperUtils.SeedDb(initialSettings);

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
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(9);
    result["Account:Types:0"].Should().Be("A");
    result["Account:Types:1"].Should().Be("B");
    result["Account:Types:2"].Should().Be("C");
    result["Account:Data:A"].Should().Be("1");
    result["Account:Data:B"].Should().Be("2");
  }

  [Fact]
  public async Task Should_Update_Sectioned_Setting_Async()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Person:Name", "test" },
        { "Person:Age", "36" },
        { "Person:NameMiddle", "middle name" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    await _sut.UpdateSettingsAsync("Person:Name", "NewName").ConfigureAwait(false);

    // Assert
    var result = _dbHelperUtils.GetAllDbSettings();
    result.Count.Should().Be(3);
    result["Person:Name"].Should().Be("NewName");
    result["Person:Age"].Should().Be("36");
    result["Person:NameMiddle"].Should().Be("middle name");
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
