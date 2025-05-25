﻿using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Windows;

namespace ReminderApp
{
    public partial class LoginWindow : Window
    {
        

        public LoginWindow()
        {
            DatabaseHelper.InitializeDatabase();
            InitializeComponent();
            EmailTextBox.Focus(); //literal na focus
        }

        

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            // tangalin ang previous error message
            ErrorMessage.Visibility = Visibility.Collapsed;

            // validate lang kung may laman ba o wala
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter your email address.");
                EmailTextBox.Focus();
                
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                PasswordBox.Focus();
                
                return;
            }

            

            try
            {
                // para lang sa admin
                if (email == "admin@gwapo.com" && password == "admingwapo")
                {
                    EnsureAdminExists();
                    OpenMainWindow(email);
                    return;
                }

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // kinukuha ang password ng user mula sa database n nakahash
                    string getUserQuery = "SELECT Password FROM Users WHERE Email = @Email LIMIT 1";
                    string storedHash = null;

                    using (var cmd = new SQLiteCommand(getUserQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        storedHash = cmd.ExecuteScalar()?.ToString();
                    }

                    if (storedHash == null)
                    {
                        ShowError("Account not found. Please register first.");
                        LogLoginAttempt(email, false);
                        EmailTextBox.Focus();
                        EmailTextBox.SelectAll();
                        return;
                    }

                    if (PasswordHasher.VerifyPassword(password, storedHash))
                    {
                        LogLoginAttempt(email, true);
                        OpenMainWindow(email);
                    }
                    else
                    {
                        ShowError("Invalid password. Please try again.");
                        LogLoginAttempt(email, false);
                        PasswordBox.Focus();
                        PasswordBox.SelectAll();
                    }
                }
            }
            catch (SQLiteException)
            {
                ShowError("Database connection failed. Please try again.");
            }
            catch (Exception ex)
            {
                ShowError($"An error occurred: {ex.Message}");
            }
        }

        private void EnsureAdminExists()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check lang kung buhay si admin
                    string checkAdmin = "SELECT COUNT(*) FROM Users WHERE Email = 'admin@gwapo.com'";
                    using (var cmd = new SQLiteCommand(checkAdmin, connection))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            // hashed kasi astig
                            string insertAdmin = "INSERT INTO Users (Email, Password) VALUES ('admin@gwapo.com', @Password)";
                            using (var insertCmd = new SQLiteCommand(insertAdmin, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@Password",
                                    PasswordHasher.HashPassword("admingwapo"));
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                // wala, edi magfafail sya na ko alam basta biglaan or silent
            }
        }

        private void OpenMainWindow(string email)
        {
            var mainWindow = new MainWindow(email);
            mainWindow.Show();
            this.Close();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();


        }

        private void LogLoginAttempt(string email, bool successful)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO LoginLogs (Email, IsSuccessful) VALUES (@Email, @Success)";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Success", successful ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}