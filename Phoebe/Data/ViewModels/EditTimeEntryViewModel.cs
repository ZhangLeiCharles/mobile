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
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryViewModel : ViewModelBase, IVModel<TimeEntryModel>
    {
        internal static readonly string DefaultTag = "mobile";

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

            var tagsView = new TimeEntryTagCollectionView (timeEntryId);
            await tagsView.ReloadAsync ();
            TagList = tagsView.Data.ToList ();

            model = new TimeEntryModel (timeEntryId);
            model.PropertyChanged += OnPropertyChange;
            await model.LoadAsync ();

            // If the entry is new, setup it a bit.
            if (model.State == TimeEntryState.New) {
                model.StartTime = Time.UtcNow.AddMinutes (-5);
                model.StopTime = Time.UtcNow;
                TagList = await GetDefaultTagList (model.Workspace.Id);
            }

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

        public bool IsPremium { get; set; }

        public bool IsRunning { get; set; }

        public bool IsManual { get; set; }

        public string Duration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StopDate { get; set; }

        public string ProjectName { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public List<TagData> TagList { get; set; }

        public bool IsBillable { get; set; }

        public Guid WorkspaceId { get; set; }

        #endregion

        public async Task SetProjectAndTask (Guid projectId, Guid taskId)
        {
            var projectModel = new ProjectModel (projectId);
            await projectModel.LoadAsync ();

            model.Project = projectModel;
            model.Workspace = new WorkspaceModel (projectModel.Workspace);
            model.Task = new TaskModel (taskId);
            UpdateView ();
        }

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

        public void ChangeTagList (List<TagData> newTagList)
        {
            TagList = new List<TagData> (newTagList);
            UpdateView ();
        }

        public void AddTag (TagData tagData)
        {
            TagList.Add (tagData);
            UpdateView ();
        }

        public async Task SaveModel ()
        {
            if (IsManual) {
                return;
            }

            model.IsBillable = IsBillable;
            model.Description = Description;

            await SaveTagRelationships (TagList);
            await model.SaveAsync ();
        }

        public async Task SaveModelManual ()
        {
            model.IsBillable = IsBillable;
            model.Description = Description;
            model.State = TimeEntryState.Finished;

            await SaveTagRelationships (TagList);
            await model.SaveAsync ();
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
            StartDate = model.StartTime == DateTime.MinValue ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime () : model.StartTime.ToLocalTime ();
            StopDate = model.StopTime.HasValue ? model.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
            Duration = TimeSpan.FromSeconds (model.GetDuration ().TotalSeconds).ToString ().Substring (0, 8); // TODO: check substring function for long times
            Description = model.Description;
            ProjectName = model.Project != null ? model.Project.Name : string.Empty;
            IsBillable = model.IsBillable;
            IsPremium = model.Workspace.IsPremium;
            WorkspaceId = model.Workspace.Id;
            IsManual = model.State == TimeEntryState.New;

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

        private async Task SaveTagRelationships (List<TagData> newTagList)
        {
            // Create tag list.
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var existingTagRelations = new List<TimeEntryTagData> ();

            var tags = await dataStore.Table<TimeEntryTagData> ()
                       .QueryAsync (r => r.TimeEntryId == model.Id && r.DeletedAt == null);
            existingTagRelations.AddRange (tags);

            // Delete unused tag relations:
            var deleteTasks = existingTagRelations
                              .Where (oldTagRelation => newTagList.All (newTag => newTag.Id != oldTagRelation.TagId))
                              .Select (tagRelation => new TimeEntryTagModel (tagRelation).DeleteAsync ())
                              .ToList();

            // Create new tag relations:
            var createTasks = newTagList
                              .Where (newTag => existingTagRelations.All (oldTagRelation => oldTagRelation.TagId != newTag.Id))
            .Select (data => new TimeEntryTagModel { TimeEntry = model, Tag = new TagModel (data)} .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                model.Touch (); // why it needs to be called?
            }
        }

        private async Task<List<TagData>> GetDefaultTagList (Guid workspaceId)
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var defaultTagList = await dataStore.Table<TagData> ().QueryAsync (r => r.Name == DefaultTag && r.WorkspaceId == workspaceId && r.DeletedAt == null);

            if (defaultTagList.Count == 0) {
                defaultTagList = new List<TagData> ();
                var defaultTag = await dataStore.PutAsync (new TagData {
                    Name = DefaultTag,
                    WorkspaceId = workspaceId,
                });
                defaultTagList.Add (defaultTag);
            }
            return defaultTagList;
        }
    }
}

