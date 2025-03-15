using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Data;
using RocketLaunchNotifier.Services;
using RocketLaunchNotifier.Models;

class Program
{   
    //Testing JSON response instead of API call
    private static readonly string JsonFile = "launches_example.json";

    //File path for DB
    private static readonly string DbFile = "rocket_launches.db";

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

        var dbService = new DatabaseService(DbFile, LoggerFactory.CreateLogger<DatabaseService>());
        var jsonService = new JsonService(LoggerFactory.CreateLogger<JsonService>());

        dbService.EnsureDatabase();
        var newLaunches = await jsonService.LoadLaunchDataFromFile(JsonFile);
        var existingLaunches = dbService.GetExistingLaunches();

        var changes = CompareAndUpdateLaunches(dbService, newLaunches, existingLaunches);
        
        Logger.LogInformation("Changes detected: " + changes.Count);
        foreach (var change in changes)
        {
            Logger.LogInformation(change);
        }

        Logger.LogInformation("Application finished execution.");
    }

    private static List<string> CompareAndUpdateLaunches(DatabaseService dbService, List<Launch> newLaunches, List<Launch> existingLaunches)
    {
        var changes = new List<string>();
        var existingDict = existingLaunches.ToDictionary(l => l.Id);
        var newIds = new HashSet<string>(newLaunches.Select(l => l.Id));

        foreach (var newLaunch in newLaunches)
        {
            if (!existingDict.TryGetValue(newLaunch.Id, out var existingLaunch))
            {
                changes.Add($"New: {newLaunch.Name} (Date: {newLaunch.Net})");
                dbService.InsertLaunch(newLaunch);
            }
            else if (existingLaunch.Name != newLaunch.Name || existingLaunch.Net != newLaunch.Net || existingLaunch.Status.Name != newLaunch.Status.Name)
            {
                changes.Add($"Updated: {newLaunch.Name} (New Date: {newLaunch.Net}, New Status: {newLaunch.Status.Name})");
                dbService.UpdateLaunch(newLaunch);
            }
        }

        foreach (var id in existingDict.Keys.Except(newIds))
        {
            changes.Add($"Postponed: {existingDict[id].Name} (Previous Date: {existingDict[id].Net})");
            dbService.DeleteLaunch(id);
        }

        return changes;
    }
}
