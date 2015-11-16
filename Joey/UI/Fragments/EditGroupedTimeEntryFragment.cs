using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class EditGroupedTimeEntryFragment : Fragment
    {
        private static readonly string TimeEntriesIdsArgument = "com.toggl.timer.time_entries_ids";

        private EditTimeEntryGroupViewModel viewModel;
        private RecyclerView recyclerView;
        private RecyclerView.Adapter listAdapter;

        private IList<string> TimeEntryIds
        {
            get {
                return Arguments != null ? Arguments.GetStringArrayList (TimeEntriesIdsArgument) : new List<string>();
            }
        }

        public EditGroupedTimeEntryFragment ()
        {
        }

        public EditGroupedTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static EditGroupedTimeEntryFragment NewInstance (IList<string> timeEntryListIds)
        {
            var fragment = new EditGroupedTimeEntryFragment ();

            var args = new Bundle ();
            args.PutStringArrayList (TimeEntriesIdsArgument, timeEntryListIds);
            fragment.Arguments = args;

            return fragment;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.EditGroupedTimeEntryFragment, container, false);
            HasOptionsMenu = true;

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));

            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            viewModel = new EditTimeEntryGroupViewModel (TimeEntryIds.ToList ());
            await viewModel.Init ();

            // Set adapter
            listAdapter = new GroupedEditAdapter (viewModel);
            (listAdapter as GroupedEditAdapter).HandleTapTimeEntry = HandleTimeEntryClick;
            recyclerView.SetAdapter (listAdapter);
        }

        public override void OnDestroyView ()
        {
            viewModel.Dispose ();
            base.OnDestroyView ();
        }

        private void HandleTimeEntryClick (TimeEntryData timeEntry)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            intent.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, new List<string> {timeEntry.Id.ToString()});
            StartActivity (intent);
        }

        private void OnProjectSelected (object sender, EventArgs e)
        {
            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutStringArrayListExtra (ProjectListActivity.ExtraTimeEntriesIds, TimeEntryIds);
            StartActivity (intent);
        }

        private void OnTagSelected (object sender, EventArgs e)
        {
            // new ChooseTimeEntryTagsDialogFragment (viewModel.Model.Workspace.Id, viewModel.Model.TimeEntryList).Show (FragmentManager, "tags_dialog");
        }
    }
}

