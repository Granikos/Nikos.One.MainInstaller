using System.ServiceModel;

namespace Installer
{
    [ServiceContract]
    public interface INikosOneInstallerService
    {
        [OperationContract]
        void Reboot(string xml);
    }
}
