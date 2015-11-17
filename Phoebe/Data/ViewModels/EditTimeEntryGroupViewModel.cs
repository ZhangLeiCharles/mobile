using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryGroupViewModel : ViewModelBase, IVModel<TimeEntryModel>
    {
        private TimeEntryModel model;
        private Timer durationTimer;
        private List<TimeEntryData> timeEntryList;
        private List<string> timeEntryIds;

        public EditTimeEntryGroupViewModel (List<TimeEntryData> timeEntryList)
        {
            this.timeEntryList = timeEntryList;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Grouped Time Entry";
        }

        public EditTimeEntryGroupViewModel (List<string> timeEntryIds)
        {
            this.timeEntryIds = timeEntryIds;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Grouped Time Entry";
        }

        public async Task Init ()
        {
            IsLoading  = true;

            durationTimer = new Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            if (timeEntryList == null) {
                timeEntryList = await GetTimeEntryDataList (timeEntryIds);
            }

            model = new TimeEntryModel (timeEntryList.Last ());
            model.PropertyChanged += OnPropertyChange;
            await model.LoadAsync ();

            UpdateView ();

            IsLoading = false;
        }

        public void Dispose ()
        {
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();

            model.PropertyChanged -= OnPropertyChange;
            model = null;
        }

        #region viewModel State properties

        public bool IsLoading { get; set; }

        public bool IsRunning { get; set; }

        public string Duration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StopDate { get; set; }

        public string ProjectName { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public Guid WorkspaceId { get; set; }

        public int ProjectColor { get; set; }

        public ObservableRangeCollection<TimeEntryData> TimeEntryCollection  { get; set; }

        #endregion

        public async Task SaveModel ()
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var saveTasks = new List<Task> ();

            foreach (var item in timeEntryList) {
                item.Description = Description;
                Model<TimeEntryData>.MarkDirty (item);
                saveTasks.Add (dataStore.PutAsync (item));
            }

            await Task.WhenAll (saveTasks);
        }

        private void OnPropertyChange (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Data" ||
                    e.PropertyName == "StartTime" ||
                    e.PropertyName == "Duration" ||
                    e.PropertyName == "StopTime") {
                UpdateView ();
            }
        }

        private void UpdateView ()
        {
            StartDate = timeEntryList.FirstOrDefault ().StartTime.ToLocalTime ();
            StopDate = model.StopTime.HasValue ? model.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
            // TODO: check substring function for long times
            var listDuration = GetTimeEntryListDuration (timeEntryList);
            Duration = TimeSpan.FromSeconds (listDuration.TotalSeconds).ToString ().Substring (0, 8);
            Description = model.Description;
            ProjectName = model.Project != null ? model.Project.Name : string.Empty;
            ProjectColor = model.Project != null ? model.Project.Color : 0;
            WorkspaceId = model.Workspace.Id;

            if (model.Project != null) {
                if (model.Project.Client != null) {
                    ClientName = model.Project.Client.Name;
                }
            }

            if (model.State == TimeEntryState.Running && !IsRunning) {
                IsRunning = true;
                durationTimer.Start ();
            } else if (model.State != TimeEntryState.Running) {
                IsRunning = false;
                durationTimer.Stop ();
            }

            if (TimeEntryCollection == null) {
                TimeEntryCollection = new ObservableRangeCollection<TimeEntryData> ();
            } else {
                TimeEntryCollection.Clear ();
            }
            TimeEntryCollection.AddRange (timeEntryList);
        }

        private void DurationTimerCallback (object sender, ElapsedEventArgs e)
        {
            var duration = model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }

        #region Time Entry list utils

        private async Task<List<TimeEntryData>> GetTimeEntryDataList (List<string> ids)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var list = new List<TimeEntryData> (ids.Count);

            foreach (var stringGuid in ids) {
                var guid = new Guid (stringGuid);
                var rows = await store.Table<TimeEntryData> ()
                           .QueryAsync (r => r.Id == guid && r.DeletedAt == null);
                var data = rows.FirstOrDefault ();
                list.Add (data);
            }
            return list;
        }

        public TimeSpan GetTimeEntryListDuration (List<TimeEntryData> timeEntries)
        {
            TimeSpan duration = TimeSpan.Zero;
            foreach (var item in timeEntries) {
                duration += TimeEntryModel.GetDuration (item, Time.UtcNow);
            }
            return duration;
        }

        #endregion
    }
}

