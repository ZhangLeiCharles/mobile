using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : Fragment, SwipeDismissCallback.IDismissListener, ItemTouchListener.IItemTouchListener
    {
        private RecyclerView recyclerView;
        private View emptyMessageView;
        private LogTimeEntriesAdapter logAdapter;
        private CoordinatorLayout coordinatorLayout;
        private TimerComponent timerComponent;

        // Recycler setup
        private DividerItemDecoration dividerDecoration;
        private ShadowItemDecoration shadowDecoration;
        private ItemTouchListener itemTouchListener;

        // binding references
        private Binding<bool, bool> hasMoreBinding, newMenuBinding;
        private Binding<TimeEntriesCollectionView, TimeEntriesCollectionView> collectionBinding;
        private Binding<bool, FABButtonState> fabBinding;

        #region Binding objects and properties.

        public LogTimeEntriesViewModel ViewModel { get; set;}
        public IMenuItem AddNewMenuItem { get; private set; }
        public StartStopFab StartStopBtn { get; private set;}

        #endregion

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            emptyMessageView.Visibility = ViewStates.Gone;
            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            coordinatorLayout = view.FindViewById<CoordinatorLayout> (Resource.Id.logCoordinatorLayout);
            StartStopBtn = view.FindViewById<StartStopFab> (Resource.Id.StartStopBtn);
            timerComponent = ((MainDrawerActivity)Activity).Timer; // TODO: a better way to do this?

            SetupRecyclerView ();
            HasOptionsMenu = true;

            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ViewModel = new LogTimeEntriesViewModel ();
            await ViewModel.Init ();

            hasMoreBinding = this.SetBinding (()=> ViewModel.HasMore).WhenSourceChanges (ShowEmptyState);
            collectionBinding = this.SetBinding (()=> ViewModel.CollectionView).WhenSourceChanges (() => {
                logAdapter = new LogTimeEntriesAdapter (recyclerView, ViewModel.CollectionView);
                recyclerView.SetAdapter (logAdapter);
            });
            fabBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning, () => StartStopBtn.ButtonAction)
                         .ConvertSourceToTarget (isRunning => isRunning ? FABButtonState.Stop : FABButtonState.Start);

            newMenuBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning)
            .WhenSourceChanges (() => {
                if (AddNewMenuItem != null) {
                    AddNewMenuItem.SetVisible (!ViewModel.IsTimeEntryRunning);
                }
            });

            // Pass ViewModel to TimerComponent.
            timerComponent.SetViewModel (ViewModel);
            StartStopBtn.Click += StartStopClick;
        }

        public async void StartStopClick (object sender, EventArgs e)
        {
            await ViewModel.StartStopTimeEntry ();
            if (ViewModel.HasMore) {
                OBMExperimentManager.Send (OBMExperimentManager.HomeEmptyState, "startButton", "click");
            }
        }

        public override void OnDestroyView ()
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            // TODO: Remove bindings to ViewModel
            // check if it is needed or not.
            timerComponent.DetachBindind ();

            ReleaseRecyclerView ();
            ViewModel.Dispose ();

            base.OnDestroyView ();
        }

        private void SetupRecyclerView ()
        {
            // Touch listeners.
            itemTouchListener = new ItemTouchListener (recyclerView, this);
            recyclerView.AddOnItemTouchListener (itemTouchListener);

            var touchCallback = new SwipeDismissCallback (ItemTouchHelper.Up | ItemTouchHelper.Down, ItemTouchHelper.Left, this);
            var touchHelper = new ItemTouchHelper (touchCallback);
            touchHelper.AttachToRecyclerView (recyclerView);

            // Decorations.
            dividerDecoration = new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList);
            shadowDecoration = new ShadowItemDecoration (Activity);
            recyclerView.AddItemDecoration (dividerDecoration);
            recyclerView.AddItemDecoration (shadowDecoration);

            recyclerView.GetItemAnimator ().SupportsChangeAnimations = false;
        }

        private void ReleaseRecyclerView ()
        {
            recyclerView.RemoveItemDecoration (shadowDecoration);
            recyclerView.RemoveItemDecoration (dividerDecoration);
            recyclerView.RemoveOnItemTouchListener (itemTouchListener);

            recyclerView.GetAdapter ().Dispose ();
            recyclerView.Dispose ();
            logAdapter = null;

            itemTouchListener.Dispose ();
            dividerDecoration.Dispose ();
            shadowDecoration.Dispose ();
        }

        private void ShowEmptyState ()
        {
            if (!OBMExperimentManager.IncludedInExperiment (OBMExperimentManager.HomeEmptyState)) { //Empty state is experimental.
                return;
            }

            recyclerView.Visibility = ViewModel.HasMore ? ViewStates.Visible : ViewStates.Gone;
            emptyMessageView.Visibility = ViewModel.HasMore ? ViewStates.Gone : ViewStates.Visible;
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate (Resource.Menu.SaveItemMenu, menu);
            AddNewMenuItem = menu.FindItem (Resource.Id.saveItem);
            AddNewMenuItem.SetVisible (!ViewModel.IsTimeEntryRunning);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            i.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, new List<string> { ViewModel.GetActiveTimeEntry ().Id.ToString ()});
            Activity.StartActivity (i);

            return base.OnOptionsItemSelected (item);
        }

        #region IDismissListener implementation

        public bool CanDismiss (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            var adapter = recyclerView.GetAdapter ();
            return adapter.GetItemViewType (viewHolder.LayoutPosition) == LogTimeEntriesAdapter.ViewTypeContent;
        }

        public async void OnDismiss (RecyclerView.ViewHolder viewHolder)
        {
            var duration = TimeEntriesCollectionView.UndoSecondsInterval * 1000;

            await ViewModel.CollectionView.RemoveItemWithUndoAsync (viewHolder.AdapterPosition);
            var snackBar = Snackbar
                           .Make (coordinatorLayout, Resources.GetString (Resource.String.UndoBarDeletedText), duration)
                           .SetAction (Resources.GetString (Resource.String.UndoBarButtonText),
                                       async v => await ViewModel.CollectionView.RestoreItemFromUndoAsync ());
            ChangeSnackBarColor (snackBar);
            snackBar.Show ();
        }

        #endregion

        #region IRecyclerViewOnItemClickListener implementation

        public void OnItemClick (RecyclerView parent, View clickedView, int position)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));

            IList<string> guids = ((TimeEntryHolder)logAdapter.GetEntry (position)).TimeEntryGuids;
            intent.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, guids);
            intent.PutExtra (EditTimeEntryActivity.IsGrouped, guids.Count > 1);

            StartActivity (intent);
        }

        public void OnItemLongClick (RecyclerView parent, View clickedView, int position)
        {
            OnItemClick (parent, clickedView, position);
        }

        public bool CanClick (RecyclerView view, int position)
        {
            var adapter = recyclerView.GetAdapter ();
            return adapter.GetItemViewType (position) == LogTimeEntriesAdapter.ViewTypeContent;
        }

        #endregion

        // Temporal hack to change the
        // action color in snack bar
        private void ChangeSnackBarColor (Snackbar snack)
        {
            var group = (ViewGroup) snack.View;
            for (int i = 0; i < group.ChildCount; i++) {
                View v = group.GetChildAt (i);
                var textView = v as TextView;
                if (textView != null) {
                    TextView t = textView;
                    if (t.Text == Resources.GetString (Resource.String.UndoBarButtonText)) {
                        t.SetTextColor (Resources.GetColor (Resource.Color.material_green));
                    }

                }
            }
        }
    }
}
