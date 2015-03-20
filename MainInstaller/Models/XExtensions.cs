using System;
using System.Xml.Linq;

namespace Installer.Models
{
    static class XExtensions
    {
        public static void ReadAttribute(this XElement element, string attribute, Action<XAttribute> action)
        {
            var a = element.Attribute(attribute);
            if (a == null)
            {
                return;
            }

            action(a);
        }
    }
}