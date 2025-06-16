using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NexusPDF
{
    public class FlashCardOBJ
    {
        // Define the FlashCard class with necessary properties
        public class FlashCard
        {
            [JsonProperty("question", Required = Required.Always)]
            public string Question { get; set; }

            [JsonProperty("explanation", NullValueHandling = NullValueHandling.Ignore)]
            public string Explanation { get; set; }

            [JsonProperty("citation", NullValueHandling = NullValueHandling.Ignore)]
            public string Citation { get; set; }

            [JsonProperty("verbatim", NullValueHandling = NullValueHandling.Ignore)]
            public string Verbatim { get; set; }

            [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
            public string Subject { get; set; }
        }

        public class Result
        {
            public List<FlashCard> FlashCards { get; set; }
        }

        // Method to parse the JSON and extract the flashcards
        public static Result FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Result { FlashCards = new List<FlashCard>() };

            try
            {
                // Try to parse the flashcards
                var flashCardList = JsonConvert.DeserializeObject<List<FlashCard>>(json);
                if (flashCardList != null && flashCardList.Count > 0)
                {
                    return new Result { FlashCards = flashCardList };
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error parsing flashcards: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return new Result { FlashCards = new List<FlashCard>() };
        }
    }
}
