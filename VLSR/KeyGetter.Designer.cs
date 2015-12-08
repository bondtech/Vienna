namespace BondTech.Vienna.App
{
    partial class KeyGetter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyGetter));
            this.btnOk = new System.Windows.Forms.Button();
            this.txtButton = new System.Windows.Forms.TextBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.emptyShortcutDialog = new Ookii.Dialogs.TaskDialog(this.components);
            this.btnUseEmptyShortcut = new Ookii.Dialogs.TaskDialogButton(this.components);
            this.btnUseDefault = new Ookii.Dialogs.TaskDialogButton(this.components);
            this.btnGoBack = new Ookii.Dialogs.TaskDialogButton(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.BackColor = System.Drawing.Color.Silver;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(50, 63);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(86, 38);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "&Save";
            this.btnOk.UseVisualStyleBackColor = false;
            // 
            // txtButton
            // 
            this.txtButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtButton.Location = new System.Drawing.Point(1, 34);
            this.txtButton.Name = "txtButton";
            this.txtButton.ShortcutsEnabled = false;
            this.txtButton.Size = new System.Drawing.Size(221, 23);
            this.txtButton.TabIndex = 0;
            this.txtButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtButton_KeyDown);
            this.txtButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtButton_KeyUp);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.Silver;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(142, 63);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 38);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // emptyShortcutDialog
            // 
            this.emptyShortcutDialog.Buttons.Add(this.btnUseEmptyShortcut);
            this.emptyShortcutDialog.Buttons.Add(this.btnUseDefault);
            this.emptyShortcutDialog.Buttons.Add(this.btnGoBack);
            this.emptyShortcutDialog.ButtonStyle = Ookii.Dialogs.TaskDialogButtonStyle.CommandLinks;
            this.emptyShortcutDialog.Content = "Vienna Logon Screen Rotator uses global shortcuts to change your logon screen any" +
    "time you press the keys specified.";
            this.emptyShortcutDialog.MainIcon = Ookii.Dialogs.TaskDialogIcon.Warning;
            this.emptyShortcutDialog.MainInstruction = "You have entered a empty shortcut. Is this what you want?";
            this.emptyShortcutDialog.WindowIcon = ((System.Drawing.Icon)(resources.GetObject("emptyShortcutDialog.WindowIcon")));
            this.emptyShortcutDialog.WindowTitle = "Vienna Logon Screen Rotator";
            // 
            // btnUseEmptyShortcut
            // 
            this.btnUseEmptyShortcut.CommandLinkNote = "You will be unable to change your logon screen picture\r\nusing a global shortcut.";
            this.btnUseEmptyShortcut.Text = "Yes, this is what I want. leave it this way";
            // 
            // btnUseDefault
            // 
            this.btnUseDefault.CommandLinkNote = "Your global shortcut will be set to the default global shortcut \"Shift + Control " +
    "+ Alt + V\"";
            this.btnUseDefault.Text = "No, use default global shortcut.";
            // 
            // btnGoBack
            // 
            this.btnGoBack.CommandLinkNote = "Displays the shortcut dialog again.";
            this.btnGoBack.Default = true;
            this.btnGoBack.Text = "Oh, I would like to change my shortcut.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(-2, -2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(225, 26);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select the key combination to let you change \r\nyour Logon Picture.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // KeyGetter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(224, 104);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtButton);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "KeyGetter";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VLSR Global Shortcut";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TextBox txtButton;
        private System.Windows.Forms.Button btnCancel;
        private Ookii.Dialogs.TaskDialog emptyShortcutDialog;
        private Ookii.Dialogs.TaskDialogButton btnUseDefault;
        private Ookii.Dialogs.TaskDialogButton btnUseEmptyShortcut;
        private Ookii.Dialogs.TaskDialogButton btnGoBack;
        private System.Windows.Forms.Label label1;
    }
}