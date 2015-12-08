using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Controls;
namespace BondTech.Vienna.Shared
{
    public class LogonPicture : ContentControl, IEquatable<LogonPicture>
    {
        string name;
        DateTime dateTime;
        string size;
        string path;
        string _filename;
        FileInfo info;
        System.Windows.Media.Imaging.BitmapDecoder bd;

        public LogonPicture() { }

        public LogonPicture(string filename)
        {
            info = new FileInfo(filename);
            size = (info.Length / 1024).ToString("N0") + " KB";
            dateTime = info.LastWriteTime;
            name = info.Name;
            path = info.DirectoryName;
            _filename = filename;
            SetContent();
            SetToolTip();
        }

        public bool Equals(LogonPicture Other)
        {
            if (this.FullPath == Other.FullPath)
                return true;
            else
                return false;
        }

        public bool Equals(object obj)
        {
            if (obj == null) return (base.Equals(obj));
            if (!(obj is LogonPicture))
                throw new InvalidCastException("The 'obj' argument is not a LogonPicture.");
            else
                return Equals(obj as LogonPicture);
        }

        public int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(LogonPicture A, LogonPicture B)
        {
                return A.Equals(B);
        }

        public static bool operator !=(LogonPicture A, LogonPicture B)
        {
            return (!A.Equals(B));
        }

        public static explicit operator string(LogonPicture ToConvert)
        {
            return ToConvert.FullPath;
        }

        public string FileName
        {
            get { return name; }
        }
        
        public DateTime DateTime
        {
            get { return dateTime; }
        }

        public string Size
        {
            get { return size; }
        }

        public string Path
        {
            get { return path; }
        }

        public string FullPath
        {
            get { return _filename; }
        }

        public object Thumbnail
        {
            get;
            set;
        }

        public override string ToString()
        {
            return FullPath;
        }

        public int GetRawSize
        {
            get { return (int)(info.Length / 1024); }
        }

        public FileAttributesEx Attributes
        {
            get
            {
                FileAttributesEx Attr = new FileAttributesEx(FullPath);
                return Attr;
            }
        }

        /// <summary>
        /// Gets the width of the file if it is an image
        /// </summary>
        public int ImageWidth
        {
            get
            {
                try
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromFile(FullPath))
                    {
                        return img.Height;
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the Height of the file if it is an image
        /// </summary>
        public int ImageHeight
        {
            get
            {
                try
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromFile(FullPath))
                    {
                        return img.Width;
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public bool IsValidLogonPicture
        {
            get
            {
                //Check if the file exists.
                if (!System.IO.File.Exists(FullPath)) { return false; }

                //Check if the File is a JPG File.
                if (IsJPEG == false) { return false; }

                //Check if the File Size is greater than 450kb.

                if (GetRawSize > 450)
                {
                    return false;
                }

                //Check if the Height or width of the image is greater than that of the screen size.

                //if (ImageWidth > SystemInformation.VirtualScreen.Width || ImageHeight > SystemInformation.VirtualScreen.Height)
                //{
                //    return false;
                //}

                return true;
            }
        }

        private bool IsJPEG
        {
            get
            {
                try
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromFile(FullPath))
                    {
                        return img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    
                }
                catch (OutOfMemoryException)
                {
                    return false;
                }
            }
        }

        private void SetToolTip()
        {
            if (!IsJPEG) return;
            // Construct a ToolTip for the item:
            System.Windows.Controls.Image toolImage = new System.Windows.Controls.Image();
            toolImage.Source = bd.Frames[0].Thumbnail;

            TextBlock textBlock1 = new TextBlock();
            textBlock1.Text = System.IO.Path.GetFileName(FullPath);
            TextBlock textBlock2 = new TextBlock();
            textBlock2.Text = Size;
            TextBlock textBlock3 = new TextBlock();
            textBlock3.Text = DateTime.ToString();

            StackPanel sp = new StackPanel();
            sp.Children.Add(toolImage);
            sp.Children.Add(textBlock1);
            sp.Children.Add(textBlock2);
            sp.Children.Add(textBlock3);

            ToolTip = sp;
        }

        private void SetContent()
        {
            if (!IsJPEG) return;
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            
            image.Height = 35;

            Uri uri = new Uri(FullPath);
            bd = System.Windows.Media.Imaging.BitmapDecoder.Create(uri,
                System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation,
                System.Windows.Media.Imaging.BitmapCacheOption.Default);
            
            if (bd.Frames[0].Thumbnail != null)
                image.Source = bd.Frames[0].Thumbnail;
            else
                image.Source = new System.Windows.Media.Imaging.BitmapImage(uri);

            System.Windows.Controls.Label InfoLabel = new System.Windows.Controls.Label();
            InfoLabel.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            InfoLabel.Content = FileName;
            InfoLabel.FontSize = 4;
            
            StackPanel ContentContainer = new StackPanel();
            ContentContainer.Children.Add(image);
            ContentContainer.Children.Add(InfoLabel);
            
            Content = ContentContainer;
            Thumbnail = image.Source;
        }
    }

    public class FileAttributesEx
    {
        private string filepath = string.Empty;

        public string FilePath
        {
            get { return filepath; }
            set { filepath = value; }
        }

        public string Location
        {
            get
            {
                return (Directory.GetParent(filepath).ToString());
            }
        }

        public FileAttributesEx() { }
        public FileAttributesEx(string path)
        {
            FilePath = path;
        }

        public bool isReadOnly
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly; }
        }

        public bool isHidden
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.Hidden) == FileAttributes.Hidden; }
        }

        public bool isArchive
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.Archive) == FileAttributes.Archive; }
        }

        public bool isSystem
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.System) == FileAttributes.System; }
        }

        public bool isNormal
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.Normal) == FileAttributes.Normal; }
        }

        public bool isTemp
        {
            get { return (File.GetAttributes(FilePath) & FileAttributes.Temporary) == FileAttributes.Temporary; }
        }

        public string GetCreationTime
        {
            get { return File.GetCreationTime(FilePath).ToString(); }
        }

        public string GetAccessedTime
        {
            get { return File.GetLastAccessTime(FilePath).ToString(); }
        }

        public string GetModifiedTime
        {
            get { return File.GetLastWriteTime(FilePath).ToString(); }
        }

        public void Clear()
        {
            try
            {
                File.SetAttributes(FilePath, File.GetAttributes(FilePath)
                & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden));
            }
            catch { }
        }
    }

    public static class StringOperations
    {
        public static bool IsDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string reason;
            if (!IsValidPathString(path, out reason))
            {
                throw new ArgumentException(reason);
            }

            if (!(Directory.Exists(path) || File.Exists(path)))
            {
                throw new InvalidOperationException(string.Format("Could not find a part of the path '{0}'", path));
            }

            return (new System.IO.FileInfo(path).Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public static bool IsValidPathString(string pathStringToTest, out string reasonForError)
        {
            reasonForError = "";
            if (string.IsNullOrEmpty(pathStringToTest))
            {
                reasonForError = "Path is Null or Whitespace.";
                return false;
            }
            if (pathStringToTest.Length > 260) // MAXPATH == 260
            {
                reasonForError = "Length of path exceeds MAXPATH.";
                return false;
            }
            if (PathContainsInvalidCharacters(pathStringToTest))
            {
                reasonForError = "Path contains invalid path characters.";
                return false;
            }
            if (pathStringToTest == ":")
            {
                reasonForError = "Path consists of only a volume designator.";
                return false;
            }
            if (pathStringToTest[0] == ':')
            {
                reasonForError = "Path begins with a volume designator.";
                return false;
            }

            if (pathStringToTest.Contains(":") && pathStringToTest.IndexOf(':') != 1)
            {
                reasonForError = "Path contains a volume designator that is not part of a drive label.";
                return false;
            }
            return true;
        }

        public static bool PathContainsInvalidCharacters(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            bool containedInvalidCharacters = false;

            for (int i = 0; i < path.Length; i++)
            {
                int n = path[i];
                if (
                    (n == 0x22) || // "
                    (n == 0x3c) || // <
                    (n == 0x3e) || // >
                    (n == 0x7c) || // |
                    (n < 0x20)    // the control characters
                  )
                {
                    containedInvalidCharacters = true;
                }
            }

            return containedInvalidCharacters;
        }

        public static bool FilenameContainsInvalidCharacters(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            bool containedInvalidCharacters = false;

            for (int i = 0; i < filename.Length; i++)
            {
                int n = filename[i];
                if (
                    (n == 0x22) || // "
                    (n == 0x3c) || // <
                    (n == 0x3e) || // >
                    (n == 0x7c) || // |
                    (n == 0x3a) || // : 
                    (n == 0x2a) || // * 
                    (n == 0x3f) || // ? 
                    (n == 0x5c) || // \ 
                    (n == 0x2f) || // /
                    (n < 0x20)    // the control characters
                  )
                {
                    containedInvalidCharacters = true;
                }
            }

            return containedInvalidCharacters;
        }
    }
}
