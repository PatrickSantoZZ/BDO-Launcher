﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;

namespace Launcher
{
    public partial class MainForm : Form
    {
        private Configuration configuration;

        public MainForm()
        {
            InitializeComponent();
        }
        
        private void OTPAsync()
        {
            var size = new Size(200, 55);
            var otpInputBox = new Form
            {
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                ClientSize = size,
                Text = "OTP"
            };

            var otpTextBox = new TextBox
            {
                Size = new Size(size.Width - 20, 25),
                Location = new Point(10, 5)
            };
            
            otpInputBox.Controls.Add(otpTextBox);
            
            var loginButton = new Button
            {
                Size = new Size(size.Width - 20, 25),
                Text = "&Login",
                Location = new Point(10, 25)
            };
            otpInputBox.Controls.Add(loginButton);
            
            async void OkButton_Click(object sender, EventArgs e)
            {
                if (string.IsNullOrEmpty(otpTextBox.Text))
                {
                    MessageBox.Show("Please enter valid OTP.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                
                if (await StartGameAsync(int.Parse(otpTextBox.Text)))
                {
                    otpInputBox.Close();
                    Close();
                }
                else
                    StartGameButton.Enabled = true;
            }
            loginButton.Click += OkButton_Click;

            void Exit(object sender, FormClosingEventArgs e)
            {
                StartGameButton.Enabled = true;
            }
            otpInputBox.FormClosing += Exit;

            void TextBox_KeyPress(object sender, KeyPressEventArgs e)
            {
                if (e.KeyChar == Convert.ToChar(Keys.Return))
                {
                    loginButton.PerformClick();
                }
            }
            otpTextBox.KeyPress += TextBox_KeyPress;
            
            otpInputBox.Show(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            configuration = ConfigurationManager.Load();

            if (configuration == null)
            {
                configuration = new Configuration();

                ConfigurationManager.Save(configuration);
            }

            if (configuration.LoginAutomatically && (Environment.GetCommandLineArgs().Length >= 2) && (Environment.GetCommandLineArgs()[1].ToLower() == "--disable-automatic-login"))
                configuration.LoginAutomatically = false;

            if (CheckGameDirectoryPathAndPrompt())
                Text = $"Launcher | {configuration.GameDirectoryPath}";

            if (configuration.RememberData)
            {
                UsernameTextBox.Text = configuration.Username;
                PasswordTextBox.Text = configuration.GetPassword();
            }

            RegionComboBox.SelectedIndex = configuration.RegionComboBox;
            OTPCheckBox.Checked = configuration.OTP;
            RememberDataCheckBox.Checked = configuration.RememberData;
            LoginAutomaticallyCheckBox.Checked = configuration.LoginAutomatically;

            if (configuration.LoginAutomatically)
            {
                GameStart();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            configuration.Username = UsernameTextBox.Text;

            configuration.SetPassword(PasswordTextBox.Text);

            ConfigurationManager.Save(configuration);
        }

        private void RegionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            configuration.RegionComboBox = RegionComboBox.SelectedIndex;
            
            ConfigurationManager.Save(configuration);
        }

        private void OTPCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            configuration.OTP = OTPCheckBox.Checked;

            ConfigurationManager.Save(configuration);
        }
        
        private void RememberDataCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            configuration.RememberData = RememberDataCheckBox.Checked;

            ConfigurationManager.Save(configuration);
        }

        private void LoginAutomaticallyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (LoginAutomaticallyCheckBox.Checked)
            {
                RememberDataCheckBox.Checked = true;
                RememberDataCheckBox.Enabled = false;
            }
            else
                RememberDataCheckBox.Enabled = true;

            configuration.LoginAutomatically = LoginAutomaticallyCheckBox.Checked;

            ConfigurationManager.Save(configuration);
        }
        
        private async void GameStart()
        {
            StartGameButton.Enabled = false;

            if (OTPCheckBox.Checked)
                OTPAsync();
            else if (await StartGameAsync(0))
                Close();
            else
                StartGameButton.Enabled = true;
        }
        
        private void StartGameButton_Click(object sender, EventArgs e)
        {
            GameStart();
        }

        private void GameDirectoryPathLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string newGameDirectoryPath = SelectGameDirectoryPath();

            if (newGameDirectoryPath != null)
            {
                configuration.GameDirectoryPath = newGameDirectoryPath;

                ConfigurationManager.Save(configuration);

                Text = $"Launcher | {configuration.GameDirectoryPath}";
            }
        }

        private void GithubLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/bdoscientist/Launcher");
        }

        private bool CheckGameDirectoryPathAndPrompt()
        {
            string messageBoxText = null;

            if (String.IsNullOrEmpty(configuration.GameDirectoryPath))
                messageBoxText = "The path to the game is not set.\nDo you want to set it now?";
            else if (!Directory.Exists(configuration.GameDirectoryPath) || !File.Exists(Path.Combine(configuration.GameDirectoryPath, "BlackDesertLauncher.exe")))
                messageBoxText = "The path to the game is invalid.\nDo you want to set it now?";
            else
                return true;

            if (MessageBox.Show(messageBoxText,
                Text, MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                string newGameDirectoryPath = SelectGameDirectoryPath();

                if (newGameDirectoryPath != null)
                {
                    configuration.GameDirectoryPath = newGameDirectoryPath;

                    ConfigurationManager.Save(configuration);

                    Text = $"Launcher | {configuration.GameDirectoryPath}";
                }
            }

            Activate();

            return false;
        }

        private string SelectGameDirectoryPath()
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    return folderBrowserDialog.SelectedPath;
            }

            return null;
        }

        private async Task<bool> StartGameAsync(int otp)
        {
            var gameExecutableFilePath = Path.Combine(configuration.GameDirectoryPath, "BlackDesertEAC.exe");

            if (!File.Exists(gameExecutableFilePath))
            {
                MessageBox.Show($"Failed to find `BlackDesertEAC.exe`.\nUsed path: `{gameExecutableFilePath}`.\nPlease set the correct path to the game.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            if (string.IsNullOrEmpty(UsernameTextBox.Text) || string.IsNullOrEmpty(PasswordTextBox.Text))
            {
                MessageBox.Show("Please enter valid credentials.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            using (AuthenticationServiceProvider authenticationServiceProvider = new AuthenticationServiceProvider())
            {
                var playToken = await authenticationServiceProvider.AuthenticateAsync(
                    UsernameTextBox.Text, 
                    PasswordTextBox.Text, 
                    RegionComboBox.SelectedItem.ToString(), 
                    otp);

                if (playToken == null)
                {
                    MessageBox.Show("Username, Password, or OTP is not correct.\n(Or there might be an authentication problem.)",
                        "Authentication Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "CMD";
                    process.StartInfo.Arguments = "/min /C set __COMPAT_LAYER=RUNASINVOKER && start \"\" \"" + gameExecutableFilePath + "\" " + playToken;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(gameExecutableFilePath);
                    process.Start();

                    //process.StartInfo.FileName = gameExecutableFilePath;
                    //process.StartInfo.Arguments = playToken;
                    //process.StartInfo.WorkingDirectory = Path.GetDirectoryName(gameExecutableFilePath);

                    //process.Start();
                }
            }

            return true;
        }
    }

}
