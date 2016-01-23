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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;

namespace Cobble.Models
{
    public class APPXVersion
    {
        //Version source.
        private const string uri = "https://raw.githubusercontent.com/bitrvmpd/Pebble-W10M/master/version.js";
        /// <summary>
        /// Download URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Checks for a new Version of the APPX
        /// </summary>
        public string Release { get; set; }
        public string VersionNumber { get; set; }
        public string FileName { get; set; }
        public async Task<APPXVersion> CheckForUpdates()
        {
            try
            {
                string localVersion = Properties.Settings.Default.CurrentVersion;
                string localRelease = Properties.Settings.Default.CurrentRelease;

                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(uri);
                var newAPPXVersion = JsonConvert.DeserializeObject<APPXVersion>(response);

                Version current = new Version(localVersion);
                Version recent = new Version(newAPPXVersion.VersionNumber);

                if (recent.CompareTo(current) < 0)
                {
                    //No new Versions Available
                    return null;
                }
                else if (recent.CompareTo(current) == 0)
                {
                    //If Zero, it may be the same version, but different Release.
                    ReleaseType localRS = (ReleaseType)Enum.Parse(typeof(ReleaseType), localRelease, true);
                    ReleaseType remoteRS = (ReleaseType)Enum.Parse(typeof(ReleaseType), newAPPXVersion.Release, true);
                    //Check if it's greater than our release type
                    if (localRS >= remoteRS)
                    {
                        //The same version
                        //Do we have the local file?.
                        if (File.Exists($"Res\\{newAPPXVersion.FileName}"))
                            return null; //Yup
                        //Nope, continue and download the file.
                    }
                }

                //Passes all the checks NEW VERSION AVAILABLE!
                return newAPPXVersion; // return the new object.
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<bool> DownloadUpdate(DownloadProgressChangedEventHandler progressEventHandler,
            AsyncCompletedEventHandler progressCompleteEventHandler)
        {
            try
            {
                if (Url != null && FileName != null)
                {
                    string filePath = $"Res\\{FileName}";
                    //We download our update.
                    //HttpClient client = new HttpClient();
                    WebClient wclient = new WebClient();

                    //var input = await client.GetByteArrayAsync(Url);

                    //We Recreate our previous Res Folder
                    if (Directory.Exists("Res"))
                        Directory.Delete("Res", true);
                    Directory.CreateDirectory("Res");
                    //using (var fileStream = File.Create(filePath))
                    //{
                    //    fileStream.Write(input, 0, input.Length);
                    //}
                    wclient.DownloadProgressChanged += progressEventHandler;
                    wclient.DownloadFileCompleted += progressCompleteEventHandler;
                    wclient.DownloadFileAsync(new Uri(Url), filePath);
                    return File.Exists(filePath);
                }
                else
                {
                    //Something went wrong.
                    return false;
                }
            }
            catch (Exception)
            {
                //Something really went wrong.
                return false;
            }
        }
    }
    public enum ReleaseType
    {
        alpha = 0,
        beta = 1,
        rc = 2,
        stable = 3
    }
}
