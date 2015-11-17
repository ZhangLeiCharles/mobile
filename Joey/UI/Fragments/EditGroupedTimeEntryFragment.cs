using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
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

        // to avoid weak references to be removed
        private Binding<string, string> durationBinding, projectBinding, clientBinding, descriptionBinding;
        private Binding<DateTime, string> startTimeBinding, stopTimeBinding;

        public EditTimeEntryGroupViewModel ViewModel { get; private set; }
        public TextView DurationTextView { get; private set; }
        public EditText StartTimeEditText { get; private set; }
        public EditText StopTimeEditText { get; private set; }
        public TogglField ProjectField { get; private set; }
        public TogglField DescriptionField { get; private set; }

        private ActionBar toolbar;
        private ListView timeEntriesListView;

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
            var activityToolbar = view.FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.EditTimeEntryFragmentToolbar);
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

            ProjectField.TextField.Click += OnProjectEditTextClick;
            ProjectField.Click += OnProjectEditTextClick;

            timeEntriesListView = view.FindViewById<ListView> (Resource.Id.timeEntryGroupListView);

            HasOptionsMenu = true;
            return view;
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ViewModel = new EditTimeEntryGroupViewModel (TimeEntryIds.ToList ());
            await ViewModel.Init ();

            durationBinding = this.SetBinding (() => ViewModel.Duration, () => DurationTextView.Text);
            startTimeBinding = this.SetBinding (() => ViewModel.StartDate, () => StartTimeEditText.Text).ConvertSourceToTarget (dateTime => dateTime.ToDeviceTimeString ());
            stopTimeBinding = this.SetBinding (() => ViewModel.StopDate, () => StopTimeEditText.Text).ConvertSourceToTarget (dateTime => dateTime.ToDeviceTimeString ());
            projectBinding = this.SetBinding (() => ViewModel.ProjectName, () => ProjectField.TextField.Text);
            clientBinding = this.SetBinding (() => ViewModel.ClientName, () => ProjectField.AssistViewTitle);
            descriptionBinding = this.SetBinding (() => ViewModel.Description, () => DescriptionField.TextField.Text, BindingMode.TwoWay);

            // Set adapter using Mvvm light utils.
            timeEntriesListView.Adapter = ViewModel.TimeEntryCollection.GetAdapter (GetTimeEntryView);
            timeEntriesListView.ItemClick += (sender, e) => HandleTimeEntryClick (ViewModel.TimeEntryCollection [e.Position]);
        }

        public override void OnDestroyView ()
        {
            ViewModel.Dispose ();
            base.OnDestroyView ();
        }

        public override void OnPause ()
        {
            // Save Time entry state every time
            // the fragment is paused.
            Task.Run (async () => await ViewModel.SaveModel ());
            base.OnPause ();
        }

        private void HandleTimeEntryClick (TimeEntryData timeEntry)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            intent.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, new List<string> {timeEntry.Id.ToString()});
            StartActivity (intent);
        }

        private void OnProjectEditTextClick (object sender, EventArgs e)
        {
            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutStringArrayListExtra (ProjectListActivity.ExtraTimeEntriesIds, TimeEntryIds);
            StartActivity (intent);
        }

        private View GetTimeEntryView (int position, TimeEntryData timeEntryData, View convertView)
        {
            View view = convertView ?? LayoutInflater.FromContext (Activity).Inflate (Resource.Layout.EditGroupedTimeEntryItem, null);

            var colorView = view.FindViewById<View> (Resource.Id.GroupedEditTimeEntryItemTimeColorView);
            var periodTextView = view.FindViewById<TextView> (Resource.Id.GroupedEditTimeEntryItemTimePeriodTextView);
            var durationTextView = view.FindViewById<TextView> (Resource.Id.GroupedEditTimeEntryItemDurationTextView);

            // Set color.
            Color color;
            string [] colours = ProjectModel.HexColors;
            color = ViewModel.ProjectColor > 0 ? Color.ParseColor (colours [ViewModel.ProjectColor % colours.Length]) : Color.Transparent;
            colorView.SetBackgroundColor (color);

            // Set rest of data.
            var stopTime = timeEntryData.StopTime.HasValue ? " – " + timeEntryData.StopTime.Value.ToLocalTime().ToShortTimeString () : "";
            periodTextView.Text = timeEntryData.StartTime.ToLocalTime().ToShortTimeString () + stopTime;
            durationTextView.Text = GetDuration (timeEntryData, Time.UtcNow).ToString (@"hh\:mm\:ss");

            return view;
        }

        private TimeSpan GetDuration (TimeEntryData data, DateTime now)
        {
            if (data.StartTime.IsMinValue ()) {
                return TimeSpan.Zero;
            }
            var duration = (data.StopTime ?? now) - data.StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            Activity.OnBackPressed ();
            return base.OnOptionsItemSelected (item);
        }
    }
}

