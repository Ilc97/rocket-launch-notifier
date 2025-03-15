using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RocketLaunchNotifier.Models;

namespace RocketLaunchNotifier.Data
{
    public class DatabaseService
    {
        private readonly string _dbFile;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(string dbFile, ILogger<DatabaseService> logger)
        {
            _dbFile = dbFile;
            _logger = logger;
        }

        public void EnsureDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Launches (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    LaunchDate TEXT,
                    Status TEXT
                );";
            command.ExecuteNonQuery();
            _logger.LogInformation("Database ensured.");
        }

        public List<Launch> GetExistingLaunches()
        {
            var launches = new List<Launch>();
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, LaunchDate, Status FROM Launches;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                launches.Add(new Launch
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Net = reader.GetString(2),
                    Status = new Status { Name = reader.GetString(3) }
                });
            }
            return launches;
        }

        public void InsertLaunch(Launch launch)
        {
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Launches (Id, Name, LaunchDate, Status) 
                VALUES (@Id, @Name, @LaunchDate, @Status);";
            command.Parameters.AddWithValue("@Id", launch.Id);
            command.Parameters.AddWithValue("@Name", launch.Name);
            command.Parameters.AddWithValue("@LaunchDate", launch.Net);
            command.Parameters.AddWithValue("@Status", launch.Status.Name);
            command.ExecuteNonQuery();
        }

        public void UpdateLaunch(Launch launch)
        {
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Launches 
                SET Name = @Name, LaunchDate = @LaunchDate, Status = @Status
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", launch.Id);
            command.Parameters.AddWithValue("@Name", launch.Name);
            command.Parameters.AddWithValue("@LaunchDate", launch.Net);
            command.Parameters.AddWithValue("@Status", launch.Status.Name);
            command.ExecuteNonQuery();
        }

        public void DeleteLaunch(string launchId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Launches WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", launchId);
            command.ExecuteNonQuery();
        }
    }
}
