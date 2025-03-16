using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RocketLaunchNotifier.Database.EmailRepository
{
    public class EmailRepository
    {
        private readonly string _dbFile;
        private readonly ILogger<EmailRepository> _logger;

        public EmailRepository(string dbFile, ILogger<EmailRepository> logger)
        {
            _dbFile = dbFile;
            _logger = logger;
        }

        public async Task EnsureEmailTable()
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();
            
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS email_receivers (
                    email TEXT PRIMARY KEY,
                    newMember BOOLEAN
                );";
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Database ensured.");
        }

        public async Task<(List<string> newMembers, List<string> existingMembers)> GetEmailsFromDatabase()
        {
            _logger.LogInformation("Fetching emails from database...");
            var newMembers = new List<string>();
            var existingMembers = new List<string>();

            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT email, newMember FROM email_receivers;";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var email = reader.GetString(0);
                var isNew = reader.GetBoolean(1);
                if (isNew)
                    newMembers.Add(email);
                else
                    existingMembers.Add(email);
            }
            _logger.LogInformation("Emails fetched.");
            return (newMembers, existingMembers);
        }

        public async Task UpdateEmailReceivers(List<string> emailList)
        {
            _logger.LogInformation("Updating email receivers.");

            await using var connection = new SqliteConnection($"Data Source={_dbFile}");
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            foreach (var email in emailList)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO email_receivers (email, newMember) 
                    VALUES (@Email, true)
                    ON CONFLICT(email) 
                    DO UPDATE SET newMember = false;";
                command.Parameters.AddWithValue("@Email", email);
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            _logger.LogInformation("Emails updated.");
        }
    }
}
