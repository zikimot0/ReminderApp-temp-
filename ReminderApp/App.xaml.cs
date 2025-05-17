using System;
using System.IO;
using System.Media;
using System.Reflection;

namespace ReminderApp
{
    public partial class App
    {
        private static SoundPlayer? _player;

        //for custom alarm sound
        private static readonly string AlarmsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ReminderApp",
            "CustomAlarms"
        );

        static App()
        {
            Directory.CreateDirectory(AlarmsDirectory);
        }

        // In case you need to display available alarms.
        public static string[] AvailableAlarms => Directory.GetFiles(AlarmsDirectory, "*.wav");


        public static void PlayReminderSound(string? selectedAlarm = null)
        {
            try
            {
                // If a custom alarm is selected, play it
                if (!string.IsNullOrEmpty(selectedAlarm) && File.Exists(selectedAlarm))
                {
                    _player = new SoundPlayer(selectedAlarm);
                    _player.PlayLooping();
                }
                else
                {

                    //built in alarm sound
                    var assembly = Assembly.GetExecutingAssembly();
                    const string resourceName = "ReminderApp.Assets.lofi-alarm-clock.wav";
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        _player = new SoundPlayer(stream);
                        _player.PlayLooping();
                    }
                    else
                    {
                        Console.WriteLine("Default alarm sound not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound: {ex.Message}.");
            }
        }


        public static void StopReminderSound()
        {
            _player?.Stop();
            _player = null;
        }

    }
}
