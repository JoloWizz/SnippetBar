using System;

namespace SnippetBar
{
    public class Snippet
    {
        // Unikt ID för att identifiera snipen oavsett vilken lista den ligger i
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Color { get; set; } = "#3E3E42";
    }
}