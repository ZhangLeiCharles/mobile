using System;
using Android.App;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.Wearable.Views;
using Android.Widget;

namespace Toggl.Chandler
{
    [Activity (Label = "Toggl", MainLauncher = true, Icon = "@drawable/Icon" )]
    public class MainActivity : Activity, IDataApiDataListener
    {
        private WatchViewStub watchViewStub;
        private Button testButton;
        private int count;

        protected override void OnCreate (Bundle bundle)
        {
            count = 1;
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.Main);

            watchViewStub = FindViewById<WatchViewStub> (Resource.Id.watch_view_stub);
            testButton = FindViewById<Button> (Resource.Id.myButton);

            watchViewStub.LayoutInflated += ViewStubInflated;
        }

        private void ViewStubInflated (object sender, WatchViewStub.LayoutInflatedEventArgs e)
        {
            testButton.Click += OnButtonClicked;
        }

        private void OnButtonClicked (object sender, System.EventArgs e)
        {
            var notification = new NotificationCompat.Builder (this)
            .SetContentTitle ("Button tapped")
            .SetContentText ("Button tapped " + count + " times!")
            .SetSmallIcon (Android.Resource.Drawable.StatNotifyVoicemail)
            .SetGroup ("group_key_demo").Build ();

            var manager = NotificationManagerCompat.From (this);
            manager.Notify (1, notification);
            testButton.Text = "Check Notification!";
        }

        public void OnDataChanged (DataEventBuffer dataEvents)
        {
        }
    }
}



