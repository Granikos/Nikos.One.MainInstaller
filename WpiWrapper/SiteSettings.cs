using System;

namespace DeploymentTools
{
    class SiteSettings : ReflectionWrapper
    {
        private readonly Func<string> _bindingInformation;

        public string BindingInformation
        {
            get { return _bindingInformation(); }
        }

        private readonly Func<string> _physicalPath;

        public string PhysicalPath
        {
            get { return _physicalPath(); }
        }

        public SiteSettings(object innerObject)
            : base(innerObject)
        {
            _bindingInformation = GetPropertyGetter<string>("BindingInformation");
            _physicalPath = GetPropertyGetter<string>("PhysicalPath");
        }
    }
}