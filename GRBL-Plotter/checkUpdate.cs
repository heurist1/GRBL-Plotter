﻿/*  GRBL-Plotter. Another GCode sender for GRBL.
    This file is part of the GRBL-Plotter application.
   
    Copyright (C) 2015-2019 Sven Hasemann contact: svenhb@web.de

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
/*
 * 2019-10-27 add logger
 * 2019-12-07 send some user info in line 56
 * 2020-05-06 add Logger.Info
*/

using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Management;
using System.Linq;
using System.Net;

namespace GRBL_Plotter
{
    class checkUpdate
    {
		private enum counterType {import, usage};
		
        private static bool showAny = false;
        // Trace, Debug, Info, Warn, Error, Fatal
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void CheckVersion(bool showAnyResult=false)
        {
            Logger.Info("CheckVersion");
            showAny = showAnyResult;
//            if (Properties.Settings.Default.guiCheckUpdate)
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(checkUpdate.AsyncCheckVersion));
        }

        private static void AsyncCheckVersion(object foo)
        {
            try
            {   TryCheckSite2();
            } //official https
            catch
            {
                CheckSite1(@"https://api.github.com/repos/svenhb/GRBL-Plotter/releases/latest");
            }
        }

        private static void TryCheckSite2()
        {
            try
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                ci = CultureInfo.CurrentUICulture;
                Logger.Trace(" Vers.:{0}  ID:{1}  LangSet:{2}  LangOri:{3}  url:{4}", Application.ProductVersion, getID(), Properties.Settings.Default.guiLanguage, ci.Name, Properties.Settings.Default.guiCheckUpdateURL);
                string get = "";
                get += "?vers=" + Application.ProductVersion;
                get += "&hwid=" + getID();
                get += "&langset=" + Properties.Settings.Default.guiLanguage; // add next get with &
                get += "&langori=" + ci.Name;
                get += "&import=" + getCounters(counterType.import);
                get += "&usage=" + getCounters(counterType.usage);
                if (!Properties.Settings.Default.guiCheckUpdateURL.StartsWith("http"))
                {
                    Properties.Settings.Default.guiCheckUpdateURL = "https://GRBL-Plotter.de";
                    Properties.Settings.Default.Save();
                    Logger.Info("Reset URL:{0}", Properties.Settings.Default.guiCheckUpdateURL);
                }
                CheckSite2(Properties.Settings.Default.guiCheckUpdateURL + "/GRBL-Plotter.php" + get);   // get Version-Nr and count individual ip to get an idea of amount of users
            }
            catch (Exception ex)
            {   Logger.Error(ex, "AsyncCheckVersion - CheckSite2"); }

        }


        private static string getCounters(counterType type)
		{	try
            {
				if (type == counterType.import)
				{	uint gcode = Properties.Settings.Default.counterImportGCode;
					uint svg = Properties.Settings.Default.counterImportSVG;
					uint dxf = Properties.Settings.Default.counterImportDXF;
					uint hpgl =Properties.Settings.Default.counterImportHPGL;
					uint csv = Properties.Settings.Default.counterImportCSV;
					uint drill =Properties.Settings.Default.counterImportDrill;
                    uint gerber = Properties.Settings.Default.counterImportGerber;
                    uint image =Properties.Settings.Default.counterImportImage;
					uint barcode=Properties.Settings.Default.counterImportBarcode;
					uint text = Properties.Settings.Default.counterImportText;
					uint shape = Properties.Settings.Default.counterImportShape;
					uint extension = Properties.Settings.Default.counterImportExtension;
					string tmp = string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}_",gcode,svg,dxf,hpgl,csv,drill,gerber);
					tmp += string.Format("{0}-{1}-{2}-{3}-{4}",image,barcode,text,shape,extension);
					Logger.Trace(" getCounters import {0}  gc,svg,dxf,hpgl,csv,drill,gerber _ img,barc,txt,shape,ext",tmp);
					return tmp;
				}
				else if (type == counterType.usage)
				{	uint laser = Properties.Settings.Default.counterUseLaserSetup;
					uint probe = Properties.Settings.Default.counterUseProbing;
					uint height = Properties.Settings.Default.counterUseHeightMap;
					string tmp = string.Format("{0}-{1}-{2}",laser, probe, height);
					Logger.Trace(" getCounters usage {0}   laser,probe,height",tmp);
					return tmp;
				}
			}
			catch (Exception ex)
			{ Logger.Error(ex, " getCounters"); }
			return "0";
		}
        // Suddenly it was not possible anymore to get latest version from here (@"https://api.github.com/repos/svenhb/GRBL-Plotter/releases/latest"); 
        // workarround: put file 'GRBL-Plotter.txt' with actual version on own server

        private static void CheckSite2(string site)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {   try
                {
                    Logger.Trace("CheckSite2 {0}", site);
                    string[] lines = wc.DownloadString(site).Split(';');
                    string vers = lines[0];
                    Logger.Trace("CheckSite2 {0}", String.Join(" | ", lines));
                    Version current = typeof(checkUpdate).Assembly.GetName().Version;
                    Version latest = new Version(vers);
					
					String info ="";
					if (lines.Length > 1)
						info = lines[1];
                    showResult(current, latest, info);
                }
                catch (Exception ex)
                { Logger.Error(ex,"CheckSite2 "); }
            }
        }

        private static void CheckSite1(string site)
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                Logger.Trace("CheckSite1 {0}", site);
                wc.Headers.Add("User-Agent: .Net WebClient");
                string json = wc.DownloadString(site);

                string url = null;
                string versionstr = null;
                string name = null;

                foreach (Match m in Regex.Matches(json, @"""browser_download_url"":""([^""]+)"""))
                    if (url == null)
                        url = m.Groups[1].Value;
                foreach (Match m in Regex.Matches(json, @"""tag_name"":""v([^""]+)"""))
                    if (versionstr == null)
                        versionstr = m.Groups[1].Value;
                foreach (Match m in Regex.Matches(json, @"""name"":""([^""]+)"""))
                    if (name == null)
                        name = m.Groups[1].Value;

                Version current = typeof(checkUpdate).Assembly.GetName().Version;
                Version latest = new Version(versionstr);

                showResult(current, latest, "");
                Logger.Trace("CheckSite1 {0}", json);
            }
        }

        private static void showResult(Version current, Version latest, String info)
        {
			String txt = "A new GRBL-Plotter version is available";
			String title = "New Version";
			if (current >= latest)
			{
				txt = "Installed version is up to date - nothing to do";
				title = "Information";
			}
			Logger.Info("CheckVersion: current:{0} latest:{1}  info:{2}",current,latest,info);
			
			if (info != "")
				info = "\r\n"+info+"\r\n";
			if ((current < latest) || showAny)
			{
				if (MessageBox.Show(txt + "\r\nInstalled Version: " + current + "\r\nLatest Version     : " + latest + "\r\n"+info+"\r\nCheck: https://github.com/svenhb/GRBL-Plotter/releases \r\n\r\nOpen website?", title,
				MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly, false) == DialogResult.Yes)    // (MessageBoxOptions)0x40000);
				{
					System.Diagnostics.Process.Start(@"https://github.com/svenhb/GRBL-Plotter/releases");
				}
			}
        }

        private static string getID()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            try
            {   foreach (ManagementObject mo in moc)
                {   if (cpuInfo == "")
                    {   //Get only the first CPU's ID
                        cpuInfo = mo.Properties["processorID"].Value.ToString();
                        break;
                    }
                }
                // Encrypt for anonymization
                var hash = new System.Security.Cryptography.SHA1Managed().ComputeHash(System.Text.Encoding.UTF8.GetBytes(cpuInfo));
                return string.Concat(hash.Select(b => b.ToString("x2")));
//                return cpuInfo;
            }
            catch { return "no-id"; }
        }

    }
}
