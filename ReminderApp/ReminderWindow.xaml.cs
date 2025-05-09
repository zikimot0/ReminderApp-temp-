using System;
using System.Data.SQLite;
using System.Windows;

namespace ReminderApp
{
    public partial class ReminderWindow : Window
    {
        private readonly string _userEmail;

        public ReminderWindow(string userEmail)
        {
            InitializeComponent();
            _userEmail = userEmail;

            // Set default time to current time
            TimeTextBox.Text = DateTime.Now.ToString("HH:mm");
            ReminderCalendar.SelectedDate = DateTime.Today;
        }

        private void SaveReminderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(TimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SubjectTextBox.Text))
                {
                    MessageBox.Show("Time and Subject are required!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Parse time
                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan reminderTime))
                {
                    MessageBox.Show("Invalid time format. Please use HH:mm (e.g., 14:30)", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Combine date and time
                DateTime reminderDateTime = (ReminderCalendar.SelectedDate ?? DateTime.Today).Date + reminderTime;

                // Get user ID from database
                int userId = GetUserId(_userEmail);
                if (userId == -1)
                {
                    MessageBox.Show("User not found!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Save to database
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Reminders (UserId, DateTime, Subject, Description)
                        VALUES (@UserId, @DateTime, @Subject, @Description)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@DateTime", reminderDateTime.ToString("o"));
                        command.Parameters.AddWithValue("@Subject", SubjectTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@Description", DescriptionTextBox.Text.Trim());
                        command.ExecuteNonQuery();
                    }
                }

                
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving reminder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetUserId(string email)
        {
            try
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
            catch
            {
                return -1;
            }
        }
    }
}