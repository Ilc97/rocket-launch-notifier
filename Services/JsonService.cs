using System.Text.Json;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Services
{
    public class JsonService
    {
        private readonly ILogger<JsonService> _logger;

        public JsonService(ILogger<JsonService> logger)
        {
            _logger = logger;
        }

        public async Task<List<Launch>> LoadLaunchDataFromFile(string jsonFile)
        {
            try
            {
                _logger.LogInformation($"Reading rocket launch data from {jsonFile}.");
                var jsonContent = await File.ReadAllTextAsync(jsonFile);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var launchData = JsonSerializer.Deserialize<LaunchApiResponse>(jsonContent, options);
                return launchData?.Results ?? new List<Launch>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading JSON file: {ex.Message}");
                return new List<Launch>();
            }
        }
    }
}
