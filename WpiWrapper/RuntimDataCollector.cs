using System;
using System.Collections.Generic;
using System.Reflection;
using DeploymentTools;
using Microsoft.Web.PlatformInstaller;

class RuntimDataCollector
{
    public Dictionary<string, InstalledProduct> Products { get; private set; }

    public RuntimDataCollector(IServiceProvider serviceProvider)
    {
        var type = Type.GetType("Microsoft.Web.PlatformInstaller.UI.InstallerService, Microsoft.Web.PlatformInstaller.UI");
        var installerService = serviceProvider.GetService(type);
        var property = installerService.GetType().GetProperty("InstallManager", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        var installManager = property.GetValue(installerService, null) as InstallManager;
        
        installManager.InstallCompleted += OnInstallCompleted;
        installManager.InstallerStatusUpdated += OnInstallerStatusUpdated;

        Products = new Dictionary<string, InstalledProduct>(StringComparer.OrdinalIgnoreCase);
    }

    private void OnInstallCompleted(object sender, EventArgs e)
    {
        foreach (var product in Products)
        {
            // TODO: get web application settings from web server
        }
    }

    private void OnInstallerStatusUpdated(object sender, Microsoft.Web.PlatformInstaller.InstallStatusEventArgs e)
    {
        var installerContext = e.InstallerContext;
        var msDeployPackage = e.InstallerContext.Installer.MSDeployPackage;
        InstalledProduct product;

        if (!Products.TryGetValue(installerContext.ProductName, out product))
        {
            product = new InstalledProduct()
            {
                Name = installerContext.ProductName,
                ProductId = installerContext.Id
            };

            Products.Add(installerContext.ProductName, product);
        }

        if (msDeployPackage != null)
        {
            product.IsWebSite = true;
            product.Parameters = msDeployPackage.SetParameters;
            product.Site = msDeployPackage.Site;
            product.AppPath = msDeployPackage.AppPath;
        }

        switch (e.InstallerContext.ReturnCode.Status)
        {
            case InstallReturnCodeStatus.Success:
                break;
            case InstallReturnCodeStatus.SuccessRebootRequired:
                product.RequiresReboot = true;
                product.HasError = true;
                break;
            case InstallReturnCodeStatus.Failure:
                product.HasError = true;
                product.Message = e.InstallerContext.ReturnCode.DetailedInformation;
                break;
            case InstallReturnCodeStatus.FailureRebootRequired:
                product.RequiresReboot = true;
                product.HasError = true;
                product.Message = e.InstallerContext.ReturnCode.DetailedInformation;
                break;
            case InstallReturnCodeStatus.None:
                break;
        }
    }
}
