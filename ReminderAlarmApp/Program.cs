using System;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Reflection;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string message = args.Length > 0 ? args[0] : "Reminder!";

        // Play the same sound as ReminderWindow
        PlayReminderSound();

        MessageBox.Show(message, "Reminder", MessageBoxButtons.OK, MessageBoxIcon.Information);

        StopReminderSound();
    }

    private static SoundPlayer? _player;

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
                const string resourceName = "ReminderAlarmApp.Assets.lofi-alarm-clock.wav";
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
