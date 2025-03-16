using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Database.LaunchRepository;
using RocketLaunchNotifier.Services;
using RocketLaunchNotifier.Models;
using RocketLaunchNotifier.Database.EmailRepository;


class Program
{   
    //Testing JSON response instead of API call
    private static readonly string JsonFile = "launches_example.json";

    //File path for DB
    private static readonly string DbFile_launches = "rocket_launches.db";
    private static readonly string DbFile_emails = "emails.db";

    //Logger init
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole(options =>
        {
            options.TimestampFormat = "[HH:mm:ss] ";
        }
    ));
    private static readonly ILogger<Program> Logger = LoggerFactory.CreateLogger<Program>();

    //Main Task
    static async Task Main()
    {
        Logger.LogInformation("Application started.");

        //Inits
        var launchRepo = new LaunchRepository(DbFile_launches, LoggerFactory.CreateLogger<LaunchRepository>());
        var emailRepo = new EmailRepository(DbFile_emails, LoggerFactory.CreateLogger<EmailRepository>());
        var jsonService = new JsonService(LoggerFactory.CreateLogger<JsonService>());
        var emailService = new EmailService(LoggerFactory.CreateLogger<EmailService>());
        var launchService = new LaunchService(LoggerFactory.CreateLogger<LaunchService>());
        //Create tables if not exist
        launchRepo.EnsureDatabase();
        emailRepo.EnsureEmailTable();

        //Loading list of emails from json
        var emailList = await jsonService.LoadEmailsFromJson("emails_config.json");

        //Updating the table if new receivers
        emailRepo.UpdateEmailReceivers(emailList);

        //List of launches from file and updating the table
        var newLaunches = await jsonService.LoadLaunchDataFromFile(JsonFile);
        var existingLaunches = await launchRepo.GetExistingLaunches();

        //Get a list of changes between the API and Database
        var changes = await launchService.CompareAndUpdateLaunches(launchRepo, newLaunches, existingLaunches);
        
        Logger.LogInformation("Changes detected: " + changes.Count);
        foreach (var change in changes)
        {
            Logger.LogInformation(change);
        }


        // Fetch emails from database
        var (newMembers, existingMembers) = emailRepo.GetEmailsFromDatabase();

        if (changes.Count > 0)
        {
            var changeMessage = string.Join("\n", changes);
            
            // Send changes to existing members
            if (existingMembers.Count > 0)
            {
                emailService.SendEmails(existingMembers, "Rocket Launch Updates", changeMessage);
            }
            
            // Send full launches table to new members
            if (newMembers.Count > 0)
            {
                var fullLaunchMessage = string.Join("\n", existingLaunches);
                emailService.SendEmails(newMembers, "Welcome! Upcoming Rocket Launches", fullLaunchMessage);
            }
        } else {
            Logger.LogInformation("Nothing new...");
        }


        Logger.LogInformation("Application finished execution.");
    }

}
