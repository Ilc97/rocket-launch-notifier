using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Database.LaunchRepository;
using RocketLaunchNotifier.Services;
using RocketLaunchNotifier.Database.EmailRepository;
using System.Globalization;
using RocketLaunchNotifier.Models;

class Program
{
    private static readonly string JsonFile = "launches_example.json";
    private static readonly string DbFile_launches = "rocket_launches.db";
    private static readonly string DbFile_emails = "emails.db";

    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole(options =>
    {
        options.TimestampFormat = "[HH:mm:ss] ";
    }));
    private static readonly ILogger<Program> Logger = LoggerFactory.CreateLogger<Program>();

    static async Task Main()
    {
        Logger.LogInformation("Application started.");

        // Initialize repositories and services
        var launchRepo = new LaunchRepository(DbFile_launches, LoggerFactory.CreateLogger<LaunchRepository>());
        var emailRepo = new EmailRepository(DbFile_emails, LoggerFactory.CreateLogger<EmailRepository>());
        var jsonService = new JsonService(LoggerFactory.CreateLogger<JsonService>());
        var emailService = new EmailService(LoggerFactory.CreateLogger<EmailService>());
        var launchService = new LaunchService(LoggerFactory.CreateLogger<LaunchService>());

        // Ensure database tables exist
        await launchRepo.EnsureDatabase();
        await emailRepo.EnsureEmailTable();

        // Load email list and update database
        var emailList = await jsonService.LoadEmailsFromJson("emails_config.json");
        await emailRepo.UpdateEmailReceivers(emailList);

        // Load launches from JSON and update database
        var newLaunches = await jsonService.LoadLaunchDataFromFile(JsonFile);
        var existingLaunches = await launchRepo.GetExistingLaunches();
        var changes = await launchService.CompareAndUpdateLaunches(launchRepo, newLaunches, existingLaunches);

        Logger.LogInformation($"Changes detected: {changes.Count}");

        // Fetch email subscribers
        var (newMembers, existingMembers, allMembers) = await emailRepo.GetEmailsFromDatabase();

        // Monday: Send full launch list to all subscribers
        if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
        {
            Logger.LogInformation("Today is Monday! Sending weekly launch schedule.");
            string emailContent = GenerateLaunchesHtml(newLaunches);
            emailService.SendEmails(allMembers, "Next Week Rocket Launches", emailContent);
        }
        else
        {
            if (changes.Count > 0)
            {
                Logger.LogInformation("New changes. Updating existing members.");
                SendUpdateEmails(emailService, changes, existingMembers);
            }
            else
            {
                Logger.LogInformation("Nothing new, checking if there are new members.");
            }

            if (newMembers.Count > 0)
            {
                Logger.LogInformation("New members. Notifying them.");
                SendNewMemberEmail(emailService, newLaunches, newMembers);
            }
        }

        Logger.LogInformation("Application finished execution.");
    }

    // Generates the HTML for weekly launch emails
    private static string GenerateLaunchesHtml(List<Launch> launches)
    {
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

        return RocketLaunchNotifier.Email.EmailTemplateHelper.LoadTemplate("Templates/NewWeekNotification.html", launchesList);
    }

    // Sends email updates for launch changes
    private static void SendUpdateEmails(EmailService emailService, List<LaunchChange> changes, List<string> existingMembers)
    {
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

        string emailContent = RocketLaunchNotifier.Email.EmailTemplateHelper.LoadTemplate("Templates/UpdatesNotification.html", changesList);
        emailService.SendEmails(existingMembers, "Rocket Launch Updates For Next Week", emailContent);
    }

    // Sends welcome email to new members with full launch schedule
    private static void SendNewMemberEmail(EmailService emailService, List<Launch> newLaunches, List<string> newMembers)
    {
        string emailContent = GenerateLaunchesHtml(newLaunches);
        emailService.SendEmails(newMembers, "Welcome! Upcoming Rocket Launches", emailContent);
    }
}
