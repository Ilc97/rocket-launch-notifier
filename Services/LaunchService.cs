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

        public async Task<List<LaunchChange>> CompareAndUpdateLaunches(LaunchRepository dbService, List<Launch> newLaunches, List<Launch> existingLaunches)
        {   
            _logger.LogInformation($"Comparing and updating launches");
            var changes = new List<LaunchChange>();
            var existingDict = existingLaunches.ToDictionary(l => l.Id);
            var newIds = new HashSet<string>(newLaunches.Select(l => l.Id));

            foreach (var newLaunch in newLaunches)
            {
                if (!existingDict.TryGetValue(newLaunch.Id, out var existingLaunch))
                {
                    // New Launch
                    changes.Add(new LaunchChange(newLaunch, LaunchChangeType.NEW));
                    await dbService.InsertLaunch(newLaunch);
                }
                else if (existingLaunch.Net != newLaunch.Net || existingLaunch.Status.Name != newLaunch.Status.Name)
                {
                    if(existingLaunch.Net != newLaunch.Net){
                        // Updated Launch (rescheduled)
                        changes.Add(new LaunchChange(newLaunch, LaunchChangeType.RESCHEDULED));
                        await dbService.UpdateLaunch(newLaunch);
                    }else{
                        // Updated Launch (new status)
                        changes.Add(new LaunchChange(newLaunch, LaunchChangeType.STATUS_CHANGE));
                        await dbService.UpdateLaunch(newLaunch);
                    }
                    
                }
            }

            foreach (var id in existingDict.Keys.Except(newIds))
            {
                // Postponed Launch (removed from new data)
                changes.Add(new LaunchChange(existingDict[id], LaunchChangeType.POSTPONED));
                await dbService.DeleteLaunch(id);
            }

            return changes;
        }
    }
}
