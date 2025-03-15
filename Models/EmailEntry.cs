using System.Text.Json.Serialization;

namespace RocketLaunchNotifier.Models
{
    public class EmailEntry
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
