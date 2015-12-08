using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using BondTech.Vienna;
using Microsoft.Win32;

namespace BondTech.Vienna.Settings
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {   
           if (e.Args.Length > 0)
            {
                if (e.Args.Length == 1)
                {
                    switch (e.Args.GetValue(0).ToString().ToLower())
                    {
                        case ("/optimize"):
                            if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                            {
                                Vienna.Shared.AdminPriviledge.Args = "/Optimize";
                                Vienna.Shared.AdminPriviledge.RestartElevated();
                            }

                            if (!Vienna.Shared.AdminPriviledge.IsAdmin()) { Application.Current.Shutdown(); return; }
                            Shared.ViennaShared.FirstTimeOptimization();

                            Vienna.Shared.Shortcut.Update(Environment.SpecialFolder.SendTo, System.Reflection.Assembly.GetExecutingAssembly().Location, "Vienna Logon Screen Rotator", true);
                            Vienna.Shared.Shortcut.Update(Environment.SpecialFolder.DesktopDirectory, System.Reflection.Assembly.GetExecutingAssembly().Location, "Vienna Logon Screen Rotator", true);

                            Registry.LocalMachine.SetValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\Background\\OEMBackground", 1, RegistryValueKind.DWord);
                            RegistryKey ViennaContextMenu = Registry.ClassesRoot.CreateSubKey("Directory\\Background\\shell\\Vienna");
                            ViennaContextMenu.SetValue("Icon", Vienna.Settings.MainWindow.GetAppPath + "VLSR.exe,0");
                            ViennaContextMenu.SetValue("Extended", "");
                            ViennaContextMenu.CreateSubKey("command").SetValue(null, Settings.MainWindow.GetAppPath + "VLSR.exe /change");

                            RegistryKey ViennaAutoStart = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
                            ViennaAutoStart.SetValue("Vienna", Settings.MainWindow.GetAppPath + "VLSR.exe");

                            Shared.ViennaShared.GrantAccess(Settings.MainWindow.GetAppPath);

                            ViennaContextMenu.Close();
                            ViennaAutoStart.Close();

                            Vienna.Shared.ViennaShared.LogEvent("Application Optimized successfully.");

                            Application.Current.Shutdown();
                            return;

                        case ("/reset"):
                            if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                            {
                                Vienna.Shared.AdminPriviledge.Args = "/Reset";
                                Vienna.Shared.AdminPriviledge.RestartElevated();
                            }

                            if (!Vienna.Shared.AdminPriviledge.IsAdmin()) { Application.Current.Shutdown(); return; }
                            Settings.Properties.Settings.Default.Reset();

                            if (System.IO.File.Exists(Vienna.Shared.ViennaShared.SettingsFilePath))
                                System.IO.File.Delete(Vienna.Shared.ViennaShared.SettingsFilePath);
                            Vienna.App.KeyGetter.Reset();

                            Vienna.Shared.Shortcut.Update(Environment.SpecialFolder.SendTo, System.Reflection.Assembly.GetExecutingAssembly().Location, "Vienna Logon Screen Rotator", false);
                            Vienna.Shared.Shortcut.Update(Environment.SpecialFolder.DesktopDirectory, System.Reflection.Assembly.GetExecutingAssembly().Location, "Vienna Logon Screen Rotator", false);
                            
                            Registry.ClassesRoot.DeleteSubKey("Directory\\Background\\shell\\Vienna\\command", false);
                            Registry.ClassesRoot.DeleteSubKey("Directory\\Background\\shell\\Vienna", false);
                            Registry.CurrentUser.DeleteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\Vienna", false);

                            Vienna.Shared.ViennaShared.LogEvent("Application reset by user");
                            Application.Current.Shutdown();
                            return;

                        case ("/desktop"):
                            Vienna.Shared.Shortcut.Update(Environment.SpecialFolder.DesktopDirectory, System.Reflection.Assembly.GetExecutingAssembly().Location, "Vienna Logon Screen Rotator", true);
                            Application.Current.Shutdown();
                            return;
                    }
                }
            }

            bool HasArgs = e.Args.Length > 0 ? true : false;

            Application.SetCookie(new Uri("http://mycomputer.vienna.firsttime"), Settings.Properties.Settings.Default.FirstTime.ToString());

            if (Settings.Properties.Settings.Default.FirstTime)
            {
                if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                {
                    Vienna.Shared.AdminPriviledge.Args = "/Optimize";
                    Settings.Properties.Settings.Default.FirstTime = !Vienna.Shared.AdminPriviledge.RestartElevated();
                }
                else
                {
                    System.Diagnostics.ProcessStartInfo Optimization = new System.Diagnostics.ProcessStartInfo();
                    Optimization.Arguments = "/Optimize";
                    Optimization.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    System.Diagnostics.Process.Start(Optimization);
                    Settings.Properties.Settings.Default.FirstTime = false;
                }
            }

            Settings.Properties.Settings.Default.Save();
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            
            Vienna.Settings.MainWindow Main = new MainWindow();
            Main.Show();
            
            if (HasArgs)
                Main.AddFile(e.Args);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Vienna.Shared.ViennaShared.LogEvent(e.Exception.Message);
            //MessageBox.Show("An error occurred, VLSR has handled this exception, you could however try to report this error", "Vienna Logon Screen Rotator", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
