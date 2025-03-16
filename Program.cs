using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Database.LaunchRepository;
using RocketLaunchNotifier.Services;
using RocketLaunchNotifier.Database.EmailRepository;

class Program
{
    private static readonly string JsonFile = "Testing/launches_example.json";
    private static readonly string DbFile_launches = "Data/Database/rocket_launches.db";
    private static readonly string DbFile_emails = "Data/Database/emails.db";
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole(options => { options.TimestampFormat = "[HH:mm:ss] "; }));
    private static readonly ILogger<Program> Logger = LoggerFactory.CreateLogger<Program>();

    static async Task Main()
    {
        Logger.LogInformation("Application started.");

        // Initialize repositories and services
        var launchRepo = new LaunchRepository(DbFile_launches, LoggerFactory.CreateLogger<LaunchRepository>());
        var emailRepo = new EmailRepository(DbFile_emails, LoggerFactory.CreateLogger<EmailRepository>());
        var jsonService = new JsonService(LoggerFactory.CreateLogger<JsonService>());
        var emailService = new EmailService(LoggerFactory.CreateLogger<EmailService>());
        var emailGenerationService = new EmailGenerationService(LoggerFactory.CreateLogger<EmailGenerationService>());
        var launchService = new LaunchService(LoggerFactory.CreateLogger<LaunchService>());
        var launchApiService = new LaunchApiService(LoggerFactory.CreateLogger<LaunchApiService>(), "https://ll.thespacedevs.com/2.3.0/launches");
        
        //Fetching data from API
        var launchData = await launchApiService.FetchLaunchDataAsync();
        
        if (launchData == null)
        {
            Logger.LogError("Failed to fetch launch data.");
        }

        // Ensure database tables exist
        await launchRepo.EnsureDatabase();
        await emailRepo.EnsureEmailTable();

        // Load email list and update database
        var emailList = await jsonService.LoadEmailsFromJson("emails_config.json");
        await emailRepo.UpdateEmailReceivers(emailList);

        // For testing, enable loading launches from JSON. Uncomment the next line.
        //var newLaunches = await jsonService.LoadLaunchDataFromFile(JsonFile);

        //Comment this line for testing data from JSON
        var newLaunches = launchData;

        //Fetching Launches stored in the DB
        var existingLaunches = await launchRepo.GetExistingLaunches();


        // Fetch email subscribers
        var (newMembers, existingMembers, allMembers) = await emailRepo.GetEmailsFromDatabase();

        // Monday: Send full launch list to all subscribers
        if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
        {
            Logger.LogInformation("Today is Monday! Sending weekly launch schedule.");
            string emailContent = emailGenerationService.GenerateLaunchesHtml(newLaunches);
            emailService.SendEmails(allMembers, "Next Week Rocket Launches", emailContent);
        }
        else
        {
            //Others days
            //Get changes between data stored in DB and from API
            var changes = await launchService.CompareAndUpdateLaunches(launchRepo, newLaunches, existingLaunches);

            Logger.LogInformation($"Number of changes detected: {changes.Count}");

            
            if (changes.Count > 0)
            {   //If there are changes, notify existing members
                Logger.LogInformation("New changes. Updating existing members.");
                emailGenerationService.SendUpdateEmails(emailService, changes, existingMembers);
            }
            else
            {   //Nothing new, but new members need to be notified of all launches next week.
                Logger.LogInformation("Nothing new, checking if there are new members.");
            }

            if (newMembers.Count > 0)
            {
                Logger.LogInformation("New members. Notifying them.");
                emailGenerationService.SendNewMemberEmail(emailService, newLaunches, newMembers);
            }
        }

        Logger.LogInformation("Application finished execution.");
    }

}
