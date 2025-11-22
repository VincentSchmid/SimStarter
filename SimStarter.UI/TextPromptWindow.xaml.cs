using System.Windows;

namespace SimStarter.UI
{
    public partial class TextPromptWindow : Window
    {
        public string Value => InputBox.Text;

        public TextPromptWindow(string prompt, string? initialValue = null)
        {
            InitializeComponent();
            PromptLabel.Text = prompt;
            InputBox.Text = initialValue ?? string.Empty;
            InputBox.SelectAll();
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
