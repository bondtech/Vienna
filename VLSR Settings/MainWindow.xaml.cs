using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BondTech.CustomMessage;
using BondTech.Vienna.Shared;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Windows.Interop;

namespace BondTech.Vienna.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum ServiceAction { Pause, Start, Stop }
        List<LogonPicture> Photos = new List<LogonPicture>();
        ScaleTransform st = new ScaleTransform(3, 3);
        private bool neverRendered = true;  // for glass
        private bool OktoExit = true; //Before exit.
        private MainSearch SearchHelper;
        private ViennaShared ThisUser;
        System.Timers.Timer ViennaServiceTimer;
        Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog JPEGDialog;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            
            ThisUser = new ViennaShared();
            lstImages.PreviewKeyDown += new KeyEventHandler(lstImages_PreviewKeyDown);
            tabHome.Visibility = System.Windows.Visibility.Collapsed;
            tabAbout.Visibility = System.Windows.Visibility.Collapsed;
            tabSettings.Visibility = System.Windows.Visibility.Collapsed;
            //Set up a timer.
            ViennaServiceTimer = new System.Timers.Timer() //timer to check if the service is running,
            {
                AutoReset = true,
                Enabled = true,
                Interval = 30000, //per minute
            };
            ViennaServiceTimer.Elapsed += new System.Timers.ElapsedEventHandler(ViennaServiceTimer_Elapsed);
            #region **Attach Handler for Header
            gridHeader.MouseLeftButtonDown +=new MouseButtonEventHandler(gridHeader_MouseLeftButtonDown);
            #endregion
        }

        #region For the Aero glass effect

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));
            System.Threading.Thread ReadSet = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReadSettings));
            ReadSet.SetApartmentState(System.Threading.ApartmentState.STA);
            ReadSet.Start();
            ViennaServiceTimer.Start();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //this.btnServiceToggle.IsChecked = ThisUser.ViennaServiceIsRunning; //Consumes too much memory at the rate it's checking.
            // handle the message for DWM when the aero glass is turned on or off
            if (msg == GlassHelper.WM_DWMCOMPOSITIONCHANGED)
            {
                if (GlassHelper.IsGlassEnabled)
                {
                    // Extend glass
                    //Rect bounds = VisualTreeHelper.GetContentBounds(lstImages);
                    //GlassHelper.ExtendGlassFrame(this, new Thickness(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
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

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            Rect bounds = VisualTreeHelper.GetContentBounds(lstImages);
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

        #region **EventHandler for Header

        void gridHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (this.WindowState == System.Windows.WindowState.Normal)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                }
                else { this.WindowState = System.Windows.WindowState.Normal; }
                return;
            }
            
            try
            {
                DragMove();
            }
            catch { }
        }

        private void imgWeb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.facebook.com/bondtech");
        }

        private void imgMin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
                return;
            }
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void imgHeaderClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool Leave= false;

            if (!OktoExit)
            {
                TaskDialog SaveDialog = new TaskDialog();
                SaveDialog.WindowTitle = "Vienna Logon Screen Rotator";
                SaveDialog.MainInstruction = "You have made changes to the form and you have not saved. What would you like to do?";
                SaveDialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                
                TaskDialogButton SaveButton = new TaskDialogButton();
                SaveButton.ButtonType = ButtonType.Custom;
                SaveButton.Text = "Save my changes.";
                SaveButton.Default = true;
                SaveButton.CommandLinkNote = "Saves your changes then exit.";
              
                TaskDialogButton DontSaveButton = new TaskDialogButton();
                DontSaveButton.ButtonType = ButtonType.Custom;
                DontSaveButton.Text = "Just Exit.";
                DontSaveButton.CommandLinkNote = "Closes the form without saving the changes you made.";

                TaskDialogButton ExitCancel = new TaskDialogButton();
                ExitCancel.ButtonType = ButtonType.Cancel;

                SaveDialog.Buttons.Add(SaveButton);
                SaveDialog.Buttons.Add(DontSaveButton);
                SaveDialog.Buttons.Add(ExitCancel);

                TaskDialogButton Response= SaveDialog.ShowDialog();

                if (Response == SaveButton)
                {
                    SaveSettings();
                    Leave = true;
                }
                else if (Response == ExitCancel)
                {
                    e.Cancel = true;
                    return;
                }
                else { Leave = true; }
            }

            if (!Leave)
            {
                if (CustomMessageBox.Show(this, "Are you sure you want to exit?", "Close Settings", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            Application.Current.Shutdown();
        }

        #endregion

        #region **EventHandler for footer
        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            txtHeaderInfo.Text = "Change your logon screen with ease.";
            tabMain.SelectedItem = tabHome;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            txtHeaderInfo.Text = "Control how VLSR operates.";
            tabMain.SelectedItem = tabSettings;
            btnRefresh_Click(this, new RoutedEventArgs());
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            txtHeaderInfo.Text = "Help and about information for VLSR.";
            tabMain.SelectedItem = tabAbout;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
//Save the Form
            SaveSettings();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region **Events.

        void lstImages_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && lstImages.SelectedItem != null) { RemoveSelImage(); }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            btnAbout.CaptureMouse();
            btnAbout_Click(this, new RoutedEventArgs());
        }

        private void zoomButton_Click(object sender, RoutedEventArgs e)
        {
            zoomPopup.IsOpen = true;
        }

        void zoomSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            st.ScaleX = zoomSlider.Value;
            st.ScaleY = zoomSlider.Value;
        }

        void zoomPopup_MouseLeave(object sender, RoutedEventArgs e)
        {
            zoomPopup.IsOpen = false;
        }

        void ViennaServiceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate()
                    {
                        ServiceIsRunning(ThisUser.ViennaServiceIsRunning);
                    }
                ));
        }

        void SearchHelper_Closed(object sender, EventArgs e)
        {
            SearchHelper = null;
        }

        private void btnAdvancedSearch_Click(object sender, RoutedEventArgs e)
        {
            if (SearchHelper == null)
            {
                SearchHelper = new MainSearch();
                SearchHelper.ParentWindow = this;
                SearchHelper.Closed += new EventHandler(SearchHelper_Closed);
            }
           
            if (!SearchHelper.IsVisible)
                SearchHelper.Show();
            else
            {
                SearchHelper.Visibility = Visibility.Visible;
                SearchHelper.Focus();
            }
        }

        private void btnShortcutChange_Click(object sender, RoutedEventArgs e)
        {
            string v= "";
            Vienna.App.KeyGetter.ChangeShortCut(ref v);
            lblGlobalShortcut.Content = v;
            OktoExit = false;
        }

        private void btnUseDefualtGlobalKey_Click(object sender, RoutedEventArgs e)
        {
            lblGlobalShortcut.Content = Vienna.App.KeyGetter.Reset();
            OktoExit = false;
        }

        private void btnFindFolder_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;

            JPEGDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            try { JPEGDialog.AddPlace(GetAppPath + "Images", Microsoft.WindowsAPICodePack.Shell.FileDialogAddPlaceLocation.Top); }
            catch (Exception ex) { Vienna.Shared.ViennaShared.LogEvent(ex.Message); }
            JPEGDialog.IsFolderPicker = true;
            JPEGDialog.AllowNonFileSystemItems = true;
            JPEGDialog.DefaultDirectory = Microsoft.WindowsAPICodePack.Shell.KnownFolders.Libraries.Path;
            JPEGDialog.Title = "Select a folder from which to find JPEG Files";
            if (JPEGDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Cancel) { this.Cursor = Cursors.Arrow; return; }

            CheckAppend();
                if (btnSubFolderToggle.IsChecked == false)
                {
                    System.Threading.Thread SearchFolder = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(SearchFolders));
                    SearchFolder.SetApartmentState(System.Threading.ApartmentState.STA);
                    SearchFolder.Start(false);
                }
                else
                {
                    System.Threading.Thread SearchFolder = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(SearchFolders));
                    SearchFolder.SetApartmentState(System.Threading.ApartmentState.STA);
                    SearchFolder.Start(true);
                }
        }

        private void btnFindFile_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            int FilesAdded = 0;
            JPEGDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            try { JPEGDialog.AddPlace(GetAppPath + "Images", Microsoft.WindowsAPICodePack.Shell.FileDialogAddPlaceLocation.Top); }
            catch (Exception ex) { Vienna.Shared.ViennaShared.LogEvent(ex.Message); }
            JPEGDialog.AllowNonFileSystemItems = true;
            JPEGDialog.DefaultDirectory = Microsoft.WindowsAPICodePack.Shell.KnownFolders.Libraries.Path;
            JPEGDialog.Title = "Select a JPEG File";
            JPEGDialog.Multiselect = true;
            Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter JpegFilesFilter = new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter();
            JpegFilesFilter.DisplayName = "JPEG Files";
            JpegFilesFilter.Extensions.Add("jpg");
            JpegFilesFilter.Extensions.Add("jpeg");
            Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter AllFilesFilter = new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("All Files", "*.*");
            JPEGDialog.Filters.Add(JpegFilesFilter);
            JPEGDialog.Filters.Add(AllFilesFilter);
            if (JPEGDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Cancel) { this.Cursor = Cursors.Arrow; return; }
            
            foreach (string file in JPEGDialog.FileNames)
            {
                if (AddFile(file)) FilesAdded++;
            }
            this.Cursor = Cursors.Arrow;
            CustomMessageBox.Show(this, FilesAdded.ToString() + " JPEG file(s) added.", "VLSR Folder Search", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void lstImages_Drop(object sender, DragEventArgs e)
        {
            //System.Collections.Specialized.StringCollection Files = e.Data.GetData(DataFormats.FileDrop, true) as System.Collections.Specialized.StringCollection;
            //int FilesAdded = 0;
            //MessageBox.Show(Files[0]);
            //if (Files != null)
            //{
            //    FilesAdded = AddFile(Files);

            //    CustomMessageBox.Show(this, FilesAdded.ToString() + " file(s) added.", "Vienna Logon Screen Rotator",
            //    MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }

        private void tabMain_Drop(object sender, DragEventArgs e)
        {
            string[] Files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            int FilesAdded = 0;

            if (Files != null)
            {
                FilesAdded = AddFile(Files);

                CustomMessageBox.Show(this, FilesAdded.ToString() + " file(s) added.", "Vienna Logon Screen Rotator",
                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDisableVienna_Checked(object sender, RoutedEventArgs e)
        {
            { txtViennaEnabled.Text = "Vienna is enabled."; btnDisableVienna.Content = "_Disable"; }
            OktoExit = false;
        }

        private void btnDisableVienna_Unchecked(object sender, RoutedEventArgs e)
        {
            { txtViennaEnabled.Text = "Vienna is disabled."; btnDisableVienna.Content = "_Enable"; }
            OktoExit = false;
        }

        private void btnDisableVienna_Click(object sender, RoutedEventArgs e)
        {
            if (btnDisableVienna.IsChecked == false)
            {
                TaskDialog DisableDialog = new TaskDialog();
                DisableDialog.MainInstruction = "Warning: Enabling or Disabling VLSR is not reccommended. VLSR will not " +
                    "change your logon pictures and you cannot use VLSR global shortcut to change them. You also run the risk " +
                    "of creating instabilities. \n\n Continue Anyways?";
                DisableDialog.AllowDialogCancellation = false;
                DisableDialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                TaskDialogButton ContinueButton = new TaskDialogButton(ButtonType.Custom);
                ContinueButton.Text = "Yes, disable VLSR.";
                ContinueButton.CommandLinkNote = "Disables VLSR and stops the Vienna Service.";

                TaskDialogButton CancelButton = new TaskDialogButton(ButtonType.Cancel);
                CancelButton.Default = true;

                DisableDialog.Buttons.Add(ContinueButton);
                DisableDialog.Buttons.Add(CancelButton);

                if (DisableDialog.ShowDialog() != ContinueButton)
                {
                    btnDisableVienna.IsChecked = true; return;
                }
                ServiceManagement(ServiceAction.Stop);
                OktoExit = false;
            }
        }

        private void btnServiceToggle_Click(object sender, RoutedEventArgs e)
        {
            if (btnServiceToggle.IsChecked == false)
            {
                if (CustomMessageBox.Show(this, "Are you sure you want to pause the service? \n Stopping the service can cause irregularities.",
                    "Vienna Logon Screen Rotator", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ServiceManagement(ServiceAction.Pause);
                }
            }
            else
            {
                if ((Application.GetCookie(new Uri("http://mycomputer.vienna.firsttime")) == bool.FalseString))
                {
                    ServiceManagement(ServiceAction.Start);
                }
                else
                {
                    CustomMessageBox.Show(this, "As this is your first time, the service will be automatically started after you save.",
                        "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Information);
                    btnServiceToggle.IsChecked = false;
                }
            }
        }

        private void btnServiceToggle_Checked(object sender, RoutedEventArgs e)
        {
            { txtServiceIsRunning.Text = "Vienna Logon Screen Service is running."; btnServiceToggle.Content = "_Pause the Service."; }
        }

        private void btnServiceToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            { txtServiceIsRunning.Text = "Vienna Logon Screen Service is NOT running."; btnServiceToggle.Content = "_Start the Service"; }
        }

        private void btnSubFolderToggle_Checked(object sender, RoutedEventArgs e)
        {
            { txtSubFolders.Text = "VLSR will include subfolders in search."; btnSubFolderToggle.Content = "Exclude Sub_folders"; }
        }

        private void btnSubFolderToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            { txtSubFolders.Text = "VLSR will NOT include subfolders in search."; btnSubFolderToggle.Content = "Include Sub_folders"; }
        }
        
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            lstImages.Items.Refresh();
            btnServiceToggle.IsChecked = ThisUser.ViennaServiceIsRunning;
            UpdateSettingBG(new LogonPicture(ViennaShared.LogonPicturePath));
        }

        private void btnAppendToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            { txtAppendFiles.Text = "VLSR will clear list before adding new files."; btnAppendToggle.Content = "_Append"; }
        }

        private void btnAppendToggle_Checked(object sender, RoutedEventArgs e)
        {
            { txtAppendFiles.Text = "The list will always retain its existing content."; btnAppendToggle.Content = "_UnAppend"; }
        }

        private void cboHowToChange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHowToChange.SelectedIndex == cboHowToChange.Items.IndexOf(cboHtCCycle))
            {
                txtHowtoChange.Text = "VLSR will pick files as arranged in the list.";
            }
            else if (cboHowToChange.SelectedIndex == cboHowToChange.Items.IndexOf(cboHtCRandom))
            {
                txtHowtoChange.Text = "VLSR will pick logon pictures at random.";
            }
        }

        private void lstImages_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsFileDropList())
            {
                ContextMenu PasteThis = new ContextMenu();
                MenuItem Paste = new MenuItem();
                Paste.Header = "_Paste";
                Paste.Click += new RoutedEventHandler(Paste_Click);
                Paste.ToolTip = "Adds the image contained in the clipboard if it isn't already in the list";
                PasteThis.Items.Add(Paste);
                PasteThis.IsOpen = true;
            }
        }

        void Paste_Click(object sender, RoutedEventArgs e)
        {

            System.Collections.Specialized.StringCollection Files = Clipboard.GetFileDropList();
            int FilesAdded = 0;

            if (Files != null)
            {
                FilesAdded = AddFile(Files);

                CustomMessageBox.Show(this, FilesAdded.ToString() + " file(s) added.", "Vienna Logon Screen Rotator",
                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region **Helpers.

        internal void RemoveMultipleEntries(LogonPicture SelectedItem)
        {
            while (HasMultipleEntries(SelectedItem) > 1)
            {
                foreach (LogonPicture Entry in Photos)
                {
                    if (Entry.FileName == SelectedItem.FileName && Entry.FullPath != SelectedItem.FullPath)
                    {
                        lstImages.Items.Remove(Entry);
                        Photos.Remove(Entry);
                        break;
                    }
                }
            }
            OktoExit = false;
            //int index = (Photos.IndexOf(SelectedItem));
            //for (int i = 0; i < Photos.Count; i++)
            //{
            //    if (i == index) continue;
            //    if (Photos[i].FileName == SelectedItem.FileName)
            //    {
            //        Photos.Remove(Photos[i]);
            //    }
            //}

            //string s = ""; string[,] Recursive = new string[Photos.Count, 2];
            //int v = 0;
            //for (int i = 0; i <= Photos.Count - 1; i++)
            //{
            //    s += string.Concat(" [block] ", Photos[i].FileName);
            //}

            //for (int p = 0; p <= Photos.Count - 1; p++)
            //{
            //    string Pattern = Photos[p].FileName;
            //    MatchCollection match = Regex.Matches(s, Pattern);

            //    foreach (Match found in match)
            //    {
            //        Recursive[v, 0] = found.Value;
            //        Recursive[v, 1] = match.Count.ToString();
            //        v++;
            //        break;
            //    }
            //}

            //for (int k = 0; k <= Photos.Count - 1; k++)
            //{
            //    if (Photos[k].FileName == Recursive[k, 0] && Recursive[k, 1] != "1")
            //    {
            //        Photos.RemoveAt(k);
            //        lstImages.Items.RemoveAt(k);
            //    }
            //}
            //lstImages.Items.Refresh();
        }

        private void ServiceManagement(ServiceAction Action)
        {
            switch (Action)
            {
                case ServiceAction.Pause:
                    System.Diagnostics.ProcessStartInfo PauseService = new System.Diagnostics.ProcessStartInfo("VLSR.exe");
                    PauseService.Arguments = "/PauseService";
                    PauseService.UseShellExecute = true;
                    //StopService.Verb = "runas";
                    System.Diagnostics.Process.Start(PauseService);
                    break;

                case ServiceAction.Start:
                    System.Diagnostics.ProcessStartInfo StartService = new System.Diagnostics.ProcessStartInfo("VLSR.exe");
                    StartService.Arguments = "/StartService";
                    StartService.UseShellExecute = true;
                    //StartService.Verb = "runas";
                    System.Diagnostics.Process.Start(StartService);
                break;

                case ServiceAction.Stop:
                    System.Diagnostics.ProcessStartInfo StopService = new System.Diagnostics.ProcessStartInfo("VLSR.exe");
                    StopService.Arguments = "/StopService";
                    StopService.UseShellExecute = true;
                    //StopService.Verb = "runas";
                    System.Diagnostics.Process.Start(StopService);
                    break;
            }
            btnServiceToggle.IsChecked = ThisUser.ViennaServiceIsRunning;
        }

        private void ServiceIsRunning(bool? arg)
        {
            this.btnServiceToggle.IsChecked = arg;
        }

        private void SaveSettings()
        {
            ThisUser.ViennaEnabled = btnDisableVienna.IsChecked.Value;
            ThisUser.AppendFiles = btnAppendToggle.IsChecked.Value;
            ThisUser.HowToChange = (ViennaShared.ChangeStyle)Enum.Parse(typeof(ViennaShared.ChangeStyle), cboHowToChange.Text);
            ThisUser.WhenToChange = (ViennaShared.ChangePictureOn)Enum.Parse(typeof(ViennaShared.ChangePictureOn), cboWhenToChange.Text);
            ThisUser.Photos = this.Photos;
            ThisUser.SearchSubDirectories = btnSubFolderToggle.IsChecked.Value;
            ThisUser.ScaleTransformation = st.ScaleX;
            ThisUser.SaveSettings();
            if ((Application.GetCookie(new Uri("http://mycomputer.vienna.firsttime")) == bool.TrueString))
            {
                ServiceManagement(ServiceAction.Start);
            }
            OktoExit = true;
            CustomMessageBox.Show(this, "Settings saved successfully", "Vienna Logon Screen rotator",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsLoaded()
        {
            this.btnDisableVienna.IsChecked = ThisUser.ViennaEnabled;
            this.cboWhenToChange.Text = ThisUser.WhenToChange.ToString();
            this.cboHowToChange.Text = ThisUser.HowToChange.ToString();
            this.btnSubFolderToggle.IsChecked = ThisUser.SearchSubDirectories;
            this.btnAppendToggle.IsChecked = ThisUser.AppendFiles;
            st.ScaleX = ThisUser.ScaleTransformation;
            st.ScaleY = ThisUser.ScaleTransformation;
            UpdateSettingBG(ThisUser.CurrentBackground);
            lblGlobalShortcut.Content = Vienna.App.KeyGetter.ViennaGlobalShortcut;
        }

        private void NoSettings()
        {
            ThisUser.ExtractImageResource(ViennaShared.ImageResources.ViennaBG1, GetAppPath + "Images\\Vienna Resource 1.jpg");
            ThisUser.ExtractImageResource(ViennaShared.ImageResources.ViennaBG2, GetAppPath + "Images\\Vienna Resource 2.jpg");
            AddFile(GetAppPath + "Images\\Vienna Resource 1.jpg");
            AddFile(GetAppPath + "Images\\Vienna Resource 2.jpg");
        }

        private void ReadSettings(object args)
        {
            if ((File.Exists(ViennaShared.SettingsFilePath) && (Application.GetCookie(new Uri("http://mycomputer.vienna.firsttime")) == bool.FalseString)))
            {
                ThisUser = new ViennaShared(true);

                Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            SettingsLoaded();
                        }
                    ));

                foreach (LogonPicture JPEGFile in ThisUser.Photos)
                {
                    //AddFile(JPEGFile.FullPath);
                    Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Background,
                      new Action(
                        delegate()
                        {
                            if (File.Exists(JPEGFile.FullPath))
                                AddFile(JPEGFile.FullPath);
                        }
                    ));
                }
                               
            }
            else
            {
                Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            NoSettings();
                        }
                    ));
            }
            OktoExit = true; //Since It was just loaded from a file. It's ok to exit now.
        }

        internal void CheckAppend()
        {
            if (btnAppendToggle.IsChecked == false)
            {
                lstImages.Items.Clear(); Photos.Clear();
            }
        }

        void SearchFolders(object args)
        {
            string[] Folder = null; int FilesAdded = 0;
            try
            {
                if ((bool)args == false)
                {
                    Folder = Directory.GetFiles(JPEGDialog.FileAsShellObject.ParsingName, "*.jpg", SearchOption.TopDirectoryOnly);
                }
                else
                {
                    Folder = Directory.GetFiles(JPEGDialog.FileAsShellObject.ParsingName, "*.jpg", SearchOption.AllDirectories);
                }
            }
            catch (Exception) // Occurs if the user selects a compressed folder like zip files.
            {
                ViennaShared.LogEvent("Could not open the folder " + JPEGDialog.FileAsShellObject.ParsingName);
                Dispatcher.Invoke(
System.Windows.Threading.DispatcherPriority.Send,
new Action(
delegate()
{
    CustomMessageBox.Show(this, "Could not open the folder " + JPEGDialog.FileAsShellObject.ParsingName,
        "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Error);
}));
            }

            if (Folder == null || Folder.Length == 0) { UpdateCursor(Cursors.Arrow); return; }

            Dispatcher.Invoke(
System.Windows.Threading.DispatcherPriority.Send,
new Action(
delegate()
{
    CheckAppend();
}));

            foreach (string strJPEG in Folder)
            {
                Dispatcher.Invoke(
System.Windows.Threading.DispatcherPriority.Background,
new Action(
delegate()
{
    if (AddFile(strJPEG))
    {
        FilesAdded++;
    }
}
));

            }
            UpdateCursor(Cursors.Arrow);
            Dispatcher.Invoke(
System.Windows.Threading.DispatcherPriority.Send,
new Action(
delegate()
{
    CustomMessageBox.Show(this, FilesAdded.ToString() + " JPEG file(s) added.", "VLSR Folder Search", MessageBoxButton.OK, MessageBoxImage.Information);
}));
        }

        void UpdateCursor(Cursor cursor)
        {
            Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Send,
                new Action(
                delegate()
                { this.Cursor = cursor; }));
        }

        internal bool AddFile(string File)
        {
            lock (this)
            {
                if (!IsValidLogonFile(File)) { return false; }

                LogonPicture Picture = new LogonPicture(File);
                

                Picture.Padding = new Thickness(3, 8, 3, 8);
                Picture.Margin = new Thickness(5, 3, 5, 2);
                Picture.MouseDoubleClick += delegate { ShowFileAttributes(); };
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(st);
                tg.Children.Add(new RotateTransform());
                Picture.LayoutTransform = tg;
                Picture.Tag = File;

                // Construct a Context Menu for the item
                ContextMenu ItemContext = new System.Windows.Controls.ContextMenu();

                //Make this background
                MenuItem mnuMakeBG = new MenuItem();
                mnuMakeBG.Header = "_Set this as background";
                mnuMakeBG.ToolTip = "Sets this picture as your current logon screen picture. \n Might require administrator priviledges.";
                Image MakeBGIcon = new Image();
                MakeBGIcon.Source = new BitmapImage(new Uri("/Vienna%20Logon%20Screen%20Rotator;component/Images/About.png", UriKind.Relative));
                MakeBGIcon.Height = 32;
                MakeBGIcon.Width = 32;
                mnuMakeBG.Icon = MakeBGIcon;
                mnuMakeBG.Click += delegate { MakeSelBG(); };

                //Find in Explorer.
                MenuItem mnuExplore = new MenuItem();
                mnuExplore.Header = "Find in _Explorer";
                Image ExploreIcon = new Image();
                ExploreIcon.Source = new BitmapImage(new Uri("/Vienna%20Logon%20Screen%20Rotator;component/Images/Explore.ico", UriKind.Relative));
                mnuExplore.Icon = ExploreIcon;
                mnuExplore.ToolTip = "Locate this file in explorer.";
                mnuExplore.Click += delegate { FindinExplorer(); };

                //Remove this image
                MenuItem mnuRemove = new MenuItem();
                mnuRemove.Header = "_Remove this image";
                Image RemoveIcon = new Image();
                RemoveIcon.Source = new BitmapImage(new Uri("/Vienna%20Logon%20Screen%20Rotator;component/Images/Remove.png", UriKind.Relative));
                mnuRemove.Icon = RemoveIcon;
                mnuRemove.ToolTip = "Remove this image from the list.";
                mnuRemove.Click += delegate { RemoveSelImage(); };

                //Add the items
                ItemContext.Items.Add(mnuMakeBG);
                ItemContext.Items.Add(mnuExplore);
                ItemContext.Items.Add(mnuRemove);

                Picture.Focus();
                Picture.ContextMenu = ItemContext;

                Photos.Add(Picture);
                lstImages.Items.Add(Picture);
                OktoExit = false;
                return true;
            }
        }

        //internal void AddFile(object items)
        //{
        //    List<SearchItem> Files = (List<SearchItem>)items;
        //    foreach (SearchItem item in Files)
        //    {
        //        AddFile(item.ParsingName);
        //    }
        //}

        internal void AddFile(List<SearchItem> items)
        {
            foreach (SearchItem file in items)
            {
                AddFile(file.ParsingName);
            }
        }

        internal int AddFile(System.Collections.Specialized.StringCollection Files)
        {
            int FilesAdded = 0;
            CheckAppend();
            try
            {
                foreach (string JPEGPath in Files)
                {
                    try
                    {
                        if (StringOperations.IsDirectory(JPEGPath))
                        {
                            string[] Folder;
                            if (btnSubFolderToggle.IsChecked.Value == true)
                            {
                                Folder = Directory.GetFiles(JPEGPath, "*.jpg", SearchOption.AllDirectories);
                            }
                            else
                            {
                                Folder = Directory.GetFiles(JPEGPath, "*.jpg", SearchOption.TopDirectoryOnly);
                            }

                            if (Folder == null || Folder.Length == 0) { continue; }

                            foreach (string JPEG in Folder)
                            {
                                if (AddFile(JPEG)) FilesAdded++;
                            }
                        }
                        else
                        {
                            if (AddFile(JPEGPath)) FilesAdded++;
                        }
                    }
                    catch
                    {
                        ViennaShared.LogEvent("Could not open " + JPEGPath);
                        continue;
                    }
                }
            }
            catch { }
            return FilesAdded;
        }

        internal int AddFile(string[] Files)
        {
            int FilesAdded = 0;
            CheckAppend();
            try
            {
                foreach (string JPEGPath in Files)
                {
                    try
                    {
                        if (StringOperations.IsDirectory(JPEGPath))
                        {
                            string[] Folder;
                            if (btnSubFolderToggle.IsChecked.Value == true)
                            {
                                Folder = Directory.GetFiles(JPEGPath, "*.jpg", SearchOption.AllDirectories);
                            }
                            else
                            {
                                Folder = Directory.GetFiles(JPEGPath, "*.jpg", SearchOption.TopDirectoryOnly);
                            }

                            if (Folder == null || Folder.Length == 0) { continue; }

                            foreach (string JPEG in Folder)
                            {
                                if (AddFile(JPEG)) FilesAdded++;
                            }
                        }
                        else
                        {
                            if (AddFile(JPEGPath)) FilesAdded++;
                        }
                    }
                    catch
                    {
                        ViennaShared.LogEvent("Could not open " + JPEGPath);
                        continue;
                    }
                }
            }
            catch { }
            return FilesAdded;
        }

        private bool IsValidLogonFile(string FileName)
        {
            bool Valid;

            if (!File.Exists(FileName)) return false;

            LogonPicture JPGFile = new LogonPicture(FileName);

            ////Check if the FilePath Matches any other file in the List.
            if (Photos.Contains(JPGFile)) return false;

            if (JPGFile.IsValidLogonPicture == false)
            {
                //SearchLog += "\n" + string.Format("The File {0} did not pass validation", JPGFile.Name);
                return false;
            }
            else
            {
                Valid = true;
            }

            return Valid;
        }

        internal static string GetAppPath
        {
            get
            {
                string AppPath = Environment.CurrentDirectory.Substring(0,
                    Environment.CurrentDirectory.IndexOf("bin") < 0
                    ? Environment.CurrentDirectory.Length
                    : Environment.CurrentDirectory.IndexOf("bin"));

                if (AppPath.ToLower().Contains("system")) { AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location; }

                AppPath = AppPath.EndsWith("\\") == true ? AppPath : AppPath + "\\";

                return AppPath;
            }
        }

        private void ShowFileAttributes()
        {
            LogonPicture SelectedFile = (lstImages.SelectedItem as LogonPicture);
            bool isLogonOk = true;

            //if (SelectedFile.ImageHeight > SystemParameters.VirtualScreenHeight || SelectedFile.ImageWidth > SystemParameters.VirtualScreenWidth)
            //{
            //    //SearchLog += "\n" + string.Format("The File {0} may not work correctly as the image height or width is greater than that of your screen size"
            //    //    , JPGFile.Name);
            //    isLogonOk = false;
            //}
            int MultipleEntries = HasMultipleEntries(SelectedFile);
            if (MultipleEntries > 1) isLogonOk = false;
       

            TaskDialog AttributeDialog = new TaskDialog();
            AttributeDialog.CustomMainIcon = MainResource.JPEG;
            AttributeDialog.MainIcon = TaskDialogIcon.Custom;
            TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
            okButton.Default = true;
            AttributeDialog.Buttons.Add(okButton);

            TaskDialogButton MultipleEntryButton = new TaskDialogButton("Remove multiple entries.");
            MultipleEntryButton.CommandLinkNote = "Find and remove all files with the same filename as this file";

            AttributeDialog.WindowTitle = "Vienna Logon Screen Rotator";
            AttributeDialog.MainInstruction = SelectedFile.FileName + " attributes.";
            AttributeDialog.AllowDialogCancellation = true;
            AttributeDialog.Content = "Location: " + SelectedFile.Attributes.Location;
            AttributeDialog.Content += "\nSize: " + SelectedFile.Size;
            AttributeDialog.Content += "\nDimensions: " + SelectedFile.ImageHeight.ToString() +
                                        " x " + SelectedFile.ImageWidth.ToString();
            AttributeDialog.Content += "\nCreated: " + SelectedFile.Attributes.GetCreationTime;
            AttributeDialog.Content += "\nModified: " + SelectedFile.Attributes.GetModifiedTime;
            AttributeDialog.Content += "\nAccessed: " + SelectedFile.Attributes.GetAccessedTime;
            
            AttributeDialog.Content += "\n\nRead Only: " + SelectedFile.Attributes.isReadOnly.ToString();
            AttributeDialog.Content += "    Hidden: " + SelectedFile.Attributes.isHidden.ToString();
            AttributeDialog.Content += "\nArchived: " + SelectedFile.Attributes.isArchive.ToString();
            AttributeDialog.Content += "    System: " + SelectedFile.Attributes.isSystem.ToString();

            AttributeDialog.ExpandedControlText = "Hide Details.";
            AttributeDialog.CollapsedControlText = "Show Additional Information.";

            AttributeDialog.ExpandedInformation = isLogonOk
                ? "No faults detected with this file."
                : MultipleEntries.ToString() + " other images have the same filename as this file but in different paths.";

            if (!isLogonOk) AttributeDialog.Buttons.Add(MultipleEntryButton);

            if (AttributeDialog.ShowDialog() == MultipleEntryButton)
            {
                RemoveMultipleEntries(SelectedFile);
            }
        }

        private void MakeSelBG()
        {
            ThisUser.ChangeLogonPicture(lstImages.SelectedItem as LogonPicture);
            UpdateSettingBG((lstImages.SelectedItem as LogonPicture));
            CustomMessageBox.Show(this, "Your Logon Screen Picture has been changed. \nYou can press 'Window Key + L' to view it",
                "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FindinExplorer()
        {
            System.Diagnostics.ProcessStartInfo Explore = new System.Diagnostics.ProcessStartInfo("Explorer");
            Explore.Arguments = "/select," + (lstImages.SelectedItem as LogonPicture).FullPath;
            System.Diagnostics.Process.Start(Explore);
        }

        private void RemoveSelImage()
        {
            if (CustomMessageBox.Show(this,string.Format("Are you sure you want to remove image '{0}'?", (lstImages.SelectedItem as LogonPicture).FileName), "Remove image",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            {
                return;
            }

            Photos.RemoveAt(lstImages.SelectedIndex);
            lstImages.Items.RemoveAt(lstImages.SelectedIndex);
        }

        private void UpdateSettingBG(LogonPicture CurrentBG)
        {
            try
            {
                if (!String.IsNullOrEmpty(CurrentBG.FullPath))
                    imgCurrentBG.Source = (ImageSource)CurrentBG.Thumbnail;
                else
                {
                    if (File.Exists(Vienna.Shared.ViennaShared.LogonPicturePath))
                        imgCurrentBG.Source = new BitmapImage(new Uri(Vienna.Shared.ViennaShared.LogonPicturePath));
                }
            }
            catch { }
        }

        private int HasMultipleEntries(LogonPicture Filename)
        {
            int MultipleEntries = 0;
            foreach (LogonPicture Selected in Photos)
            {
                if (Selected.FileName == Filename.FileName) { MultipleEntries++; }
            }

            return MultipleEntries;
        }

        #endregion
    }

    /// <summary>
    /// ImageView displays image files using themselves as their icons.
    /// In order to write our own visual tree of a view, we should override its
    /// DefaultStyleKey and ItemContainerDefaultKey. DefaultStyleKey specifies
    /// the style key of ListView; ItemContainerDefaultKey specifies the style
    /// key of ListViewItem.
    /// </summary>
    //public class ImageView : ViewBase
    //{
    //    #region DefaultStyleKey

    //    protected override object DefaultStyleKey
    //    {
    //        get { return new ComponentResourceKey(GetType(), "ImageView"); }
    //    }

    //    #endregion

    //    #region ItemContainerDefaultStyleKey

    //    protected override object ItemContainerDefaultStyleKey
    //    {
    //        get { return new ComponentResourceKey(GetType(), "ImageViewItem"); }
    //    }

    //    #endregion
    //}
}
