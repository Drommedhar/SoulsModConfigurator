namespace SoulsConfigurator
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events to prevent memory leaks
                if (_presetService != null)
                {
                    _presetService.PresetChanged -= OnPresetChanged;
                }
                
                // Dispose download service
                _downloadService?.Dispose();
                
                // Dispose version check service
                _versionCheckService?.Dispose();
                
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            cmbGames = new ComboBox();
            lblGame = new Label();
            lblInstallPath = new Label();
            txtInstallPath = new TextBox();
            btnBrowse = new Button();
            lblMods = new Label();
            btnInstallMods = new Button();
            btnClearMods = new Button();
            btnConfigureMod = new Button();
            btnDownloadFiles = new Button();
            lblStatus = new Label();
            panelModsContainer = new Panel();
            SuspendLayout();
            // 
            // cmbGames
            // 
            cmbGames.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGames.FormattingEnabled = true;
            cmbGames.Location = new Point(10, 29);
            cmbGames.Margin = new Padding(3, 2, 3, 2);
            cmbGames.Name = "cmbGames";
            cmbGames.Size = new Size(176, 23);
            cmbGames.TabIndex = 0;
            cmbGames.SelectedIndexChanged += cmbGames_SelectedIndexChanged;
            // 
            // lblGame
            // 
            lblGame.AutoSize = true;
            lblGame.Location = new Point(10, 12);
            lblGame.Name = "lblGame";
            lblGame.Size = new Size(75, 15);
            lblGame.TabIndex = 1;
            lblGame.Text = "Select Game:";
            // 
            // lblInstallPath
            // 
            lblInstallPath.AutoSize = true;
            lblInstallPath.Location = new Point(10, 60);
            lblInstallPath.Name = "lblInstallPath";
            lblInstallPath.Size = new Size(102, 15);
            lblInstallPath.TabIndex = 2;
            lblInstallPath.Text = "Game Install Path:";
            // 
            // txtInstallPath
            // 
            txtInstallPath.Location = new Point(10, 77);
            txtInstallPath.Margin = new Padding(3, 2, 3, 2);
            txtInstallPath.Name = "txtInstallPath";
            txtInstallPath.Size = new Size(630, 23);
            txtInstallPath.TabIndex = 3;
            txtInstallPath.TextChanged += txtInstallPath_TextChanged;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(650, 77);
            btnBrowse.Margin = new Padding(3, 2, 3, 2);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(66, 23);
            btnBrowse.TabIndex = 4;
            btnBrowse.Text = "Browse";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // lblMods
            // 
            lblMods.AutoSize = true;
            lblMods.Location = new Point(10, 112);
            lblMods.Name = "lblMods";
            lblMods.Size = new Size(91, 15);
            lblMods.TabIndex = 5;
            lblMods.Text = "Available Mods:";
            // 
            // btnInstallMods
            // 
            btnInstallMods.Location = new Point(12, 285);
            btnInstallMods.Margin = new Padding(3, 2, 3, 2);
            btnInstallMods.Name = "btnInstallMods";
            btnInstallMods.Size = new Size(131, 26);
            btnInstallMods.TabIndex = 7;
            btnInstallMods.Text = "Install Selected Mods";
            btnInstallMods.UseVisualStyleBackColor = true;
            btnInstallMods.Click += btnInstallMods_Click;
            // 
            // btnClearMods
            // 
            btnClearMods.Location = new Point(160, 285);
            btnClearMods.Margin = new Padding(3, 2, 3, 2);
            btnClearMods.Name = "btnClearMods";
            btnClearMods.Size = new Size(131, 26);
            btnClearMods.TabIndex = 8;
            btnClearMods.Text = "Remove All Mods";
            btnClearMods.UseVisualStyleBackColor = true;
            btnClearMods.Click += btnClearMods_Click;
            // 
            // btnConfigureMod
            // 
            btnConfigureMod.Location = new Point(308, 285);
            btnConfigureMod.Margin = new Padding(3, 2, 3, 2);
            btnConfigureMod.Name = "btnConfigureMod";
            btnConfigureMod.Size = new Size(131, 26);
            btnConfigureMod.TabIndex = 9;
            btnConfigureMod.Text = "Check Files";
            btnConfigureMod.UseVisualStyleBackColor = true;
            btnConfigureMod.Click += btnConfigureMod_Click;
            // 
            // btnDownloadFiles
            // 
            btnDownloadFiles.Location = new Point(445, 285);
            btnDownloadFiles.Margin = new Padding(3, 2, 3, 2);
            btnDownloadFiles.Name = "btnDownloadFiles";
            btnDownloadFiles.Size = new Size(131, 26);
            btnDownloadFiles.TabIndex = 12;
            btnDownloadFiles.Text = "Download Files";
            btnDownloadFiles.UseVisualStyleBackColor = true;
            btnDownloadFiles.Click += btnDownloadFiles_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(12, 320);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 15);
            lblStatus.TabIndex = 10;
            // 
            // panelModsContainer
            // 
            panelModsContainer.Location = new Point(12, 130);
            panelModsContainer.Name = "panelModsContainer";
            panelModsContainer.Size = new Size(705, 150);
            panelModsContainer.TabIndex = 11;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(730, 350);
            Controls.Add(panelModsContainer);
            Controls.Add(lblStatus);
            Controls.Add(btnDownloadFiles);
            Controls.Add(btnConfigureMod);
            Controls.Add(btnClearMods);
            Controls.Add(btnInstallMods);
            Controls.Add(lblMods);
            Controls.Add(btnBrowse);
            Controls.Add(txtInstallPath);
            Controls.Add(lblInstallPath);
            Controls.Add(lblGame);
            Controls.Add(cmbGames);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Souls Configurator";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbGames;
        private Label lblGame;
        private Label lblInstallPath;
        private TextBox txtInstallPath;
        private Button btnBrowse;
        private Label lblMods;
        private Button btnInstallMods;
        private Button btnClearMods;
        private Button btnConfigureMod;
        private Button btnDownloadFiles;
        private Label lblStatus;
        private Panel panelModsContainer;
    }
}