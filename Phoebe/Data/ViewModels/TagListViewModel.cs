using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class TagListViewModel : IViewModel<WorkspaceModel>
    {
        // This viewMode is apparently simple but
        // it needs the code related with the update of
        // the list. (subscription and reload of data
        // everytime a tag is changed/created/deleted

        private Guid workspaceId;
        private List<Guid> previousSelectedIds;

        public TagListViewModel (Guid workspaceId, List<Guid> previousSelectedIds)
        {
            this.previousSelectedIds = previousSelectedIds;
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Tags";
        }

        public async Task Init ()
        {
            IsLoading = true;

            // Create tag list.
            await LoadTagsAsync ();

            IsLoading = false;
        }

        public void Dispose ()
        {
            TagCollection = null;
        }

        public bool IsLoading { get; set; }

        public ObservableRangeCollection<TagData> TagCollection { get; set; }

        private async Task LoadTagsAsync ()
        {
            TagCollection = new ObservableRangeCollection<TagData> ();
            var store = ServiceContainer.Resolve<IDataStore> ();

            var workspaceTags = await store.Table<TagData> ()
                                .QueryAsync (r => r.DeletedAt == null
                                             && r.WorkspaceId == workspaceId);
            var currentSelectedTags = await store.Table<TagData> ()
                                      .QueryAsync (r => r.DeletedAt == null && previousSelectedIds.Contains (r.Id));

            // TODO:
            // There is an strange case, tags are created again across
            // workspaces. To avoid display similar names
            // on the list the diff and corrupt data, the filter is done by
            // names. The bad point is that tags will appears unselected.
            var diff = currentSelectedTags.Where (sTag => workspaceTags.All (wTag => sTag.Name != wTag.Name));

            workspaceTags.AddRange (diff);

            workspaceTags.Sort ((a, b) => {
                var aName = a != null ? (a.Name ?? String.Empty) : String.Empty;
                var bName = b != null ? (b.Name ?? String.Empty) : String.Empty;
                return String.Compare (aName, bName, StringComparison.Ordinal);
            });

            TagCollection.AddRange (workspaceTags);
        }

    }
}

