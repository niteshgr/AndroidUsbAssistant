using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;
using AndroidUsbAssistant.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Xunit;

namespace AndroidUsbAssistant.Tests;

public class AdbServiceTests
{
    private class FakeConfigService : IConfigurationService
    {
        public AppConfiguration GetConfiguration() => new AppConfiguration { AdbPath = "nonexistent-adb" };
        public Task UpdateConfigurationAsync(AppConfiguration configuration) => Task.CompletedTask;
    }

    [Fact]
    public async Task SetUsbTetheringAsync_WhenAdbExecutableNotFound_ReturnsTrueDueToFallback()
    {
        // Arrange
        var configService = new FakeConfigService();
        var logger = NullLogger<AdbService>.Instance;
        var adbService = new AdbService(configService, logger);

        // Act
        // This will attempt to run nonexistent-adb, throw an exception,
        // which the inner catch treats as a possible ADB disconnection reset and returns true.
        var result = await adbService.SetUsbTetheringAsync("12345", true);

        // Assert
        Assert.True(result);
    }
}
