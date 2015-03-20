using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Installer.Models
{
    class ModelSerilisation
    {
        private readonly static object FileLock = new object();

        public static bool Deserialize(Root root)
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(dir, "Granikos\\NikosOne\\installerstate.xml");

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var doc = XDocument.Load(path);
                var e0 = doc.Root;

                if (e0 == null)
                {
                    return false;
                }

                e0.ReadAttribute("currentStep", a => root.CurrentStep = root.Steps[(int)a]);
                e0.ReadAttribute("useIisExpress", a => root.UseIisExpress = (bool)a);
                e0.ReadAttribute("rebootArguments", a => root.RebootArguments = (string)a);

                var e1 = e0.Element("connectionString");
                if (e1 != null)
                {
                    var cs = root.ConnectionString;
                    e1.ReadAttribute("server", a => cs.Server = (string)a);
                    e1.ReadAttribute("database", a => cs.Database = (string)a);
                    e1.ReadAttribute("userName", a => cs.UserName = (string)a);
                    e1.ReadAttribute("password", a => cs.Password = (string)a);
                    e1.ReadAttribute("isIntegratedAuthentication", a => cs.IsIntegratedAuthentication = (bool)a);
                }

                e1 = e0.Element("tasks");
                if (e1 != null)
                {
                    foreach (var e2 in e1.Elements("task"))
                    {
                        InstallerTask task = null;
                        e2.ReadAttribute("index", a => task = root.Tasks[(int)a]);
                        if (task == null) continue;
                        e2.ReadAttribute("isEnabled", a => task.IsEnabled = (bool)a);
                        e2.ReadAttribute("isSkipped", a => task.IsCancelled = (bool)a);
                        e2.ReadAttribute("isSelected", a => task.IsSelected = (bool)a);
                        e2.ReadAttribute("text", a => task.Text = (string)a);
                        e2.ReadAttribute("progress", a => task.Progress = (double)a);
                        e2.ReadAttribute("isError", a => task.IsError = (bool)a);
                        e2.ReadAttribute("isSuccess", a => task.IsSuccess = (bool)a);
                        e2.ReadAttribute("isRunning", a => task.IsRunning = (bool)a);

                    }
                }

                e1 = e0.Element("webSites");
                if (e1 == null)
                {
                    return true;
                }

                foreach (var e2 in e1.Elements("webSite"))
                {
                    var webSite = new WebSite();
                    e2.ReadAttribute("name", a => webSite.Name = (string)a);
                    if (root.InstalledWebSites.ContainsKey(webSite.ProductId))
                    {
                        continue;
                    }

                    e2.ReadAttribute("physicalPath", a => webSite.PhysicalPath = (string)a);
                    e2.ReadAttribute("productId", a => webSite.ProductId = (string)a);
                    e2.ReadAttribute("siteName", a => webSite.SiteName = (string)a);
                    e2.ReadAttribute("url", a => webSite.Url = (string)a);

                    root.InstalledWebSites.Add(webSite.ProductId, webSite);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                return false;
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }

        public static void Serialize(Root root)
        {
            var doc = new XDocument(new XElement("nikosonesetup",
                new XAttribute("currentStep", root.Steps.IndexOf(root.CurrentStep)),
                new XAttribute("useIisExpress", root.UseIisExpress),
                new XAttribute("rebootArguments", root.RebootArguments),
                new XElement("connectionString",
                    new XAttribute("server", root.ConnectionString.Server),
                    new XAttribute("database", root.ConnectionString.Database),
                    new XAttribute("userName", root.ConnectionString.UserName),
                    new XAttribute("password", root.ConnectionString.Password),
                    new XAttribute("isIntegratedAuthentication", root.ConnectionString.IsIntegratedAuthentication)),
                new XElement("tasks",
                    from task in root.Tasks
                    select new XElement("task",
                        new XAttribute("index", root.Tasks.IndexOf(task)),
                        new XAttribute("isEnabled", task.IsEnabled),
                        new XAttribute("isSkipped", task.IsCancelled),
                        new XAttribute("isSelected", task.IsSelected),
                        new XAttribute("text", task.Text),
                        new XAttribute("progress", task.Progress),
                        new XAttribute("isError", task.IsError),
                        new XAttribute("isSuccess", task.IsSuccess),
                        new XAttribute("isRunning", task.IsRunning))),
                new XElement("webSites",
                    from website in root.InstalledWebSites.Values
                    select new XElement("webSite",
                        new XAttribute("name", website.Name),
                        new XAttribute("physicalPath", website.PhysicalPath),
                        new XAttribute("productId", website.ProductId),
                        new XAttribute("siteName", website.SiteName),
                        new XAttribute("url", website.Url)))));

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(dir, "Granikos\\NikosOne\\installerstate.xml");

            lock (FileLock)
            {
                doc.Save(path, SaveOptions.DisableFormatting);
            }
        }
    }
}