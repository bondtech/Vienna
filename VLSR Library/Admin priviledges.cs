using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;

namespace BondTech.Vienna.Shared
{
    public static class AdminPriviledge
    {
        [DllImport("user32")]
        public static extern UInt32 SendMessage(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);

        internal const int BCM_FIRST = 0x1600;
        internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);
        public static string Args = "";

        public static bool IsVistaOrHigher()
        {
            return Environment.OSVersion.Version.Major < 6;
        }

        /// <summary>
        /// Checks if the process is elevated
        /// </summary>
        /// <returns>If is elevated</returns>
        public static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Add a shield icon to a button
        /// </summary>
        /// <param name="b">The button</param>
        public static void AddShieldToButton(Button b)
        {
            b.FlatStyle = FlatStyle.System;
            SendMessage(b.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
        }

        /// <summary>
        /// Restart the current process with administrator credentials
        /// </summary>
        public static bool RestartElevated()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas";
            startInfo.Arguments = Args;
            try
            {
                Process p = Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false; //If cancelled, do nothing
            }
            
            Application.Exit();
            return true;
        }
    }
}

