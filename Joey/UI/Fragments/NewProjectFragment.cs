using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Praeclarum.Bind;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class NewProjectFragment : Fragment
    {
        private ActionBar Toolbar;
        private NewProjectViewModel viewModel;
        private Binding binding;
        private List<TimeEntryData> timeEntryList;
        private TogglField projectBit;
        private ColorPickerRecyclerView colorPicker;

        private List<TimeEntryData> TimeEntryList
        {
            get {
                if (timeEntryList == null)
                    timeEntryList = BaseActivity.GetDataFromIntent <List<TimeEntryData>>
                                    (Activity.Intent, NewProjectActivity.ExtraTimeEntryDataListId);
                return timeEntryList;
            }
        }

        public NewProjectFragment ()
        {
        }

        public NewProjectFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static NewProjectFragment NewInstance ()
        {
            return new NewProjectFragment ();
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.NewProjectFragment, container, false);
            var activity = (Activity)Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.NewProjectsFragmentToolbar);
            activity.SetSupportActionBar (toolbar);

            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.NewProjectTitle);

            projectBit = view.FindViewById<TogglField> (Resource.Id.NewProjectProjectNameBit)
                         .DestroyAssistView().DestroyArrow()
                         .SetName (Resource.String.NewProjectProjectFieldName);

            colorPicker = view.FindViewById<ColorPickerRecyclerView> (Resource.Id.NewProjectColorPickerRecyclerView);
            HasOptionsMenu = true;
            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            viewModel = new NewProjectViewModel (TimeEntryList);
            binding = Binding.Create (() =>
                                      selectClientBit.TextField.Text == viewModel.ClientName &&
                                      projectBit.TextField.Text == viewModel.ProjectName &&
                                      colorPicker.Adapter.SelectedColor == viewModel.ProjectColor);

            await viewModel.Init ();
        }

        public override void OnDestroyView ()
        {
            binding.Unbind ();
            viewModel.Dispose ();
            base.OnDestroyView ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // show keyboard
            var inputService = (InputMethodManager)Activity.GetSystemService (Context.InputMethodService);
            projectBit.TextField.PostDelayed (delegate {
                inputService.ShowSoftInput (projectBit.TextField, ShowFlags.Implicit);
            }, 100);
        }

        private async void SaveButtonHandler (object sender, EventArgs e)
        {
            var result = await viewModel.SaveProjectModel ();

            switch (result) {
            case NewProjectViewModel.SaveProjectResult.NameIsEmpty:
                new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.NewProjectEmptyDialogTitle)
                .SetMessage (Resource.String.NewProjectEmptyDialogMessage)
                .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show ();
                break;
            case NewProjectViewModel.SaveProjectResult.NameExists:
                new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.NewProjectDuplicateDialogTitle)
                .SetMessage (Resource.String.NewProjectDuplicateDialogMessage)
                .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show ();
                break;
            case NewProjectViewModel.SaveProjectResult.SaveOk:
                FinishActivity (true);
                break;
            }
        }

        private void SelectClientBitClickedHandler (object sender, EventArgs e)
        {
            //new ClientListDialogFragment (viewModel.Model.Data.WorkspaceId, viewModel.Model)

            //.Show (FragmentManager, "clients_dialog");
            var dialog = new ClientListDialogFragment (viewModel.Model.Data.WorkspaceId, viewModel.Model);
            dialog.Dialog.DismissEvent += (object sender, EventArgs e) => {

            };
            dialog.Show ();
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            menu.Add (Resource.String.NewProjectSaveButtonText).SetShowAsAction (ShowAsAction.Always);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                FinishActivity (false);
            } else {
                SaveButtonHandler (this, null);
            }
            return base.OnOptionsItemSelected (item);
        }

        private void FinishActivity (bool isProjectCreated)
        {
            var resultIntent = new Intent ();
            Activity.SetResult (isProjectCreated ? Result.Ok : Result.Canceled, resultIntent);
            Activity.Finish();
        }
    }
}

