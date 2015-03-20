using System;
using System.IO;
using System.Text.RegularExpressions;
using Installer.Models;
using Microsoft.Win32;

namespace Installer
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "RebootService" in both code and config file together.
    public class NikosOneInstallerService : INikosOneInstallerService
    {
        private readonly static Regex RegexRunOnce = new Regex(@"(?<=WebPlatformInstaller\.exe\W*?\s).*", RegexOptions.Compiled);
        public readonly static RegistryKey RunOnceRegistryKey;
        public readonly static string ApplicationPath;

        static NikosOneInstallerService()
        {
            RunOnceRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
            ApplicationPath = Path.GetFullPath(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

        public void Reboot(string xml)
        {
            try
            {
                Root.Instance.ReadWebSites(xml);

                GetRebootArguments();

                ModelSerilisation.Serialize(Root.Instance);

                RunOnceRegistryKey.SetValue("NikosOneInstaller", ApplicationPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private static void GetRebootArguments()
        {
            if (RunOnceRegistryKey == null)
            {
                return;
            }

            var value = RunOnceRegistryKey.GetValue("WebPlatformInstaller") as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var m = RegexRunOnce.Match(value);
            if (!m.Success)
            {
                return;
            }

            RunOnceRegistryKey.DeleteValue("WebPlatformInstaller");
            Root.Instance.RebootArguments = m.Value;
        }
    }
}
