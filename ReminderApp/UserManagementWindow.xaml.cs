using System;
using System.Data.SQLite;
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
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // First check if CreatedAt column exists
                    bool hasCreatedAt = false;
                    string checkColumn = "PRAGMA table_info(Users)";
                    using (var cmd = new SQLiteCommand(checkColumn, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["name"].ToString() == "CreatedAt")
                                {
                                    hasCreatedAt = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Build query based on available columns
                    string query = hasCreatedAt
                        ? "SELECT Email, CreatedAt FROM Users WHERE Email != 'admin@gwapo.com'"
                        : "SELECT Email, 'Unknown' AS CreatedAt FROM Users WHERE Email != 'admin@gwapo.com'";

                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        var dt = new System.Data.DataTable();
                        adapter.Fill(dt);
                        UsersDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a user to delete", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (System.Data.DataRowView)UsersDataGrid.SelectedItem;
            string email = selectedRow["Email"].ToString();

            var result = MessageBox.Show($"Delete user '{email}' and all their reminders?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        string deleteQuery = "DELETE FROM Users WHERE Email = @Email";
                        using (var cmd = new SQLiteCommand(deleteQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@Email", email);
                            int affected = cmd.ExecuteNonQuery();

                            if (affected > 0)
                            {
                                MessageBox.Show("User deleted successfully", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadUsers(); // Refresh the list
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Deletion failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}