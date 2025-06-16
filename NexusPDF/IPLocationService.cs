using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NexusPDF
{
    public static class IPLocationService
    {
        // Method to get the public IP address
        public static async Task<string> GetPublicIpAddress()
        {
            string publicIp = string.Empty;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Requesting the public IP from ipify API
                    publicIp = await client.GetStringAsync("https://api.ipify.org");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving public IP: " + ex.Message);
                }
            }

            return publicIp;
        }

        // Method to get country information based on the public IP Address
        public static async Task<string> GetCountryByIp(string ip)
        {
            string country = string.Empty;

            // Using ip-api.com API
            string apiUrl = $"http://ip-api.com/json/{ip}"; // ip-api.com API

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Making the request to the API
                    var response = await client.GetStringAsync(apiUrl);

                    // Parse the JSON response
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response);

                    // Get the country from the response
                    country = jsonResponse.country;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving country info: " + ex.Message);
                }
            }

            return country;
        }

        // Method to check if the country is banned
        public static bool IsBannedCountry(string country)
        {
            // List of banned countries
            string[] bannedCountries = { "Crimea", "Cuba", "Iran", "North Korea", "Syria" };

            if (string.IsNullOrEmpty(country))
            {
                return true; // Consider empty country as banned
            }

            // Check if the country is in the banned list
            foreach (string banned in bannedCountries)
            {
                if (country.Equals(banned, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsInternetAvailable()
        {
            try
            {
                // Ping a reliable server like google.com to check for internet connectivity
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 1000); // Google's public DNS server
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                // If there's an exception, it means the internet is not available
                return false;
            }
        }
        // Combined method that returns a tuple with publicIp, country, and isBanned status
        public static async Task<(string publicIp, string country, bool isBanned)> GetIpCountryAndBannedStatus()
        {
            string publicIp = string.Empty;
            string country = string.Empty;
            bool isBanned = false;

            try
            {
                // Get the public IP address
                publicIp = await GetPublicIpAddress();

                // Get the country by public IP address
                country = await GetCountryByIp(publicIp);

                // Check if the country is banned
                isBanned = IsBannedCountry(country);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            // Return the tuple with publicIp, country, and isBanned status
            return (publicIp, country, isBanned);
        }
    }

}
