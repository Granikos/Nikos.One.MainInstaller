using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Windows;

namespace Installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceHost _serviceHost;

        static App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Error(e.ExceptionObject as Exception);
        }

        public App()
        {
            try
            {
                _serviceHost = new ServiceHost(typeof(NikosOneInstallerService), new Uri(InstallerServiceHelper.BaseAddress));
                _serviceHost.AddServiceEndpoint(typeof(INikosOneInstallerService), new NetNamedPipeBinding(), InstallerServiceHelper.ServiceAddress);
                _serviceHost.Open();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceHost != null)
            {
                try
                {
                    _serviceHost.Close();
                }
                catch
                {
                }

                try
                {
                    ((IDisposable)_serviceHost).Dispose();
                }
                catch
                {
                }
            }

            base.OnExit(e);
        }
    }
}
