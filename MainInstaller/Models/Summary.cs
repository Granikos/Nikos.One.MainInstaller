using System.Diagnostics;

namespace Installer.Models
{
    class Summary
    {
        public Command Navigate { get; private set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Argument { get; set; }

        public Summary(Root root, string name, WebSite webSite)
        {
            Name = name;

            if (root.UseIisExpress)
            {
                Url = root.GetWebMatrixExe();
                Argument = " #ExecuteCommand# OpenSiteAndLaunchInBrowser " + webSite.SiteName;
            }
            else
            {
                Url = webSite.Url;
            }

            Navigate = new Command(p =>
                {
                    var url = p as string;
                    if (url != null)
                    {
                        Process.Start(new ProcessStartInfo(Url)
                        {
                            Arguments = Argument
                        });
                    }
                }, () => true);
        }
    }
}
