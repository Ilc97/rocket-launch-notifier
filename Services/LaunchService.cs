using System.Text.Json;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Database.LaunchRepository;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Services
{
    public class LaunchService
    {
        private readonly ILogger<LaunchService> _logger;

        public LaunchService(ILogger<LaunchService> logger)
        {
            _logger = logger;
        }

        public async Task<List<string>> CompareAndUpdateLaunches(LaunchRepository dbService, List<Launch> newLaunches, List<Launch> existingLaunches)
        {
            var changes = new List<string>();
            var existingDict = existingLaunches.ToDictionary(l => l.Id);
            var newIds = new HashSet<string>(newLaunches.Select(l => l.Id));

            foreach (var newLaunch in newLaunches)
            {
                if (!existingDict.TryGetValue(newLaunch.Id, out var existingLaunch))
                {
                    changes.Add($"New: {newLaunch.Name} (Date: {newLaunch.Net})");
                    await dbService.InsertLaunch(newLaunch);
                }
                else if (existingLaunch.Name != newLaunch.Name || existingLaunch.Net != newLaunch.Net || existingLaunch.Status.Name != newLaunch.Status.Name)
                {
                    changes.Add($"Updated: {newLaunch.Name} (New Date: {newLaunch.Net}, New Status: {newLaunch.Status.Name})");
                    await dbService.UpdateLaunch(newLaunch);
                }
            }

            foreach (var id in existingDict.Keys.Except(newIds))
            {
                changes.Add($"Postponed: {existingDict[id].Name} (Previous Date: {existingDict[id].Net})");
                await dbService.DeleteLaunch(id);
            }

            return changes;
        }
    }
}
