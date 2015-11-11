using System;


namespace Toggl.Phoebe
{
    public interface IPlatformUtils
    {
        /// <summary>
        /// Gets the app identifier. The app identifier and app version are used for model CreatedWith fields, and also
        /// for HTTP User-Agent field.
        /// </summary>
        /// <value>The app identifier.</value>
        string AppIdentifier { get; }

        /// <summary>
        /// Gets the app version. The app identifier and app version are used for model CreatedWith fields, and also
        /// for HTTP User-Agent field.
        /// </summary>
        /// <value>The app version.</value>
        string AppVersion { get; }

        /// <summary>
        /// Detect if widgets are availables or not in current device system.
        /// </summary>
        /// <value>Detect if widget is available or not</value>
        bool IsWidgetAvailable { get; }

        /// <summary>
        /// Run an action using the UI thread.
        /// </summary>
        /// <value>Detect if widget is available or not</value>
        void DispatchOnUIThread  (Action action);

    }
}
