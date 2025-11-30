using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;

using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;

namespace SnippetBar
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();

        private readonly string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snippets.json");
        private readonly string _settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private bool _isDocked = false;
        private double _originalWidth = 300;
        private AppSettings _settings = new AppSettings();
        private bool _isLoaded = false;
        private bool _isMenuOpen = false;

        private bool _isResizing = false;
        private DispatcherTimer _resizeTimer;

        // TRAY & AUTO-HIDE
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isRealExit = false;
        private DispatcherTimer _autoHideTimer;
        private bool _isCollapsed = false;
        private DockSide _dockSide = DockSide.None;

        private enum DockSide { None, Left, Right }

        public MainWindow()
        {
            InitializeComponent();

            // Resize Timer
            _resizeTimer = new DispatcherTimer();
            _resizeTimer.Interval = TimeSpan.FromMilliseconds(500);
            _resizeTimer.Tick += ResizeTimer_Tick;

            // Auto Hide Timer
            _autoHideTimer = new DispatcherTimer();
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(500);
            _autoHideTimer.Tick += AutoHideTimer_Tick;

            // Tray Ikon
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try { _notifyIcon.Icon = new System.Drawing.Icon("app.ico"); }
            catch { _notifyIcon.Icon = System.Drawing.SystemIcons.Application; }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = LanguageManager.Instance["AppTitle"];
            _notifyIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); };

            var trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.Items.Add(LanguageManager.Instance["TrayOpen"], null, (s, e) => { this.Show(); this.WindowState = WindowState.Normal; });
            trayMenu.Items.Add(LanguageManager.Instance["TrayExit"], null, (s, e) => { _isRealExit = true; Application.Current.Shutdown(); });
            _notifyIcon.ContextMenuStrip = trayMenu;

            LoadData();
            LoadSettings(); // Här sätts även språket

            CategoryTabs.ItemsSource = Categories;
            this.MouseEnter += Window_MouseEnter;
            this.MouseLeave += Window_MouseLeave;

            UpdateUIFromSettings();
            _isLoaded = true;
        }

        public class AppSettings
        {
            public bool EnableTransparency { get; set; } = true;
            public double TransparencyLevel { get; set; } = 0.4;
            public bool IsAutoPasteMode { get; set; } = true;
            public bool IsAutoHideEnabled { get; set; } = false;
            // ÄNDRING: Default till engelska
            public string Language { get; set; } = "en";
        }

        // --- CLOSING & TRAY ---
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Om det inte är en "riktig" exit (via menyn), avbryt stängning och göm fönstret
            if (!_isRealExit)
            {
                e.Cancel = true;
                this.Hide();
                _notifyIcon.ShowBalloonTip(1000,
                    LanguageManager.Instance["AppTitle"],
                    LanguageManager.Instance["MsgMinimized"],
                    System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _notifyIcon.Dispose();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Detta är "Avsluta" i menyn -> Stäng på riktigt
            _isRealExit = true;
            Application.Current.Shutdown();
        }

        // --- AUTO HIDE LOGIK ---
        private void ToggleAutoHide_Click(object sender, RoutedEventArgs e)
        {
            _settings.IsAutoHideEnabled = !_settings.IsAutoHideEnabled;
            if (MenuAutoHide != null) MenuAutoHide.IsChecked = _settings.IsAutoHideEnabled;
            SaveSettings();
        }

        private void AutoHideTimer_Tick(object? sender, EventArgs e)
        {
            _autoHideTimer.Stop();
            if (_isDocked && !_isCollapsed && _settings.IsAutoHideEnabled && !IsMouseOver && !_isMenuOpen)
            {
                this.Width = 5;
                if (_dockSide == DockSide.Right)
                {
                    this.Left = SystemParameters.WorkArea.Width - 5;
                }
                _isCollapsed = true;
            }
        }

        // --- MOUSE EVENTS ---
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            _autoHideTimer.Stop();

            if (_isCollapsed)
            {
                this.Width = 85;
                if (_dockSide == DockSide.Right)
                {
                    this.Left = SystemParameters.WorkArea.Width - 85;
                }
                _isCollapsed = false;
            }

            this.Opacity = 1.0;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isMenuOpen) return;
            if (_isResizing) return;
            if (Mouse.LeftButton == MouseButtonState.Pressed) return;

            if (_isDocked && _settings.IsAutoHideEnabled)
            {
                _autoHideTimer.Start();
            }

            if (_settings.EnableTransparency)
            {
                this.Opacity = _settings.TransparencyLevel;
            }
            else
            {
                this.Opacity = 1.0;
            }
        }

        // --- RESIZE LOGIK ---
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _isResizing = true;
            this.Opacity = 1.0;
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private void ResizeTimer_Tick(object? sender, EventArgs e)
        {
            _isResizing = false;
            _resizeTimer.Stop();
            if (!IsMouseOver) Window_MouseLeave(null!, null!);
        }

        // --- MENY HANTERING ---
        private void MoreMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Menu_Opened(object sender, RoutedEventArgs e)
        {
            _isMenuOpen = true;
            this.Opacity = 1.0;
        }

        private void Menu_Closed(object sender, RoutedEventArgs e)
        {
            _isMenuOpen = false;
            if (!IsMouseOver) Window_MouseLeave(null!, null!);
        }

        // --- SPRÅK & LÄGESHANTERING ---
        private void SetPasteMode_Click(object sender, RoutedEventArgs e)
        {
            _settings.IsAutoPasteMode = true;
            UpdateModeMenuState();
            SaveSettings();
        }

        private void SetCopyMode_Click(object sender, RoutedEventArgs e)
        {
            _settings.IsAutoPasteMode = false;
            UpdateModeMenuState();
            SaveSettings();
        }

        private void SetLanguageSv_Click(object sender, RoutedEventArgs e) => SetLanguage("sv");
        private void SetLanguageEn_Click(object sender, RoutedEventArgs e) => SetLanguage("en");

        private void SetLanguage(string lang)
        {
            _settings.Language = lang;
            LanguageManager.Instance.SetLanguage(lang);
            SaveSettings();
            UpdateTrayMenuText();

            // Uppdatera systemflikens namn direkt i UI
            var sysCat = Categories.FirstOrDefault(c => c.IsSystemCategory);
            if (sysCat != null)
            {
                sysCat.Name = LanguageManager.Instance["AllSnips"];
                CategoryTabs.Items.Refresh();
            }
        }

        private void UpdateTrayMenuText()
        {
            if (_notifyIcon != null && _notifyIcon.ContextMenuStrip != null)
            {
                _notifyIcon.ContextMenuStrip.Items[0].Text = LanguageManager.Instance["TrayOpen"];
                _notifyIcon.ContextMenuStrip.Items[1].Text = LanguageManager.Instance["TrayExit"];
            }
        }

        private void UpdateModeMenuState()
        {
            if (MenuModePaste != null && MenuModeCopy != null)
            {
                MenuModePaste.IsChecked = _settings.IsAutoPasteMode;
                MenuModeCopy.IsChecked = !_settings.IsAutoPasteMode;
            }
        }

        // --- DOCKNING ---
        private void Dock_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDocked)
            {
                if (this.Width > 100) _originalWidth = this.Width;

                double screenHeight = SystemParameters.WorkArea.Height;
                double screenWidth = SystemParameters.WorkArea.Width;
                double centerX = this.Left + (this.Width / 2);

                this.Height = screenHeight;
                this.Top = 0;
                this.Width = 85;

                if (txtAppTitle != null) txtAppTitle.Visibility = Visibility.Collapsed;

                if (centerX < screenWidth / 2)
                {
                    this.Left = 0;
                    _dockSide = DockSide.Left;
                }
                else
                {
                    this.Left = screenWidth - this.Width;
                    _dockSide = DockSide.Right;
                }

                _isDocked = true;
            }
            else
            {
                this.Height = 500;
                this.Width = _originalWidth;
                _dockSide = DockSide.None;
                _isCollapsed = false;

                if (txtAppTitle != null) txtAppTitle.Visibility = Visibility.Visible;

                if (this.Left < 100) this.Left += 50;
                else this.Left -= 50;

                this.Top = 100;
                this.Opacity = 1.0;
                _isDocked = false;
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_isDocked)
                {
                    _isDocked = false;
                    _dockSide = DockSide.None;
                    _isCollapsed = false;
                    this.Opacity = 1.0;
                    this.Height = 500;
                    this.Width = _originalWidth;
                    if (txtAppTitle != null) txtAppTitle.Visibility = Visibility.Visible;
                }
                this.DragMove();
            }
        }

        // --- SETTINGS (INPUT) ---
        private void LoadSettings()
        {
            if (File.Exists(_settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFile);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) _settings = s;
                }
                catch { }
            }

            // Sätt språk baserat på settings
            LanguageManager.Instance.SetLanguage(_settings.Language);
            UpdateTrayMenuText();

            if (chkTransparent != null) chkTransparent.IsChecked = _settings.EnableTransparency;
            if (txtOpacityInput != null) txtOpacityInput.Text = ((int)(_settings.TransparencyLevel * 100)).ToString();

            if (MenuAutoHide != null) MenuAutoHide.IsChecked = _settings.IsAutoHideEnabled;

            UpdateModeMenuState();
        }

        private void SaveSettings()
        {
            if (chkTransparent == null) return;

            try
            {
                _settings.EnableTransparency = chkTransparent.IsChecked == true;
                string json = JsonSerializer.Serialize(_settings);
                File.WriteAllText(_settingsFile, json);
            }
            catch { }
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            SaveSettings();
            if (!IsMouseOver && !_isMenuOpen) Window_MouseLeave(null!, null!);
        }

        private void OpacityInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded || txtOpacityInput == null) return;

            if (int.TryParse(txtOpacityInput.Text, out int val))
            {
                if (val > 100) val = 100;
                if (val < 10) val = 10;

                _settings.TransparencyLevel = val / 100.0;
                SaveSettings();

                if (!IsMouseOver) this.Opacity = _settings.TransparencyLevel;
            }
        }

        private void OpacityUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtOpacityInput.Text, out int val))
            {
                val += 5;
                if (val > 100) val = 100;
                txtOpacityInput.Text = val.ToString();
            }
        }

        private void OpacityDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtOpacityInput.Text, out int val))
            {
                val -= 5;
                if (val < 10) val = 10;
                txtOpacityInput.Text = val.ToString();
            }
        }

        private void UpdateUIFromSettings() => UpdateModeMenuState();

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsOverlay != null)
            {
                SettingsOverlay.Visibility = (SettingsOverlay.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // --- SNIPPET ACTIONS ---
        private void Snippet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Snippet snippet)
            {
                string text = snippet.Content == "{DATE}" ? DateTime.Now.ToString("yyyy-MM-dd") : snippet.Content;

                bool isShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool shouldPaste = _settings.IsAutoPasteMode;
                if (isShiftDown) shouldPaste = !shouldPaste;

                if (shouldPaste) SendText(text);
                else try { System.Windows.Forms.Clipboard.SetText(text); } catch { }
            }
        }

        private void SendText(string text)
        {
            try
            {
                string? prev = null;
                if (System.Windows.Forms.Clipboard.ContainsText()) prev = System.Windows.Forms.Clipboard.GetText();
                System.Windows.Forms.Clipboard.SetText(text);
                System.Windows.Forms.SendKeys.SendWait("^v");
                System.Threading.Thread.Sleep(50);
                if (prev != null) System.Windows.Forms.Clipboard.SetText(prev); else System.Windows.Forms.Clipboard.Clear();
            }
            catch
            {
                string fb = text.Replace("\r\n", "+{ENTER}").Replace("\n", "+{ENTER}").Replace("+", "{+}").Replace("^", "{^}").Replace("%", "{%}").Replace("~", "{~}").Replace("(", "{(}").Replace(")", "{)}");
                System.Windows.Forms.SendKeys.SendWait(fb);
            }
        }

        // --- DATA LOAD/SAVE ---
        private void LoadData()
        {
            if (File.Exists(_dataFile))
            {
                try
                {
                    string json = File.ReadAllText(_dataFile);
                    var loadedList = JsonSerializer.Deserialize<ObservableCollection<Category>>(json);
                    if (loadedList != null) Categories = loadedList;
                }
                catch { }
            }

            var allSnipsCat = Categories.FirstOrDefault(c => c.IsSystemCategory);
            if (allSnipsCat == null)
            {
                allSnipsCat = new Category { Name = LanguageManager.Instance["AllSnips"], IsSystemCategory = true };
                Categories.Insert(0, allSnipsCat);
            }
            else
            {
                Categories.Move(Categories.IndexOf(allSnipsCat), 0);
            }

            var masterMap = new Dictionary<Guid, Snippet>();
            foreach (var snip in allSnipsCat.Snippets) if (!masterMap.ContainsKey(snip.Id)) masterMap[snip.Id] = snip;
            foreach (var cat in Categories)
            {
                for (int i = 0; i < cat.Snippets.Count; i++)
                {
                    var snip = cat.Snippets[i];
                    if (masterMap.ContainsKey(snip.Id)) cat.Snippets[i] = masterMap[snip.Id];
                    else
                    {
                        masterMap[snip.Id] = snip;
                        if (!allSnipsCat.Snippets.Contains(snip)) allSnipsCat.Snippets.Add(snip);
                    }
                }
            }
            CategoryTabs.SelectedIndex = 0;
        }

        private void SaveData()
        {
            string json = JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFile, json);
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (CategoryTabs.SelectedItem is Category currentCategory)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(currentCategory.Snippets);
                if (view != null)
                {
                    view.Filter = item => {
                        if (string.IsNullOrEmpty(txtSearch.Text)) return true;
                        if (item is Snippet snippet) return snippet.Title.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase) || snippet.Content.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase);
                        return false;
                    };
                    view.Refresh();
                }
            }
        }

        // --- KATEGORI ACTIONS ---
        private void NewCat_Click(object sender, RoutedEventArgs e)
        {
            var input = new InputWindow();
            input.Owner = this;
            if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.Answer))
            {
                var newCat = new Category { Name = input.Answer };
                Categories.Add(newCat);
                SaveData();
                CategoryTabs.SelectedItem = newCat;
            }
        }

        private void RenameActiveCat_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryTabs.SelectedItem is Category category)
            {
                if (category.IsSystemCategory) { MessageBox.Show(LanguageManager.Instance["CannotRenameSystem"]); return; }
                var input = new InputWindow(category.Name);
                input.Owner = this;
                if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.Answer))
                {
                    category.Name = input.Answer;
                    SaveData();
                    CategoryTabs.Items.Refresh();
                }
            }
        }

        private void DeleteActiveCat_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryTabs.SelectedItem is Category category)
            {
                if (category.IsSystemCategory) { MessageBox.Show(LanguageManager.Instance["CannotDeleteSystem"]); return; }

                string msg = string.Format(LanguageManager.Instance["ConfirmDeleteTab"], category.Name);
                if (MessageBox.Show(msg, LanguageManager.Instance["Confirm"], MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Categories.Remove(category);
                    SaveData();
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var currentCategory = CategoryTabs.SelectedItem as Category;
            var allSnipsCat = Categories.FirstOrDefault(c => c.IsSystemCategory);
            var addWindow = new AddSnippetWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                var newSnip = addWindow.SnippetData;
                if (allSnipsCat != null) allSnipsCat.Snippets.Add(newSnip);
                if (currentCategory != null && !currentCategory.IsSystemCategory) currentCategory!.Snippets.Add(newSnip);
                SaveData();
                ApplyFilter();
            }
        }

        // --- SNIPPET MENUS ---
        private void Snippet_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu && CategoryTabs.SelectedItem is Category currentCategory)
            {
                var removeItem = menu.Items.OfType<MenuItem>().FirstOrDefault(i => i.Name == "MenuRemoveFromCat");
                if (removeItem != null) removeItem.Visibility = currentCategory.IsSystemCategory ? Visibility.Collapsed : Visibility.Visible;
            }
            _isMenuOpen = true;
            this.Opacity = 1.0;
        }

        private void RemoveFromCat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is Snippet snippet && CategoryTabs.SelectedItem is Category currentCategory && !currentCategory.IsSystemCategory)
            {
                currentCategory.Snippets.Remove(snippet);
                SaveData();
            }
        }

        private void DeletePermanent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is Snippet snippet)
            {
                string msg = string.Format(LanguageManager.Instance["ConfirmDeleteSnip"], snippet.Title);
                if (MessageBox.Show(msg, LanguageManager.Instance["Delete"], MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    foreach (var cat in Categories) if (cat.Snippets.Contains(snippet)) cat.Snippets.Remove(snippet);
                    SaveData();
                }
            }
        }

        private void Edit_Click_Double(object sender, MouseButtonEventArgs e) { if (sender is Button btn && btn.Tag is Snippet snippet) OpenEdit(snippet); }
        private void Edit_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem menuItem && menuItem.Tag is Snippet snippet) OpenEdit(snippet); }

        private void OpenEdit(Snippet snippet)
        {
            var editWindow = new AddSnippetWindow(snippet);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true)
            {
                snippet.Title = editWindow.SnippetData.Title;
                snippet.Content = editWindow.SnippetData.Content;
                snippet.Color = editWindow.SnippetData.Color;
                SaveData();
                CategoryTabs.Items.Refresh();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            // FIX: Använd Close() istället för Shutdown().
            // Detta triggar Window_Closing, som kollar _isRealExit.
            // Resultat: Appen minimeras istället för att dödas.
            this.Close();
        }

        private const int GWL_EXSTYLE = -20; private const int WS_EX_NOACTIVATE = 0x08000000;
        [DllImport("user32.dll")] public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); var helper = new WindowInteropHelper(this); SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE); }
    }

    // --- DROP HANDLER (Sortering FIXAD) ---
    public class SnippetDropHandler : DefaultDropHandler
    {
        private Category? GetTargetCategory(IDropInfo dropInfo)
        {
            if (dropInfo.TargetItem is Category c) return c;

            var visualTarget = dropInfo.VisualTarget as FrameworkElement;
            while (visualTarget != null)
            {
                if (visualTarget.DataContext is Category cat) return cat;
                visualTarget = VisualTreeHelper.GetParent(visualTarget) as FrameworkElement;
            }
            return null;
        }

        public override void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.TargetCollection != null)
            {
                base.DragOver(dropInfo);
                if (dropInfo.DragInfo.SourceCollection != dropInfo.TargetCollection) dropInfo.Effects = DragDropEffects.Copy;
                else dropInfo.Effects = DragDropEffects.Move;
                return;
            }

            var targetCat = GetTargetCategory(dropInfo);
            if (targetCat != null)
            {
                dropInfo.Effects = DragDropEffects.Copy;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
        }

        public override void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.TargetCollection != null)
            {
                var targetList = dropInfo.TargetCollection as ObservableCollection<Snippet>;
                var sourceList = dropInfo.DragInfo.SourceCollection as ObservableCollection<Snippet>;

                if (targetList != null && sourceList != null)
                {
                    if (targetList == sourceList) base.Drop(dropInfo);
                    else
                    {
                        var snippet = dropInfo.Data as Snippet;
                        if (snippet != null && !targetList.Contains(snippet))
                        {
                            int idx = dropInfo.InsertIndex;
                            if (idx < 0) idx = 0;
                            if (idx > targetList.Count) idx = targetList.Count;
                            targetList.Insert(idx, snippet);
                        }
                    }
                }
                return;
            }

            var targetCat = GetTargetCategory(dropInfo);
            if (targetCat != null)
            {
                var snippet = dropInfo.Data as Snippet;
                if (snippet != null && !targetCat.Snippets.Contains(snippet))
                {
                    targetCat.Snippets.Add(snippet);
                }
            }
        }
    }
}