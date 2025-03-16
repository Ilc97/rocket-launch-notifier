using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Database.LaunchRepository
{
    public class LaunchRepository
    {
        private readonly string _dbFile;
        private readonly ILogger<LaunchRepository> _logger;

        public LaunchRepository(string dbFile, ILogger<LaunchRepository> logger)
        {
            _dbFile = dbFile;
            _logger = logger;
        }

        public async Task EnsureDatabase()
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Launches (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    LaunchDate TEXT,
                    Status TEXT
                );";
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Database ensured.");
        }

        public async Task<List<Launch>> GetExistingLaunches()
        {   
            var launches = new List<Launch>();

            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, LaunchDate, Status FROM Launches;";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                launches.Add(new Launch
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Net = reader.GetString(2),
                    Status = new Status { Name = reader.GetString(3) }
                });
            }

            _logger.LogInformation("Existing launches fetched.");
            return launches;
        }

        public async Task InsertLaunch(Launch launch)
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Launches (Id, Name, LaunchDate, Status) 
                VALUES (@Id, @Name, @LaunchDate, @Status);";
            command.Parameters.AddWithValue("@Id", launch.Id);
            command.Parameters.AddWithValue("@Name", launch.Name);
            command.Parameters.AddWithValue("@LaunchDate", launch.Net);
            command.Parameters.AddWithValue("@Status", launch.Status.Name);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Launch inserted.");
        }

        public async Task UpdateLaunch(Launch launch)
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Launches 
                SET Name = @Name, LaunchDate = @LaunchDate, Status = @Status
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", launch.Id);
            command.Parameters.AddWithValue("@Name", launch.Name);
            command.Parameters.AddWithValue("@LaunchDate", launch.Net);
            command.Parameters.AddWithValue("@Status", launch.Status.Name);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Launch updated.");
        }

        public async Task DeleteLaunch(string launchId)
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Launches WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", launchId);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Launch deleted.");
        }
    }
}
