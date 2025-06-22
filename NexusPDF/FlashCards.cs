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
        private static readonly SqlHelper SqlHelper = new SqlHelper(Properties.Settings.Default.ConnectionString);
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

        private async void NextQA_Click(object sender, EventArgs e)
        {
            if (Cards == null || Cards.Count == 0)
            {
                return;
            }
            ;
            await InsertFlashCardAsync(Cards[currentCardIndex]);
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

        private void Correct_CheckedChanged(object sender, EventArgs e)
        {
            if (!Correct.Checked)
            {
                Correct.Image = Properties.Resources.icons8_exit_50__1_;
            }
        }

        public async Task InsertFlashCardAsync(FlashCardOBJ.FlashCard card)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PDFName))
                    throw new ArgumentException("PDF Name cannot be null or empty.", nameof(PDFName));
                if (card == null)
                    throw new ArgumentNullException(nameof(card), "FlashCard object cannot be null.");
                if (string.IsNullOrWhiteSpace(card.Question))
                    throw new ArgumentException("Question in FlashCard object cannot be null or empty.", nameof(card.Question));

                const string query = @"
                    INSERT INTO [PDF].[FlashCards] (
                        [pdf_name],
                        [question],
                        [explanation],
                        [citation],
                        [verbatim],
                        [subject],
                        [remembered]
                    )
                    VALUES (
                        @pdf_name,
                        @question,
                        @explanation,
                        @citation,
                        @verbatim,
                        @subject,
                        @remembered
                    );
                    SELECT SCOPE_IDENTITY();";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@pdf_name", SqlDbType.NVarChar, 255) { Value = PDFName },
                    new SqlParameter("@question", SqlDbType.NVarChar) { Value = card.Question },
                    new SqlParameter("@explanation", SqlDbType.NVarChar) { Value = (object)card.Explanation ?? DBNull.Value },
                    new SqlParameter("@citation", SqlDbType.NVarChar) { Value = (object)card.Citation ?? DBNull.Value },
                    new SqlParameter("@verbatim", SqlDbType.NVarChar) { Value = (object)card.Verbatim ?? DBNull.Value },
                    new SqlParameter("@subject", SqlDbType.NVarChar, 255) { Value = (object)card.Subject ?? DBNull.Value },
                    new SqlParameter("@remembered", SqlDbType.Bit) { Value = Correct.Checked }
                };
                int x = await SqlHelper.ExecuteNonQueryAsync(query, parameters);
                Correct.Image = Properties.Resources.icons8_x_501;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inserting FlashCard into database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
