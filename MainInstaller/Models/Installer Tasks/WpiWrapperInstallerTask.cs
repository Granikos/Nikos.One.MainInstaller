using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Installer.Models
{
    internal abstract class WpiWrapperInstallerTask : InstallerTask
    {
        private const string ProcessName = "NikosOneWpiWrapper";

        private static readonly string[] ReferencedAssemblies =
        {
            "Microsoft.Web.PlatformInstaller.dll",
            "Microsoft.Web.PlatformInstaller.UI.dll"
        };

        private readonly string _wpiPath;

        private readonly Dictionary<string, WebSite> _webSites = new Dictionary<string, WebSite>(StringComparer.OrdinalIgnoreCase);

        protected WpiWrapperInstallerTask(Root root, string wpiPath, string name, string urlEula, string urlPrivacy)
            : base(root, name, urlEula, urlPrivacy)
        {
            _wpiPath = wpiPath;
        }

        protected Dictionary<string, WebSite> Install(string productId, bool addCustomFeed = false)
        {
            _webSites.Clear();

            if (!KillAll(ProcessName))
            {
                return _webSites;
            }

            var processPath = Environment.CurrentDirectory;
            var wpiPath = Path.GetDirectoryName(_wpiPath);

            foreach (var referencedAssembly in ReferencedAssemblies)
            {
                var dst = Path.Combine(processPath, referencedAssembly);
                if (File.Exists(dst))
                {
                    continue;
                }

                Debug.Assert(wpiPath != null, "wpiPath != null");

                var src = Path.Combine(wpiPath, referencedAssembly);
                File.Copy(src, dst);
            }

            string originalFeed = null;

            if (addCustomFeed)
            {
                var webPiPreferences = new WebPiPreferences();

                originalFeed = webPiPreferences.SelectedFeeds;
                var newFeed = Path.Combine(Environment.CurrentDirectory, "Installers", "wpi", "GranikosFeed.xml");

                webPiPreferences.SelectedFeeds = newFeed;
                webPiPreferences.Save();
            }

            try
            {
                string arguments;

                if (!string.IsNullOrWhiteSpace(Root.RebootArguments))
                {
                    arguments = Root.RebootArguments;
                    Root.RebootArguments = null;
                }
                else
                {
                    arguments = "/id " + productId;
                    if (Root.UseIisExpress) arguments += " /iisexpress";
                }

                Call(ProcessName + ".exe", arguments, "NikosOneWpiWrapper");
                return _webSites;
            }
            finally
            {
                if (addCustomFeed)
                {
                    try
                    {
                        new WebPiPreferences
                        {
                            SelectedFeeds = originalFeed
                        }.Save();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        throw;
                    }
                }
            }
        }

        protected override void OnProcessOutputReceived(string output)
        {
            if (output != null)
            {
                var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var line = lines.FirstOrDefault(l => l.StartsWith("PACKAGES: "));

                if (line != null)
                {
                    var xml = line.Substring(10);
                    Root.ReadWebSites(xml, _webSites);

                    var sb = new StringBuilder();

                    foreach (var webSite in _webSites.Values.Where(s => !File.Exists(Path.Combine(s.PhysicalPath, "web.config"))))
                    {
                        IsError = true;
                        sb.AppendLine(string.Format("The path '{0}' does not exist.", webSite.PhysicalPath));
                    }

                    if (IsError)
                    {
                        Text = sb.ToString();
                        Log.Error(Text);
                    }
                }

                line = lines.FirstOrDefault(l => l.StartsWith("ERROR: "));

                if (line != null)
                {
                    IsError = true;
                    Text = line.Substring(7);
                    Log.Error(Text);
                }
            }

            base.OnProcessOutputReceived(output);
        }
    }
}