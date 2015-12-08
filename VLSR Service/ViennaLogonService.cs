using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using BondTech.Vienna.Shared;

namespace BondTech.Vienna.Service
{
    partial class ViennaLogonService : ServiceBase
    {

        public ViennaLogonService()
        {
            InitializeComponent();
        }

        // Handle starting the service.
        protected override void OnStart(string[] args)
        {
#if LOGEVENTS
            EventLog.WriteEntry("ViennaLogonService.OnStart", DateTime.Now.ToLongTimeString() +
                " - Start notice received."
#endif
            ViennaServiceClass ThisService = new ViennaServiceClass();
            ThisService.ChangeLogonPicture(ViennaShared.ChangePictureOn.Startup);
        }

        // Handle Stopping the service.
        [STAThread]
        protected override void OnStop()
        {
            // Do Nothing. Log events for the service.
#if LOGEVENTS
            EventLog.WriteEntry("ViennaLogonService.OnStop", DateTime.Now.ToLongTimeString() +
                " - Stop notice received."
#endif
        }

        // Handle the session change notice.
        [STAThread]
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
#if LOGEVENTS
            EventLog.WriteEntry("ViennaLogonService.OnSessionChange", DateTime.Now.ToLongTimeString() +
                " - Session change notice received: " +
                changeDescription.Reason.ToString() + "  Session ID: " +
                changeDescription.SessionId.ToString());
#endif

            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " SessionLogon, total users: " +
                        userCount.ToString());
#endif
                    break;
                case SessionChangeReason.SessionLogoff:

#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " SessionLogoff, total users: " +
                        userCount.ToString());
#endif
                    break;
                case SessionChangeReason.RemoteConnect:
#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " RemoteConnect, total users: " +
                        userCount.ToString());
#endif
                    break;
                case SessionChangeReason.RemoteDisconnect:
#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " RemoteDisconnect, total users: " +
                        userCount.ToString());
#endif
                    break;
                case SessionChangeReason.SessionLock:
#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " SessionLock");
#endif
                    ViennaServiceClass ThisService = new ViennaServiceClass();
                    ThisService.ChangeLogonPicture(ViennaShared.ChangePictureOn.Lock);
                    break;
                case SessionChangeReason.SessionUnlock:
#if LOGEVENTS
                    EventLog.WriteEntry("ViennaLogonService.OnSessionChange",
                        DateTime.Now.ToLongTimeString() +
                        " SessionUnlock");
#endif
                    break;
                default:
                    break;
            }
        }

        // Handle Shutdown.
        [STAThread]
        protected override void OnShutdown()
        {
#if LOGEVENTS
            EventLog.WriteEntry("ViennaLogonService.OnShutdown", DateTime.Now.ToLongTimeString() +
                " - Shutdown notice received."
#endif
            ViennaServiceClass ThisService = new ViennaServiceClass();
            ThisService.ChangeLogonPicture(ViennaShared.ChangePictureOn.Shutdown);
        }
    }
}
