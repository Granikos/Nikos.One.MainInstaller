namespace Installer.Models
{
    internal class SqlExpressInstallerTask : WpiWrapperInstallerTask
    {
        public SqlExpressInstallerTask(Root root, string wpiPath)
            : base(root, wpiPath, "SQL Server Express*", null, null)
        {
            IsSelected = true;
        }

        protected override void OnExecute()
        {
            Install("SQLExpress");
        }
    }
}