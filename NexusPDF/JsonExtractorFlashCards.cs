using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NexusPDF
{
    public static class JsonExtractorFlashCards
    {
        private static readonly Regex CodeBlockRegex = new Regex(
            @"^\s*```(?:json)?\s*|```\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public static string ExtractAndFormatJson(string rawText, List<string> requiredFields = null)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return "[]";

            // Use default fields if none provided or empty
            if (requiredFields == null || requiredFields.Count == 0)
            {
                requiredFields = new List<string>
                {
                    "question", "explanation", "citation", "verbatim", "subject"
                };
            }

            string cleanedText = CleanCodeBlockMarkers(rawText);

            var validBlocks = ExtractValidJsonBlocks(cleanedText, requiredFields);

            if (validBlocks.Count == 0)
                return "[]";

            return JsonConvert.SerializeObject(validBlocks, Formatting.Indented);
        }

        private static string CleanCodeBlockMarkers(string text)
        {
            string result = CodeBlockRegex.Replace(text, "").Trim();
            result = result.Replace("\0", "");

            return result;
        }

        private static List<JObject> ExtractValidJsonBlocks(string jsonText, List<string> requiredFields)
        {
            var validBlocks = new List<JObject>();

            if (string.IsNullOrWhiteSpace(jsonText))
                return validBlocks;

            try
            {
                var jArray = JArray.Parse(jsonText);
                foreach (var token in jArray)
                {
                    if (token is JObject obj && IsValidObject(obj, requiredFields))
                    {
                        validBlocks.Add(obj);
                    }
                }
                return validBlocks;
            }
            catch (JsonReaderException)
            {
                // Not an array, fallback to individual object parsing
            }

            foreach (var objStr in SplitJsonObjects(jsonText))
            {
                if (TryParseSingleObject(objStr, requiredFields, out JObject obj))
                {
                    validBlocks.Add(obj);
                }
            }

            return validBlocks;
        }

        private static bool TryParseSingleObject(string objStr, List<string> requiredFields, out JObject obj)
        {
            obj = null;
            try
            {
                obj = JObject.Parse(objStr);
                if (IsValidObject(obj, requiredFields))
                    return true;
            }
            catch (JsonReaderException)
            {
                // Parsing failed, ignore this block
            }
            obj = null;
            return false;
        }

        private static bool IsValidObject(JObject obj, List<string> requiredFields)
        {
            foreach (var field in requiredFields)
            {
                if (!obj.TryGetValue(field, out var token) || string.IsNullOrWhiteSpace(token?.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<string> SplitJsonObjects(string jsonText)
        {
            var objects = new List<string>();
            int braceCount = 0;
            int objStart = -1;
            var sb = new StringBuilder();

            for (int i = 0; i < jsonText.Length; i++)
            {
                char c = jsonText[i];

                if (c == '{')
                {
                    if (braceCount == 0)
                    {
                        objStart = i;
                        sb.Clear();
                    }
                    braceCount++;
                }

                if (braceCount > 0)
                {
                    sb.Append(c);
                }

                if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0 && objStart != -1)
                    {
                        objects.Add(sb.ToString());
                        objStart = -1;
                        sb.Clear();
                    }
                }
            }

            return objects;
        }
    }
}
