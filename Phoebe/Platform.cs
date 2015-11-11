using System;
using XPlatUtils;

namespace Toggl.Phoebe
{
    internal static class Platform
    {
        private static readonly Lazy<IPlatformUtils> platform =
            new Lazy<IPlatformUtils> (() => ServiceContainer.Resolve<IPlatformUtils> ());

        public static string AppIdentifier
        {
            get { return platform.Value.AppIdentifier; }
        }

        public static string AppVersion
        {
            get { return platform.Value.AppVersion; }
        }

        public static string DefaultCreatedWith
        {
            get { return String.Format ("{0}/{1}", AppIdentifier, AppVersion); }
        }
    }
}
