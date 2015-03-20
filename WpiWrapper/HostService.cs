using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.PlatformInstaller.UI;

namespace DeploymentTools
{
class HostService : IHostService
{
    public Icon ShellIcon { get; private set; }
    public bool ShowProducts { get; private set; }
    public bool AllowIisExpressAppInstall { get; set; }
    public bool UseIisExpressForAppInstall { get; set; }
    public IWin32Window WindowHandle { get; private set; }
    public string ProductForumUri { get; private set; }
    public string ProductTitle { get; private set; }
    public string ProductTitleLong { get; private set; }

    public event EventHandler<FormClosingEventArgs> ShellFormClosing;

    public HostService()
    {
        ShellIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        ShowProducts = false;
        WindowHandle = Control.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
    }

    internal void RaiseShellFormClosing(object sender, FormClosingEventArgs e)
    {
        if (ShellFormClosing != null)
        {
            ShellFormClosing(sender, e);
        }
    }
}
}