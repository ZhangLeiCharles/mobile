using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
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

        public EditTimeEntryViewModel ViewModel { get; private set; }
        public TextView DurationTextView { get; private set; }
        public EditText StartTimeEditText { get; private set; }
        public EditText StopTimeEditText { get; private set; }
        public CheckBox BillableCheckBox { get; private set; }
        public TogglField ProjectField { get; private set; }
        public TogglField DescriptionField { get; private set; }
        public TogglTagsField TagsField { get; private set; }
        private ActionBar toolbar;

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
            DurationTextView = durationLayout.FindViewById<TextView> (Resource.Id.DurationTextViewTextView);

            toolbar.SetCustomView (durationLayout, new ActionBar.LayoutParams ((int)GravityFlags.Center));
            toolbar.SetDisplayShowCustomEnabled (true);
            toolbar.SetDisplayShowTitleEnabled (false);

            StartTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText).SetFont (Font.Roboto);
            StopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText).SetFont (Font.Roboto);

            DescriptionField = view.FindViewById<TogglField> (Resource.Id.Description)
                               .DestroyAssistView().DestroyArrow()
                               .SetName (Resource.String.BaseEditTimeEntryFragmentDescription);

            ProjectField = view.FindViewById<TogglField> (Resource.Id.Project)
                           .SetName (Resource.String.BaseEditTimeEntryFragmentProject)
                           .SimulateButton();

            TagsField = view.FindViewById<TogglTagsField> (Resource.Id.TagsBit);
            BillableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox).SetFont (Font.RobotoLight);

            DurationTextView.Click += (sender, e) =>
                                      ChangeTimeEntryDurationDialogFragment.NewInstance (ViewModel.StopDate, ViewModel.StartDate)
                                      .SetChangeDurationHandler (this)
                                      .Show (FragmentManager, "duration_dialog");

            StartTimeEditText.Click += (sender, e) => {
                var title = GetString (Resource.String.ChangeTimeEntryStartTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance (ViewModel.StartDate, title)
                .SetOnChangeTimeHandler (this)
                .Show (FragmentManager, "start_time_dialog");
            };

            StopTimeEditText.Click += (sender, e) => {
                var title = GetString (Resource.String.ChangeTimeEntryStopTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance (ViewModel.StopDate, title)
                .SetOnChangeTimeHandler (this)
                .Show (FragmentManager, "stop_time_dialog");
            };

            ProjectField.TextField.Click += OnProjectEditTextClick;
            ProjectField.Click += OnProjectEditTextClick;
            TagsField.OnPressTagField += OnTagsEditTextClick;

            HasOptionsMenu = true;
            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ViewModel = new EditTimeEntryViewModel (TimeEntryId);
            await ViewModel.Init ();

            durationBinding = this.SetBinding (() => ViewModel.Duration, () => DurationTextView.Text);
            startTimeBinding = this.SetBinding (() => ViewModel.StartDate, () => StartTimeEditText.Text).ConvertSourceToTarget (dateTime => dateTime.ToDeviceTimeString ());
            stopTimeBinding = this.SetBinding (() => ViewModel.StopDate, () => StopTimeEditText.Text).ConvertSourceToTarget (dateTime => dateTime.ToDeviceTimeString ());
            projectBinding = this.SetBinding (() => ViewModel.ProjectName, () => ProjectField.TextField.Text);
            clientBinding = this.SetBinding (() => ViewModel.ClientName, () => ProjectField.AssistViewTitle);
            tagBinding = this.SetBinding (() => ViewModel.TagNames, () => TagsField.TagNames);
            descriptionBinding = this.SetBinding (() => ViewModel.Description, () => DescriptionField.TextField.Text, BindingMode.TwoWay);
            isPremiumBinding = this.SetBinding (() => ViewModel.IsPremium, () => BillableCheckBox.Visibility).ConvertSourceToTarget (isVisible => isVisible ? ViewStates.Visible : ViewStates.Gone);
            isBillableBinding = this.SetBinding (() => ViewModel.IsBillable, () => BillableCheckBox.Checked, BindingMode.TwoWay);
            billableBinding = this.SetBinding (() => ViewModel.IsBillable)
            .WhenSourceChanges (() => {
                var label = ViewModel.IsBillable ? GetString (Resource.String.CurrentTimeEntryEditBillableChecked) : GetString (Resource.String.CurrentTimeEntryEditBillableUnchecked);
                BillableCheckBox.Text = label;
            });
        }

        public override void OnDestroyView ()
        {
            ViewModel.Dispose ();
            base.OnDestroyView ();
        }

        public override void OnPause ()
        {
            Task.Run (async () => await ViewModel.SaveModel ());
            base.OnPause ();
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

        public void OnChangeDateTime (TimeSpan timeDiff, string dialogTag)
        {
            if (dialogTag == "start_time_dialog") {
                ViewModel.ChangeTimeEntryStart (timeDiff);
            } else {
                ViewModel.ChangeTimeEntryStop (timeDiff);
            }
        }

        public void OnChangeDuration (TimeSpan newDuration)
        {
            ViewModel.ChangeTimeEntryDuration (newDuration);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            Activity.OnBackPressed ();
            return base.OnOptionsItemSelected (item);
        }

        private Binding<string, string> durationBinding, projectBinding, clientBinding, descriptionBinding;
        private Binding<DateTime, string> startTimeBinding, stopTimeBinding;
        private Binding<List<string>, List<string>> tagBinding;
        private Binding<bool, ViewStates> isPremiumBinding;
        private Binding<bool, bool> isBillableBinding, billableBinding;
    }
}
