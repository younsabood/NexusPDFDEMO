using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms; 

namespace NexusPDF
{
    public class UrlContentReader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ReadContentFromUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Error: Default URL is null or empty.", "URL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                // Return the fetched content.
                return content;
            }
            catch (HttpRequestException e)
            {
                string errorMessage = $"HttpRequestException: An error occurred while making the request: {e.Message}";

                if (e.InnerException is System.Net.WebException webEx)
                {
                    if (webEx.Response is System.Net.HttpWebResponse httpResponse)
                    {
                        errorMessage += $"\nStatus Code: {httpResponse.StatusCode}";
                    }
                    else
                    {
                        errorMessage += $"\nWebException Status: {webEx.Status}";
                    }
                }
                else
                {
                    errorMessage += "\nCould not determine HTTP Status Code. It might be a network error before an HTTP response was received.";
                }

                MessageBox.Show(errorMessage, "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (UriFormatException e)
            {
                MessageBox.Show($"UriFormatException: The default URL format is invalid: {e.Message}", "URL Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An unexpected error occurred: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}