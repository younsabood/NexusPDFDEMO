using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GenerativeAI.Types;
using static NexusPDF.QuestionsOBJ;

namespace NexusPDF
{
    public partial class QAShow : Form
    {
        private HtmlMultipleChoice htmlQuestion;
        private HtmlMultipleChoiceCode htmlQuestionCode;
        private HtmlMultipleChoiceMath htmlQuestionMath;

        private List<OptionQuestion> optionQuestions;
        private List<YesNoQuestion> YesNoQuestions;
        private int currentQuestionIndex = 0;
        DateTime dateTime = DateTime.MinValue;

        public QAShow(string formattedJson)
        {
            InitializeComponent();
            var (result, type) = QuestionsOBJ.FromJson(formattedJson);
            if (result.OptionQuestions != null && result.OptionQuestions.Count > 0)
            {
                optionQuestions = result.OptionQuestions;
                label2.Text += optionQuestions.Count.ToString();
                dateTime = DateTime.Now.AddMinutes(optionQuestions.Count * 2.5);
                LoadQuestion(currentQuestionIndex);
            }
            if (result.YesNoQuestions != null && result.YesNoQuestions.Count > 0)
            {
                YesNoQuestions = result.YesNoQuestions;
                label2.Text += YesNoQuestions.Count.ToString();
                dateTime = DateTime.Now.AddMinutes(YesNoQuestions.Count * 2.5);
                LoadYesNo(currentQuestionIndex);
            }
        }
        private void LoadYesNo(int index)
        {
            if (index < 0 || index >= YesNoQuestions.Count)
                return;

            List<string> options = new List<string> { "Yes", "No" };
            var question = YesNoQuestions[index];
            exam.Controls.Clear();

            InitializeYesNoComponents(
                question.Question,
                question.Answer,
                options,
                question.Explanation,
                question.Difficulty,
                question.Source,
                YesNoQuestions.Count); // Ensure QAcount is passed correctly
        }
        private void LoadQuestion(int index)
        {
            if (index < 0 || index >= optionQuestions.Count)
                return;

            var question = optionQuestions[index];
            exam.Controls.Clear(); // Clear previous question(s)
            InitializeQAComponents(
                question.Question,
                question.GetCorrectAnswer(),
                question.Options,
                question.Explanation,
                question.Difficulty,
                question.Source,
                optionQuestions.Count);
        }

        private void NextQA_Click(object sender, EventArgs e)
        {
            float Degre = (float)AI.counter;
            string Degretxt = "Your Degre : " + Degre.ToString("F2");
            string DegreWF = "";
            if (Degre >= 60) DegreWF = "You Pass The Exam";
            if (Degre < 60) DegreWF = "You Did Not Pass The Exam";
            if (optionQuestions != null)
            {
                if (currentQuestionIndex < optionQuestions.Count - 1)
                {
                    currentQuestionIndex++;
                    NextQA.Text = $"Next QA ({currentQuestionIndex + 1})";
                    LoadQuestion(currentQuestionIndex);
                }
                else
                {
                    NextQA.Text = "End Exam";
                    MessageBox.Show(Degretxt, DegreWF, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AI.counter = 0;
                    this.Close();
                }
            }
            else if (YesNoQuestions != null)
            {
                if (currentQuestionIndex < YesNoQuestions.Count - 1)
                {
                    currentQuestionIndex++;
                    NextQA.Text = $"Next QA ({currentQuestionIndex + 1})";
                    LoadYesNo(currentQuestionIndex);
                }
                else
                {
                    NextQA.Text = "End Exam";
                    MessageBox.Show(Degretxt, DegreWF, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AI.counter = 0;
                    this.Close();
                }
            }
        }

        private void InitializeQAComponents(string question, string correctAnswer, List<string> options, string Explanation, string Difficulty, string Source, decimal QAcount)
        {
            try
            {
                int opts = 4;
                // Parse correct answer
                string correctOptionPrefix = correctAnswer.Split('|')[0].Trim();
                string correctOptionLetter = correctOptionPrefix.Replace("Option ", "").Trim();
                string correctOption = options.Find(opt => opt.StartsWith($"Option {correctOptionLetter}:")) ?? "";

                // Process options for display
                var processedOptions = new List<string>();
                foreach (var option in options)
                {
                    int optionColonIndex = option.IndexOf(": ");
                    processedOptions.Add(optionColonIndex >= 0
                        ? option.Substring(optionColonIndex + 2).Trim()
                        : option.Trim());
                }

                if(AI.Template == "Math Multiple Choice")
                {
                    MessageBox.Show(correctAnswer);
                    // Initialize HTML question component
                    HtmlMultipleChoiceMath.numOfOption = opts;
                    htmlQuestionMath = new HtmlMultipleChoiceMath
                    {
                        QuestionText = question,
                        Options = processedOptions.ToArray(),
                        CorrectAnswer = correctAnswer,
                        Explanation = Explanation,
                        Difficulty = Difficulty,
                        Source = Source
                    };
                    htmlQuestionMath.QAencrement = (100) / (QAcount);
                    htmlQuestionMath.Dock = DockStyle.Top;
                    exam.Controls.Add(htmlQuestionMath);
                }
                else if(AI.Template == "Programming Multiple Choice")
                {
                    // Initialize HTML question component
                    HtmlMultipleChoiceCode.numOfOption = opts;
                    htmlQuestionCode = new HtmlMultipleChoiceCode
                    {
                        QuestionText = question,
                        Options = processedOptions.ToArray(),
                        CorrectAnswer = correctAnswer,
                        Explanation = Explanation,
                        Difficulty = Difficulty,
                        Source = Source
                    };
                    htmlQuestionCode.QAencrement = (100) / (QAcount);
                    htmlQuestionCode.Dock = DockStyle.Top;
                    exam.Controls.Add(htmlQuestionCode);
                }
                else
                {
                    // Initialize HTML question component
                    HtmlMultipleChoice.numOfOption = opts;
                    htmlQuestion = new HtmlMultipleChoice
                    {
                        QuestionText = question,
                        Options = processedOptions.ToArray(),
                        CorrectAnswer = correctAnswer,
                        Explanation = Explanation,
                        Difficulty = Difficulty,
                        Source = Source
                    };
                    htmlQuestion.QAencrement = (100) / (QAcount);
                    htmlQuestion.Dock = DockStyle.Top;
                    exam.Controls.Add(htmlQuestion);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while setting up the question components: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeYesNoComponents(string question, string correctAnswer, List<string> options, string Explanation, string Difficulty, string Source, decimal QAcount)
        {
            try
            {
                int opts = 2;
                // Ensure QAcount is not zero to prevent division by zero error
                if (QAcount == 0)
                {
                    throw new ArgumentException("QAcount cannot be zero.");
                }
                HtmlMultipleChoice.numOfOption = opts;
                htmlQuestion = new HtmlMultipleChoice
                {
                    QuestionText = question,
                    Options = options.ToArray(),
                    CorrectAnswer = correctAnswer,
                    Explanation = Explanation,
                    Difficulty = Difficulty,
                    Source = Source,
                };

                htmlQuestion.QAencrement = (100) / (QAcount);
                htmlQuestion.Dock = DockStyle.Top;
                exam.Controls.Add(htmlQuestion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while setting up the question components: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void QAShow_Shown(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            float Degre = (float)AI.counter;
            label1.Text = "Your Degre : " + Degre.ToString("F2");
            if (AI.counter == 100)
            {
                label1.Text = "Your Degre : " + AI.counter;
            }

            TimeSpan remainingTime = dateTime - DateTime.Now;
            this.Text = "New Exam Time Left : " + remainingTime.ToString(@"hh\:mm\:ss");

            if (dateTime <= DateTime.Now)
            {
                timer1.Stop();
                this.Text = "New Exam Time Left : 00:00:00";
                MessageBox.Show("Time's up! Your Degre : " + AI.counter);
                this.Close();
                return;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}
