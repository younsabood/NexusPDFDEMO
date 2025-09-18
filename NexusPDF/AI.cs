using System;
using System.Threading;
using System.Threading.Tasks;
using GenerativeAI.Core;
using GenerativeAI.Types;
using GenerativeAI;
using System.Windows.Forms;
using System.Web;

namespace NexusPDF
{
    public static class AI
    {
        public static decimal counter;
        public static string AIModels;
        public static GeminiModel GeminiModel;
        public static string LanguageAI;
        public static string Template;
        private static CancellationTokenSource _cancellationTokenSource;
        private static CancellationToken _cancellationToken;
        private static int _operationId = 0;

        public static GeminiModel CreateGeminiModel(string AIModel)
        {
            var modelParams = GoogleAIModel.GetModelFromName(AIModel);
            return new GeminiModel(Properties.Settings.Default.googleAI, modelParams);
        }


        public static CancellationToken CancellationToken => _cancellationToken;

        public static bool IsOperationRunning =>
            _cancellationTokenSource != null &&
            !_cancellationTokenSource.Token.IsCancellationRequested;

        public static void InitializeCancellationToken()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _operationId++;
        }

        public static void CancelOperation()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public static async Task<string> GenerateQAContentAsync(RemoteFile file, GenerationQAParameters parameters)
        {
            var request = new GenerateContentRequest();
            request.AddText(BuildQAPrompt(parameters, file.DisplayName));
            request.AddRemoteFile(file);

            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var response = await GeminiModel.GenerateContentAsync(request, _cancellationToken);
                return response.Text;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        public static async Task<string> GenerateQAContentAsync(RemoteFile file1, RemoteFile file2, GenerationQAParameters parameters)
        {
            var request = new GenerateContentRequest();
            request.AddText(BuildQAPrompt(parameters, file1.DisplayName, file2.DisplayName));
            request.AddRemoteFile(file1);
            request.AddRemoteFile(file2);

            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var response = await GeminiModel.GenerateContentAsync(request, _cancellationToken);
                return response.Text;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        private static string BuildQAPrompt(GenerationQAParameters parameters, string sourceDocument, string exampleExam = null)
        {
            LanguageAI = parameters.Language;
            string template = GetTemplate(parameters.Type, exampleExam != null);
            if (parameters.Type == "Flash Card")
            {
                template = template
                    .Replace(PromptTemplates.LanguagePlaceholder, parameters.Language)
                    .Replace(PromptTemplates.SourceDocument, sourceDocument)
                    .Replace(PromptTemplates.ContentDomainPlaceholder, parameters.ContentDomain);
                return template;
            }
            template = template
                .Replace(PromptTemplates.DifficultyPlaceholder, parameters.Difficulty.ToString())
                .Replace(PromptTemplates.LanguagePlaceholder, parameters.Language)
                .Replace(PromptTemplates.SourceDocument, sourceDocument)
                .Replace(PromptTemplates.ContentDomainPlaceholder, parameters.ContentDomain)
                .Replace(PromptTemplates.ExampleExam, exampleExam ?? string.Empty);
            return template;
        }

        private static string GetTemplate(string type, bool hasExample)
        {
            Template = type;
            switch (type)
            {
                case "Yes No Questions":
                    return PromptTemplates.YesNoTemplate;
                case "Flash Card":
                    return PromptTemplates.FLASHCARDS;
                case "Regular Multiple Choice":
                    return hasExample ? PromptTemplates.OptionsTemplateTwoPDF : PromptTemplates.OptionsTemplateOnePDF;
                case "Math Multiple Choice":
                    return hasExample ? PromptTemplates.MathOptionsTemplateTwoPDF : PromptTemplates.MathOptionsTemplateOnePDF;
                case "Programming Multiple Choice":
                    return hasExample ? PromptTemplates.ProgrammingOptionsTemplateTwoPDF : PromptTemplates.ProgrammingOptionsTemplateOnePDF;
                default:
                    throw new ArgumentException("Invalid question type");
            }
        }

        public static void Cleanup()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}