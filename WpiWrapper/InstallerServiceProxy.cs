using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Installer;
using Microsoft.Web.Administration;
using Microsoft.Web.PlatformInstaller;

namespace DeploymentTools
{
    class InstallerServiceProxy : ReflectionWrapper
    {
        private readonly object _installerService;

        public event EventHandler<EventArgs> ProductInstallChainCompleted;
        public event EventHandler<InstallStatusEventArgs> ProductInstallerStatusUpdated;
        public event EventHandler<EventArgs> ProductInstalledUpdated;

        public bool IsError { get; set; }


        private readonly Func<bool> _canceled;
        public bool Cancelled
        {
            get { return _canceled(); }
        }

        private readonly Func<ICollection<InstallerContext>> _installerContexts;
        public ICollection<InstallerContext> InstallerContexts
        {
            get { return _installerContexts(); }
        }

        private readonly Func<string> _installPlanLocation;
        public string InstallPlanLocation
        {
            get { return _installPlanLocation(); }
        }

        private readonly Func<bool> _rebootRequired;
        public bool RebootRequired
        {
            get { return _rebootRequired(); }
        }

        private readonly Func<bool> _localInstall;
        public bool LocalInstall
        {
            get { return _localInstall(); }
        }

        private readonly Func<InstallManager> _installManager;
        private readonly IServiceProvider _serviceProvider;

        public InstallManager InstallManager
        {
            get { return _installManager(); }
        }

        public Dictionary<string, InstalledProduct> Packages { get; private set; }

        public bool UseIisExpress { get; set; }

        public InstallerServiceProxy(IServiceProvider serviceProvider, object installerService)
            : base(installerService)
        {

            _installerService = installerService;
            Packages = new Dictionary<string, InstalledProduct>();

            _serviceProvider = serviceProvider;

            _canceled = GetPropertyGetter<bool>("Cancelled");
            _installerContexts = GetPropertyGetter<ICollection<InstallerContext>>("InstallerContexts");
            _installPlanLocation = GetPropertyGetter<string>("InstallPlanLocation");
            _rebootRequired = GetPropertyGetter<bool>("RebootRequired");
            _localInstall = GetPropertyGetter<bool>("LocalInstall");
            _installManager = GetPropertyGetter<InstallManager>("InstallManager");
            
            AddEventHandler<EventArgs>("ProductInstallChainCompleted", OnProductInstallChainCompleted);
            AddEventHandler<InstallStatusEventArgs>("ProductInstallerStatusUpdated", OnProductInstallerStatusUpdated);
            AddEventHandler<EventArgs>("ProductInstalledUpdated", OnProductInstalledUpdated, true);
        }

        private void OnInstallCompleted(object sender, EventArgs e)
        {
            
        }

        private void OnProductInstalledUpdated(object sender, EventArgs e)
        {
            if (ProductInstalledUpdated != null)
            {
                ProductInstalledUpdated(sender, e);
            }
        }

        private bool _iisDefaultsApplied = false;

        private void OnProductInstallerStatusUpdated(object sender, InstallStatusEventArgs e)
        {
            var installerContext = e.InstallerContext;
            InstalledProduct product;

            var msDeployPackage = e.InstallerContext.Installer.MSDeployPackage;

            if (!Packages.TryGetValue(installerContext.ProductName, out product))
            {
                product = new InstalledProduct
                {
                    Name = installerContext.ProductName,
                    ProductId = installerContext.Id
                };
                Packages.Add(installerContext.ProductName, product);
            }

            if (msDeployPackage != null)
            {
                product.IsWebSite = true;
                product.Parameters = msDeployPackage.SetParameters;
                product.Site = msDeployPackage.Site;
                product.AppPath = msDeployPackage.AppPath;

                if (installerContext.InstallationState == InstallationState.Installing && !_iisDefaultsApplied)
                {
                    ConfigureAppPoolDefaults();
                    _iisDefaultsApplied = true;
                }
            }

            product.Message = installerContext.InstallStateDetails;
            product.Status = installerContext.ReturnCode.Status == InstallReturnCodeStatus.None
                ? installerContext.InstallationState.ToString()
                : installerContext.ReturnCode.Status.ToString();

            switch (e.InstallerContext.ReturnCode.Status)
            {
                case InstallReturnCodeStatus.Success:
                    break;
                case InstallReturnCodeStatus.SuccessRebootRequired:
                    product.RequiresReboot = true;
                    break;
                case InstallReturnCodeStatus.Failure:
                    product.HasError = true;
                    IsError = true;
                    if (string.IsNullOrWhiteSpace(e.InstallerContext.ReturnCode.DetailedInformation))
                    {
                        product.Message = e.InstallerContext.ReturnCode.DetailedInformation;
                        Log.Error(product.ProductId + ": " + product.Message);
                    }
                    else
                    {
                        Log.Error(product.ProductId + ": " + "Error reason could not be determined");
                    }
                    break;
                case InstallReturnCodeStatus.FailureRebootRequired:
                    product.RequiresReboot = true;
                    product.HasError = true;
                    IsError = true;
                    if (string.IsNullOrWhiteSpace(e.InstallerContext.ReturnCode.DetailedInformation))
                    {
                        product.Message = e.InstallerContext.ReturnCode.DetailedInformation;
                        Log.Error(product.ProductId + ": " + product.Message);
                    }
                    else
                    {
                        Log.Error(product.ProductId + ": " + "Error reason could not be determined");
                    }
                    break;
                case InstallReturnCodeStatus.None:
                    break;
            }

            if (ProductInstallerStatusUpdated != null)
            {
                ProductInstallerStatusUpdated(sender, e);
            }
        }

        void OnProductInstallChainCompleted(object sender, EventArgs e)
        {
            InstallationStarted = true;

            _iisDefaultsApplied = false;

            Print();

            if (ProductInstallChainCompleted != null)
            {
                ProductInstallChainCompleted(sender, e);
            }
        }

        public bool InstallationStarted { get; set; }

        public void Print()
        {
            if (IsError)
            {
                var msg = string.Join("\r\n", Packages.Values.Where(p => p.HasError).Select(p => p.Message).Where(s => !string.IsNullOrWhiteSpace(s)));
                Console.WriteLine("ERROR: " + msg);
                Log.Error(msg);
                return;
            }

            if (!InstallationStarted || Cancelled)
            {
                Console.WriteLine("CANCELLED");
                return;
            }

            var type = Type.GetType("Microsoft.Web.PlatformInstaller.UI.IISService, Microsoft.Web.PlatformInstaller.UI");
            var iisService = _serviceProvider.GetService(type);

            // siteName, string appPath
            var f1 = (Func<string, string, string>)type.GetMethod("GetClickableLink", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).CreateDelegate(typeof(Func<string, string, string>), iisService);
            var f2 = (Func<string, object>)(n => type.GetMethod("GetSiteSettings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(iisService, new object[] { n }));

            Func<string, string, string> getUrl = (s1, s2) =>
            {
                try
                {
                    return f1(s1, s2);
                }
                catch
                {
                    return null;
                }
            };

            Func<string, object> getSettings = (s1) =>
            {
                try
                {
                    return f2(s1);
                }
                catch
                {
                    return null;
                }
            };

            var doc = new XDocument(new XElement("packages", from package in Packages.Values
                where package.IsWebSite
                let s = getSettings(package.Site)
                where s != null
                let url = getUrl(package.Site, package.AppPath)
                where url != null
                let settings = new SiteSettings(s)
                select new XElement("package",
                    new XAttribute("name", package.Name),
                    new XAttribute("productId", package.ProductId),
                    new XAttribute("url", url),
                    new XAttribute("siteName", package.Site),
                    new XAttribute("appPath", package.AppPath),
                    new XAttribute("physicalPath", settings.PhysicalPath),
                    from parameter in package.Parameters
                    select new XElement("parameter", new XAttribute("name", parameter.Key), new XCData(parameter.Value)))));

            if (RebootRequired)
            {
                try
                {
                    var svc = InstallerServiceHelper.CreateClient();
                    svc.Reboot(doc.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            Console.WriteLine("PACKAGES: " + doc.Root.ToString(SaveOptions.DisableFormatting));
        }


        private void ConfigureAppPoolDefaults()
        {
            if (UseIisExpress)
            {
                return;
            }

            try
            {
                // TODO: enabled this for IIS6?
                var manager = new ServerManager();
                if (manager.ApplicationPoolDefaults.ManagedRuntimeVersion != "v4.0")
                {
                    manager.ApplicationPoolDefaults.ManagedRuntimeVersion = "v4.0";
                    manager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}