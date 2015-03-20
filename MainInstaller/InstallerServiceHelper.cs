using System;
using System.ServiceModel;

namespace Installer
{
    public class InstallerServiceHelper
    {
        private readonly static ChannelFactory<INikosOneInstallerService> _serviceFactory;

        public static string ServiceAddress { get; private set; }

        public static string BaseAddress { get { return "net.pipe://localhost"; } }

        static InstallerServiceHelper()
        {
            ServiceAddress = string.Format("{0}_{1}_NikosOne_Installer", Environment.UserDomainName, Environment.UserName);


            _serviceFactory = new ChannelFactory<INikosOneInstallerService>(
                new NetNamedPipeBinding(),
                new EndpointAddress(string.Format("{0}/{1}", BaseAddress, ServiceAddress)));
        }

        public static INikosOneInstallerService CreateClient()
        {
            return _serviceFactory.CreateChannel();
        }
    }
}