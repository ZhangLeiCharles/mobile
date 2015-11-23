using System;
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
    public class ProjectListViewModel : IViewModel<ITimeEntryModel>
    {
        public ProjectListViewModel (Guid workspaceId)
        {
            CurrentWorkspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
        }

        public async Task Init ()
        {
            IsLoading = true;

            ProjectList = new WorkspaceProjectsView ();
            await ProjectList.ReloadAsync ();

            IsLoading = false;
        }

        public void Dispose ()
        {
            ProjectList.Dispose ();
        }

        public WorkspaceProjectsView ProjectList { get; set; }

        public Guid CurrentProjectId { get; set; }

        public Guid CurrentWorkspaceId { get; set; }

        public Guid CurrentTaskId { get; set; }

        public bool IsLoading { get; set; }
    }
}
