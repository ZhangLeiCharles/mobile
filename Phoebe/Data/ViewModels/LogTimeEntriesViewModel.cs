using System;
using System.ComponentModel;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesViewModel : ViewModelBase, IVModel<TimeEntryModel>
    {
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private ActiveTimeEntryManager timeEntryManager;
        private TimeEntriesCollectionView collectionView;
        private TimeEntryModel model;

        public LogTimeEntriesViewModel ()
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";
            timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            timeEntryManager.PropertyChanged += OnActiveTimeEntryManagerPropertyChanged;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
        }

        public async Task Init ()
        {
            IsLoading = true;

            SyncModel ();
            await SyncCollectionView ();

            IsLoading = false;
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }

            collectionView.Dispose ();
            timeEntryManager.PropertyChanged -= OnActiveTimeEntryManagerPropertyChanged;
            timeEntryManager = null;

            model = null;
        }

        public bool IsLoading  { get; set; }

        public bool IsProcessingAction { get; set; }

        public bool IsTimeEntryRunning { get; set; }

        public bool IsGroupedMode { get; set; }

        public bool HasMore { get; set; }

        public TimeEntriesCollectionView CollectionView { get; set; }

        public async Task StartStopTimeEntry ()
        {
            // Protect from double clicks
            if (IsProcessingAction) {
                return;
            }

            if (model.State == TimeEntryState.Running) {
                await model.StopAsync ();
            } else {
                await model.StartAsync ();
            }

            IsProcessingAction = false;
        }

        private void OnActiveTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                SyncModel ();
            }
        }

        private async void OnSettingChanged (SettingChangedMessage msg)
        {
            // Implement a GetPropertyName
            if (msg.Name == "GroupedTimeEntries") {
                await SyncCollectionView ();
            }
        }

        private void SyncModel ()
        {
            var data = timeEntryManager.Active;
            if (data != null) {
                if (model == null) {
                    model = new TimeEntryModel (data);
                } else {
                    model.Data = data;
                }
                // Set if an entry is running.
                IsTimeEntryRunning = data.State == TimeEntryState.Running;
            }
        }

        private async Task SyncCollectionView ()
        {
            IsGroupedMode = ServiceContainer.Resolve<ISettingsStore> ().GroupedTimeEntries;

            if (collectionView != null) {
                collectionView.Dispose ();
                collectionView.CollectionChanged -= OnCollectionChanged;
                collectionView.HasMoreChanged -= OnCollectionChanged;
            }

            // In a near future, CollectionView will be only
            // one object and not divided in two separated classes
            // like that: LogTimeEntriesView and GroupedTimeEntriesView

            collectionView = IsGroupedMode ? (TimeEntriesCollectionView)new GroupedTimeEntriesView () : new LogTimeEntriesView ();
            collectionView.CollectionChanged += OnCollectionChanged;
            collectionView.HasMoreChanged += OnCollectionChanged;
            await collectionView.ReloadAsync ();
            CollectionView = collectionView;
        }

        private void OnCollectionChanged (object sender, EventArgs e)
        {
            HasMore = collectionView.Count > 0 || collectionView.HasMore;
        }
    }
}