using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Security.AccessControl;

namespace BondTech.Vienna.Shared
{
    public class ViennaShared
    {
        #region **Properties.
        /// <summary>Returns the Windows\System32 directory of the user.
        /// </summary>
        public static string SystemDirectory
        {
            get
            {
                string SysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

                SysDir = SysDir.EndsWith("\\") == true ? SysDir : SysDir + "\\";
                return SysDir;
            }
        }
        /// <summary>Returns the Root directory of the user
        /// </summary>
        private static string RootDirectory
        {
            get
            {
                string RootDir = Directory.GetDirectoryRoot( Environment.GetFolderPath(Environment.SpecialFolder.System));

                RootDir = RootDir.EndsWith("\\") == true ? RootDir : RootDir + "\\";
                return RootDir;
            }
        }
        /// <summary>This is where settings and other files for Vienna will be saved.
        /// </summary>
        private static string ViennaRootPath
        {
            get
            {
                return (RootDirectory + "VLSR\\");
            }
        }
        /// <summary>Returns the path where Logon background pictures are saved.
        /// </summary>
        public static string LogonPicturePath
        {
            get
            {
                return (SystemDirectory + "oobe\\info\\backgrounds\\backgroundDefault.jpg");
            }
        }
        /// <summary>Returns the path where the settings file for vienna is saved.
        /// </summary>
        public static string SettingsFilePath
        {
            get
            {
                return (ViennaRootPath + "ViennaSettings.XML");
            }
        }
        /// <summary>This will be used to Log errors.
        /// </summary>
        public static string EventLogPath
        {
            get
            {
                return (ViennaRootPath + "ViennaEventLog.Log");
            }
        }
        /// <summary>The service controller for ViennaLogonService
        /// </summary>
        private ServiceController ViennaLogonService;
        /// <summary>Represents when the service changes the logon pictures.
        /// </summary>
        public enum ChangePictureOn
        {
            Hotkey = 0,
            Lock = 1,
            Shutdown = 2,
            Startup = 3,

        }
        /// <summary>Represents how the pictures are picked.
        /// </summary>
        public enum ChangeStyle
        {
            Cycle = 0,
            Random =1
        }
        /// <summary>A collection of all the images that can be used as a background picture.
        /// </summary>
        public List<LogonPicture> Photos { get; set; }
        /// <summary>Gets or sets if Vienna Logon Screen Rotator should be disabled.
        /// </summary>
        public bool ViennaEnabled { get; set; }
        /// <summary>Gets or sets when the picture should be changed.
        /// </summary>
        public ChangePictureOn WhenToChange { get; set; }
        /// <summary>Gets or sets if subdirectories should be included in Folder search.
        /// </summary>
        public bool SearchSubDirectories { get; set; }
        /// <summary>Gets or sets if the list should be cleared before new files are added.
        /// </summary>
        public bool AppendFiles { get; set; }
        /// <summary>Gets or sets if the how the images will be changed.
        /// Whether as arranged in the list or at random.
        /// </summary>
        public ChangeStyle HowToChange { get; set; }
        /// <summary>Value controlling how the items are liad out. Zoom.
        /// </summary>
        public double ScaleTransformation { get; set; }
        /// <summary>Returns the original filename of the current background picture set by VLSR
        /// </summary>
        public LogonPicture CurrentBackground { get; private set; }
        /// <summary>Gets the next picture that would be used as the Logon Picture.
        /// </summary>
        private LogonPicture NextLogonPicture
        {
            get
            {
                int CurrentBGIndex = Photos.IndexOf(CurrentBackground);
                switch (HowToChange)
                {
                    case ChangeStyle.Cycle:
                        int CycleIndex = 0;
                        if (CurrentBGIndex != Photos.Count - 1) CycleIndex = CurrentBGIndex + 1;
                        return Photos[CycleIndex];

                    case ChangeStyle.Random:
                        Random rndNextPic = new Random();
                        int RandomIndex;
                    GenRandom: RandomIndex = rndNextPic.Next(0, Photos.Count - 1);
                        if (RandomIndex == CurrentBGIndex) goto GenRandom;
                        return Photos[RandomIndex];
                }

                return null;
            }
        }
        /// <summary>Contains a List of Embedded Image Resources.
        /// </summary>
        public enum ImageResources
        {
            ViennaBG1 = 0,
            ViennaBG2 = 1
        }
        #endregion

        #region **Ctors.
        /// <summary>The main constructor for this class.
        /// </summary>
        /// <param name="ReadSettings">Specifies if the Class properties should be initialized with
        /// the ones in the user settings file.</param>
        public ViennaShared(bool ReadConfig = false)
        {
            this.Photos = new List<LogonPicture>();
            if (ReadConfig == true) ReadSettings();
            ViennaLogonService = new ServiceController("ViennaLogonService");
        }
        #endregion

        #region **Helpers.
        /// <summary>Grants full access to the path specified.
        /// This method should be called with Administrator priviledges.
        /// </summary>
        /// <param name="fullPath">The full path of an existing file or folder to grant full access to.</param>
        /// <returns></returns>
        public static bool GrantAccess(string fullPath)
        {
            if (!AdminPriviledge.IsAdmin()) return false;
            try
            {
                DirectoryInfo dInfo = new DirectoryInfo(fullPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl,
                                                                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                                 PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
                return true;
            }
            catch
            {
                LogEvent("Could not grant access to " + fullPath);
                return false;
            }
        }
        /// <summary>Logs an event in the applications EventLogPath
        /// </summary>
        /// <param name="EventMessage">The occurence to log.</param>
        public static void LogEvent(string EventMessage)
        {
            try
            {
                using (StreamWriter Logger = new StreamWriter(EventLogPath, true))
                {
                    Logger.WriteLine("Event logged at " + DateTime.Now.ToString());
                    Logger.WriteLine(EventMessage);
                    Logger.WriteLine(Environment.NewLine);
                }
            }
            catch { }
        }
        /// <summary>Optimizes Vienna Logon Screen Rotator for first time use. Create directories and backs up old ones.
        /// This should be called with Administrator priviledges.
        /// </summary>
        public static void FirstTimeOptimization()
        {
            //Create the needed directories if they exist and backup any existing data.
            if (!AdminPriviledge.IsAdmin()) return;
            if (!Directory.Exists(Directory.GetParent(LogonPicturePath).FullName))
            {
                Directory.CreateDirectory(Directory.GetParent(LogonPicturePath).FullName);
            }

            if (!Directory.Exists(ViennaRootPath))
            {
                Directory.CreateDirectory(ViennaRootPath);
            }

            if (File.Exists(LogonPicturePath))
            {
                //The user already has a custom logon picture, back it up.
                string LogonBackup = LogonPicturePath + "_backup";
                File.Copy(LogonPicturePath, LogonBackup, true);
            }

            GrantAccess((SystemDirectory + "oobe\\info\\backgrounds"));
            GrantAccess(ViennaRootPath);
        }
        /// <summary>Stops ViennaLogonService and deletes the user configuration file.
        /// </summary>
        public void ResetConfiguration()
        {
            LogEvent("Configuration reset");
            StopViennaService();
            if (File.Exists(SettingsFilePath)) File.Delete(SettingsFilePath);
        }
        /// <summary>Gets a value indication if ViennaLogonService is running, stopped or paused.
        /// True if ViennaLogonService is already running or start pending.
        /// False if the service is stopped or stop pending.
        /// null if the service is paused.
        /// </summary>
        public bool? ViennaServiceIsRunning
        {
            get
            {
                try
                {
                    ViennaLogonService.Refresh();
                    switch (ViennaLogonService.Status)
                    {
                        case ServiceControllerStatus.Stopped:
                        case ServiceControllerStatus.StopPending:
                            return false;

                        case ServiceControllerStatus.Running:
                        case ServiceControllerStatus.StartPending:
                        case ServiceControllerStatus.ContinuePending:
                            return true;

                        default:
                            return null;
                    }
                }
                catch { return false; }//Service is not installed.
            }
        }
        /// <summary>Starts ViennaLogonService if it is paused or stopped.
        /// This should be called with administrator priviledges
        /// </summary>
        public void StartViennaService()
        {
            //Starting or stopping a windows service requires Administrator priviledges.
            if (!AdminPriviledge.IsAdmin()) return;
            try
            {
                if (ViennaServiceIsRunning.Value == false)
                {
                    ViennaLogonService.Start();
                }
                else if (ViennaServiceIsRunning.HasValue == false)
                    ViennaLogonService.Continue();
            }
            catch
            {
                CustomMessage.CustomMessageBox.Show("ViennaLogonService could not be started. Verify that the service is installed on this machine",
                    "Vienna Logon Screen Rotator");//, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        /// <summary>Stops the service if it running or pauses it.
        /// This method should be called with administrator priviledges.
        /// </summary>
        /// <param name="IsPause">Specifies that the service should just be paused.</param>
        public void StopViennaService(bool IsPause = false)
        {
            //Starting or stopping a windows service requires Administrator priviledges.
            if (!AdminPriviledge.IsAdmin()) return;
            try
            {
                if (IsPause)
                {
                    if (ViennaServiceIsRunning.Value == true)
                        ViennaLogonService.Pause();
                }
                else
                {
                    if (ViennaServiceIsRunning.Value == true || ViennaServiceIsRunning.HasValue == false)
                        ViennaLogonService.Stop();
                }
            }
            catch
            {
                CustomMessage.CustomMessageBox.Show("ViennaLogonService could not be stopped/paused. Verify that the service is installed on this machine",
                    "Vienna Logon Screen Rotator");//, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        /// <summary>Extracts an Image Resource to a specified location.
        /// </summary>
        /// <param name="resName">The Name of the Image resource to extract.</param>
        /// <param name="Dest">The Path to extract the image to.</param>
        public void ExtractImageResource(ImageResources resName, string Dest)
        {
            try
            {
                //FileStream writer = new FileStream(Dest, FileMode.Create) { Position = 0 };
                switch (resName)
                {
                    case ImageResources.ViennaBG1:
                        ((System.Drawing.Image)MainResource.ViennaBG1).Save(Dest);
                        //((System.Drawing.Image)MainResource.ViennaBG1).Save(writer, System.Drawing.Imaging.ImageFormat.Jpeg);
                        //writer.Flush();
                        break;
                    case ImageResources.ViennaBG2:
                        ((System.Drawing.Image)MainResource.ViennaBG2).Save(Dest);
                        break;
                }
            }
            catch (Exception e)
            {
                LogEvent(e.Message + " when extracting resource.");
            }
        }
        #endregion

        #region **Handle Settings.
        /// <summary>Reads the user's saved settings from the saved XML file.
        /// </summary>
        public void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ViennaSettings));
            
                ViennaSettings XmlFile = new ViennaSettings();
                string[] Pictures = new string[Photos.Count];
                int i = 0;
                foreach (LogonPicture JPEGFile in Photos)
                {
                    Pictures[i] = JPEGFile.FullPath;
                    i++;
                }
                
                XmlFile.AppendFiles = this.AppendFiles;

                try
                {
                    XmlFile.CurrentBackGround = this.CurrentBackground.FullPath;
                }
                catch
                {
                    XmlFile.CurrentBackGround = null;
                }

                XmlFile.HowToChange = this.HowToChange;
                XmlFile.Pictures = Pictures;
                XmlFile.SearchSubDirectories = this.SearchSubDirectories;
                XmlFile.ViennaEnabled = this.ViennaEnabled;
                XmlFile.WhenToChange = this.WhenToChange;
                XmlFile.ScaleTransformation = this.ScaleTransformation;

                using (TextWriter writer = new StreamWriter(SettingsFilePath))
                {
                    serializer.Serialize(writer, XmlFile);
                }
        }
        /// <summary>Saves the user's settings to an XML file. in the SettingsFilePath.
        /// </summary>
        private void ReadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ViennaSettings));
                serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
                serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);

                ViennaSettings XMLFile = new ViennaSettings();

                using (FileStream Reader = new FileStream(SettingsFilePath, FileMode.Open))
                {
                    XMLFile = (ViennaSettings)serializer.Deserialize(Reader);
                }

                this.Photos.Clear();

                foreach (string JPEGFile in XMLFile.Pictures)
                {
                    if (File.Exists(JPEGFile))
                    {
                        LogonPicture jpgFile = new LogonPicture(JPEGFile);
                        this.Photos.Add(jpgFile);
                    }
                }

                this.ScaleTransformation = XMLFile.ScaleTransformation;
                this.AppendFiles = XMLFile.AppendFiles;
                this.CurrentBackground = (!string.IsNullOrEmpty(XMLFile.CurrentBackGround))
                    ? new LogonPicture(XMLFile.CurrentBackGround) : null;
                this.HowToChange = XMLFile.HowToChange;
                this.SearchSubDirectories = XMLFile.SearchSubDirectories;
                this.ViennaEnabled = XMLFile.ViennaEnabled;
                this.WhenToChange = XMLFile.WhenToChange;
            }
        }
        /// <summary>Catches an unknown node in the XML file.
        /// </summary>
        /// <param name="sender">The sender </param>
        /// <param name="e"></param>
        void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            LogEvent(string.Format("{0} \n The XML file has been tampered with", e.Text));
        }
        /// <summary>Catches and logs an unknown attribute in the XML file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            LogEvent(string.Format("{0} \n The XML file has been tampered with", e.Attr));
        }
        #endregion

        #region **Main.
        /// <summary>Changes the Logon Picture.
        /// </summary>
        /// <param name="LogonPic">The instance of the LogonPicture to use as the background.</param>
        public void ChangeLogonPicture(LogonPicture LogonPic)
        {
            if (LogonPic.IsValidLogonPicture)
            {
                FileAttributesEx LogonFileAttr = new FileAttributesEx(LogonPicturePath);
                LogonFileAttr.Clear();
                LogonPic.Attributes.Clear();

                File.Copy(LogonPic.FullPath, LogonPicturePath, true);
                CurrentBackground = LogonPic;
                SaveSettings();
                LogEvent("Logon Screen Picture changed by user.");
            }
        }
        /// <summary>Changes the Logon Picture.
        /// </summary>
        /// <param name="OccurringEvent">The event on which this event is being called.</param>
        public void ChangeLogonPicture(ChangePictureOn OccurringEvent)
        {
            if (!ViennaEnabled) return;
            if (OccurringEvent == ChangePictureOn.Hotkey || OccurringEvent == this.WhenToChange)
            {
                FileAttributesEx LogonPicAttr = new FileAttributesEx(LogonPicturePath);
                LogonPicAttr.Clear();
                CurrentBackground = NextLogonPicture;
                File.Copy(CurrentBackground.FullPath, LogonPicturePath, true);
                SaveSettings();
                if (OccurringEvent == ChangePictureOn.Hotkey)
                    LogEvent("Logon picture changed by Hotkey.");
            }
        }
        #endregion
    }

    /// <summary>Class used in serializing the user's settings to an XML file.
    /// </summary>
    [XmlRootAttribute(ElementName = "Vienna", Namespace = "http://www.facebook.com/bondtech", IsNullable = false)]
    public class ViennaSettings
    {
        [XmlArrayItem(ElementName = "Path")]
        public string[] Pictures;

        [XmlElement(IsNullable = true)]
        public string CurrentBackGround;

        public bool ViennaEnabled;

        public bool SearchSubDirectories;

        public bool AppendFiles;

        public ViennaShared.ChangePictureOn WhenToChange;

        public ViennaShared.ChangeStyle HowToChange;

        public double ScaleTransformation;
    }

    public class ViennaServiceClass
    {
        #region **Properties
        public string[] Pictures { get; private set; }

        public string CurrentBackGround { get; private set; }

        public bool ViennaEnabled { get; private set; }

        private bool SearchSubDirectories { get; set; }

        private bool AppendFiles { get; set; }

        public ViennaShared.ChangePictureOn WhenToChange { get; set; }

        private ViennaShared.ChangeStyle HowToChange { get; set; }

        private double ScaleTransformation;

        /// <summary>Gets the next picture in the list according to how the pictures are meant to change.
        /// </summary>
        /// <param name="CurrentPicture">The picture file path to serve as current picture that needs changing.</param>
        /// <param name="Files">The collection of images.</param>
        /// <param name="HowToChange">Specifies how the next image should be picked.</param>
        /// <returns>The next string(file path)in the collection of Files.</returns>
        private string NextLogonPicture
        {
            get
            {
                if (Pictures == null || Pictures.Length == 0) return null;

                switch (HowToChange)
                {
                    case ViennaShared.ChangeStyle.Random:
                        Random Rand = new Random();
                    GenRand: int RandIndex = Rand.Next(0, (this.Pictures.Length - 1));
                        //Eliminate redundancy.
                        if (this.Pictures.Length > 2 && this.Pictures[RandIndex] == this.CurrentBackGround)
                        {
                            goto GenRand;
                        }
                        return this.Pictures[RandIndex];

                    case ViennaShared.ChangeStyle.Cycle:
                        int index = 0;
                                if (!string.IsNullOrEmpty(this.CurrentBackGround) && Pictures.Length > 1)
                                {
                                    for (int CycleIndex = 0; CycleIndex <= Pictures.Length - 1; CycleIndex++)
                                    {
                                        if (Pictures[CycleIndex] == this.CurrentBackGround)
                                        {
                                            index = CycleIndex + 1;
                                            break;
                                        }
                                    }
                                }

                                if (index == Pictures.Length) { index = 0; }
                                return Pictures[index];
                }
                return null;
            }
        }
        #endregion

        #region **Ctor.
        public ViennaServiceClass() { this.ReadSettings(); }
        #endregion

        #region **Handle Settings.
        private void ReadSettings()
        {
            if (File.Exists(ViennaShared.SettingsFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ViennaSettings));
                serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
                serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);

                ViennaSettings XMLFile = new ViennaSettings();

                using (FileStream Reader = new FileStream(ViennaShared.SettingsFilePath, FileMode.Open))
                {
                    XMLFile = (ViennaSettings)serializer.Deserialize(Reader);
                }

                this.Pictures = XMLFile.Pictures;
                this.ScaleTransformation = XMLFile.ScaleTransformation;
                this.AppendFiles = XMLFile.AppendFiles;
                this.CurrentBackGround = (!string.IsNullOrEmpty(XMLFile.CurrentBackGround))
                    ? XMLFile.CurrentBackGround : null;
                this.HowToChange = XMLFile.HowToChange;
                this.SearchSubDirectories = XMLFile.SearchSubDirectories;
                this.ViennaEnabled = XMLFile.ViennaEnabled;
                this.WhenToChange = XMLFile.WhenToChange;
            }
        }

        private void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ViennaSettings));

            ViennaSettings XmlFile = new ViennaSettings();

            XmlFile.AppendFiles = this.AppendFiles;
            XmlFile.HowToChange = this.HowToChange;
            XmlFile.Pictures = Pictures;
            XmlFile.CurrentBackGround = this.CurrentBackGround;
            XmlFile.SearchSubDirectories = this.SearchSubDirectories;
            XmlFile.ViennaEnabled = this.ViennaEnabled;
            XmlFile.WhenToChange = this.WhenToChange;
            XmlFile.ScaleTransformation = this.ScaleTransformation;

            using (TextWriter writer = new StreamWriter(ViennaShared.SettingsFilePath))
            {
                serializer.Serialize(writer, XmlFile);
            }
        }

        void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            ViennaShared.LogEvent(string.Format("{0} \n The XML file has been tampered with", e.Text));
        }

        void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            ViennaShared.LogEvent(string.Format("{0} \n The XML file has been tampered with", e.Attr));
        }
        #endregion

        #region *Main
        public void ChangeLogonPicture(ViennaShared.ChangePictureOn OccurringEvent)
        {
            if (!this.ViennaEnabled) return;
            if (OccurringEvent != this.WhenToChange) return;

            FileAttributesEx LogonPicAttr = new FileAttributesEx(ViennaShared.LogonPicturePath);
            LogonPicAttr.Clear();

            this.CurrentBackGround = NextLogonPicture;

            if (CurrentBackGround == null) return;

            File.Copy(CurrentBackGround, ViennaShared.LogonPicturePath, true);
            SaveSettings();
        }
        #endregion
    }
}