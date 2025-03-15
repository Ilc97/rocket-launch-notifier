using System.Text.Json.Serialization;

namespace RocketLaunchNotifier.Models
{
     public class EmailConfig
    {   
        [JsonPropertyName("emailReceivers")]
        public List<EmailEntry> EmailReceivers { get; set; } = new List<EmailEntry>();
    }
}
