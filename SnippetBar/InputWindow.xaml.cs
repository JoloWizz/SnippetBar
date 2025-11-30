using System.Windows;

namespace SnippetBar
{
    public partial class InputWindow : Window
    {
        // FIX: Sätt ett startvärde
        public string Answer { get; private set; } = string.Empty;

        public InputWindow(string defaultText = "")
        {
            InitializeComponent();
            txtInput.Text = defaultText;
            txtInput.Focus();
            txtInput.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Answer = txtInput.Text;
            DialogResult = true;
        }
    }
}