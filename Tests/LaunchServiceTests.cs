using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Moq;
using RocketLaunchNotifier.Services;
using RocketLaunchNotifier.Models;
using RocketLaunchNotifier.Database.LaunchRepository;
using Microsoft.Extensions.Logging;

public class LaunchApiServiceIntegrationTests
{
    [Fact]
    public async Task FetchLaunchDataAsync_ShouldReturnValidJson_EvenIfNoLaunches()
    {
        // Arrange
        var logger = new Mock<ILogger<LaunchApiService>>();
        var launchApiService = new LaunchApiService(logger.Object, "https://ll.thespacedevs.com/2.3.0/launches");

        // Act
        var result = await launchApiService.FetchLaunchDataAsync();

        // Assert
        Assert.NotNull(result); // Ensure we get a valid list
        Assert.IsType<List<Launch>>(result); // Ensures it's a valid list type
    }
}

