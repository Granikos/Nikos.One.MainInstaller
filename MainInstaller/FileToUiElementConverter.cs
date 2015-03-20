using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Installer
{
    public class FileToUiElementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var p = parameter == null ? string.Empty : parameter.ToString();
            var path = value is string ? Path.Combine(p, (string)value) : p;
            path += ".xaml";
            path = typeof(FileToUiElementConverter).Namespace + "." + path.Replace("/", ".").Replace("\\", ".");
            var ass = Assembly.GetExecutingAssembly();

            using (var stream = ass.GetManifestResourceStream(path))
            {
                var logo = XamlReader.Load(stream);

                if (logo != null)
                {
                    return logo;
                }
            }

            throw new Exception("Resource not found");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}