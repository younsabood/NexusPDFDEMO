using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using GenerativeAI.Types;
using static NexusPDF.QuestionsOBJ;
using System.Drawing;

namespace NexusPDF
{
    public partial class FlashCards : Form
    {
        private FlashCard FlashCard;
        private List<FlashCardOBJ.FlashCard> Cards;
        private FlashCardOBJ.FlashCard Card;
        private int currentCardIndex = 0;
        private string PDFName;
        public FlashCards(string formattedJson, string pdfname)
        {
            PDFName = pdfname;
            InitializeComponent();
            var result = FlashCardOBJ.FromJson(formattedJson);
            if (result.FlashCards != null && result.FlashCards.Count > 0)
            {
                Cards = result.FlashCards;
                label2.Text = Cards.Count.ToString();
                LoadQuestion(currentCardIndex);
            }
        }

        private void LoadQuestion(int index)
        {
            if (index < 0 || index >= Cards.Count)
                return;

            Card = Cards[index];
            exam.Controls.Clear();
            InitializeComponents(
                Card.Question,
                Card.Explanation,
                Card.Citation,
                Card.Verbatim,
                Card.Subject);
        }

        private void NextQA_Click(object sender, EventArgs e)
        {
            if (Cards == null || Cards.Count == 0)
            {
                return;
            }
            ;
            if (currentCardIndex < Cards.Count - 1)
            {
                currentCardIndex++;
                NextQA.Text = $"Next Flash Cards ({currentCardIndex + 1})";
                LoadQuestion(currentCardIndex);
            }
            else
            {
                NextQA.Text = "End Flash Cards";
            }
        }

        private void InitializeComponents(string question, string Explanation, string Citation, string Verbatim, string Subject)
        {
            try
            {
                FlashCard = new FlashCard
                {
                    QuestionText = question,
                    Explanation = Explanation,
                    Source = Citation,
                    Subject = Subject,
                    Verbatim = Verbatim,
                    PdfName = PDFName
                };
                FlashCard.Dock = DockStyle.Top;
                exam.Controls.Add(FlashCard);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while setting up the question components: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
    }
}
