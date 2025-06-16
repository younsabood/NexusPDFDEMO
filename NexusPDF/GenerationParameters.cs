using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPDF
{
    public class GenerationQAParameters
    {
        public string Language { get; set; }
        public string Type { get; set; }
        public int Difficulty { get; set; }
        public string ContentDomain { get; set; }
    }
    public class GenerationFlashcardParameters
    {
        public string Language { get; set; }
        public string ContentDomain { get; set; }
    }
}
