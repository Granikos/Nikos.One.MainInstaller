using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Installer.Annotations;

namespace Installer.Models
{
    abstract class InstallerTask : INotifyPropertyChanged
    {
        private string _text;
        private double _progress;
        private bool _isError;
        private bool _isWaiting;
        private bool _isRunning;
        private bool _isSuccess;
        private bool _isSelected;
        private bool _isEnabled;
        private bool _isCancelled;

        public Command Navigate { get; private set; }

        protected Root Root { get; private set; }

        public string Name { get; private set; }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (value.Equals(_isEnabled)) return;
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public bool IsCancelled
        {
            get { return _isCancelled; }
            set
            {
                if (value.Equals(_isCancelled)) return;
                _isCancelled = value;
                if (value)
                {
                    IsWaiting = false;
                    IsSuccess = false;
                    IsRunning = false;
                    IsError = false;
                }
                OnPropertyChanged("IsCancelled");
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value.Equals(_isSelected)) return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
                Root.OnPropertyChanged("SelectedTasks");
                OnSelectionChanged();
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public double Progress
        {
            get { return _progress; }
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        public bool IsError
        {
            get { return _isError; }
            set
            {
                if (value.Equals(_isError)) return;
                _isError = value;
                if (value)
                {
                    Root.IsError = true;
                    IsWaiting = false;
                    IsSuccess = false;
                    IsRunning = false;
                    IsCancelled = false;
                }
                OnPropertyChanged("IsError");
            }
        }

        public bool IsSuccess
        {
            get { return _isSuccess; }
            set
            {
                if (value.Equals(_isSuccess)) return;
                _isSuccess = value;
                if (value)
                {
                    IsWaiting = false;
                    IsError = false;
                    IsRunning = false;
                    IsCancelled = false;
                }
                OnPropertyChanged("IsSuccess");
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (value.Equals(_isRunning)) return;
                _isRunning = value;
                if (value)
                {
                    IsWaiting = false;
                    IsSuccess = false;
                    IsError = false;
                    IsCancelled = false;
                }
                OnPropertyChanged("IsRunning");
            }
        }

        public bool HasUrlEula { get; private set; }

        public string UrlEula { get; private set; }

        public bool HasUrlPrivacy { get; private set; }

        public string UrlPrivacy { get; private set; }

        public bool IsWaiting
        {
            get { return _isWaiting; }
            set
            {
                if (value.Equals(_isWaiting)) return;
                _isWaiting = value;
                if (value)
                {
                    IsError = false;
                    IsSuccess = false;
                    IsRunning = false;
                }
                OnPropertyChanged("IsWaiting");
            }
        }

        protected InstallerTask(Root root, string name, string urlEula, string urlPrivacy)
        {
            UrlEula = urlEula;
            HasUrlEula = !string.IsNullOrEmpty(UrlEula);
            UrlPrivacy = urlPrivacy;
            HasUrlPrivacy = !string.IsNullOrEmpty(UrlPrivacy);

            if (HasUrlEula || HasUrlPrivacy)
            {
                Navigate = new Command(p =>
                {
                    var url = p as string;
                    if (url != null)
                    {
                        Process.Start(new ProcessStartInfo(url));
                    }
                }, () => true);
            }
            Root = root;
            Name = name;
            IsWaiting = true;
            IsEnabled = true;
        }

        public void Execute()
        {
            if (!IsSelected)
            {
                return;
            }

            try
            {
                IsRunning = true;

                OnExecute();

                if (!IsCancelled && !IsError)
                {
                    IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                IsError = true;
                Text = ex.Message;
            }
        }

        protected abstract void OnExecute();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        protected void Call(string fileName, string arguments, string waitFor = null, bool hideWindow = false)
        {
            Log.Info("Calling'" + fileName + " " + arguments + "'.");

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas",
                }
            };

            if (p.Start())
            {
                p.ErrorDataReceived += (s, e) => OnProcessErrorReceived(e.Data);
                p.OutputDataReceived += (s, e) => OnProcessOutputReceived(e.Data);
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (hideWindow)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                        {
                            var mainWindow = Application.Current.MainWindow;
                            mainWindow.Hide();
                        });
                }
            }

            p.WaitForExit();

            if (waitFor != null)
            {
                Process[] processes;

                do
                {
                    processes = Process.GetProcessesByName(waitFor);
                    if (processes.Length > 0)
                    {
                        Thread.CurrentThread.Join(100);
                    }
                } while (processes.Length > 0);
            }

            if (hideWindow)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    try
                    {
                        var mainWindow = Application.Current.MainWindow;
                        mainWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    //var mainWindowHandle = new WindowInteropHelper(mainWindow).Handle;
                    //SetParent(p.MainWindowHandle, mainWindowHandle);
                });
            }

            //var result = p.StandardOutput.ReadToEnd();
            //Log.Info(result);
            //return result;
        }

        protected virtual void OnProcessOutputReceived(string output)
        {
        }

        protected virtual void OnProcessErrorReceived(string output)
        {
        }

        protected virtual void OnSelectionChanged()
        {
        }

        protected bool KillAll(string processName)
        {
            var processes = Process.GetProcessesByName("WebPlatformInstaller");
            if (processes.Length == 0)
            {
                return true;
            }

            //if (MessageBox.Show("Some instances of Web Platform Installer are still running and must be closed. Should these instances be closed now?", "Close Web Platform Installer", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            //{
            //    return false;
            //}

            foreach (var process in processes)
            {
                process.Kill();
            }

            return true;
        }
    }
}