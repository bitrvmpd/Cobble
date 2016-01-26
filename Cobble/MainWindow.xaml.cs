/*
    Copyright (C) 2016  Eduardo Elías Noyer Silva

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using Cobble.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cobble
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        APPXVersion AppxVersion { get; set; }
        bool DownloadCompleted { get; set; }
        string WinAppDeploymentPath{get;set;}
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await SearchForUpdates();
        }

        /// <summary>
        /// Checks for phone connection
        /// </summary>
        /// <returns>Connected state</returns>
        private async Task<bool> CheckPhoneConnection()
        {
            //Only if we have the .cmd file, we can proceed. otherwise ask for the location of the win10SDK
            if (File.Exists(Properties.Settings.Default.WinAppDeploymentPath + @"bin\x86\WinAppDeployCmd.exe"))
                WinAppDeploymentPath = Properties.Settings.Default.WinAppDeploymentPath + @"bin\x86\WinAppDeployCmd.exe";
            else
            {
                //Ask the user if it has installed the windows 10 SDK.
                var response = MessageBox.Show("Windows 10 SDK Not found\n Do you have it Installed?", "Cobble", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    //Ask for the location of the file.
                    MessageBox.Show("Please locate the \"Windows Kits\\10\\\" Folder.", "Cobble", MessageBoxButton.OK);
                    var dialog = new System.Windows.Forms.FolderBrowserDialog();
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        Properties.Settings.Default.WinAppDeploymentPath = dialog.SelectedPath+@"\";
                        Properties.Settings.Default.Save();
                        WinAppDeploymentPath = Properties.Settings.Default.WinAppDeploymentPath + @"bin\x86\WinAppDeployCmd.exe";
                    }
                    else
                        return false;                    
                }
                else
                {
                    //Not installed.
                    MessageBox.Show("Please Install the Windows 10 SDK First.", "Cobble", MessageBoxButton.OK);
                    return false;
                }                
            }

            return await Task<bool>.Run(() =>
            {
                if (!File.Exists(WinAppDeploymentPath))
                {
                    MessageBox.Show("Windows 10 SDK Not found.");
                    return false;
                }
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = WinAppDeploymentPath;
                p.StartInfo.Arguments = "devices";
                p.Start();


                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Contains("127.0.0.1");
            });
        }
        /// <summary>
        /// Install APPX located at /Res folder.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> InstallAPPXToPhone()
        {
            return await Task<bool>.Run(() =>
            {
                if (!Directory.Exists("Res"))
                    return false;
                string[] files = Directory.GetFiles("Res");
                string fileName = "";
                if (files.Count() > 0)
                    fileName = files.First();
                if (fileName == "")
                    return false;
                if (!File.Exists(WinAppDeploymentPath))
                    return false;

                //Install the APPX
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = WinAppDeploymentPath;

                p.StartInfo.Arguments = $"install -file {fileName} -ip 127.0.0.1";
                p.Start();

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Contains("Installing app..." + Environment.NewLine + "Remote action succeeded.");

            });
        }
        /// <summary>
        /// Update APPX located at /Res folder
        /// </summary>
        /// <returns></returns>
        private async Task<bool> UpdateAPPXFromPhone()
        {
            return await Task<bool>.Run(() =>
            {
                if (!Directory.Exists("Res"))
                    return false;
                string[] files = Directory.GetFiles("Res");
                string fileName = "";
                if (files.Count() > 0)
                    fileName = files.First();
                if (fileName == "")
                    return false;
                if (!File.Exists(WinAppDeploymentPath))
                    return false;

                //Update the APPX
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = WinAppDeploymentPath;

                p.StartInfo.Arguments = $"update -file {fileName} -ip 127.0.0.1";
                p.Start();

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Contains("Updating app..." + Environment.NewLine + "Remote action succeeded.");
            });
        }
        /// <summary>
        /// Checks if APPX is currently installed.
        /// TODO: Change the APPX name to another one.
        /// </summary>
        /// <returns></returns>
        private async Task<string> CheckAPPXInstalled()
        {
            return await Task<string>.Run(() =>
            {
                if (!Directory.Exists("Res"))
                    return null;
                string[] files = Directory.GetFiles("Res");
                string fileName = "";
                if (files.Count() > 0)
                    fileName = files.First();
                if (fileName == "")
                    return null;
                if (!File.Exists(WinAppDeploymentPath))
                    return null;

                //First Check if the app is installed.
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = WinAppDeploymentPath;
                p.StartInfo.Arguments = $"list -ip 127.0.0.1";
                p.Start();


                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                IEnumerable<string> installedApps = (from i in output.Split(Environment.NewLine.ToCharArray())
                                                     where i.Contains("PebbleWuff")
                                                     select i);
                if (installedApps.Count() > 0)
                    return installedApps.First();
                else
                    return null;
            });
        }
        /// <summary>
        /// Uninstalls the APPX from phone.
        /// </summary>
        /// <param name="installedAPPXName"></param>
        /// <returns></returns>
        private async Task<bool> UnInstallAPPXFromPhone(string installedAPPXName)
        {
            return await Task<bool>.Run(() =>
            {
                if (!Directory.Exists("Res"))
                    return false;
                string[] files = Directory.GetFiles("Res");
                string fileName = "";
                if (files.Count() > 0)
                    fileName = files.First();
                if (fileName == "")
                    return false;
                if (!File.Exists(WinAppDeploymentPath))
                    return false;

                //Uninstall the APPX
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = @"C:\Program Files (x86)\Windows Kits\10\bin\x86\WinAppDeployCmd.exe";

                p.StartInfo.Arguments = $"uninstall -package {installedAPPXName} -ip 127.0.0.1";
                p.Start();

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Contains("Uninstalling app..." + Environment.NewLine + "Remote action succeeded.");
            });
        }
        /// <summary>
        /// Download an updated version of the APPX
        /// </summary>
        private async void DownloadUpdate()
        {
            if (AppxVersion != null)
            {
                txbUpToDate.Text = $"v{AppxVersion.VersionNumber}-{AppxVersion.Release}";
                if (!await AppxVersion.DownloadUpdate(DownloadProgressChanged, DownloadFileCompleted))
                {
                    txbUpToDate.Text = $"Download Falied v{AppxVersion.VersionNumber}-{AppxVersion.Release}";
                }
            }
        }
        /// <summary>
        /// Search for updates in the server (GitHub).
        /// </summary>
        /// <returns></returns>
        private async Task<int> SearchForUpdates()
        {
            txbUpToDate.Text = $"Checking for new APPX Version...";
            string localVersion = Properties.Settings.Default.CurrentVersion;
            string localRelease = Properties.Settings.Default.CurrentRelease;

            //Check for new Updates or Download Binary.
            AppxVersion = await (new APPXVersion().CheckForUpdates());
            if (AppxVersion != null)
            {
                //Yai! Update
                txbUpToDate.Text = $"New Update Available! v{AppxVersion.VersionNumber}-{AppxVersion.Release}";
            }
            else
            {
                //No Updates Available. Show Current Version.
                txbUpToDate.Text = $"Up to date, Current: v{localVersion}-{localRelease}";
            }
            return 0;
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //Report Download
            txbProgress.Text = $"Downloading... {e.ProgressPercentage}%";
        }
        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //Unzip the file.            
            string filepath = $@"Res\{AppxVersion.FileName}.zip";
            if (!File.Exists(filepath))
            {
                txbUpToDate.Text = $"Download Failed! v{AppxVersion.VersionNumber}-{AppxVersion.Release}";
                txbProgress.Text = "";
                AppxVersion = null;
                return;
            }
            else
            {
                ZipFile.ExtractToDirectory(filepath, "Res");
                //Download Completed
                txbUpToDate.Text = $"Download Complete! v{AppxVersion.VersionNumber}-{AppxVersion.Release}";
                txbProgress.Text = "";
                //Hago Update a la version local.
                Properties.Settings.Default.CurrentRelease = AppxVersion.Release;
                Properties.Settings.Default.CurrentVersion = AppxVersion.VersionNumber;
                Properties.Settings.Default.Save();
                AppxVersion = null;
                DownloadCompleted = true;
            }            
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnUpdate.IsEnabled = false;
            btnInstall.IsEnabled = false;
            btnUninstall.IsEnabled = false;
            await SearchForUpdates();
            btnUpdate.IsEnabled = true;
            btnInstall.IsEnabled = true;
            btnUninstall.IsEnabled = true;
        }
        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            DownloadCompleted = false;
            btnUpdate.IsEnabled = false;
            btnInstall.IsEnabled = false;
            btnUninstall.IsEnabled = false;
            txbProgress.Text = "";
            //If AppxVersion is Null, Install the current one.            
            if (AppxVersion != null)
            {
                //Else, download and install the new one.
                DownloadUpdate();
            }
            else if (Directory.EnumerateFiles("Res").Count() == 0) //File not exists!
            {
                await SearchForUpdates();
                DownloadUpdate();
            }
            else if (Directory.EnumerateFiles("Res").Count() == 1) //Simplest way to verify..
            {
                //File Exists, proceed to deploy.
                DownloadCompleted = true;
            }

            await Task.Run(async () =>
            {
                while (!DownloadCompleted)
                {
                    await Task.Delay(1);
                }
            });

            //Proceed to Deploy..
            txbUpToDate.Text = $"v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
            txbProgress.Text = "Cheking device connection...";

            if (await CheckPhoneConnection())
            {
                txbProgress.Text = "Device Connected!";
                await Task.Delay(1000);
                txbProgress.Text = "Deploying XAPP...";
                if (await CheckAPPXInstalled() != null)
                {
                    txbProgress.Text = "Updating XAPP...";
                    //Installed, Update new version.
                    if (await UpdateAPPXFromPhone())
                        txbUpToDate.Text = $"Update Completed! v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
                    else
                        txbUpToDate.Text = $"Update Failed v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
                }
                else
                {
                    //Not Installed, proceed to install.
                    txbProgress.Text = "Installing XAPP...";
                    if (await InstallAPPXToPhone())
                        txbUpToDate.Text = $"Install Completed! v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
                    else
                        txbUpToDate.Text = $"Install Failed v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
                }
                txbProgress.Text = "";
            }
            else
            {
                txbProgress.Text = "Device not Connected";
                txbUpToDate.Text = $"Install Failed v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
            }
            btnUpdate.IsEnabled = true;
            btnInstall.IsEnabled = true;
            btnUninstall.IsEnabled = true;
        }
        /// <summary>
        /// Uninstall the current version of the Pebble APPX
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            btnUpdate.IsEnabled = false;
            btnInstall.IsEnabled = false;
            btnUninstall.IsEnabled = false;
            txbUpToDate.Text = $"v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
            txbProgress.Text = "Cheking device connection...";
            if (await CheckPhoneConnection())
            {
                txbProgress.Text = "Device Connected!";
                await Task.Delay(1000);
                txbProgress.Text = "Cheking if APPX is Installed...";
                string installedAPPXName = await CheckAPPXInstalled();
                if (installedAPPXName != null)
                {
                    txbProgress.Text = "Uninstalling...";
                    await UnInstallAPPXFromPhone(installedAPPXName);
                    txbUpToDate.Text = $"Uninstall Completed! v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
                }
                else
                    txbUpToDate.Text = $"APPX Not Installed! v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";

                txbProgress.Text = "";
            }
            else
            {
                txbProgress.Text = "Device not Connected";
                txbUpToDate.Text = $"Uninstall Failed v{Properties.Settings.Default.CurrentVersion}-{Properties.Settings.Default.CurrentRelease}";
            }
            btnUpdate.IsEnabled = true;
            btnInstall.IsEnabled = true;
            btnUninstall.IsEnabled = true;
        }
    }
}
