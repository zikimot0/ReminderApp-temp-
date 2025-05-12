using System;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace ReminderApp
{
    public partial class RegisterWindow : Window
    {

        // Regular expression for email validation
        private readonly Regex _validEmailRegex = new Regex(
            @"^(?!(admin@gwapo\.com)$)[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com|outlook\.com|hotmail\.com|protonmail\.com|icloud\.com|aol\.com|mail\.com|yandex\.com|zoho\.com|\w+\.\w+)$",
            RegexOptions.IgnoreCase);

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            ErrorMessage.Text = string.Empty;
            ErrorMessage.Visibility = Visibility.Collapsed;
        }


        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            // Validate email   
            if (string.IsNullOrWhiteSpace(email) )
            {
                ShowError("Please Enter Your Email.");
                
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please Enter Your Password.");
                
                return;
            }

            
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long.");
                
                return;
            }

            // Email format validation (only for non-admin accounts)
            if (!email.Equals("admin@gwapo.com", StringComparison.OrdinalIgnoreCase) && !_validEmailRegex.IsMatch(email))

            {
                ShowError("Please use a valid email address from common providers.");
                EmailTextBox.Focus();
                EmailTextBox.SelectAll();
                return;
            }

            try
            {
                // Hash the password
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