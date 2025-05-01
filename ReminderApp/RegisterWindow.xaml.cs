using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ReminderApp
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }




        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Email and Password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string hashedPassword = PasswordHasher.HashPassword(password);

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check if user exists
                    string checkUser = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    using (var cmd = new SQLiteCommand(checkUser, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show("Email already registered", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Insert new user
                    string insertUser = "INSERT INTO Users (Email, Password) VALUES (@Email, @Password)";
                    using (var cmd = new SQLiteCommand(insertUser, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                new LoginWindow().Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}