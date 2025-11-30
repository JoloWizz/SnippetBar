using System.Collections.Generic;
using System.ComponentModel;

namespace SnippetBar
{
    public class LanguageManager : INotifyPropertyChanged
    {
        public static LanguageManager Instance { get; } = new LanguageManager();

        // ÄNDRING: Engelska som standard ("en")
        private string _currentLanguage = "en";
        private Dictionary<string, Dictionary<string, string>> _languages;

        private LanguageManager()
        {
            _languages = new Dictionary<string, Dictionary<string, string>>();
            LoadLanguages();
        }

        private void LoadLanguages()
        {
            // SVENSKA
            var sv = new Dictionary<string, string>
            {
                { "AppTitle", "SnippetBar" },
                
                // Tray & Messages
                { "TrayOpen", "Öppna" },
                { "TrayExit", "Avsluta" },
                { "MsgMinimized", "Minimerad till systemfältet." },
                { "Confirm", "Bekräfta" },
                { "Delete", "Radera" },
                { "AllSnips", "Alla Snips" },

                // Main Menu
                { "MenuNewSnip", "Ny Snip" },
                { "MenuCategory", "Kategori" },
                { "MenuNewCat", "Ny Kategori..." },
                { "MenuRenameCat", "Byt namn..." },
                { "MenuDeleteCat", "Ta bort..." },
                { "MenuMode", "Läge / Funktion" },
                { "MenuPasteMode", "Klistra in direkt (Ctrl+V)" },
                { "MenuCopyMode", "Endast till Urklipp" },
                { "MenuAutoHide", "Auto-göm vid dockning" },
                
                // ÄNDRING: Enkel text för svenska
                { "MenuLanguage", "Språk" },
                { "LangSv", "Svenska" },
                { "LangEn", "English" },

                { "MenuSettings", "Inställningar" },
                { "MenuExit", "Avsluta" },

                // Context Menus
                { "CtxEdit", "Redigera" },
                { "CtxRemoveFromCat", "Ta bort från Kategori" },
                { "CtxDeletePerm", "Radera PERMANENT" },

                // UI Elements
                { "SearchPlaceholder", "Sök..." },
                { "SettingsTitle", "Inställningar" },
                { "LblTransparency", "Transparens" },
                { "ChkActive", "Aktiv" },
                { "LblOpacity", "Opacitet (%)" },
                { "BtnClose", "Stäng" },

                // Dialogs / Logic
                { "ConfirmDeleteTab", "Ta bort fliken '{0}'?" },
                { "ConfirmDeleteSnip", "Radera '{0}' permanent?" },
                { "CannotRenameSystem", "Du kan inte byta namn på 'Alla Snips'." },
                { "CannotDeleteSystem", "Du kan inte ta bort 'Alla Snips'." }
            };

            // ENGELSKA
            var en = new Dictionary<string, string>
            {
                { "AppTitle", "SnippetBar" },

                // Tray & Messages
                { "TrayOpen", "Open" },
                { "TrayExit", "Exit" },
                { "MsgMinimized", "Minimized to system tray." },
                { "Confirm", "Confirm" },
                { "Delete", "Delete" },
                { "AllSnips", "All Snips" },

                // Main Menu
                { "MenuNewSnip", "New Snip" },
                { "MenuCategory", "Category" },
                { "MenuNewCat", "New Category..." },
                { "MenuRenameCat", "Rename..." },
                { "MenuDeleteCat", "Delete..." },
                { "MenuMode", "Mode / Function" },
                { "MenuPasteMode", "Auto Paste (Ctrl+V)" },
                { "MenuCopyMode", "Copy to Clipboard only" },
                { "MenuAutoHide", "Auto-hide when docked" },

                // ÄNDRING: Enkel text för engelska
                { "MenuLanguage", "Language" },
                { "LangSv", "Svenska" },
                { "LangEn", "English" },

                { "MenuSettings", "Settings" },
                { "MenuExit", "Exit" },

                // Context Menus
                { "CtxEdit", "Edit" },
                { "CtxRemoveFromCat", "Remove from Category" },
                { "CtxDeletePerm", "Delete PERMANENTLY" },

                // UI Elements
                { "SearchPlaceholder", "Search..." },
                { "SettingsTitle", "Settings" },
                { "LblTransparency", "Transparency" },
                { "ChkActive", "Active" },
                { "LblOpacity", "Opacity (%)" },
                { "BtnClose", "Close" },

                // Dialogs / Logic
                { "ConfirmDeleteTab", "Delete tab '{0}'?" },
                { "ConfirmDeleteSnip", "Permanently delete '{0}'?" },
                { "CannotRenameSystem", "You cannot rename 'All Snips'." },
                { "CannotDeleteSystem", "You cannot delete 'All Snips'." }
            };

            _languages.Add("sv", sv);
            _languages.Add("en", en);
        }

        public void SetLanguage(string langCode)
        {
            if (_languages.ContainsKey(langCode))
            {
                _currentLanguage = langCode;
                OnPropertyChanged("Item[]");
            }
        }

        public string this[string key]
        {
            get
            {
                if (_languages[_currentLanguage].ContainsKey(key))
                    return _languages[_currentLanguage][key];
                return key;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}