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
                // Fetch all users from the database
                List<User> users = DatabaseHelper.GetAllUsers();

                // Bind the users to the ListBox
                UsersListBox.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected user
            User selectedUser = (User)UsersListBox.SelectedItem;

            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user to delete.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete user '{selectedUser.Email}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete the user from the database
                    DatabaseHelper.DeleteUser(selectedUser.Id);

                    // Reload the user list
                    LoadUsers();

                    MessageBox.Show("User deleted successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}