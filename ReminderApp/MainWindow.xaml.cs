using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
                Interval = TimeSpan.FromSeconds(1) // Check every 1 second para wala delay
            };
            _alarmTimer.Tick += CheckForDueReminders;
            _alarmTimer.Start();
        }

       


        private void CheckForDueReminders(object sender, EventArgs e)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                string query = @"
                SELECT Id, Subject, Description, AlarmPath, DateTime
                FROM Reminders
                WHERE DateTime <= @CurrentTime AND Triggered = 0 AND UserId = @UserId
                ORDER BY DateTime";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CurrentTime", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("@UserId", GetUserId(_userEmail));

                    using (var reader = command.ExecuteReader())
                    {
                        List<int> triggeredReminderIds = new List<int>();
                        DateTime? nextReminderTime = null;

                        while (reader.Read())
                        {
                            int reminderId = reader.GetInt32(0);
                            string subject = reader.GetString(1);
                            string description = reader.GetString(2);
                            string? alarmPath = reader.IsDBNull(3) ? null : reader.GetString(3);
                            DateTime reminderTime = reader.GetDateTime(4);

                            Dispatcher.Invoke(() => { TriggerAlarm(subject, description, alarmPath); });

                            // Mark the reminder as triggered
                            MarkReminderAsTriggered(reminderId, connection);
                            triggeredReminderIds.Add(reminderId);

                            // Track the next reminder time for adjusting the timer interval
                            if (nextReminderTime == null || reminderTime < nextReminderTime)
                            {
                                nextReminderTime = reminderTime;
                            }
                        }

                        // Adjust the timer interval to the next reminder time if any
                        if (nextReminderTime != null)
                        {
                            TimeSpan timeToNextReminder = nextReminderTime.Value - DateTime.Now;
                            if (timeToNextReminder > TimeSpan.Zero)
                            {
                                _alarmTimer.Interval = timeToNextReminder;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking reminders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void TriggerAlarm(string subject, string description, string? alarmPath)
        {
            App.PlayReminderSound(alarmPath);
            var result = MessageBox.Show($"Reminder: {subject}\n{description}", "Reminder",
                MessageBoxButton.OK, MessageBoxImage.Information);
            if (result == MessageBoxResult.OK)
            {
                App.StopReminderSound();
            }
        }

        private static void MarkReminderAsTriggered(int reminderId, SQLiteConnection connection)
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
            var reminderWindow = new ReminderWindow(_userEmail);
            var result = reminderWindow.ShowDialog();

            // Check if the user actually added a reminder
            if (result == true && reminderWindow.NewReminder != null)
            {
                LoadReminders(); // Refresh after adding

                // Use the actual values from the new reminder
                ScheduleReminder(reminderWindow.NewReminder.DateTime,
                                 $"{reminderWindow.NewReminder.Subject}: {reminderWindow.NewReminder.Description}");
            }
        }


        private void ScheduleReminder(DateTime reminderDateTime, string reminderText)
        {
            
            string exePath = @"C:\Users\Admin\source\repos\ReminderApp\publish\ReminderAlarmApp.exe"; //my folder path
            string time = reminderDateTime.ToString("HH:mm");
            string date = reminderDateTime.ToString("MM/dd/yyyy");
            string taskName = "ReminderApp_Alarm_" + Guid.NewGuid();

            // Properly quote the path and arguments
            string arguments = $"/Create /SC ONCE /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" \\\"{reminderText}\\\"\" /ST {time} /SD {date} /F";
            System.Diagnostics.Process.Start("schtasks.exe", arguments);
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

    }

    public class Reminder
    {
        public DateTime DateTime { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }
}