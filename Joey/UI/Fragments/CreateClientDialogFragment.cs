using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Praeclarum.Bind;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateClientDialogFragment : BaseDialogFragment
    {
        public interface IOnClientSelectedListener
        {

            void OnClientSelected (ClientData data);
        }

        private const string WorkspaceIdArgument = "workspace_id";
        private IOnClientSelectedListener listener;
        private EditText nameEditText;
        private Button positiveButton;
        private CreateClientViewModel viewModel;
        private Binding binding;

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

        public CreateClientDialogFragment ()
        {
        }

        public CreateClientDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static CreateClientDialogFragment NewInstance (Guid workspaceId)
        {
            var fragment = new CreateClientDialogFragment ();

            var args = new Bundle();
            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            fragment.Arguments = args;

            return fragment;
        }

        public override async void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            viewModel = new CreateClientViewModel (WorkspaceId);
            await viewModel.Init ();
            binding = Binding.Create (() => nameEditText.Text == viewModel.ClientName);

            ValidateClientName ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            nameEditText = new EditText (Activity);
            nameEditText.SetHint (Resource.String.CreateClientDialogHint);
            nameEditText.InputType = InputTypes.TextFlagCapSentences;
            nameEditText.TextChanged += OnNameEditTextTextChanged;

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.CreateClientDialogTitle)
                   .SetView (nameEditText)
                   .SetPositiveButton (Resource.String.CreateClientDialogOk, OnPositiveButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            ValidateClientName ();
        }

        public CreateClientDialogFragment SetOnClientSelectedListener (IOnClientSelectedListener listener)
        {
            this.listener = listener;
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            ValidateClientName ();
        }

        private async void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            await viewModel.SaveNewClient ();
            if (listener != null) {
                listener.OnClientSelected (viewModel.GetClientData ());
            }
        }

        private void ValidateClientName ()
        {
            if (positiveButton == null || nameEditText == null) {
                return;
            }

            var valid = true;
            var name = nameEditText.Text;

            if (String.IsNullOrWhiteSpace (name)) {
                valid = false;
            }

            positiveButton.Enabled = valid;
        }

        public override void OnDestroy ()
        {
            binding.Unbind ();
            viewModel.Dispose ();
            base.OnDestroy ();
        }
    }
}
