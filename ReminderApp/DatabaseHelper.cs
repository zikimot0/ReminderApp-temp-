using System;
using System.Data.SQLite;
using System.IO;

namespace ReminderApp
{
    public static class DatabaseHelper
    {
        private static string _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ReminderApp",
            "ReminderApp.db"
        );

        private static string _connectionString = $"Data Source={_dbPath};Version=3;";

        public static void InitializeDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // In InitializeDatabase() method:

                // Update Users table creation
                string createUsersTable = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT UNIQUE NOT NULL,
    Password TEXT NOT NULL,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
)";

                // Update LoginLogs table creation
                string createLogsTable = @"
CREATE TABLE IF NOT EXISTS LoginLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT NOT NULL,
    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
    IsSuccessful INTEGER NOT NULL
)";
                // Create Reminders table
                string createRemindersTable = @"
                CREATE TABLE IF NOT EXISTS Reminders (
                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                 DateTime TEXT NOT NULL,
                 Subject TEXT NOT NULL,
                 Description TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                )";


                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = createUsersTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createRemindersTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createLogsTable;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }
    }
}