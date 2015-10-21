using Android.Content;
using Android.Gms.Wearable;
using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Common.Data;
using Android.Gms.Common;
using Java.Util.Concurrent;
using Android.Util;

namespace Toggl.Joey.Wear
{
    /// <summary>
    /// Listens to DataItems and Messages from the local node
    /// </summary>
    [Service(), IntentFilter (new [] { "com.google.android.gms.wearable.BIND_LISTENER" }) ]
    public class WearService : WearableListenerService
    {
        public const string Tag = "WearableTag";
        public const string DataStorePath = "/TimeEntryDataStore";
        IGoogleApiClient googleApiClient;

        public override void OnCreate ()
        {
            base.OnCreate ();
            googleApiClient = new GoogleApiClientBuilder (this)
            .AddApi (WearableClass.Api)
            .Build ();
            googleApiClient.Connect ();
        }

        public override void OnDataChanged (DataEventBuffer dataEvents)
        {

            if (!googleApiClient.IsConnected) {
                ConnectionResult connectionResult = googleApiClient.BlockingConnect (30, TimeUnit.Seconds);
                if (!connectionResult.IsSuccess) {
                    Log.Error (Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }
        }

        public override void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);

//            // Check to see if the message is to start an activity
//            if (messageEvent.Path.Equals (DataStorePath)) {
//                Intent startIntent = new Intent (this, typeof (MainActivity));
//                startIntent.AddFlags (ActivityFlags.NewTask);
//                StartActivity (startIntent);
//            }
        }

        public override void OnPeerConnected (INode peer)
        {
            LOGD (Tag, "OnPeerConnected: " + peer);
        }

        public override void OnPeerDisconnected (INode peer)
        {
            LOGD (Tag, "OnPeerDisconnected: " + peer);
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }

    }
}

