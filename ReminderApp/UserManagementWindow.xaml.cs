using System;
using System.Collections.Generic;
using System.Windows;

namespace ReminderApp
{
    public partial class UserManagementWindow : Window
    {
        public UserManagementWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                // kunin ang mga tao sa database
                List<User> users = DatabaseHelper.GetAllUsers();

                // bind natin sa ListBox
                UsersListBox.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
    }
}