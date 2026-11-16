using System.Windows;

namespace GamingLobbyDuplexClient.Helpers
{
    public partial class InputDialog : Window
    {
        public string ResponseText
        {
            get;
            private set;
        }

        public InputDialog(string question, string title = "")
        {
            InitializeComponent();
            Title = string.IsNullOrWhiteSpace(title) ? "Input" : title;
            QuestionText.Text = question;
            AnswerTextBox.Focus();
            AnswerTextBox.SelectAll();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = AnswerTextBox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
