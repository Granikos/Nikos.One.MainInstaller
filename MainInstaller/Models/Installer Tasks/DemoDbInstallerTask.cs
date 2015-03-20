using System;
using System.IO;

namespace Installer.Models
{
    internal class DemoDbInstallerTask : InstallerTask
    {
        private readonly string _msDeployPath;
        private readonly SqlExpressInstallerTask _sqlExpressTask;

        public string ConnectionString
        {
            get
            {
                return Root.ConnectionString.ToString();
            }
        }

        public DemoDbInstallerTask(Root root, string msDeployPath, string wpiPath)
            : base(root, "Demo Product Database", null, null)
        {
            _msDeployPath = msDeployPath;
            _sqlExpressTask = new SqlExpressInstallerTask(root, wpiPath)
            {
                IsSelected = true
            };
        }

        protected override void OnSelectionChanged()
        {
            if (IsSelected && !Root.Tasks.Contains(_sqlExpressTask))
            {
                var index = Root.Tasks.IndexOf(this);
                Root.Tasks.Insert(index, _sqlExpressTask);
            }
            else if (!IsSelected && Root.Tasks.Contains(_sqlExpressTask))
            {
                Root.Tasks.Remove(_sqlExpressTask);
            }
        }

        protected override void OnExecute()
        {
            var demoDbPackagePath = Path.Combine(Environment.CurrentDirectory, @"Installers\demodb\DemoProductDatabase.zip");
            var fileName = _msDeployPath;
            var arguments = string.Format("-source:package=\"{0}\" -dest:dbdacfx=\"{1}\" -verb:sync", demoDbPackagePath, ConnectionString);

            Call(fileName, arguments);
        }
    }
}