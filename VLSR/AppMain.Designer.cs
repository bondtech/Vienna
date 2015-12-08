namespace BondTech.Vienna.App
{
    partial class AppMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppMain));
            this.taskDialogButton1 = new Ookii.Dialogs.TaskDialogButton(this.components);
            this.HotkeyPressedDialog = new Ookii.Dialogs.TaskDialog(this.components);
            this.SuspendLayout();
            // 
            // taskDialogButton1
            // 
            this.taskDialogButton1.ButtonType = Ookii.Dialogs.ButtonType.Ok;
            // 
            // HotkeyPressedDialog
            // 
            this.HotkeyPressedDialog.AllowDialogCancellation = true;
            this.HotkeyPressedDialog.Buttons.Add(this.taskDialogButton1);
            this.HotkeyPressedDialog.EnableHyperlinks = true;
            this.HotkeyPressedDialog.Footer = "Vienna Logon Screen Rotator by <a Href=\"http://www.facebook.com/BondTech\">Bond Te" +
    "chnologies</a>";
            this.HotkeyPressedDialog.IsVerificationChecked = true;
            this.HotkeyPressedDialog.MainIcon = Ookii.Dialogs.TaskDialogIcon.Information;
            this.HotkeyPressedDialog.MainInstruction = resources.GetString("HotkeyPressedDialog.MainInstruction");
            this.HotkeyPressedDialog.VerificationText = "Always show this message.";
            this.HotkeyPressedDialog.WindowIcon = ((System.Drawing.Icon)(resources.GetObject("HotkeyPressedDialog.WindowIcon")));
            this.HotkeyPressedDialog.WindowTitle = "Vienna Logon Screen Rotator.";
            // 
            // AppMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AppMain";
            this.ShowInTaskbar = false;
            this.Text = "AppMain";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.AppMain_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Ookii.Dialogs.TaskDialogButton taskDialogButton1;
        private Ookii.Dialogs.TaskDialog HotkeyPressedDialog;


    }
}