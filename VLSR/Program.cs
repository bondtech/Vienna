using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BondTech.Vienna.App
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "/pauseservice":
                        if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                        {
                            Vienna.Shared.AdminPriviledge.Args = "/pauseservice";
                            Vienna.Shared.AdminPriviledge.RestartElevated();
                        }
                        Vienna.Shared.ViennaShared PauseService = new Shared.ViennaShared();
                        PauseService.StopViennaService(true);
                        Application.Exit();
                        return;

                    case "/startservice":
                        if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                        {
                            Vienna.Shared.AdminPriviledge.Args = "/startservice";
                            Vienna.Shared.AdminPriviledge.RestartElevated();
                        }
                        Vienna.Shared.ViennaShared StartService = new Shared.ViennaShared();
                        StartService.StartViennaService();
                        Application.Exit();
                        return;

                    case "/stopservice":
                        if (!Vienna.Shared.AdminPriviledge.IsAdmin())
                        {
                            Vienna.Shared.AdminPriviledge.Args = "/stopservice";
                            Vienna.Shared.AdminPriviledge.RestartElevated();
                        }
                        Vienna.Shared.ViennaShared StopService = new Shared.ViennaShared();
                        StopService.StopViennaService();
                        Application.Exit();
                        return;

                    case "/change":
                        AppMain MainForm = new AppMain(true);
                        MainForm.ChangeLogonPic();
                        Application.Exit();
                        return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppMain());
        }
    }
}
