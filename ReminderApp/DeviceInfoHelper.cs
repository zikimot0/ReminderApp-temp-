using System;
using System.Net;

public static class DeviceInfoHelper
{
    // Method to get the device name
    public static string GetDeviceName()
    {
        return Environment.MachineName;
    }

    // Method to get the IP address
    public static string GetIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Handle errors gracefully
        }
        return "Unknown";
    }
}