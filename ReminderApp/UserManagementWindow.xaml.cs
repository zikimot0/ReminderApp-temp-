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

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            // pindutin ung user para patayin
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
                    // patayn natin ang user sa database
                    DatabaseHelper.DeleteUser(selectedUser.Id);

                    // reload natin para kunwari walang nangyari
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