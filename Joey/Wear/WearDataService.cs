using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Util;
using Java.Util.Concurrent;

namespace Toggl.Joey.Wear
{
    /// <summary>
    /// Listens to DataItems and Messages from the local node
    /// </summary>
    [Service, IntentFilter (new [] { "com.google.android.gms.wearable.BIND_LISTENER" }) ]
    public class WearDataService : WearableListenerService
    {
        public const string Tag = "WearableTag";
        public const string DataStorePath = "/TimeEntryDataStore";
        IGoogleApiClient googleApiClient;

        public override void OnCreate ()
        {
            base.OnCreate ();
            googleApiClient = new GoogleApiClientBuilder (this)
            .AddApi (WearableClass.API)
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

        public async override void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);

            base.OnMessageReceived (messageEvent);
            if (messageEvent.Path != Common.StartTimeEntryPath &&
                    messageEvent.Path != Common.StopTimeEntryPath &&
                    messageEvent.Path != Common.RestartTimeEntryPath) {
                return;
            }

            await HandleMessage (messageEvent);
        }

        public override void OnPeerConnected (INode peer)
        {
            LOGD (Tag, "OnPeerConnected: " + peer);
        }

        public override void OnPeerDisconnected (INode peer)
        {
            LOGD (Tag, "OnPeerDisconnected: " + peer);
        }

        private async Task HandleMessage (IMessageEvent message)
        {
            try {
                Log.Info ("WearIntegration", "Received Message");
                var client = new GoogleApiClientBuilder (this)
                .AddApi (WearableClass.API)
                .Build ();

                var result = client.BlockingConnect (30, TimeUnit.Seconds);
                if (!result.IsSuccess) {
                    return;
                }
                var path = message.Path;

                try {
                    if (path == Common.StartTimeEntryPath) {
                        // Start new time entry.

                        // Refresh shared data.
                        await UpdateSharedTimeEntryList (client);

                    } else if (path == Common.StopTimeEntryPath) {


                    } else if (path == Common.RestartTimeEntryPath) {
                        // Get time entry Id needed.
                    }
                } finally {
                    await UpdateSharedTimeEntryList (client);
                    client.Disconnect ();

                }
            } catch (Exception e) {
                Log.Error ("WearIntegration", e.ToString ());
            }
        }

        private async Task SendMessageToWearable (string content, string path)
        {
        }

        private async Task UpdateSharedTimeEntryList (IGoogleApiClient client)
        {
            // Get last 4 grouped TEs

            // Create SimpleTimeEntryData
            var timeEntryData = new List<SimpleTimeEntryData> ();

            // Publis changes to weareable using DataItems
            var mapReq = PutDataMapRequest.Create (Common.TimeEntryListPath);
            var map = mapReq.DataMap;

            var children = new List<DataMap> (5);
            foreach (var entry in timeEntryData ) {
                var obj = new DataMap ();
                children.Add (obj);
            }
            map.PutDataMapArrayList (Common.TimeEntryListKey, children.ToList ());
            WearableClass.DataApi.PutDataItem (client, mapReq.AsPutDataRequest ());
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }
    }
}

