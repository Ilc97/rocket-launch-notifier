using System.Text.Json;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Services
{
    public class LaunchApiService
    {
        
        private static readonly HttpClient _httpClient = new HttpClient();

        
        private readonly ILogger<LaunchApiService> _logger;

        private readonly string _apiUrl;

        public LaunchApiService(ILogger<LaunchApiService> logger, string apiUrl)
        {
            _logger = logger;
            _apiUrl = apiUrl;
        }
        
        public async Task<List<Launch>> FetchLaunchDataAsync()
        {
            _logger.LogInformation($"Fetching data from the spacedevs API");

            // Calculate next week's start and end date in UTC
            (string windowStart, string windowEnd) = GetNextWeekDateRange();

            // Construct the API URL with dynamic dates
            //string apiUrl = $"https://ll.thespacedevs.com/2.3.0/launches/?window_start__gt={windowStart}&window_end__lt={windowEnd}&mode=list";
            string apiUrl = $"{_apiUrl}/?window_start__gt={windowStart}&window_end__lt={windowEnd}&mode=list";
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                
                string responseData = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var launchData = JsonSerializer.Deserialize<LaunchApiResponse>(responseData, options);
                return launchData?.Results ?? new List<Launch>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching launch data: {ex.Message}");
                return null;
            }
        }

        private (string, string) GetNextWeekDateRange()
        {
            DateTime now = DateTime.UtcNow;

            // Find next Monday
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            DateTime nextMonday = now.AddDays(daysUntilMonday).Date;

            // Find next Sunday
            DateTime nextSunday = nextMonday.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            // Format dates in ISO 8601 format (UTC)
            string windowStart = nextMonday.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string windowEnd = nextSunday.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return (windowStart, windowEnd);
        }
    }
}