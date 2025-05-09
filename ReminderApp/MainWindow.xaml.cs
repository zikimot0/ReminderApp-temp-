using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ReminderApp
{
    public partial class MainWindow : Window
    {
        private readonly string _userEmail;
        private List<Reminder> _reminders;
        private DispatcherTimer _alarmTimer;

        public MainWindow(string userEmail)
        {
            InitializeComponent();
            _userEmail = userEmail;

            if (_userEmail == "admin@gwapo.com")
            {
                ViewLoginLogsButton.Visibility = Visibility.Visible;
                ManageUsersButton.Visibility = Visibility.Visible;
            }

            // Ensure database schema is up-to-date
            EnsureDatabaseSchema();

            // Load reminders
            LoadReminders();

            // Initialize and start the alarm timer
            InitializeAlarmTimer();
        }

        private void EnsureDatabaseSchema()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Add Triggered column to Reminders table if it doesn't exist
                    string query = "ALTER TABLE Reminders ADD COLUMN Triggered INTEGER DEFAULT 0";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SQLiteException ex) when (ex.Message.Contains("duplicate column name"))
            {
                // Column already exists, ignore the error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error ensuring database schema: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeAlarmTimer()
        {
            _alarmTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Check every 30 seconds
            };
            _alarmTimer.Tick += CheckForDueReminders;
            _alarmTimer.Start();
        }

        private void CheckForDueReminders(object sender, EventArgs e)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT Id, Subject, Description 
                        FROM Reminders 
                        WHERE DateTime <= @CurrentTime AND Triggered = 0 AND UserId = @UserId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CurrentTime", DateTime.Now.ToString("o"));
                        command.Parameters.AddWithValue("@UserId", GetUserId(_userEmail));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int reminderId = reader.GetInt32(0);
                                string subject = reader.GetString(1);
                                string description = reader.GetString(2);

                                // Trigger the alarm
                                TriggerAlarm(subject, description);

                                // Mark the reminder as triggered
                                MarkReminderAsTriggered(reminderId, connection);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking reminders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TriggerAlarm(string subject, string description)
        {
            App.PlayReminderSound();
            MessageBox.Show($"Reminder: {subject}\n{description}", "Reminder",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MarkReminderAsTriggered(int reminderId, SQLiteConnection connection)
        {
            string updateQuery = "UPDATE Reminders SET Triggered = 1 WHERE Id = @Id";
            using (var command = new SQLiteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", reminderId);
                command.ExecuteNonQuery();
            }
        }

        private void LoadReminders()
        {
            try
            {
                _reminders = new List<Reminder>();

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        SELECT DateTime, Subject, Description 
                        FROM Reminders 
                        WHERE UserId = @UserId 
                        ORDER BY DateTime";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", GetUserId(_userEmail));
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _reminders.Add(new Reminder
                                {
                                    DateTime = DateTime.Parse(reader["DateTime"].ToString()),
                                    Subject = reader["Subject"].ToString(),
                                    Description = reader["Description"].ToString()
                                });
                            }
                        }
                    }
                }

                ReminderListBox.ItemsSource = null;
                ReminderListBox.ItemsSource = _reminders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reminders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetUserId(string email)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "SELECT Id FROM Users WHERE Email = @Email";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userManagementWindow = new UserManagementWindow();
                userManagementWindow.Owner = this;
                userManagementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening user management: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddReminderButton_Click(object sender, RoutedEventArgs e)
        {
            ReminderWindow reminderWindow = new ReminderWindow(_userEmail);
            reminderWindow.ShowDialog();
            LoadReminders(); // Refresh after adding
        }

        private void DeleteReminderButton_Click(object sender, RoutedEventArgs e)
        {
            Reminder selectedReminder = (Reminder)ReminderListBox.SelectedItem;

            if (selectedReminder == null)
            {
                MessageBox.Show("Please select a reminder to delete.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Delete reminder '{selectedReminder.Subject}'?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        string query = @"
                            DELETE FROM Reminders 
                            WHERE UserId = @UserId 
                            AND DateTime = @DateTime 
                            AND Subject = @Subject";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", GetUserId(_userEmail));
                            command.Parameters.AddWithValue("@DateTime", selectedReminder.DateTime.ToString("o"));
                            command.Parameters.AddWithValue("@Subject", selectedReminder.Subject);
                            command.ExecuteNonQuery();
                        }
                    }

                    LoadReminders(); // Refresh the list
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting reminder: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }

        private void ViewLoginLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check if LoginLogs table exists
                    bool tableExists = false;
                    string checkTable = "SELECT name FROM sqlite_master WHERE type='table' AND name='LoginLogs'";
                    using (var cmd = new SQLiteCommand(checkTable, connection))
                    {
                        tableExists = (cmd.ExecuteScalar() != null);
                    }

                    if (!tableExists)
                    {
                        MessageBox.Show("No login logs available yet", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Get logs
                    string query = "SELECT Email, Timestamp, IsSuccessful FROM LoginLogs ORDER BY Timestamp DESC";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            StringBuilder logs = new StringBuilder();
                            while (reader.Read())
                            {
                                logs.AppendLine($"{reader["Timestamp"]}: {reader["Email"]} - " +
               (Convert.ToInt32(reader["IsSuccessful"]) == 1 ? "Success" : "Failed"));
                            }

                            if (logs.Length == 0)
                            {
                                MessageBox.Show("No login records found", "Information",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show(logs.ToString(), "Login Logs",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Reminder
    {
        public DateTime DateTime { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }
}
