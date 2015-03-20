using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Installer.Models
{
    internal class DnnInstallerTask : WpiWrapperInstallerTask
    {
        public DnnInstallerTask(Root root, string wpiPath)
            : base(root, wpiPath, "DotNetNuke (DNN)**", "https://dotnetnuke.codeplex.com/license", null)
        {
            IsSelected = true;
        }

        protected override void OnExecute()
        {
            var websites = Install("DotNetNuke");
            WebSite webSite;

            if (!websites.TryGetValue("DotNetNuke", out webSite))
            {
                Log.Error("DotNetNuke was not installed.");
                return;
            }

            var modulePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".."));
            modulePath = Directory.EnumerateFiles(modulePath, "*_Install.zip").FirstOrDefault();
            Debug.Assert(modulePath != null, "modulePath != null");

            var destPath = Path.Combine(webSite.PhysicalPath, "Install\\Module", Path.GetFileName(modulePath));

            try
            {
                Log.Info(string.Format("Copying {0} to {1}.", modulePath, destPath));
                File.Copy(modulePath, destPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            Root.Summary.Add(new Summary(Root, "DotNetNuke", webSite));
        }
    }
}