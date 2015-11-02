using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryViewModel : IVModel<TimeEntryModel>
    {
        private TagCollectionView tagsView;
        private TimeEntryModel model;
        private Guid timeEntryId;

        public EditTimeEntryViewModel (Guid timeEntryId)
        {
            this.timeEntryId = timeEntryId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public async Task Init ()
        {
            IsLoading  = true;

            tagsView = new TagCollectionView (timeEntryId);
            await tagsView.ReloadAsync ();

            model = new TimeEntryModel (timeEntryId);
            model.PropertyChanged += OnPropertyChange;
            await model.LoadAsync ();

            SyncModel ();

            IsLoading = false;
        }

        public void Dispose ()
        {
            model.PropertyChanged -= OnPropertyChange;
            model = null;
        }

        #region viewModel State properties

        public bool IsLoading { get; set; }

        public bool IsPremium { get; set; }

        public string Duration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StopDate { get; set; }

        public string ProjectName { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public List<string> TagNames { get; set; }

        public bool IsBillable { get; set; }

        #endregion

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            model.SetDuration (newDuration);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (DateTime newStartTime)
        {
            model.StartTime = newStartTime;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (DateTime newStopTime)
        {
            model.StopTime = newStopTime;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public async Task SaveModel ()
        {
            model.IsBillable = IsBillable;
            model.Description = Description;
            await model.SaveAsync ();
        }

        private void OnPropertyChange (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Data") {
                SyncModel ();
            }
        }

        private void SyncModel ()
        {
            IsPremium = model.Workspace.IsPremium;
            StartDate = model.StartTime;
            StopDate = model.StopTime.HasValue ? model.StopTime.Value : DateTime.UtcNow;
            Description = model.Description;
            Duration = TimeSpan.FromSeconds (model.GetDuration ().TotalSeconds).ToString ();
            ProjectName = model.Project != null ? model.Project.Name : string.Empty;
            TagNames = tagsView.Data.ToList ();

            if (model.Project != null) {
                if (model.Project.Client != null) {
                    ClientName = model.Project.Client.Name;
                }
            }
        }
    }
}

