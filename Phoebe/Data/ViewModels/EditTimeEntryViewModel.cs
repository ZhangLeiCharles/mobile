using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
    public class EditTimeEntryViewModel : IVModel<TimeEntryModel>
    {
        private TimeEntryTagCollectionView tagsView;
        private TimeEntryModel model;
        private Guid timeEntryId;
        private Timer durationTimer;

        public EditTimeEntryViewModel (Guid timeEntryId)
        {
            this.timeEntryId = timeEntryId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public async Task Init ()
        {
            IsLoading  = true;

            durationTimer = new Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            tagsView = new TimeEntryTagCollectionView (timeEntryId);
            await tagsView.ReloadAsync ();

            model = new TimeEntryModel (timeEntryId);
            model.PropertyChanged += OnPropertyChange;
            await model.LoadAsync ();

            SyncModel ();

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

        public bool IsPremium { get; set; }

        public bool IsRunning { get; set; }

        public string Duration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StopDate { get; set; }

        public string ProjectName { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public List<string> TagNames { get; set; }

        public bool IsBillable { get; set; }

        public Guid WorkspaceId { get; set; }

        #endregion

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            model.SetDuration (newDuration);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            model.StartTime += diffTime;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            model.StopTime += diffTime;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public async Task ChangeTagList (List<TagData> tagList)
        {
            // Delete unused tag relations:
            var deleteTasks = tagsView.Data
                              .Where (oldTag => tagList.All (newTag => newTag.Id != oldTag.Id))
                              .Select (data => new TimeEntryTagModel (data).DeleteAsync ())
                              .ToList();

            // Create new tag relations:
            var createTasks = tagsView.Data
                              .Where (newTag => tagsView.Data.All (oldTag => oldTag.Id != newTag.Id))
            .Select (data => new TimeEntryTagModel { TimeEntry = model, Tag = new TagModel (data)} .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                //model.Touch ();
                //await model.SaveAsync ();
            }
        }

        public async Task AddTag (TagData tagData)
        {
            var store = ServiceContainer.Resolve<IDataStore>();

            // Check if the relation already exists before adding it
            var relations = await store.Table<TimeEntryTagData> ()
                            .CountAsync (r => r.TimeEntryId == model.Id && r.TagId == tagData.Id && r.DeletedAt == null)
                            .ConfigureAwait (false);

            if (relations > 0) {
                var relationModel = new TimeEntryTagModel {
                    TimeEntry = model,
                    Tag = new TagModel (tagData),
                };
                await relationModel.SaveAsync ();
                //model.Touch ();
                //await model.SaveAsync ();
            }
        }

        public async Task SaveModel ()
        {
            model.IsBillable = IsBillable;
            model.Description = Description;
            await model.SaveAsync ();
        }

        private void OnPropertyChange (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Data" ||
                    e.PropertyName == "StartTime" ||
                    e.PropertyName == "Duration" ||
                    e.PropertyName == "StopTime") {
                SyncModel ();
            }
        }

        private void SyncModel ()
        {
            StartDate = model.StartTime.ToLocalTime ();
            StopDate = model.StopTime.HasValue ? model.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
            Duration = TimeSpan.FromSeconds (model.GetDuration ().TotalSeconds).ToString ().Substring (0, 8);
            Description = model.Description;
            ProjectName = model.Project != null ? model.Project.Name : string.Empty;
            TagNames = tagsView.TagNames;
            IsBillable = model.IsBillable;
            IsPremium = model.Workspace.IsPremium;
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
    }
}

