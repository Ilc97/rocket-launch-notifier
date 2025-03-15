using System.Text.Json.Serialization;

namespace RocketLaunchNotifier.Models
{
    public class Launch
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("net")]
        public string Net { get; set; }
    }
}
