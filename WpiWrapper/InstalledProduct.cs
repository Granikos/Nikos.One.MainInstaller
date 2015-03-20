using System.Collections.Generic;

namespace DeploymentTools
{
class InstalledProduct
{
    //
    public string Status { get; set; }
        
    public string Message { get; set; }

    public bool RequiresReboot { get; set; }

    public string Name { get; set; }

    public Dictionary<string, string> Parameters { get; set; }
    public string ProductId { get; set; }
    public string Site { get; set; }
    public string AppPath { get; set; }
    public bool IsWebSite { get; set; }
    public bool HasError { get; set; }
}
}