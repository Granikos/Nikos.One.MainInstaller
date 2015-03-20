using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IISVersionManagerLibrary;
using Microsoft.Web.Administration;

namespace Installer
{
    class Website
    {
        public Int32 Identity
        {
            get;
            set;
        }

        public String Name
        {
            get;
            set;
        }

        public String PhysicalPath
        {
            get;
            set;
        }

        public ServerState Status
        {
            get;
            set;
        }

        public string Url { get; set; }

        public override string ToString()
        {
            return Url;
        }
    }

    enum ServerState
    {
        Starting = 1,
        Started = 2,
        Stopping = 3,
        Stopped = 4,
        Pausing = 5,
        Paused = 6,
        Continuing = 7
    }

    class Websites
    {
        public static IEnumerable<Website> GetSites()
        {
            var result = GetIisSites().Union(GetIisExpressSites()).ToList();

            return result;
        }

        private static IEnumerable<Website> GetIisExpressSites()
        {
            try
            {
                var versionManager = new IISVersionManagerClass();
                var obs = versionManager.GetAllVersionObjects().Cast<IIISVersion>();
                return (from ob in obs
                        from s in GetIisExpressSites(ob)
                        select s).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return new Website[0];
        }

        private static IEnumerable<Website> GetIisExpressSites(IIISVersion ob)
        {
            try
            {
                var userData = ob.GetPropertyValue("userInstanceHelper") as IIISUserData;

                var path = Path.Combine(userData.IISDirectory, "config", "applicationhost.config");
                var doc = XDocument.Load(path);
                return from e in doc.Root.Element("system.applicationHost").Element("sites").Elements("site")
                       select new Website
                       {
                           Identity = (int)e.Attribute("id"),
                           Name = (string)e.Attribute("name"),
                           PhysicalPath = (string)e.Element("application").Element("virtualDirectory").Attribute("physicalPath"),
                           Url = GetIisExpressUrl(e)
                       };
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return new Website[0];
        }

        private readonly static Regex RegexIisExpressUrl = new Regex(@"(?<port>\d+)\:(?<host>[^\:]+)", RegexOptions.Compiled);

        private static string GetIisExpressUrl(XElement site)
        {
            var b = site.Element("bindings");
            if (b == null) return null;

            b = b.Element("binding");
            if (b == null) return null;

            var s = (string)b.Attribute("bindingInformation");
            var m = RegexIisExpressUrl.Match(s);
            if (!m.Success) return null;

            return string.Format("http://{0}:{1}", m.Groups["host"].Value, m.Groups["port"].Value);
        }

        private static IEnumerable<Website> GetIisSites(string path = "IIS://localhost/W3SVC")
        {
            IEnumerable<Website> result;

            try
            {
                var isEntities = new DirectoryEntry(path);
                result = (from s in isEntities.Children.OfType<DirectoryEntry>()
                          where s.SchemaClassName == "IIsWebServer"
                          select new Website
                          {
                              Identity = Convert.ToInt32(s.Name),
                              Name = s.Properties["ServerComment"].Value.ToString(),
                              PhysicalPath = (from p in s.Children.OfType<DirectoryEntry>()
                                              where p.SchemaClassName == "IIsWebVirtualDir"
                                              select p.Properties["Path"].Value.ToString()).Single(),
                              Status = (ServerState)s.Properties["ServerState"].Value
                          }).ToList();
            }
            catch (COMException ex)
            {
                var iis = new ServerManager();

                result = (from site in iis.Sites
                          let binding = site.Bindings.FirstOrDefault()
                          where binding != null && binding.EndPoint != null
                          let baseAddress = string.Format("{0}://{1}:{2}",
                              binding.Protocol,
                              IPAddress.Any.Equals(binding.EndPoint.Address) ? "localhost" : binding.EndPoint.Address.ToString(),
                              binding.EndPoint.Port)
                          from app in site.Applications
                          select new Website
                          {
                              Identity = (int)site.Id,
                              Name = app.Path,
                              PhysicalPath = app.VirtualDirectories[0].PhysicalPath,
                              Status = (ServerState)site.State,
                              Url = baseAddress + app.Path
                          }).ToList();
            }

            return result;
        }
    }
}
