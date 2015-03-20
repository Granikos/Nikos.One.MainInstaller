using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Installer.Models;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var root = new Root();
            DataContext = root;
            root.Init();
            //root.PropertyChanged += root_PropertyChanged;

            InitializeComponent();
        }

        //void root_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "CurrentStep")
        //    {
        //        tab.GetBindingExpression(Selector.SelectedValueProperty).UpdateSource();
        //    }
        //}
    }
}
