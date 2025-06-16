using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static NexusPDF.QuestionsOBJ;

namespace NexusPDF
{
    public static class QuestionsOBJ
    {
        public enum QuestionType
        {
            None,
            OptionQuestions,
            YesNoQuestions
        }

        public class OptionQuestion
        {
            [JsonProperty("question", Required = Required.Always)]
            public string Question { get; set; }

            [JsonProperty("answer", Required = Required.Always)]
            public string Answer { get; set; }

            [JsonProperty("options", Required = Required.Always)]
            public List<string> Options { get; set; }

            [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
            public string Source { get; set; }

            [JsonProperty("explanation", NullValueHandling = NullValueHandling.Ignore)]
            public string Explanation { get; set; }

            [JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
            public string Difficulty { get; set; }

            [JsonProperty("domain", NullValueHandling = NullValueHandling.Ignore)]
            public string Domain { get; set; }
            public string GetCorrectAnswer()
            {
                try
                {
                    // Convert Options to array first
                    string[] optionsArray = Options.ToArray();

                    // Format 1: Arabic full-text format (contains colon)
                    if (Answer.Contains(":"))
                    {
                        // Extract the text after the colon for Arabic format
                        int colonIndex = Answer.IndexOf(": ");
                        if (colonIndex >= 0)
                        {
                            return Answer.Substring(colonIndex + 2).Trim();
                        }
                    }

                    // Format 2: Pipe-separated format (contains pipe)
                    if (Answer.Contains("|"))
                    {
                        // Split the Answer by pipe and get the first option
                        string correctOptionPrefix = Answer.Split('|')[0].Trim();

                        // Extract the option letter (A, B, C, D) from the first answer
                        string correctOptionLetter = correctOptionPrefix.Replace("Option ", "").Trim();

                        // Find the matching option text
                        string correctOption = optionsArray.FirstOrDefault(opt => opt.StartsWith($"Option {correctOptionLetter}:"));

                        if (correctOption != null)
                        {
                            // Return just the text part after "Option X: "
                            int colonIndex = correctOption.IndexOf(": ");
                            if (colonIndex >= 0)
                            {
                                return correctOption.Substring(colonIndex + 2).Trim();
                            }
                        }

                        return correctOptionPrefix; // Fallback to the option prefix
                    }

                    if (Answer.StartsWith("Option "))
                    {
                        // Extract the option letter (A, B, C, D)
                        string optionLetter = Answer.Replace("Option ", "").Trim();

                        // Find the matching option text
                        string correctOption = optionsArray.FirstOrDefault(opt => opt.StartsWith($"Option {optionLetter}:"));

                        if (correctOption != null)
                        {
                            // Return just the text part after "Option X: "
                            int colonIndex = correctOption.IndexOf(": ");
                            if (colonIndex >= 0)
                            {
                                return correctOption.Substring(colonIndex + 2).Trim();
                            }
                        }
                    }

                    // If no format matches, return the answer as is
                    return Answer;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in GetCorrectAnswer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return Answer; // Fallback to the original Answer
                }
            }
        }

        public class YesNoQuestion
        {
            [JsonProperty("question", Required = Required.Always)]
            public string Question { get; set; }

            [JsonProperty("answer", Required = Required.Always)]
            public string Answer { get; set; } // "Yes" or "No"

            [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
            public string Source { get; set; }

            [JsonProperty("explanation", NullValueHandling = NullValueHandling.Ignore)]
            public string Explanation { get; set; }

            [JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
            public string Difficulty { get; set; }

            [JsonProperty("domain", NullValueHandling = NullValueHandling.Ignore)]
            public string Domain { get; set; }

            // Method to get the correct answer for Yes/No type questions
            public string GetCorrectAnswer()
            {
                return Answer; // Simply return "Yes" or "No"
            }
        }

        public class Result
        {
            public List<OptionQuestion> OptionQuestions { get; set; }
            public List<YesNoQuestion> YesNoQuestions { get; set; }
        }

        // Updated QA class with all properties
        public class QA
        {
            public string Question { get; set; }
            public List<string> Options { get; set; }
            public string CorrectAnswer { get; set; }
            public string Explanation { get; set; }
            public string Source { get; set; }
            public string Difficulty { get; set; }
            public string Domain { get; set; }
        }

        public static (Result result, QuestionType type) FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (new Result(), QuestionType.None);

            try
            {
                // Try to parse OptionQuestions
                var optionList = JsonConvert.DeserializeObject<List<OptionQuestion>>(json);
                if (optionList != null && optionList.Any(q => q.Options != null && q.Options.Count > 0))
                {
                    return (new Result { OptionQuestions = optionList }, QuestionType.OptionQuestions);
                }
            }
            catch (JsonException)
            {
                // Ignored
            }

            try
            {
                // Try to parse YesNoQuestions
                var yesNoList = JsonConvert.DeserializeObject<List<YesNoQuestion>>(json);
                if (yesNoList != null && yesNoList.Count > 0)
                {
                    return (new Result { YesNoQuestions = yesNoList }, QuestionType.YesNoQuestions);
                }
            }
            catch (JsonException)
            {
                // Ignored
            }

            return (new Result(), QuestionType.None);
        }
    }
}
