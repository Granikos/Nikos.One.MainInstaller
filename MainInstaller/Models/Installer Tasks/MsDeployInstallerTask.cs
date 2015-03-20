using System;
using System.IO;

namespace Installer.Models
{
    internal class MsDeployInstallerTask : WpiWrapperInstallerTask
    {
        public string Path { get; set; }

        public MsDeployInstallerTask(Root root, string wpiPath)
            : base(root, wpiPath, "MS Deploy*", null, null)
        {
            IsEnabled = false;
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            Path = System.IO.Path.Combine(programFiles, @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
            IsSelected = !File.Exists(Path);
        }

        protected override void OnExecute()
        {
            var l = Install("WDeploy_3_5");
        }
    }
}