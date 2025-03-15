using System.Text.Json.Serialization;

namespace RocketLaunchNotifier.Models
{
    public class Status
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
