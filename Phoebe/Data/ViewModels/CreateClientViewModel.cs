using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class CreateClientViewModel : IViewModel<ClientModel>
    {
        private ClientModel model;
        private Guid workspaceId;
        private WorkspaceModel workspaceModel;

        public CreateClientViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public async Task Init ()
        {
            IsLoading = true;

            workspaceModel = new WorkspaceModel (workspaceId);
            await workspaceModel.LoadAsync ();

            model = new ClientModel {
                Workspace = workspaceModel
            };

            IsLoading = false;
        }

        public void Dispose ()
        {
            workspaceModel = null;
            model = null;
        }

        public bool IsLoading { get; set; }

        public string ClientName { get; set; }

        public async Task<ClientData> SaveNewClient ()
        {
            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<ClientData>()
                           .QueryAsync (r => r.WorkspaceId == workspaceId && r.Name == ClientName)
                           .ConfigureAwait (false);

            if (existing.Count > 0) {
                model = new ClientModel (existing [0]);
            } else {
                model.Name = ClientName;
            }

            await model.SaveAsync ();

            return model.Data;
        }
    }
}

