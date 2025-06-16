using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerativeAI.Core;
using GenerativeAI;

namespace NexusPDF
{
    public class GoogleAIModel
    {
        /*
        Gemini 2 Flash Latest
        Gemini 2 Flash Thinking
        Gemmma 3 27B
        Gemini 2.5 Flash Preview
        Gemini 2.5 Pro Preview
        Gemini 2 Flash
        Gemini 2 Flash Lite Preview
        Gemini 1.5 Flash
        Gemini 1.5 Flash 8B Latest
        */
        public static ModelParams GetModelFromName(string modelName)
        {
            switch (modelName)
            {
                case "Gemini 2 Flash Latest":
                    return new ModelParams { Model = GoogleAIModels.Gemini2FlashLatest };
                case "Gemini 2 Flash Thinking":
                    return new ModelParams { Model = GoogleAIModels.Gemini2FlashThinkingExp0121 };
                case "Gemmma 3 27B":
                    return new ModelParams { Model = GoogleAIModels.Gemmma3_27B };
                case "Gemini 2.5 Flash Preview":
                    return new ModelParams { Model = GoogleAIModels.Gemini25FlashPreview0417 };
                case "Gemini 2.5 Pro Preview":
                    return new ModelParams { Model = GoogleAIModels.Gemini25ProPreview0520 };
                case "Gemini 2 Flash":
                    return new ModelParams { Model = GoogleAIModels.Gemini2Flash };
                case "Gemini 2 Flash Lite Preview":
                    return new ModelParams { Model = GoogleAIModels.Gemini2FlashLitePreview };
                case "Gemini 1.5 Flash":
                    return new ModelParams { Model = GoogleAIModels.Gemini15Flash };
                case "Gemini 1.5 Flash 8B Latest":
                    return new ModelParams { Model = GoogleAIModels.Gemini15Flash8BLatest };
                default:
                    throw new ArgumentException($"Model name '{modelName}' is not valid.");
            }
        }
    }
}
