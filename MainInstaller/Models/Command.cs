using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace Installer.Models
{
    public delegate T Func<T>();

    public delegate void Action();

    class Command : ICommand, INotifyPropertyChanged
    {
        private readonly Func<bool> _canExecute;

        private readonly Action _execute;

        private readonly Action<object> _execute2;

        public bool IsVisible { get { return _canExecute(); } }

        public Command(Action execute, Func<bool> canExecute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public Command(Action<object> execute, Func<bool> canExecute)
        {
            _canExecute = canExecute;
            _execute2 = execute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            if (_execute2 != null)
            {
                _execute2(parameter);
            }
            else
            {
                _execute();
            }
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseEvent()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("IsVisible"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}