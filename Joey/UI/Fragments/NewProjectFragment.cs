using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using GalaSoft.MvvmLight.Helpers;
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
    public class NewProjectFragment : Fragment, IOnClientSelectedListener
    {
        private static readonly string WorkspaceIdArgument = "workspace_id_param";
        public static readonly int ClientSelectedRequestCode = 1;

        private ActionBar Toolbar;
        public TogglField ProjectBit { get; private set; }
        public TogglField SelectClientBit { get; private set; }
        public ColorPickerRecyclerView ColorPicker { get; private set; }
        public NewProjectViewModel ViewModel { get; private set; }

        // Binding to avoid weak references
        // to be collected by the
        // garbage collector. Under investigation ;)
        private Binding<int, int> colorBinding;
        private Binding<string, string> nameBinding;
        private Binding<string, string> clientBinding;

        private Guid WorkspaceId
        {
            get {
                Guid id;
                Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                return id;
            }
        }

        public NewProjectFragment ()
        {
        }

        public NewProjectFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static NewProjectFragment NewInstance (string workspaceId)
        {
            var fragment = new NewProjectFragment ();

            var args = new Bundle ();
            args.PutString (WorkspaceIdArgument, workspaceId);
            fragment.Arguments = args;

            return fragment;
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

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.NewProjectProjectNameBit)
                         .DestroyAssistView().DestroyArrow()
                         .SetName (Resource.String.NewProjectProjectFieldName);

            SelectClientBit = view.FindViewById<TogglField> (Resource.Id.SelectClientNameBit)
                              .DestroyAssistView().SetName (Resource.String.NewProjectSelectClientFieldName)
                              .SimulateButton();

            SelectClientBit.TextField.Click += SelectClientBitClickedHandler;
            SelectClientBit.Click += SelectClientBitClickedHandler;

            ColorPicker = view.FindViewById<ColorPickerRecyclerView> (Resource.Id.NewProjectColorPickerRecyclerView);
            HasOptionsMenu = true;
            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ViewModel = new NewProjectViewModel (WorkspaceId);

            clientBinding = this.SetBinding (() => ViewModel.ClientName, () => SelectClientBit.TextField.Text);
            nameBinding = this.SetBinding (() => ViewModel.ProjectName, () => ProjectBit.TextField.Text, BindingMode.TwoWay);
            colorBinding = this.SetBinding (() => ViewModel.ProjectColor, () => ColorPicker.Adapter.SelectedColor, BindingMode.TwoWay).UpdateTargetTrigger ("SelectedColorChanged");

            await ViewModel.Init ();
        }

        public override void OnDestroyView ()
        {
            ViewModel.Dispose ();
            base.OnDestroyView ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // show keyboard
            var inputService = (InputMethodManager)Activity.GetSystemService (Context.InputMethodService);
            ProjectBit.TextField.PostDelayed (delegate {
                inputService.ShowSoftInput (ProjectBit.TextField, ShowFlags.Implicit);
            }, 100);
        }

        private async void SaveButtonHandler (object sender, EventArgs e)
        {
            var result = await ViewModel.SaveProjectModel ();

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
            ClientListDialogFragment.NewInstance (WorkspaceId)
            .SetClientSelectListener (this)
            .Show (FragmentManager, "clients_dialog");
        }

        #region IOnClientSelectedListener implementation

        public void OnClientSelected (ClientData data)
        {
            ViewModel.SetClient (data);
        }

        #endregion

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate (Resource.Menu.SaveItemMenu, menu);
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
            resultIntent.PutExtra (BaseActivity.IntentProjectIdArgument, ViewModel.ProjectId.ToString ());
            Activity.SetResult (isProjectCreated ? Result.Ok : Result.Canceled, resultIntent);
            Activity.Finish();
        }
    }
}
