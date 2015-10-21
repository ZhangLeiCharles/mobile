using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.Wearable.Views;
using Android.Util;
using Android.Widget;
using Java.Interop;
using Java.Util.Concurrent;

namespace Toggl.Chandler
{
    [Activity (Label = "Toggl", MainLauncher = true, Icon = "@drawable/Icon" )]
    public class MainActivity : Activity, IGoogleApiClientConnectionCallbacks,IGoogleApiClientOnConnectionFailedListener, IDataApiDataListener,
        IMessageApiMessageListener, INodeApiNodeListener
    {
        public const string Tag = "MainActivity";

        private WatchViewStub watchViewStub;
        private ImageButton testButton;
        private GridViewPager pager;
        private DotsPageIndicator dots;
        private int count;

        private IGoogleApiClient googleApiClient;

        protected override void OnCreate (Bundle savedInstanceState)
        {
            count = 1;
            base.OnCreate (savedInstanceState);

            SetContentView (Resource.Layout.Main);
            watchViewStub = FindViewById<WatchViewStub> (Resource.Id.watch_view_stub);
            watchViewStub.LayoutInflated += ViewStubInflated;

            googleApiClient = new GoogleApiClientBuilder (this)
            .AddApi (WearableClass.API)
            .AddConnectionCallbacks (this)
            .AddOnConnectionFailedListener (this)
            .Build ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            googleApiClient.Connect ();
        }

        protected override void OnPause ()
        {
            base.OnPause ();
            WearableClass.DataApi.RemoveListener (googleApiClient, this);
            WearableClass.MessageApi.RemoveListener (googleApiClient, this);
            WearableClass.NodeApi.RemoveListener (googleApiClient, this);
            googleApiClient.Disconnect ();
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

        private void SendStartStopMessage ()
        {
            var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
            var nodes = apiResult.Nodes;
            var phoneNode = nodes.FirstOrDefault ();

            WearableClass.MessageApi.SendMessage (googleApiClient, phoneNode.Id,
                                                  Common.StartTimeEntryPath,
                                                  new byte[0]);
        }

        #region Interface implementation

        public override void OnDataChanged (DataEventBuffer dataEvents)
        {
            if (!googleApiClient.IsConnected) {
                ConnectionResult connectionResult = googleApiClient.BlockingConnect (30, TimeUnit.Seconds);
                if (!connectionResult.IsSuccess) {
                    Log.Error (Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }

            var dataEvent = Enumerable.Range (0, dataEvents.Count)
                            .Select (i => dataEvents.Get (i).JavaCast<IDataEvent> ())
                            .FirstOrDefault (de => de.Type == DataEvent.TypeChanged && de.DataItem.Uri.Path == Common.TimeEntryListPath);

            if (dataEvent == null) {
                return;
            }

            SetTimeEntryListFromData (dataEvent.DataItem);
        }

        public void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);
        }

        public void OnConnected (Bundle bundle)
        {
            LOGD (Tag, "OnConnected(): Successfully connected to Google API client");
            WearableClass.DataApi.AddListener (googleApiClient, this);
            WearableClass.MessageApi.AddListener (googleApiClient, this);
            WearableClass.NodeApi.AddListener (googleApiClient, this);
        }

        public void OnConnectionSuspended (int p0)
        {
            LOGD (Tag, "OnConnectionSuspended(): Connection to Google API clinet was suspended");
        }

        public void OnConnectionFailed (Android.Gms.Common.ConnectionResult result)
        {
            LOGD (Tag, "OnConnectionFailed(): Failed to connect, with result: " + result);
        }

        public void OnPeerConnected (INode peer)
        {
            LOGD (Tag, "OnPeerConnected: " + peer);
        }

        public void OnPeerDisconnected (INode peer)
        {
            LOGD (Tag, "OnPeerDisconnected: " + peer);
        }

        #endregion

        private void SetTimeEntryListFromData (IDataItem dataItem)
        {
            var dataMapItem = DataMapItem.FromDataItem (dataItem);
            var map = dataMapItem.DataMap;

            var serializer = new System.Xml.Serialization.XmlSerializer (SimpleTimeEntryData);
            var itemList = new List<SimpleTimeEntryData> (map.Size ());
            var dataArray = map.GetDataMapArrayList (Common.TimeEntryListKey);

            foreach (var data in dataArray) {
                var item = (SimpleTimeEntryData)serializer.Deserialize (new System.IO.MemoryStream (data));
                itemList.Add (item);
            }
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }
    }
}



