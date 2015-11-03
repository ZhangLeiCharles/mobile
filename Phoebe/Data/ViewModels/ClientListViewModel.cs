using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class ClientListViewModel : IVModel<ClientModel>
    {
        private ClientModel model;
        private Guid workspaceId;

        public ClientListViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Client";
        }

        public async Task Init ()
        {
            IsLoading = true;

            ClientListDataView = new WorkspaceClientsView (workspaceId);

            IsLoading = false;
        }

        public void Dispose ()
        {
            model = null;
        }

        public int SelectedClientIndex { get; set; }

        public bool IsLoading { get; set; }

        public string ClientName { get; set; }

        public IDataView<ClientData> ClientListDataView { get; set;}


        public void SaveClient (ClientModel client)
        {

        }

        public async Task CreateNewClient ()
        {

        }

    }
}
