using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Praeclarum.Bind;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class EditTimeEntryFragment : Fragment, ChangeTimeEntryDurationDialogFragment.IChangeDuration, ChangeDateTimeDialogFragment.IChangeDateTime
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        private EditTimeEntryViewModel viewModel;
        private Binding binding;
        private DateTime startTime;
        private DateTime stopTime;

        // components
        private TextView durationTextView;
        private EditText startTimeEditText;
        private EditText stopTimeEditText;
        private CheckBox billableCheckBox;
        private TogglField projectBit;
        private TogglField descriptionBit;
        private TogglTagsField tagsBit;
        private ActionBar toolbar;

        #region Binded properties

        // For the moment, our Bind library doesn't let us
        // to use something like converters.
        // that's why we have to bind to direct properties.

        private DateTime StartTime
        {
            get {
                return startTime;
            } set {
                startTime = value;
                startTimeEditText.Text = startTime.ToDeviceTimeString ();
            }
        }

        private DateTime StopTime
        {
            get {
                return stopTime;
            } set {
                stopTime = value;
                stopTimeEditText.Text = stopTime.ToDeviceTimeString ();
            }
        }

        private bool IsBillable
        {
            get { return billableCheckBox.Checked; }
            set {
                var label = value ? GetString (Resource.String.CurrentTimeEntryEditBillableChecked) : GetString (Resource.String.CurrentTimeEntryEditBillableUnchecked);
                billableCheckBox.Text = label;
                billableCheckBox.Checked = value;
            }
        }

        private bool IsPremium
        {
            get {
                return false;
            } set {
                billableCheckBox.Visibility = value ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        #endregion

        private Guid TimeEntryId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        public EditTimeEntryFragment ()
        {
        }

        public EditTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static EditTimeEntryFragment NewInstance (string timeEntryId)
        {
            var fragment = new EditTimeEntryFragment ();

            var bundle = new Bundle ();
            bundle.PutString (TimeEntryIdArgument, timeEntryId);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.EditTimeEntryFragment, container, false);
            var activityToolbar = view.FindViewById<Toolbar> (Resource.Id.EditTimeEntryFragmentToolbar);
            var activity = (Activity)Activity;

            activity.SetSupportActionBar (activityToolbar);
            toolbar = activity.SupportActionBar;
            toolbar.SetDisplayHomeAsUpEnabled (true);

            var durationLayout = inflater.Inflate (Resource.Layout.DurationTextView, null);
            durationTextView = durationLayout.FindViewById<TextView> (Resource.Id.DurationTextViewTextView);

            toolbar.SetCustomView (durationLayout, new ActionBar.LayoutParams ((int)GravityFlags.Center));
            toolbar.SetDisplayShowCustomEnabled (true);
            toolbar.SetDisplayShowTitleEnabled (false);

            startTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText).SetFont (Font.Roboto);
            stopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText).SetFont (Font.Roboto);

            descriptionBit = view.FindViewById<TogglField> (Resource.Id.Description)
                             .DestroyAssistView().DestroyArrow()
                             .SetName (Resource.String.BaseEditTimeEntryFragmentDescription);

            projectBit = view.FindViewById<TogglField> (Resource.Id.Project)
                         .SetName (Resource.String.BaseEditTimeEntryFragmentProject)
                         .SimulateButton();

            tagsBit = view.FindViewById<TogglTagsField> (Resource.Id.TagsBit);

            billableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox).SetFont (Font.RobotoLight);
            billableCheckBox.CheckedChange += (sender, e) => IsBillable = billableCheckBox.Checked;

            durationTextView.Click += (sender, e) =>
                                      ChangeTimeEntryDurationDialogFragment.NewInstance (StopTime, StartTime)
                                      .SetChangeDurationHandler (this)
                                      .Show (FragmentManager, "duration_dialog");

            startTimeEditText.Click += (sender, e) => {
                var title = GetString (Resource.String.ChangeTimeEntryStartTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance (StartTime, title)
                .SetOnChangeTimeHandler (this)
                .Show (FragmentManager, "start_time_dialog");
            };

            stopTimeEditText.Click += (sender, e) => {
                var title = GetString (Resource.String.ChangeTimeEntryStopTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance (StopTime, title)
                .SetOnChangeTimeHandler (this)
                .Show (FragmentManager, "stop_time_dialog");
            };

            projectBit.TextField.Click += OnProjectEditTextClick;
            projectBit.Click += OnProjectEditTextClick;
            tagsBit.OnPressTagField += OnTagsEditTextClick;

            HasOptionsMenu = true;
            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            viewModel = new EditTimeEntryViewModel (TimeEntryId);
            await viewModel.Init ();

            binding = Binding.Create (() =>
                                      durationTextView.Text == viewModel.Duration &&
                                      StartTime == viewModel.StartDate &&
                                      StopTime == viewModel.StopDate &&
                                      projectBit.TextField.Text == viewModel.ProjectName &&
                                      descriptionBit.TextField.Text == viewModel.Description &&
                                      projectBit.AssistViewTitle == viewModel.ClientName &&
                                      tagsBit.TagNames == viewModel.TagNames &&
                                      IsPremium == viewModel.IsPremium &&
                                      IsBillable == viewModel.IsBillable );
        }

        public override void OnDestroyView ()
        {
            binding.Unbind ();
            viewModel.Dispose ();
            base.OnDestroyView ();
        }

        private void OnProjectEditTextClick (object sender, EventArgs e)
        {
            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutStringArrayListExtra (ProjectListActivity.ExtraTimeEntriesIds, new List<string> {TimeEntryId.ToString ()});
            StartActivity (intent);
        }

        private void OnTagsEditTextClick (object sender, EventArgs e)
        {
            //new ChooseTimeEntryTagsDialogFragment (TimeEntry.Workspace.Id, new List<TimeEntryData> {TimeEntry.Data}).Show (FragmentManager, "tags_dialog");
        }

        public void OnChangeDateTime (DateTime newDateTime, string dialogTag)
        {
            if (dialogTag == "start_time_dialog") {
                viewModel.ChangeTimeEntryStart (newDateTime);
            } else {
                viewModel.ChangeTimeEntryStop (newDateTime);
            }
        }

        public void OnChangeDuration (TimeSpan newDuration)
        {
            viewModel.ChangeTimeEntryDuration (newDuration);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            Task.Run (async () => await viewModel.SaveModel ());
            Activity.OnBackPressed ();
            return base.OnOptionsItemSelected (item);
        }
    }
}
