using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class CreateTagViewModel : IVModel<TagModel>
    {
        private Guid workspaceId;
        private WorkspaceModel workspace;

        public CreateTagViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public void Init ()
        {
            IsLoading = true;

            // Create workspace.
            workspace = new WorkspaceModel (workspaceId);

            IsLoading = false;
        }

        public bool IsLoading { get; set; }

        public string TagName { get; set; }

        public void Dispose ()
        {
            workspace = null;
        }

        public async Task<TagData> SaveTagModel ()
        {
            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<TagData>()
                           .QueryAsync (r => r.WorkspaceId == workspace.Id && r.Name == TagName)
                           .ConfigureAwait (false);

            TagModel tag;
            if (existing.Count > 0) {
                tag = new TagModel (existing [0]);
            } else {
                tag = new TagModel {
                    Name = TagName,
                    Workspace = workspace,
                };
                await tag.SaveAsync ().ConfigureAwait (false);
            }
            return tag.Data;
        }
    }
}

