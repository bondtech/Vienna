using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Windows.Media;
using System.Windows.Interop;
using System.Globalization;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Linq;
using BondTech.Vienna.Shared;

namespace BondTech.Vienna.Settings
{
    /// <summary>
    /// Interaction logic for MainSearch.xaml
    /// </summary>
    public partial class MainSearch : Window
    {
        private int top = 10, left = 10;    // for glass
        private bool neverRendered = true;  // for glass
        internal ShellSearchFolder searchFolder = null;
        AdvancedSearch advWindow;   // keep only one instance of the advanced window (unless user closes it)
        internal MainWindow ParentWindow; // to manipulate and export pictures to the parent.

        private Ookii.Dialogs.Wpf.TaskDialog helpTaskDialog;
        private Ookii.Dialogs.Wpf.ProgressDialog ExportHelper = new Ookii.Dialogs.Wpf.ProgressDialog()
        {
            CancellationText = "Stopping Export....",
            Description = "Exporting items...",
            ProgressBarStyle = Ookii.Dialogs.Wpf.ProgressBarStyle.ProgressBar,
            ShowTimeRemaining = true,
            Text = "Exporting your files.",
            WindowTitle = "Vienna Logon Screen Rotator",
        };

        // Background thread for our search
        private Thread backgroundSearchThread = null;

        private ShellContainer selectedScope = (ShellContainer)KnownFolders.UsersFiles;

        public MainSearch()
        {
            InitializeComponent();
            DragThumb.DragDelta += OnMove;

            this.SourceInitialized += new EventHandler(MainSearch_SourceInitialized);
            this.Loaded += new RoutedEventHandler(MainSearch_Loaded);
            ExportHelper.DoWork += new System.ComponentModel.DoWorkEventHandler(ExportHelper_DoWork);
            // Because the search can take some time, using a background thread.
            // This timer will check if that thread is still alive and accordingly update
            // the cursor
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.IsEnabled = true;
            timer.Tick += new EventHandler(timer_Tick);

            // Update the Scopes combobox with all the known folders
            var sortedKnownFolders =
                from folder in KnownFolders.All
                where (folder.CanonicalName != null &&
                    folder.CanonicalName.Length > 0)
                orderby folder.CanonicalName
                select folder;

            // Add the Browse... item so users can select any arbitrary location
            StackPanel browsePanel = new StackPanel();
            browsePanel.Margin = new Thickness(5, 2, 5, 2);
            browsePanel.Orientation = Orientation.Horizontal;

            Image browseImg = new Image();
            browseImg.Source = (new StockIcons()).FolderOpen.BitmapSource;
            browseImg.Height = 32;

            TextBlock browseTextBlock = new TextBlock();
            browseTextBlock.Background = Brushes.Transparent;
            browseTextBlock.FontSize = 10;
            browseTextBlock.Margin = new Thickness(4);
            browseTextBlock.VerticalAlignment = VerticalAlignment.Center;
            browseTextBlock.Text = "Browse...";

            browsePanel.Children.Add(browseImg);
            browsePanel.Children.Add(browseTextBlock);

            SearchScopesCombo.Items.Add(browsePanel);

            foreach (ShellContainer obj in sortedKnownFolders)
            {
                StackPanel panel = new StackPanel();
                panel.Margin = new Thickness(5, 2, 5, 2);
                panel.Orientation = Orientation.Horizontal;

                Image img = new Image();
                img.Source = obj.Thumbnail.SmallBitmapSource;
                img.Height = 32;

                TextBlock textBlock = new TextBlock();
                textBlock.Background = Brushes.Transparent;
                textBlock.FontSize = 10;
                textBlock.Margin = new Thickness(4);
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                textBlock.Text = obj.Name;

                panel.Children.Add(img);
                panel.Children.Add(textBlock);

                panel.Tag = obj;

                SearchScopesCombo.Items.Add(panel);


                // Set our initial search scope.
                // If Shell Libraries are supported, search in all the libraries,
                // else, use user's profile (my documents, etc)
                if (ShellLibrary.IsPlatformSupported)
                {
                    if (obj == (ShellContainer)KnownFolders.Libraries)
                        SearchScopesCombo.SelectedItem = panel;
                }
                else
                {
                    if (obj == (ShellContainer)KnownFolders.UsersFiles)
                        SearchScopesCombo.SelectedItem = panel;
                }
            }

            SearchScopesCombo.ToolTip = "Change the scope of the search. Use SearchHomeFolder\nto search your entire search index.";

            SearchScopesCombo.SelectionChanged += new SelectionChangedEventHandler(SearchScopesCombo_SelectionChanged);

            // Create our help task dialog

            helpTaskDialog = new Ookii.Dialogs.Wpf.TaskDialog();
            //helpTaskDialog.OwnerWindowHandle = (new WindowInteropHelper(this)).Handle;

            helpTaskDialog.MainIcon = Ookii.Dialogs.Wpf.TaskDialogIcon.Information;
            helpTaskDialog.AllowDialogCancellation = true;

            helpTaskDialog.WindowTitle = "Vienna Logon Screen Rotator JPEG Finder.";
            helpTaskDialog.MainInstruction = "Find JPEG files on your computer with ease.";
            helpTaskDialog.Content = "This is the advanced search engine of VLSR which mimicks Windows Search\n\n";
            helpTaskDialog.Content += "The search textbox allows you type the filename of the picture you want to find.\n\n";
            helpTaskDialog.Content += "The file is displayed if...\n\n";
            helpTaskDialog.Content += "     The file is a JPEG file.\n";
            helpTaskDialog.Content += "     The file size is less than 450 KB.\n";
            helpTaskDialog.Content += "     The file is not a hidden or system file.\n\n";
            helpTaskDialog.Content += "The advanced search dialog shows how to search against some common properties. ";
            helpTaskDialog.Content += " Multiple conditions can be combined together for the search.";
            helpTaskDialog.Content += "\n\nExport the files to VLSR by clicking the EXPORT button on the top right.";

            Ookii.Dialogs.Wpf.TaskDialogButton okButton = new Ookii.Dialogs.Wpf.TaskDialogButton(Ookii.Dialogs.Wpf.ButtonType.Ok);
            helpTaskDialog.Buttons.Add(okButton);

            helpTaskDialog.IsVerificationChecked = global::BondTech.Vienna.Settings.Properties.Settings.Default.ShowSearchHelpDialog;
            helpTaskDialog.VerificationText = "Always show this on startup";
            helpTaskDialog.VerificationClicked += new EventHandler(helpTaskDialog_VerificationClicked);

            helpTaskDialog.ExpandedInformation = "To report bugs or give suggestions. Visit\n\n";
            helpTaskDialog.ExpandedInformation += "<a href=\"http://www.facebook.com/bondtech\">Bond Technologies Facebook page.</a>\n";
            helpTaskDialog.ExpandedInformation += "<a href=\"http://bondtech.nazuka.net/\">Bond Technologies website.</a>\n";

            helpTaskDialog.EnableHyperlinks = true;
            helpTaskDialog.HyperlinkClicked += new EventHandler<Ookii.Dialogs.Wpf.HyperlinkClickedEventArgs>(helpTaskDialog_HyperlinkClick);
        }

        void ExportHelper_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Implement the operation that the progress bar is showing progress of here, same as you would do with a background worker.
            //for (int x = 0; x <= 100; ++x)
            {
                int Processed = 0;
                int FilesAdded = 0; decimal Report; decimal Factor = (decimal)100 / (decimal)listView1.Items.Count;
                foreach (SearchItem searchItem in listView1.Items)
                {
                    Thread.Sleep(200);
                    
                    // Periodically check CancellationPending and abort the operation if required.
                    if (ExportHelper.CancellationPending)
                    {
                        ExportHelper.ReportProgress(((FilesAdded / listView1.Items.Count) * 100), null, "Export cancelled. " + FilesAdded.ToString() + " file(s) added.");
                        return;
                    }

                    Processed++;
                    Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            if(ParentWindow.AddFile(searchItem.ParsingName))
                                FilesAdded++;
                        }
                    ));

                    Report = (decimal)Processed * Factor;
                    
                    ExportHelper.ReportProgress((int)Report,
                        string.Format(System.Globalization.CultureInfo.CurrentCulture, "Exporting your files... {0} file(s) added.", FilesAdded),
                        string.Format(System.Globalization.CultureInfo.CurrentCulture, "Exporting: {0}", searchItem.FileName));
                }
            }
        }

        void helpTaskDialog_VerificationClicked(object sender, EventArgs e)
        {
            global::BondTech.Vienna.Settings.Properties.Settings.Default.ShowSearchHelpDialog = helpTaskDialog.IsVerificationChecked;
            global::BondTech.Vienna.Settings.Properties.Settings.Default.Save();
        }

        void SearchScopesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StackPanel previousSelection = e.RemovedItems[0] as StackPanel;

            if (SearchScopesCombo.SelectedIndex == 0)
            {
                // Show a folder selection dialog
                CommonOpenFileDialog cfd = new CommonOpenFileDialog();
                cfd.AllowNonFileSystemItems = true;
                cfd.IsFolderPicker = true;

                cfd.Title = "Select a folder as your search scope...";

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ShellContainer container = cfd.FileAsShellObject as ShellContainer;

                    if (container != null)
                    {
                        #region Add it to the bottom of our combobox
                        StackPanel panel = new StackPanel();
                        panel.Margin = new Thickness(5, 2, 5, 2);
                        panel.Orientation = Orientation.Horizontal;

                        Image img = new Image();
                        img.Source = container.Thumbnail.SmallBitmapSource;
                        img.Height = 32;

                        TextBlock textBlock = new TextBlock();
                        textBlock.Background = Brushes.Transparent;
                        textBlock.FontSize = 10;
                        textBlock.Margin = new Thickness(4);
                        textBlock.VerticalAlignment = VerticalAlignment.Center;
                        textBlock.Text = container.Name;

                        panel.Children.Add(img);
                        panel.Children.Add(textBlock);

                        SearchScopesCombo.Items.Add(panel);
                        #endregion

                        // Set our selected scope
                        selectedScope = container;
                        SearchScopesCombo.SelectedItem = panel;
                    }
                    else
                        SearchScopesCombo.SelectedItem = previousSelection;
                }
                else
                    SearchScopesCombo.SelectedItem = previousSelection;
            }
            else if (SearchScopesCombo.SelectedItem != null && SearchScopesCombo.SelectedItem is ShellContainer)
                selectedScope = ((StackPanel)SearchScopesCombo.SelectedItem).Tag as ShellContainer;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            // Using a timer, check if our background search thread is still alive.
            // If not alive, update the cursor
            if (backgroundSearchThread != null && !backgroundSearchThread.IsAlive)
            {
                this.Cursor = Cursors.Arrow;

                // Also enable the search textbox again
                SearchBox.IsEnabled = true;
                buttonSearchAdv.IsEnabled = true;
            }
        }

        private void helpTaskDialog_HyperlinkClick(object sender, Ookii.Dialogs.Wpf.HyperlinkClickedEventArgs e)
        {
            // Launch the application associated with http links
            Process.Start(e.Href);
        }

        void MainSearch_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            if (Settings.Properties.Settings.Default.ShowSearchHelpDialog)
                helpTaskDialog.Show();
        }

        internal void Search(List<SearchItem> searchItemsList)
        {
            // Update the listview's itemsource
            listView1.ItemsSource = searchItemsList;

            if (listView1.Items.Count > 0)
                listView1.SelectedIndex = 0;
        }

        #region For the Aero glass effect

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // handle the message for DWM when the aero glass is turned on or off
            if (msg == GlassHelper.WM_DWMCOMPOSITIONCHANGED)
            {
                if (GlassHelper.IsGlassEnabled)
                {
                    // Extend glass
                    Rect bounds = VisualTreeHelper.GetContentBounds(listView1);
                    GlassHelper.ExtendGlassFrame(this, new Thickness(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
                }
                else
                {
                    // turn off glass...
                    GlassHelper.DisableGlassFrame(this);
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private void OnMove(object s, DragDeltaEventArgs e)
        {
            left += (int)e.HorizontalChange;
            top += (int)e.VerticalChange;
            this.Left = left;
            this.Top = top;
        }

        void MainSearch_SourceInitialized(object sender, EventArgs e)
        {
            Rect bounds = VisualTreeHelper.GetContentBounds(listView1);
            GlassHelper.ExtendGlassFrame(this, new Thickness(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (this.neverRendered)
            {
                // The window takes the size of its content because SizeToContent
                // is set to WidthAndHeight in the markup. We then allow
                // it to be set by the user, and have the content take the size
                // of the window.
                this.SizeToContent = SizeToContent.Manual;

                FrameworkElement root = this.Content as FrameworkElement;
                if (root != null)
                {
                    root.Width = double.NaN;
                    root.Height = double.NaN;
                }

                this.neverRendered = false;
            }

            base.OnContentRendered(e);
        }


        #endregion

        private void SearchTextBox_Search(object sender, RoutedEventArgs e)
        {
            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();

            // Set the cursor to wait
            this.Cursor = Cursors.Wait;

            // Also disable the search textbox while our search is going on
            SearchBox.IsEnabled = false;
            buttonSearchAdv.IsEnabled = false;

            // Search... on any letters typed
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                // Create a background thread to do the search
                backgroundSearchThread = new Thread(new ParameterizedThreadStart(DoSimpleSearch));
                // ApartmentState.STA is required for COM
                backgroundSearchThread.SetApartmentState(ApartmentState.STA);
                backgroundSearchThread.Start(SearchBox.Text);
            }
            else
            {
                listView1.ItemsSource = null;   // nothing was typed, or user deleted the search query (clear the list).
            }
        }

        // Helper method to do the search on a background thread
        internal void DoSimpleSearch(object arg)
        {
            string sent = arg as string;
            string text;

            if (sent.Contains("Type:") || sent.ToLower().Contains("kind:") || sent.ToLower().Contains("size:") || sent.ToLower().Contains("name:"))
            {
                MessageBox.Show("You cannot specify query for this search");
                backgroundSearchThread.Abort();
                return;
            }
            else
            {
                text = "Name:" + sent + " Type:Jpeg Size:<450kb";
            }

            // Specify a culture for our query.
            CultureInfo cultureInfo = new CultureInfo("en-US");

            SearchCondition searchCondition = SearchConditionFactory.ParseStructuredQuery(text, cultureInfo);

            // Create a new search folder by setting our search condition and search scope
            // KnownFolders.SearchHome - This is the same scope used by Windows search
            searchFolder = new ShellSearchFolder(
                searchCondition,
                selectedScope);

            List<SearchItem> items = new List<SearchItem>();

            try
            {
                // Because we cannot pass ShellObject or IShellItem (native interface)
                // across multiple threads, creating a helper object and copying the data we need from the ShellObject
                foreach (ShellObject so in searchFolder)
                {
                    // For each of our ShellObject,
                    // create a SearchItem object
                    // We will bind these items to the ListView
                    SearchItem item = new SearchItem();
                    item.FileName = so.Name;

                    // We must freeze the ImageSource before passing across threads
                    BitmapSource thumbnail = so.Thumbnail.MediumBitmapSource;
                    thumbnail.Freeze();
                    item.Thumbnail = thumbnail;
                    
                    item.Authors = so.Properties.System.Author.Value;
                    item.Title = so.Properties.System.Title.Value;
                    item.Keywords = so.Properties.System.Keywords.Value;
                    item.Copyright = so.Properties.System.Copyright.Value;
                    item.FileType = so.Properties.System.FileOwner.Value; // so.Properties.System.Document.PageCount.Value.HasValue ? so.Properties.System.Document.PageCount.Value.Value : 0;
                    item.Rating = so.Properties.System.SimpleRating.Value.HasValue ? (int)so.Properties.System.SimpleRating.Value.Value : 0;
                    item.ParsingName = so.ParsingName;

                    items.Add(item);
                }

                // Invoke the search on the main thread

                Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate()
                    {
                        UpdateSearchItems(items);
                    }
                ));
            }
            catch
            {
                searchFolder.Dispose();
                searchFolder = null;
            }
        }

        // Updates the items on the listview on the main thread.
        // This method should not be called from a background thread
        internal void UpdateSearchItems(List<SearchItem> items)
        {
            // Update the listview's itemsource
            listView1.ItemsSource = items;

            if (listView1.Items.Count > 0)
                listView1.SelectedIndex = 0;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchBox.Focus();
        }

        private void buttonSearchAdv_Click(object sender, RoutedEventArgs e)
        {
            if (advWindow == null)
            {
                advWindow = new AdvancedSearch();
                advWindow.MainWindow = this;
                advWindow.Closed += new EventHandler(advWindow_Closed);
            }

            if (!advWindow.IsVisible)
                advWindow.Show();
            else
            {
                advWindow.Visibility = Visibility.Visible;
                advWindow.Focus();
            }
        }

        void advWindow_Closed(object sender, EventArgs e)
        {
            advWindow = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (backgroundSearchThread != null && backgroundSearchThread.IsAlive) backgroundSearchThread.Abort();
            if (advWindow != null)
                advWindow.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            helpTaskDialog.Show();
        }

        private void buttonExportAll_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.Items.Count == 0)
            {
                CustomMessage.CustomMessageBox.Show(this, "There is nothing to export.", "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (ParentWindow.btnAppendToggle.IsChecked == false)
            {
                Ookii.Dialogs.Wpf.TaskDialog AppendDialog = new Ookii.Dialogs.Wpf.TaskDialog();
                AppendDialog.ButtonStyle = Ookii.Dialogs.Wpf.TaskDialogButtonStyle.CommandLinks;
                AppendDialog.MainInstruction = "You have set the list to clear images before adding new ones." +
                    " You cannot export search items if you keep this setting. What would you like to do?";
                AppendDialog.MainIcon = Ookii.Dialogs.Wpf.TaskDialogIcon.Warning;
                Ookii.Dialogs.Wpf.TaskDialogButton Append = new Ookii.Dialogs.Wpf.TaskDialogButton();
                Append.ButtonType = Ookii.Dialogs.Wpf.ButtonType.Custom;
                Append.Text = "Append Files";
                Append.CommandLinkNote = "This will make the image list retain the current files already in the list";
                Ookii.Dialogs.Wpf.TaskDialogButton Cancel = new Ookii.Dialogs.Wpf.TaskDialogButton();
                Cancel.ButtonType = Ookii.Dialogs.Wpf.ButtonType.Cancel;
                AppendDialog.Buttons.Add(Append);
                AppendDialog.Buttons.Add(Cancel);
                if (AppendDialog.ShowDialog() == Cancel) { return; } else { ParentWindow.btnAppendToggle.IsChecked = true; }
            }

            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();

            ParentWindow.CheckAppend();
   
            if (!ExportHelper.IsBusy)
                ExportHelper.Show();
            else
            {
                CustomMessage.CustomMessageBox.Show(this, "Your files are currently being exported, please wait until those are done before exporting again.",
                     "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // disable the search textbox while our export is going on
            //SearchBox.IsEnabled = false;
            //buttonSearchAdv.IsEnabled = false;

            //this.Cursor = Cursors.Wait;
            //ParentWindow.AddFile((List<SearchItem>)listView1.ItemsSource);

            //// Create a background thread to do the search
            //backgroundSearchThread = new Thread(new ParameterizedThreadStart(ParentWindow.AddFile));
            //// ApartmentState.STA is required for COM
            //backgroundSearchThread.SetApartmentState(ApartmentState.STA);
            //backgroundSearchThread.Start(listView1.ItemsSource);
            
            //CustomMessage.CustomMessageBox.Show(this, "Your files have been exported.", "Vienna Logon Screen Rotator",
            //    MessageBoxButton.OK, MessageBoxImage.Information);

            //this.Cursor = Cursors.Arrow;
        }

        private void buttonExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.Items.Count == 0)
            {
                CustomMessage.CustomMessageBox.Show(this, "There is nothing to export.",
                    "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ParentWindow.CheckAppend();

            //if (ParentWindow.btnAppendToggle.IsChecked == false)
            //{
            //    Ookii.Dialogs.Wpf.TaskDialog AppendDialog = new Ookii.Dialogs.Wpf.TaskDialog();
            //    AppendDialog.ButtonStyle = Ookii.Dialogs.Wpf.TaskDialogButtonStyle.CommandLinks;
            //    AppendDialog.MainInstruction = "You have set the list to clear images before adding new ones." +
            //        " You cannot export search items if you keep this setting. What would you like to do?";
            //    AppendDialog.MainIcon = Ookii.Dialogs.Wpf.TaskDialogIcon.Warning;
            //    Ookii.Dialogs.Wpf.TaskDialogButton Append = new Ookii.Dialogs.Wpf.TaskDialogButton();
            //    Append.ButtonType = Ookii.Dialogs.Wpf.ButtonType.Custom;
            //    Append.Text = "Append Files";
            //    Append.CommandLinkNote = "This will make the image list retain the current files already in the list";
            //    Ookii.Dialogs.Wpf.TaskDialogButton Cancel = new Ookii.Dialogs.Wpf.TaskDialogButton();
            //    Cancel.ButtonType = Ookii.Dialogs.Wpf.ButtonType.Cancel;
            //    AppendDialog.Buttons.Add(Append);
            //    AppendDialog.Buttons.Add(Cancel);
            //    if (AppendDialog.ShowDialog() == Cancel) { return; } else { ParentWindow.btnAppendToggle.IsChecked = true; }
            //}

            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();

            this.Cursor = Cursors.Wait;
            int FilesAdded =0;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                if (ParentWindow.AddFile((listView1.SelectedItems[i] as SearchItem).ParsingName))
                    FilesAdded++;
            }

            CustomMessage.CustomMessageBox.Show(this, FilesAdded.ToString() + " JPEG files added.",
                "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
        /// <summary>
        /// ImageView displays image files using themselves as their icons.
        /// In order to write our own visual tree of a view, we should override its
        /// DefaultStyleKey and ItemContainerDefaultKey. DefaultStyleKey specifies
        /// the style key of ListView; ItemContainerDefaultKey specifies the style
        /// key of ListViewItem.
        /// </summary>
        public class ImageView : ViewBase
        {
            #region DefaultStyleKey

            protected override object DefaultStyleKey
            {
                get { return new ComponentResourceKey(GetType(), "ImageView"); }
            }

            #endregion

            #region ItemContainerDefaultStyleKey

            protected override object ItemContainerDefaultStyleKey
            {
                get { return new ComponentResourceKey(GetType(), "ImageViewItem"); }
            }

            #endregion
        }

        public static class CustomCommands
        {
            public static RoutedCommand SearchCommand = new RoutedCommand("SearchCommand", typeof(CustomCommands));
        }

        /// <summary>
        /// Represents a single item in the search results.
        /// This item will store the file's thumbnail, display name,
        /// and some properties (that will be displayed in the properties pane)
        /// </summary>
        public class SearchItem
        {
            public string FileName { get; set; }
            public BitmapSource Thumbnail { get; set; }
            public string[] Authors { get; set; }
            public int Rating { get; set; }
            public string Copyright { get; set; }
            public string FileType { get; set; }
            public string[] Keywords { get; set; }
            public string Title { get; set; }
            public string ParsingName { get; set; }
        }
    }

