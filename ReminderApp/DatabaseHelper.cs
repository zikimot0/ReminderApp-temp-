using System;
using System.Collections.Generic;
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

        public static void DeleteUser(int userId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM Users WHERE Id = @Id";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
                    command.ExecuteNonQuery();
                }
            }
        }


        public static void InitializeDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Existing table creation logic
                string createUsersTable = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT UNIQUE NOT NULL,
    Password TEXT NOT NULL,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
)";

                string createLogsTable = @"
CREATE TABLE IF NOT EXISTS LoginLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT NOT NULL,
    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
    IsSuccessful INTEGER NOT NULL
)";

                string createRemindersTable = @"
CREATE TABLE IF NOT EXISTS Reminders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    DateTime TEXT NOT NULL,
    Subject TEXT NOT NULL,
    Description TEXT,
    Triggered INTEGER DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
)";

                // New table for registration
                string createRegistrationTable = @"
CREATE TABLE IF NOT EXISTS Registration (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    RegistrationDate TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
)";

                using (var command = new SQLiteCommand(connection))
                {
                    // Execute existing table creation commands
                    command.CommandText = createUsersTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createLogsTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createRemindersTable;
                    command.ExecuteNonQuery();

                    // Execute new table creation command
                    command.CommandText = createRegistrationTable;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        public static List<User> GetAllUsers()
        {
            var users = new List<User>();

            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT Id, Email, CreatedAt FROM Users";

                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                Email = reader.GetString(1),
                                CreatedAt = DateTime.Parse(reader.GetString(2))
                            });
                        }
                    }
                }
            }

            return users;
        }
    }


    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
