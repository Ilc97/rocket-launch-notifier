using System.Text.Json.Serialization;

namespace RocketLaunchNotifier.Models
{
    public class LaunchApiResponse
    {
        [JsonPropertyName("results")]
        public List<Launch> Results { get; set; } = new List<Launch>();
    }
}
