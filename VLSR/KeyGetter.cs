using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ookii.Dialogs;

namespace BondTech.Vienna.App
{
    public partial class KeyGetter : Form
    {
        bool KeyisSet;
        public KeyGetter()
        {
            InitializeComponent();
        }

        public static string Reset()
        {
            App.Properties.Settings.Default.Reset();
            return ViennaGlobalShortcut;
        }

        public static void ChangeShortCut(ref string v)
        {
            KeyGetter thisForm = new KeyGetter();
            thisForm.txtButton.Text = ViennaGlobalShortcut;
            DialogResult UserResult = thisForm.ShowDialog();

            if (UserResult != System.Windows.Forms.DialogResult.Cancel)
            {
                if (thisForm.txtButton.Text == Keys.None.ToString())
                {
                    TaskDialogButton MessageResult = thisForm.emptyShortcutDialog.ShowDialog();
                    if (MessageResult == thisForm.btnGoBack)
                    {
                        ChangeShortCut(ref v);
                    }
                    else if (MessageResult == thisForm.btnUseDefault)
                    {
                        App.Properties.Settings.Default.UserDefined = false;
                        App.Properties.Settings.Default.UserKey = thisForm.txtButton.Text;
                        App.Properties.Settings.Default.Save();
                        v = ViennaGlobalShortcut;
                    }
                    else
                    {
                        App.Properties.Settings.Default.UserDefined = true;
                        App.Properties.Settings.Default.UserKey = thisForm.txtButton.Text;
                        App.Properties.Settings.Default.Save();
                        v = ViennaGlobalShortcut;
                    }
                }
                else
                {
                    App.Properties.Settings.Default.UserDefined = true;
                    App.Properties.Settings.Default.UserKey = thisForm.txtButton.Text;
                    App.Properties.Settings.Default.Save();
                    v = ViennaGlobalShortcut;
                }
            }
            v = ViennaGlobalShortcut;
        }

        public static string ViennaGlobalShortcut
        {
            get
            {
                if (Vienna.App.Properties.Settings.Default.UserDefined == false)
                {
                    return Vienna.App.Properties.Settings.Default.DefaultGlobalKey;
                }
                else
                {
                    return Vienna.App.Properties.Settings.Default.UserKey == "" 
                        ? Keys.None.ToString() 
                        : Vienna.App.Properties.Settings.Default.UserKey;
                }
            }
        }

        private void txtButton_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            txtButton.Text = "";
            KeyisSet = false;

            if (e.KeyData == Keys.Back)
            {
                txtButton.Text = Keys.None.ToString();
                return;
            }

            if (e.Modifiers == Keys.None)
            {
                MessageBox.Show("You have to specify a modifier like 'Control', 'Alt' or 'Shift'");
                txtButton.Text = Keys.None.ToString();
                return;
            }

            foreach (string s in e.Modifiers.ToString().Split(new char[] { ',' }))
            {
                txtButton.Text += s + " + ";
            }

            if (e.KeyCode == Keys.ShiftKey | e.KeyCode == Keys.ControlKey | e.KeyCode == Keys.Menu)
            {
                KeyisSet = false;
            }
            else { txtButton.Text += e.KeyCode.ToString(); KeyisSet = true; }
        }

        private void txtButton_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyisSet == false)
            {
                txtButton.Text = Keys.None.ToString();
            }
        }
    }
}