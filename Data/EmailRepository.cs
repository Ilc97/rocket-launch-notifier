using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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

        public void EnsureEmailTable()
        {
            
            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS email_receivers (
                    email TEXT PRIMARY KEY,
                    newMember BOOLEAN
                );";
            command.ExecuteNonQuery();

            _logger.LogInformation("Database ensured.");
        }

        public (List<string> newMembers, List<string> existingMembers) GetEmailsFromDatabase()
        {
            _logger.LogInformation("Fetching emails from database...");
            var newMembers = new List<string>();
            var existingMembers = new List<string>();

            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT email, newMember FROM email_receivers;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
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

        public void UpdateEmailReceivers(List<string> emailList)
        {
            _logger.LogInformation("Updating email receivers.");

            using var connection = new SqliteConnection($"Data Source={_dbFile}");
            connection.Open();

            using var transaction = connection.BeginTransaction();
            foreach (var email in emailList)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO email_receivers (email, newMember) 
                    VALUES (@Email, true)
                    ON CONFLICT(email) 
                    DO UPDATE SET newMember = false;";
                command.Parameters.AddWithValue("@Email", email);
                command.ExecuteNonQuery();
            }
            transaction.Commit();

            _logger.LogInformation("Emails updated.");
        }
    }
}
