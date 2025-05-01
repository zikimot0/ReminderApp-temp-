using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Media; // For playing sound
using System.Timers;
using System.Windows;

namespace ReminderApp
{
    public partial class App : Application
    {
        private Timer _reminderTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseHelper.InitializeDatabase();
            base.OnStartup(e);

            // Initialize the reminder timer to check every 60 seconds
            _reminderTimer = new Timer(60000);
            _reminderTimer.Elapsed += CheckReminders;
            _reminderTimer.Start();
        }

        private void CheckReminders(object sender, ElapsedEventArgs e)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT r.DateTime, r.Subject, u.Email
                FROM Reminders r
                JOIN Users u ON r.UserId = u.Id
                WHERE r.DateTime BETWEEN @Now AND @OneMinuteLater";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("o"));
                        command.Parameters.AddWithValue("@OneMinuteLater", DateTime.Now.AddMinutes(1).ToString("o"));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string subject = reader["Subject"].ToString();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    PlayReminderSound();
                                    MessageBox.Show($"Reminder: {subject}", "Reminder", MessageBoxButton.OK, MessageBoxImage.Information);
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking reminders: {ex.Message}");
            }
        }

        private void PlayReminderSound()
        {
            try
            {
                // Use the System.Media.SoundPlayer to play a custom sound
                string soundPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "alarm.wav" // Replace with your custom sound file
                );

                if (File.Exists(soundPath))
                {
                    SoundPlayer player = new SoundPlayer(soundPath);
                    player.Play(); // Play the sound asynchronously
                }
                else
                {
                    // Fallback to system beep if no sound file is found
                    PlayMarioTheme();
                }
            }
            catch (Exception ex)
            {
                // Handle sound-playing errors gracefully
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error playing sound: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void PlayMarioTheme()
        {
            try
            {
                // Mario Theme melody using Console.Beep
                int[] notes = {
                    659, 659, 0, 659, 0, 523, 659, 0, 784, 0, 0, 0, 392, 0, 0, 0, // First phrase
                    523, 0, 392, 0, 330, 0, 440, 494, 466, 440, 0, 392, 659, 784, 880, 0, // Second phrase
                    698, 784, 0, 659, 523, 587, 494, 0, 523, 392, 330, 440, 494, 466, 440 // Third phrase
                };

                int[] durations = {
                    125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, // First phrase
                    125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, // Second phrase
                    125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125, 125 // Third phrase
                };

                // Play the Mario Theme
                for (int i = 0; i < notes.Length; i++)
                {
                    if (notes[i] == 0)
                    {
                        // Rest (silence)
                        System.Threading.Thread.Sleep(durations[i]);
                    }
                    else
                    {
                        // Play the note
                        Console.Beep(notes[i], durations[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle errors during Mario Theme playback (optional)
                Console.WriteLine($"Error playing Mario Theme: {ex.Message}");
            }
        }
    }
}