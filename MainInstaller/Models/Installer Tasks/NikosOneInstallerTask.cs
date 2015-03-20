using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Installer.Models
{
    internal class NikosOneInstallerTask : WpiWrapperInstallerTask
    {
        public NikosOneInstallerTask(Root root, string wpiPath)
            : base(root, wpiPath, "nikos one", null, null)
        {
            IsSelected = true;
        }

        private static readonly Regex RegexUrl = new Regex(@"(?<=\w+\:\/\/).+", RegexOptions.Compiled);

        protected override void OnExecute()
        {
            var websites = Install("GranikosNikosOne", true);

            if (IsError)
            {
                return;
            }

            if (!websites.ContainsKey("GranikosNikosOne"))
            {
                Text = "Not all nikos one services have been installed.";
                Log.Error("GranikosNikosOne was not installed");
                return;
            }

            if (!websites.ContainsKey("GranikosNikosOneFileService"))
            {
                Text = "Not all nikos one services have been installed.";
                Log.Error("GranikosNikosOneFileService was not installed");
                return;
            }

            var nikosOneService = websites["GranikosNikosOne"];
            var nikosOneServiceUrl = RegexUrl.Match(nikosOneService.Url).Value;
            var nikosOneFileService = websites["GranikosNikosOneFileService"];
            var nikosOneFileServiceUrl = RegexUrl.Match(nikosOneFileService.Url).Value;

            WriteWebConfig(nikosOneService, nikosOneServiceUrl, nikosOneFileServiceUrl);
            WriteWebConfig(nikosOneFileService, nikosOneServiceUrl, nikosOneFileServiceUrl);

            Root.Summary.Add(new Summary(Root, "nikos one", nikosOneService));
        }

        private void WriteWebConfig(WebSite webSite, string nikosOneServiceUrl, string nikosOneFileServiceUrl)
        {
            var path = Path.Combine(webSite.PhysicalPath, "web.config");
            var doc = XDocument.Load(path);

            foreach (var e in doc.Root.Element("nikos").Elements("data"))
            {
                var e2 = e.Element("SERVICEBASE");
                if (e2 != null)
                {
                    e2.Value = nikosOneServiceUrl;
                }

                e2 = e.Element("FILESERVICEBASE");
                if (e2 != null)
                {
                    e2.Value = nikosOneFileServiceUrl;
                }

                UpdateStoreConnectionString(e);
            }
            doc.Save(path);
        }

        private void UpdateStoreConnectionString(XElement e)
        {
            if (Root.ConnectionString == null)
            {
                return;
            }

            var connectionString = Root.ConnectionString.ToString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            var e2 = e.Element("stores");

            if (e2 == null)
            {
                return;
            }

            e2 = e.Elements("store").FirstOrDefault(e3 => e3.Attribute("name") == null);
            if (e2 == null)
            {
                return;
            }

            e2 = e.Element("connectionstring");
            if (e2 == null)
            {
                return;
            }

            var a = e.Attribute("connectionString");
            if (a == null)
            {
                e.Add(new XAttribute("connectionString", connectionString));
            }
            else
            {
                a.Value = connectionString;
            }
        }
    }
}