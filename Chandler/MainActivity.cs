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
        private ImageButton testButton;
        private GridViewPager pager;
        private DotsPageIndicator dots;
        private int count;

        protected override void OnCreate (Bundle bundle)
        {
            count = 1;
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.Main);

            watchViewStub = FindViewById<WatchViewStub> (Resource.Id.watch_view_stub);

            watchViewStub.LayoutInflated += ViewStubInflated;
        }

        private void ViewStubInflated (object sender, WatchViewStub.LayoutInflatedEventArgs e)
        {
            pager = FindViewById<GridViewPager> (Resource.Id.pager);
            dots = FindViewById<DotsPageIndicator> (Resource.Id.indicator);
            pager.Adapter = new TimeEntriesPagerAdapter (this, FragmentManager);
            dots.SetPager (pager);
            testButton = FindViewById<ImageButton> (Resource.Id.testButton);
            testButton.Click += OnButtonClicked;
        }

        private void OnButtonClicked (object sender, EventArgs e)
        {
            var notification = new NotificationCompat.Builder (this)
            .SetContentTitle ("Button tapped")
            .SetContentText ("Button tapped " + count + " times!")
            .SetSmallIcon (Android.Resource.Drawable.StatNotifyVoicemail)
            .SetGroup ("group_key_demo").Build ();

            var manager = NotificationManagerCompat.From (this);
            manager.Notify (1, notification);
        }

        public void OnDataChanged (DataEventBuffer dataEvents)
        {
        }
    }
}



