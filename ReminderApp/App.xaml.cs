using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows;

namespace ReminderApp
{
    public partial class App : Application
    {
        public static void PlayReminderSound()
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
                    // Log that the alarm.wav file is missing
                    Console.WriteLine("alarm.wav file not found. Playing Mario Theme as fallback.");
                    PlayMarioTheme();
                }
            }
            catch (Exception ex)
            {
                // Log the error and play the Mario theme as a fallback
                Console.WriteLine($"Error playing sound: {ex.Message}. Playing Mario Theme as fallback.");
                PlayMarioTheme();
            }
        }

        public static void PlayMarioTheme()
        {
            try
            {
                // Debugging: Confirm the method is called
                Console.WriteLine("Playing Mario Theme...");

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
                        Thread.Sleep(durations[i]);
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
                // Handle errors during Mario Theme playback
                Console.WriteLine($"Error playing Mario Theme: {ex.Message}");
                MessageBox.Show($"Error playing Mario Theme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
