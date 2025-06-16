using GenerativeAI.Types;

namespace NexusPDF
{
    public class UploadeData
    {
        public class MainData
        {
            public string QuestionsType { get; set; }
            public string ContentDomain { get; set; }
            public string QuestionsLanguage { get; set; }
            public int NumberOfQuestions { get; set; }
            public int DifficultyOfQuestions { get; set; }
        }

        public class Source_Document
        {
            public string SourceDocument { get; set; }
            public RemoteFile SourceDocumentRemoteFile { get; set; }
        }

        public class Example_Exam
        {
            public string SourceDocument { get; set; }
            public RemoteFile SourceDocumentRemoteFile { get; set; }
        }
    }

    public static class MainDataFactory
    {
        public static UploadeData.MainData Create(string questionsType, string contentDomain, string questionsLanguage, int numberOfQuestions, int difficultyOfQuestions)
        {
            return new UploadeData.MainData
            {
                QuestionsType = questionsType,
                ContentDomain = contentDomain,
                QuestionsLanguage = questionsLanguage,
                NumberOfQuestions = numberOfQuestions,
                DifficultyOfQuestions = difficultyOfQuestions
            };
        }
    }

    public static class SourceDocumentFactory
    {
        public static UploadeData.Source_Document Create(string sourceDocument, RemoteFile remoteFile)
        {
            return new UploadeData.Source_Document
            {
                SourceDocument = sourceDocument,
                SourceDocumentRemoteFile = remoteFile
            };
        }
    }

    public static class ExampleExamFactory
    {
        public static UploadeData.Example_Exam Create(string sourceDocument, RemoteFile remoteFile)
        {
            return new UploadeData.Example_Exam
            {
                SourceDocument = sourceDocument,
                SourceDocumentRemoteFile = remoteFile
            };
        }
    }
}
