using System.Windows;
using System.Windows.Controls;

namespace SnippetBar
{
    public partial class AddSnippetWindow : Window
    {
        public Snippet SnippetData { get; private set; }

        public AddSnippetWindow(Snippet existingSnippet = null)
        {
            InitializeComponent();

            if (existingSnippet != null)
            {
                // Redigera-läge
                SnippetData = new Snippet
                {
                    Title = existingSnippet.Title,
                    Content = existingSnippet.Content,
                    Color = existingSnippet.Color
                };
                txtTitle.Text = SnippetData.Title;
                txtContent.Text = SnippetData.Content;

                // Försök hitta rätt färg i listan
                foreach (ComboBoxItem item in cbColor.Items)
                {
                    if (item.Tag.ToString() == SnippetData.Color)
                    {
                        cbColor.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                // Ny-läge
                SnippetData = new Snippet();
                txtTitle.Focus();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SnippetData.Title = txtTitle.Text;
            SnippetData.Content = txtContent.Text;

            if (cbColor.SelectedItem is ComboBoxItem item)
            {
                SnippetData.Color = item.Tag.ToString();
            }

            DialogResult = true;
            Close();
        }
    }
}