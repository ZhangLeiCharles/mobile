using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateTagDialogFragment : BaseDialogFragment
    {
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";

        private Button positiveButton;
        private EditText nameEditText { get; set;}
        private CreateTagViewModel viewModel { get; set;}
        private Binding<string, string> tagBinding;
        private IUpdateTagList updateTagHandler;

        private Guid WorkspaceId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                }
                return id;
            }
        }

        public CreateTagDialogFragment ()
        {
        }

        public CreateTagDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static CreateTagDialogFragment NewInstance (Guid workspaceId)
        {
            var fragment = new CreateTagDialogFragment ();

            var args = new Bundle ();
            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            fragment.Arguments = args;

            return fragment;
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            viewModel = new CreateTagViewModel (WorkspaceId);
            viewModel.Init ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            nameEditText = new EditText (Activity);
            nameEditText.SetHint (Resource.String.CreateTagDialogHint);
            nameEditText.InputType = InputTypes.TextFlagCapSentences;
            nameEditText.TextChanged += OnNameEditTextTextChanged;

            // Again we need to define binding inside OnCreateDialog
            tagBinding = this.SetBinding (() => viewModel.TagName, () => nameEditText.Text, BindingMode.TwoWay);

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.CreateTagDialogTitle)
                   .SetView (nameEditText)
                   .SetPositiveButton (Resource.String.CreateTagDialogOk, OnPositiveButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            ValidateTagName ();
        }

        public override void OnDestroy ()
        {
            viewModel.Dispose ();
            base.OnDestroy ();
        }

        public CreateTagDialogFragment SetCreateNewTagHandler (IUpdateTagList handler)
        {
            updateTagHandler = handler;
            return this;
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            ValidateTagName ();
        }

        private async void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            var newTagData = await viewModel.SaveTagModel ();
            if (updateTagHandler != null) {
                updateTagHandler.OnCreateNewTag (newTagData);
            }
        }

        private void ValidateTagName ()
        {
            if (positiveButton == null || nameEditText == null) {
                return;
            }

            positiveButton.Enabled = !String.IsNullOrWhiteSpace (nameEditText.Text);
        }
    }
}
