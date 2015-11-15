using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class TagListViewModel : IVModel<WorkspaceModel>
    {
        // This viewMode is apparently simple but
        // it needs the code related with the update of
        // the list. (subscription and reload of data
        // everytime a tag is changed/created/deleted

        private Guid workspaceId;

        public TagListViewModel (Guid workspaceId)
        {
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
            var tags = await store.Table<TagData> ()
                       .QueryAsync (r => r.DeletedAt == null
                                    && r.WorkspaceId == workspaceId);

            tags.Sort ((a, b) => {
                var aName = a != null ? (a.Name ?? String.Empty) : String.Empty;
                var bName = b != null ? (b.Name ?? String.Empty) : String.Empty;
                return String.Compare (aName, bName, StringComparison.Ordinal);
            });

            TagCollection.AddRange (tags);
        }
    }
}

