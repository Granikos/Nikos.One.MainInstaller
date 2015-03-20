using System;
using System.IO;
using System.Linq;

namespace Installer.Models
{
    internal class WpiInstallerTask : InstallerTask
    {
        private bool _gotOutput;

        public string Path { get; set; }

        public WpiInstallerTask(Root root)
            : base(root, "Web Platform Installer*", "http://go.microsoft.com/fwlink/?LinkId=251729", "http://go.microsoft.com/fwlink/?LinkId=251732")
        {
            IsEnabled = false;
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            //Path = System.IO.Path.Combine(programFiles, @"Microsoft\Web Platform Installer\WebpiCmd.exe");
            Path = System.IO.Path.Combine(programFiles, @"Microsoft\Web Platform Installer\WebPlatformInstaller.exe");
            IsSelected = !File.Exists(Path);
        }

        protected override void OnExecute()
        {
            _gotOutput = false;

            var path = System.IO.Path.Combine(Environment.CurrentDirectory, @"Installers\wpilauncher.exe");
            Call(path, null);

            if (!File.Exists(Path))
            {
                IsError = true;
                Text = "WPI did not install correctly";
                Log.Error(string.Format("The path '{0}' does not exist.", Path));
            }

            if (IsCancelled)
            {
                IsSuccess = true;
            }
        }

        protected override void OnProcessErrorReceived(string output)
        {
            _gotOutput = true;

            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            IsError = true;
            Text = output;
            Log.Error(output);
        }

        protected override void OnProcessOutputReceived(string output)
        {
            _gotOutput = true;

            if (output == null)
            {
                IsCancelled = true;
            }
            else
            {
                var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l == "CANCELLED");

                if (line != null)
                {
                    IsCancelled = true;
                }
            }
        }
    }
}