using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Installer.Annotations;

namespace Installer.Models
{
    internal class ConnectionString : INotifyPropertyChanged
    {
        private bool _isIntegratedAuthentication;
        private string _password;
        private string _userName;
        private string _database;
        private string _server;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Server
        {
            get { return _server; }
            set
            {
                if (value == _server) return;
                _server = value;
                OnPropertyChanged("Server");
            }
        }

        public string Database
        {
            get { return _database; }
            set
            {
                if (value == _database) return;
                _database = value;
                OnPropertyChanged("Database");
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                if (value == _userName) return;
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (value == _password) return;
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        public bool IsIntegratedAuthentication
        {
            get { return _isIntegratedAuthentication; }
            set
            {
                if (value.Equals(_isIntegratedAuthentication)) return;
                _isIntegratedAuthentication = value;
                OnPropertyChanged("IsIntegratedAuthentication");
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Data Source=");
            sb.Append(Server);
            sb.Append(";");

            sb.Append("Initial Catalog=");
            sb.Append(Database);
            sb.Append(";");

            if (IsIntegratedAuthentication)
            {
                sb.Append("Integrated Security=SSPI;");
            }
            else
            {
                sb.Append("User ID=");
                sb.Append(UserName);
                sb.Append(";");

                sb.Append("Password=");
                sb.Append(Password);
                sb.Append(";");
            }

            sb.Append("MultipleActiveResultSets=true;");

            return sb.ToString();
        }
    }
}