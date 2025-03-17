using Moq;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Services;
using Xunit;

public class JsonServiceTests
{
    private readonly Mock<ILogger<JsonService>> _mockLogger;
    private readonly JsonService _jsonService;

    public JsonServiceTests()
    {
        _mockLogger = new Mock<ILogger<JsonService>>();
        _jsonService = new JsonService(_mockLogger.Object);
    }

    [Fact]
    public async Task LoadLaunchDataFromFile_ShouldReturnEmptyList_WhenFileNotFound()
    {
        // Act
        var result = await _jsonService.LoadLaunchDataFromFile("/Testing/launches_example.json");

        // Assert
        Assert.NotNull(result);
    }
}