using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BondTech.Vienna.Shared;
using Ookii.Dialogs;

namespace BondTech.Vienna.App
{
    public partial class AppMain : Form
    {
        private GlobalHotkey ViennaGlobalShortcut;
        ViennaShared ThisUser;

        public AppMain()
        {
            InitializeComponent();
            this.FormClosing += delegate { UnregisterShortcut(); };
            HotkeyPressedDialog.VerificationClicked +=new EventHandler(HotkeyPressedDialog_VerificationClicked);
            HotkeyPressedDialog.HyperlinkClicked += new EventHandler<HyperlinkClickedEventArgs>(HotkeyPressedDialog_HyperlinkClicked);
            
            ThisUser = new ViennaShared(true);

            if (ThisUser.ViennaEnabled == true &&
                App.Properties.Settings.Default.UserKey != Keys.None.ToString())
            {
                bool HasControl = false; bool HasShift = false; bool HasAlt = false;
                int Modifier = 0;
                int key = 0;
                string[] result;
                string[] separators = new string[] { " + " };
                result = KeyGetter.ViennaGlobalShortcut.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in result)
                {
                    if (s.Trim() == Keys.Control.ToString())
                    {
                        HasControl = true;
                    }
                    if (s.Trim() == Keys.Alt.ToString())
                    {
                        HasAlt = true;
                    }
                    if (s.Trim() == Keys.Shift.ToString())
                    {
                        HasShift = true;
                    }
                }

                if (HasControl) { Modifier = Constants.CTRL; }
                if (HasAlt) { Modifier += Constants.ALT; }
                if (HasShift) { Modifier += Constants.SHIFT; }

                key = Microsoft.VisualBasic.Strings.Asc(result.GetValue(result.Length - 1).ToString());
                ViennaGlobalShortcut = new GlobalHotkey(Modifier, key, this);
            }
        }

        public AppMain(bool NoLoad)
        {
            InitializeComponent();
            HotkeyPressedDialog.VerificationClicked += new EventHandler(HotkeyPressedDialog_VerificationClicked);
            HotkeyPressedDialog.HyperlinkClicked += new EventHandler<HyperlinkClickedEventArgs>(HotkeyPressedDialog_HyperlinkClicked);
            ThisUser = new ViennaShared(true);
        }

        void HotkeyPressedDialog_HyperlinkClicked(object sender, HyperlinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Href);
        }

        void HotkeyPressedDialog_VerificationClicked(object sender, EventArgs e)
        {
            App.Properties.Settings.Default.ShowHelpMessage = HotkeyPressedDialog.IsVerificationChecked;
            App.Properties.Settings.Default.Save();

        }

        private void AppMain_Load(object sender, EventArgs e)
        {
            this.Hide();
            if (ViennaGlobalShortcut != null)
            {
                if (!ViennaGlobalShortcut.Register())
                {
                    ViennaShared.LogEvent("Could not register the global shortcut");
                }
            }
        }

        private void UnregisterShortcut()
        {
            if (!ViennaGlobalShortcut.Unregiser())
            {
                ViennaShared.LogEvent("Could not unregister the global shortcut");
            }
        }

        private void HandleHotkey()
        {
            ChangeLogonPic();
        }

        internal void ChangeLogonPic()
        {
            ThisUser.ChangeLogonPicture(ViennaShared.ChangePictureOn.Hotkey);
            if (App.Properties.Settings.Default.ShowHelpMessage)
                HotkeyPressedDialog.Show();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                HandleHotkey();
            base.WndProc(ref m);
        }
    }
}
