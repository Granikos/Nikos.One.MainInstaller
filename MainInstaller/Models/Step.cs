using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Installer.Annotations;

namespace Installer.Models
{
    internal class Step : INotifyPropertyChanged
    {
        public bool IsExecuting
        {
            get { return _isExecuting; }
            private set
            {
                if (value.Equals(_isExecuting)) return;
                _isExecuting = value;
                OnPropertyChanged("IsExecuting");
            }
        }

        private readonly Func<bool> _execute;
        private bool _isExecuting;
        private readonly Root _root;
        private Action _onEnter;

        public string Caption { get; set; }

        public string Name { get; set; }

        public ObservableCollection<Error> Errors { get; set; }

        public Step(Root root, Func<bool> execute = null, Action onEnter = null)
        {
            _onEnter = onEnter;
            _root = root;
            _execute = execute;
        }

        public void OnEnter()
        {
            if (_onEnter != null)
            {
                _onEnter();
            }
        }

        public bool Execute()
        {
            if (_execute == null)
            {
                IsExecuting = false;
                return true;
            }

            IsExecuting = true;


            try
            {
                _root.Dispatcher.Invoke(new Action(() =>
                {
                    _root.Next.RaiseEvent();
                    _root.Back.RaiseEvent();
                }));

                return _execute();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}