using System.Collections.ObjectModel;

namespace SnippetBar
{
    public class Category
    {
        public string Name { get; set; } = "Namnlös";

        // Är detta huvudlistan "Alla Snips"?
        public bool IsSystemCategory { get; set; } = false;

        public ObservableCollection<Snippet> Snippets { get; set; } = new ObservableCollection<Snippet>();
    }
}