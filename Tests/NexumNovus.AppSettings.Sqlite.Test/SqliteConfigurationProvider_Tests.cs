namespace NexumNovus.AppSettings.Json.Test;

using NexumNovus.AppSettings.Common.Secure;
using NexumNovus.AppSettings.Sqlite;

public class SqliteConfigurationProvider_Tests : IDisposable
{
  private readonly SqliteConfigurationProvider _sut;
  private readonly SqliteConfigurationSource _source;
  private readonly DbHelperUtils _dbHelperUtils;

  public SqliteConfigurationProvider_Tests()
  {
    var mockProtector = new Mock<ISecretProtector>();
    mockProtector.Setup(x => x.Protect(It.IsAny<string>())).Returns("***");
    mockProtector.Setup(x => x.Unprotect(It.IsAny<string>())).Returns("unprotected");

    _source = new SqliteConfigurationSource
    {
      ConnectionString = "Data Source=NexumNovus.AppSettings.Sqlite.SqliteConfigurationProvider_Tests.db",
      Protector = mockProtector.Object,
      ReloadOnChange = false,
    };
    _dbHelperUtils = new DbHelperUtils(_source);

    _sut = new SqliteConfigurationProvider(_source);

    _dbHelperUtils.CreateDb();
    _dbHelperUtils.CleanDb();
  }

  [Fact]
  public void Should_Load_Keys_From_Database()
  {
    // Arrange
    var initialSettings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
      {
        { "Name", "test" },
        { "Age*", "***" },
      };
    _dbHelperUtils.SeedDb(initialSettings);

    // Act
    _sut.Load();
    var result = _sut.GetChildKeys(Enumerable.Empty<string>(), null).ToList();

    // Assert
    result.Count.Should().Be(2);
    _sut.TryGet("Name", out var tmpStr);
    tmpStr.Should().Be("test");
    _sut.TryGet("Age", out tmpStr);
    tmpStr.Should().Be("unprotected");
  }

  public void Dispose()
  {
    _sut?.Dispose();
    GC.SuppressFinalize(this);
  }
}
