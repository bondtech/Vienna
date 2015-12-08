using System;
using System.IO;
using BondTech.CustomMessage;
using IWshRuntimeLibrary;

namespace BondTech.Vienna.Shared
{
    /// <summary>
    /// Summary description for Shortcut.
    /// </summary>
    public class Shortcut
    {
        /// <summary>
        /// Check to see if a shortcut exists in a given directory with a specified file name
        /// </summary>
        /// <param name="DirectoryPath">The directory in which to look</param>
        /// <param name="FullPathName">The name of the shortcut (without the .lnk extension) or the full path to a file of the same name</param>
        /// <returns>Returns true if the link exists</returns>
        public static bool Exists(string DirectoryPath, string LinkPathName)
        {
            // Get some file and directory information
            DirectoryInfo SpecialDir = new DirectoryInfo(DirectoryPath);
            // First get the filename for the original file and create a new file
            // name for a link in the Startup directory
            //
            FileInfo originalfile = new FileInfo(LinkPathName);
            string NewFileName = SpecialDir.FullName + "\\" + originalfile.Name + ".lnk";
            FileInfo linkfile = new FileInfo(NewFileName);
            return linkfile.Exists;
        }

        /// <summary>Check to see if a shell link exists to the given path in the specified special folder
        /// </summary>
        /// <param name="folder">The full path of the directory in which the link will reside</param>
        /// <param name="LinkPathName">The file name for the link itself or, if a path name the directory information will be ignored.</param>
        /// <returns>return true if it exists</returns>
        public static bool Exists(Environment.SpecialFolder folder, string LinkPathName)
        {
            return Shortcut.Exists(Environment.GetFolderPath(folder), LinkPathName);
        }

        /// <summary>Update the specified folder by creating or deleting a Shell Link if necessary
        /// </summary>
        /// <param name="folder">A SpecialFolder in which the Shortcut will reside</param>
        /// <param name="TargetPathName">The path name of the target file for the link</param>
        /// <param name="LinkPathName">The file name for the link itself or, if a path name the directory information will be ignored.</param>
        /// <param name="Create">If true, create the link, otherwise delete it</param>
        public static void Update(Environment.SpecialFolder folder, string TargetPathName, string LinkPathName, bool install)
        {
            // Get some file and directory information
            Shortcut.Update(Environment.GetFolderPath(folder), TargetPathName, LinkPathName, install);
        }

        // Boolean variable "install" determines whether the link should be there or not.
        // Update the folder by creating or deleting the link as required.

        /// <summary>Update the specified folder by creating or deleting a Shell Link if necessary
        /// </summary>
        /// <param name="DirectoryPath">The full path of the directory in which the link will reside</param>
        /// <param name="TargetPathName">The path name of the target file for the link</param>
        /// <param name="LinkPathName">The file name for the link itself or, if a path name the directory information will be ignored.</param>
        /// <param name="Create">If true, create the link, otherwise delete it</param>
        public static void Update(string DirectoryPath, string TargetPathName, string LinkPathName, bool Create)
        {
            // Get some file and directory information
            DirectoryInfo SpecialDir = new DirectoryInfo(DirectoryPath);
            // First get the filename for the original file and create a new file
            // name for a Shortcut in the Startup directory
            //
            FileInfo OriginalFile = new FileInfo(LinkPathName);
            string NewFileName = SpecialDir.FullName + "\\" + OriginalFile.Name + ".lnk";
            FileInfo LinkFile = new FileInfo(NewFileName);

            if (Create) // If the link doesn't exist, create it
            {
                if (LinkFile.Exists) return; // We're all done if it already exists
                //Place a shortcut to the file in the special folder 
                try
                {
                    // Create a shortcut in the special folder for the file
                    // Making use of the Windows Scripting Host
                    WshShell shell = new WshShell();
                    IWshShortcut link = (IWshShortcut)shell.CreateShortcut(LinkFile.FullName);
                    link.TargetPath = TargetPathName;
                    link.Save();
                }
                catch
                {
                    CustomMessageBox.Show("Unable to create link in special directory: " + NewFileName,
                        "Shell Link Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else // otherwise delete it from the startup directory
            {
                if (!LinkFile.Exists) return; // It doesn't exist so we are done!
                try
                {
                    LinkFile.Delete();
                }
                catch
                {
                    CustomMessageBox.Show("Error deleting link in special directory: " + NewFileName,
                        "Shell Link Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

    }
}
