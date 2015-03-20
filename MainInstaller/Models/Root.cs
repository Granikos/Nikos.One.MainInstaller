using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Installer.Annotations;
using Microsoft.Web.Administration;
using WPFCustomMessageBox;
using Application = System.Windows.Application;

namespace Installer.Models
{
    class Root : INotifyPropertyChanged
    {
        public static Root Instance { get; private set; }

        private Step _currentStep;

        private readonly Step _demoDbStep;
        private bool _useIisExpress;
        private bool _isError;
        private bool _isEnded;
        private static bool _isCanceled;

        public Dispatcher Dispatcher { get; private set; }

        public DemoDbInstallerTask DemoDbTask { get; private set; }

        public NikosOneInstallerTask NikosOneTask { get; private set; }

        public ConnectionString ConnectionString { get; private set; }

        public Command Back { get; private set; }

        public Command Next { get; private set; }

        public Command Cancel { get; private set; }

        public Command End { get; private set; }

        public ObservableCollection<InstallerTask> Tasks { get; private set; }

        public ObservableCollection<Summary> Summary { get; private set; }

        public ObservableCollection<Step> Steps { get; private set; }

        public IEnumerable<InstallerTask> SelectedTasks
        {
            get
            {
                return Tasks.Where(task => task.IsSelected);
            }
        }

        public bool IsCanceled
        {
            get { return _isCanceled; }
            set
            {
                if (Equals(value, _isCanceled)) return;
                _isCanceled = value;
                OnPropertyChanged("IsCanceled");
                OnPropertyChanged("IsSuccess");
            }
        }

        public bool IsError
        {
            get { return _isError && !_isCanceled; }
            set
            {
                if (Equals(value, _isError)) return;
                _isError = value;
                OnPropertyChanged("IsError");
                OnPropertyChanged("IsSuccess");
            }
        }

        public bool IsEnded
        {
            get { return _isEnded; }
            set
            {
                if (Equals(value, _isEnded)) return;
                _isEnded = value;
                OnPropertyChanged("IsEnded");
            }
        }

        public bool IsSuccess
        {
            get { return !_isError && !_isCanceled; }
        }

        public Step CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (Equals(value, _currentStep)) return;
                _currentStep = value;
                IsEnded = false;

                if (_currentStep != null)
                {
                    _currentStep.OnEnter();
                }

                OnPropertyChanged("CurrentStep");
            }
        }

        public bool UseIisExpress
        {
            get { return _useIisExpress; }
            set
            {
                if (Equals(value, _useIisExpress)) return;
                _useIisExpress = value;
                OnPropertyChanged("UseIisExpress");
            }
        }

        public Dictionary<string, WebSite> InstalledWebSites { get; private set; }

        public string RebootArguments { get; set; }

        public Root()
        {
            InstalledWebSites = new Dictionary<string, WebSite>(StringComparer.OrdinalIgnoreCase);
            UseIisExpress = IsClient();
            Application.Current.SessionEnding += Current_SessionEnding;
            Dispatcher = Dispatcher.CurrentDispatcher;
            Tasks = new ObservableCollection<InstallerTask>();
            Summary = new ObservableCollection<Summary>();

            _demoDbStep = new Step(this) { Name = "DemoDbSettings", Caption = "Database Settings" };

            ConnectionString = new ConnectionString();

            Steps = new ObservableCollection<Step>
            {
                new Step(this) {Name = "Welcome", Caption = "Welcome"},
                new Step(this, PrepareInstall) {Name = "SelectComponents", Caption = "Select Components"},
                new Step(this, Install) {Name = "Installation", Caption = "Installation"},
                new Step(this, Finished, Finishing) {Name = "Finished", Caption = "Finished"}
            };

            CurrentStep = Steps[0];

            Next = new Command(
                () => new Thread(new ThreadStart(delegate
                {
                    if (!CurrentStep.Execute())
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Next.RaiseEvent();
                            Back.RaiseEvent();
                        }));
                        return;
                    }

                    Dispatcher.Invoke(new Action(() =>
                    {
                        CurrentStep = Steps[Steps.IndexOf(CurrentStep) + 1];
                        Next.RaiseEvent();
                        Back.RaiseEvent();
                    }));
                })).Start(),

                () => !CurrentStep.IsExecuting && CurrentStep != Steps[Steps.Count - 1]);

            Back = new Command(() =>
            {
                CurrentStep = Steps[Steps.IndexOf(CurrentStep) - 1];
                Next.RaiseEvent();
                Back.RaiseEvent();
            }, () => !CurrentStep.IsExecuting && CurrentStep != Steps[0]);

            Cancel = new Command(() =>
            {
                if (ConfirmExit())
                {
                    Application.Current.Shutdown();
                }
            }, () => !CurrentStep.IsExecuting);

            End = new Command(() => Application.Current.Shutdown(), () => IsEnded);

            Instance = this;

            if (ModelSerilisation.Deserialize(this))
            {
                // The installer has been resumed. 
                // TODO: what do?
            }
        }

        private void Finishing()
        {
            IsEnded = true;
        }

        private bool Finished()
        {
            NikosOneInstallerService.RunOnceRegistryKey.DeleteValue("NikosOneInstaller", false);

            return true;
        }

        private static void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            e.Cancel = !ConfirmExit();
        }

        private static bool ConfirmExit()
        {
            return MessageBox.Show("Do you want to cancel the nikos one installation?", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private bool PrepareInstall()
        {
            foreach (var task in Tasks)
            {
                task.IsWaiting = true;
            }

            const int insertIndex = 2;
            Dispatcher.BeginInvoke(new Action(() =>
           {
               //if (NikosOneTask.IsSelected)
               //{
               //    if (!Steps.Contains(_nikosOneStep))
               //    {
               //        Steps.Insert(insertIndex, _nikosOneStep);
               //    }
               //}
               //else
               //{
               //    Steps.Remove(_nikosOneStep);
               //}

               if (DemoDbTask.IsSelected)
               {
                   if (!Steps.Contains(_demoDbStep))
                   {
                       Steps.Insert(insertIndex, _demoDbStep);
                   }
               }
               else
               {
                   Steps.Remove(_demoDbStep);
               }
           }));

            return true;
        }

        public void Init()
        {
            var wpiTask = new WpiInstallerTask(this);
            var msDeployTask = new MsDeployInstallerTask(this, wpiTask.Path);

            DemoDbTask = new DemoDbInstallerTask(this, msDeployTask.Path, wpiTask.Path);
            NikosOneTask = new NikosOneInstallerTask(this, wpiTask.Path);
            var dnnTask = new DnnInstallerTask(this, wpiTask.Path);

            Tasks.Add(wpiTask);
            Tasks.Add(msDeployTask);
            Tasks.Add(DemoDbTask);
            Tasks.Add(NikosOneTask);
            Tasks.Add(dnnTask);
        }

        public bool Install()
        {
            try
            {
                foreach (var task in SelectedTasks)
                {
                    ExecuteTask(task);

                    if (IsCanceled)
                    {
                        CurrentStep = Steps.Last();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                CurrentStep.Errors.Add(new Error { Caption = ex.Message });
                return false;
            }

            return Tasks.All(task => !task.IsError);
        }

        private void ExecuteTask(InstallerTask task)
        {
            task.Execute();
            if (task.IsError)
            {
                ShowErrorDialog(task, "Error", "An error occurred while installing " + task.Name + ".");
            }
            else if (task.IsCancelled)
            {
                ShowErrorDialog(task, "Canceled", "The installation of " + task.Name + " was canceled.");
            }
        }

        private void ShowErrorDialog(InstallerTask task, string caption, string message)
        {
            while (true)
            {
                var dialogResult = Application.Current.Dispatcher.Invoke(() => CustomMessageBox.ShowYesNoCancel(message, caption, "Retry", "Continue", "Cancel", MessageBoxImage.Error));
                switch (dialogResult)
                {
                    case MessageBoxResult.Yes:
                        ExecuteTask(task);
                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                        if (MessageBox.Show("Really quit nikos one Installer?", "Quit", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            IsCanceled = true;
                        }
                        else
                        {
                            continue;
                        }
                        break;
                }
                break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void ReadWebSites(string xml, Dictionary<string, WebSite> output = null)
        {
            var doc = XElement.Parse(xml);

            var serverManager = UseIisExpress ? null : new ServerManager();

            foreach (var website in from package in doc.Elements("package")
                                    let parameters = package.Elements("parameter").ToDictionary(e => (string)e.Attribute("name"), e => e.Value)
                                    select new WebSite
                                    {
                                        Name = (string)package.Attribute("name"),
                                        ProductId = (string)package.Attribute("productId"),
                                        SiteName = (string)package.Attribute("siteName"),
                                        AppPath = (string)package.Attribute("appPath"),
                                        Url = (string)package.Attribute("url"),
                                        PhysicalPath = (string)package.Attribute("physicalPath")
                                    })
            {
                if (!UseIisExpress)
                {
                    UpdateWebSite(serverManager, website);
                }

                website.PhysicalPath = Environment.ExpandEnvironmentVariables(website.PhysicalPath);

                Log.Info(string.Format(@"Installed website: 
    SiteName: {0}
    AppPath: {1}
    Name: {2}
    PhysicalPath: {3}
    ProductId: {4}
    Url: {5}", website.SiteName, website.AppPath, website.Name, website.PhysicalPath, website.ProductId, website.Url));

                if (InstalledWebSites.ContainsKey(website.ProductId))
                {
                    InstalledWebSites[website.ProductId] = website;
                }
                else
                {
                    InstalledWebSites.Add(website.ProductId, website);
                }

                if (output == null)
                {
                    continue;
                }

                if (output.ContainsKey(website.ProductId))
                {
                    output[website.ProductId] = website;
                }
                else
                {
                    output.Add(website.ProductId, website);
                }
            }
        }

        private static void UpdateWebSite(ServerManager serverManager, WebSite website)
        {
            Log.Info("Rewriting physical path for IIS7: " + website.PhysicalPath + ".");

            try
            {
                var site = serverManager.Sites[website.SiteName];
                if (site == null)
                {
                    Log.Error("Could not find web site " + website.SiteName + ". Available sites: " + string.Join(", ", serverManager.Sites.Select(s => s.Name)));
                    return;
                }

                var appPath = website.AppPath.StartsWith("/") ? website.AppPath : "/" + website.AppPath;
                var app = site.Applications[appPath];
                if (app == null)
                {
                    Log.Error("Could not find web application " + appPath + ". Available applications: " + string.Join(", ", site.Applications.Select(s => s.Path)));
                    return;
                }

                var vdir = app.VirtualDirectories.FirstOrDefault();
                if (vdir == null)
                {
                    Log.Error("Could not find a virtual directory.");
                    return;
                }

                website.PhysicalPath = vdir.PhysicalPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        internal static bool IsClient()
        {
            return NativeMethods.IsWindowsProductTypeEqual(1);
        }

        internal string GetWebMatrixExe()
        {
            var result = Environment.ExpandEnvironmentVariables("%SystemDrive%\\Program Files (x86)\\Microsoft WebMatrix\\WebMatrix.exe");
            if (!File.Exists(result))
            {
                result = Environment.ExpandEnvironmentVariables("%SystemDrive%\\Program Files\\Microsoft WebMatrix\\WebMatrix.exe");
            }

            return result;
        }
    }
}
