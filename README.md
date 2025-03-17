# Rocket Launch Notifier  

Rocket Launch Notifier is a .NET console application that retrieves upcoming rocket launches for the next week, stores them in an SQLite database, and notifies subscribers via email.

## Features  
- Fetches upcoming rocket launch schedules from an API.  
- Stores and updates launch data in an SQLite database.  
- Sends email notifications about upcoming launches.  
- Differentiates between new subscribers and existing ones.  

## Requirements  
- .NET 8.0 or later  
- Packages:  
  - SQLite (`Microsoft.Data.Sqlite`)  
  - Logging (`Microsoft.Extensions.Logging.Console`)  
  - Unit Testing: Moq, xUnit  

## Configuration  
Before running the app, set up the required configuration files.  

### `appsettings.json`  
Create a file named `appsettings.json` in the root directory with your SMTP settings:  
```json
{
  "SmtpSettings": {
    "Host": "smtp.example.com",
    "Port": 587,
    "User": "your-email@example.com",
    "Pass": "your-password",
  }
}
