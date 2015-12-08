namespace BondTech.Vienna.Service
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ViennaServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ViennaServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ViennaServiceProcessInstaller
            // 
            this.ViennaServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ViennaServiceProcessInstaller.Password = null;
            this.ViennaServiceProcessInstaller.Username = null;
            // 
            // ViennaServiceInstaller
            // 
            this.ViennaServiceInstaller.Description = "Manages changes your logon screen pictures. Starting or stopping this manually ca" +
    "n cause irregularities.";
            this.ViennaServiceInstaller.DisplayName = "Vienna Logon Service";
            this.ViennaServiceInstaller.ServiceName = "ViennaLogonService";
            this.ViennaServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ViennaServiceProcessInstaller,
            this.ViennaServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ViennaServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ViennaServiceInstaller;
    }
}