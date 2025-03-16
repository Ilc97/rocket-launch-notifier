using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Services
{
    public class EmailGenerationService
    {
        private readonly ILogger<EmailGenerationService> _logger;
        
        public EmailGenerationService(ILogger<EmailGenerationService> logger)
        {
            _logger = logger;
        }

        // Generates the HTML for weekly launch emails
        public string GenerateLaunchesHtml(List<Launch> launches)
        {   
            _logger.LogInformation($"Generating HTML email for new week launches");

            //Group launches by days
            var groupedLaunches = launches
                .Select(l => new
                {
                    Launch = l,
                    Date = DateTime.ParseExact(l.Net, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                })
                .GroupBy(l => l.Date.DayOfWeek)
                .OrderBy(g => (int)g.Key)
                .Select(g => new
                {
                    Day = g.Key,
                    Launches = g.OrderBy(l => l.Date)
                });
            
            string launchesList = "";

            //Loops through each day
            foreach (var group in groupedLaunches)
            {
                string dayName = group.Day.ToString();
                launchesList += $@"
                    <div class='day-section'>
                        <h3>{dayName}</h3>
                        {string.Join("", group.Launches.Select(l => $@"
                            <div class='launch-card'>
                                <span class='rocket-icon'>🚀</span> {l.Launch.Name} | {l.Date:HH:mm 'UTC'}
                            </div>"))}
                    </div>";
            }

            return Email.EmailTemplateHelper.LoadTemplate("Templates/EmailTemplate.html", launchesList, "Upcoming Rocket Launches", "Here are the scheduled launches for the upcoming week:");
        }

        // Sends email updates for launch changes
        public void SendUpdateEmails(EmailService emailService, List<LaunchChange> changes, List<string> existingMembers)
        {   
            _logger.LogInformation($"Generating HTML email for new updates");

            if (existingMembers.Count == 0) return;

            string changesList = "";
            var groupedChanges = changes
                .Select(c => new
                {
                    Launch = c.Launch,
                    Date = DateTime.ParseExact(c.Launch.Net, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    ChangeType = c.ChangeType
                })
                .GroupBy(c => c.ChangeType)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(c => c.Date.DayOfWeek)
                        .OrderBy(g => (int)g.Key)
                        .Select(g => new
                        {
                            Day = g.Key,
                            Launches = g.OrderBy(l => l.Date)
                        }).ToList()
                );

            foreach (var changeGroup in groupedChanges)
            {
                string changeTypeLabel = changeGroup.Key switch
                {
                    LaunchChangeType.NEW => "🆕 New Launches",
                    LaunchChangeType.STATUS_CHANGE => "❓ Updated Launch Status",
                    LaunchChangeType.RESCHEDULED => "🕒 Rescheduled Launches",
                    LaunchChangeType.POSTPONED => "🚫 Postponed/Canceled Launches",
                    _ => "❓ Other Changes"
                };

                changesList += $"<h2>{changeTypeLabel}</h2>";
                changesList += "<div class='day-section'>";
                foreach (var group in changeGroup.Value)
                {
                    changesList += $@"
                            {string.Join("", group.Launches.Select(l => $@"
                                <div class='launch-card'>
                                    <span class='rocket-icon'>🚀</span> {l.Launch.Name} | 
                                    {(changeGroup.Key == LaunchChangeType.POSTPONED ? "" : $"{l.Date}")}
                                    {(changeGroup.Key == LaunchChangeType.STATUS_CHANGE ? $" | {l.Launch.Status.Name}" : "")}
                                </div>"))}
                    ";
                }

                changesList += "</div>";
            }

            string emailContent = Email.EmailTemplateHelper.LoadTemplate("Templates/EmailTemplate.html", changesList, "Updates Of Upcoming Rocket Launches", "What's new?");
            emailService.SendEmails(existingMembers, "Rocket Launch Updates For Next Week", emailContent);
        }

        // Sends welcome email to new members with full launch schedule
        public void SendNewMemberEmail(EmailService emailService, List<Launch> newLaunches, List<string> newMembers)
        {
            _logger.LogInformation($"Generating HTML email for new members");

            string emailContent = GenerateLaunchesHtml(newLaunches);
            emailService.SendEmails(newMembers, "Welcome! Upcoming Rocket Launches", emailContent);
        }
            
        }
}
