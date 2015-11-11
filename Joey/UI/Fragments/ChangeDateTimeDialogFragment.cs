using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text.Format;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeDateTimeDialogFragment : BaseDialogFragment, DatePicker.IOnDateChangedListener
    {
        public interface IChangeDateTime
        {
            void OnChangeDateTime (TimeSpan timeDiff, string dialogTag);
        }

        private static readonly string InitialTimeArgument = "com.toggl.timer.initialtime";
        private static readonly string DialogTitleArgument = "com.toggl.timer.dialogtitle";

        private bool viewsSetup;
        private RadioGroup tabsRadioButton;
        private RadioButton timeTabRadioButton;
        private RadioButton dateTabRadioButton;
        private TimePicker timePicker;
        private DatePicker datePicker;
        private IChangeDateTime changeDateTimeHandler;

        private DateTime InitialTime
        {
            get { return new DateTime (Arguments.GetLong (InitialTimeArgument)); }
        }

        private string DialogTitle
        {
            get { return Arguments.GetString (DialogTitleArgument); }
        }

        protected ChangeDateTimeDialogFragment ()
        {
        }

        protected ChangeDateTimeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static ChangeDateTimeDialogFragment NewInstance (DateTime initialTime, string dialogTitle)
        {
            var fragment = new ChangeDateTimeDialogFragment ();

            var args = new Bundle ();

            // Save time without second or millisecond component
            // cause neither of this components will be edited.
            args.PutLong (InitialTimeArgument, GetTrunkedTime (initialTime).Ticks);
            args.PutString (DialogTitleArgument, dialogTitle);
            fragment.Arguments = args;

            return fragment;
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            var view = LayoutInflater.From (Activity)
                       .Inflate (Resource.Layout.ChangeTimeEntryStartTimeDialogFragment, null);
            tabsRadioButton = view.FindViewById<RadioGroup> (Resource.Id.TabsRadioGroup);
            timeTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.TimeTabRadioButton).SetFont (Font.Roboto);
            dateTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.DateTabRadioButton).SetFont (Font.Roboto);
            timePicker = view.FindViewById<TimePicker> (Resource.Id.TimePicker);
            datePicker = view.FindViewById<DatePicker> (Resource.Id.DatePicker);

            // WORKAROUND: Without these two lines the app will crash on rotation. See #258.
            timePicker.SaveFromParentEnabled = false;
            timePicker.SaveEnabled = true;

            tabsRadioButton.CheckedChange += OnTabsRadioGroupCheckedChange;

            SetupViews ();
            Rebind ();

            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (DialogTitle)
            .SetView (view)
            .SetPositiveButton (Resource.String.ChangeTimeEntryStartTimeDialogOk, OnOkButtonClicked)
            .Create ();

            return dia;
        }

        private void SetupViews ()
        {
            if (viewsSetup || timePicker == null) {
                return;
            }

            viewsSetup = true;

            var time = InitialTime;
            var date = InitialTime.Date;

            timePicker.SetIs24HourView (new Java.Lang.Boolean (
                                            DateFormat.Is24HourFormat (ServiceContainer.Resolve<Context> ())));
            timePicker.CurrentHour = new Java.Lang.Integer (time.Hour);
            timePicker.CurrentMinute = new Java.Lang.Integer (time.Minute);
            timePicker.TimeChanged += OnTimePickerTimeChanged;

            datePicker.Init (date.Year, date.Month - 1, date.Day, this);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
                var userData = ServiceContainer.Resolve<AuthManager> ().User;
                datePicker.FirstDayOfWeek =  ((int) userData.StartOfWeek) + 1; // FirstDayOfWeek must be between 1 - 7, Our days go from 0 - 6.
            }
        }

        public ChangeDateTimeDialogFragment SetOnChangeTimeHandler (IChangeDateTime handler)
        {
            changeDateTimeHandler = handler;
            return this;
        }

        private void OnTabsRadioGroupCheckedChange (object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            Rebind ();
        }

        private void Rebind ()
        {
            if (tabsRadioButton == null) {
                return;
            }

            if (tabsRadioButton.CheckedRadioButtonId == timeTabRadioButton.Id) {
                timeTabRadioButton.SetTextSize (ComplexUnitType.Dip, 18);
                dateTabRadioButton.SetTextSize (ComplexUnitType.Dip, 14);
                timePicker.Visibility = ViewStates.Visible;
                datePicker.Visibility = ViewStates.Gone;
            } else {
                timeTabRadioButton.SetTextSize (ComplexUnitType.Dip, 14);
                dateTabRadioButton.SetTextSize (ComplexUnitType.Dip, 18);
                timePicker.Visibility = ViewStates.Gone;
                datePicker.Visibility = ViewStates.Visible;
            }

            var dateTime = DateTimeFromPicker;
            timeTabRadioButton.Text = dateTime.ToDeviceTimeString ();
            dateTabRadioButton.Text = dateTime.ToDeviceDateString ();
        }

        private void OnTimePickerTimeChanged (object sender, TimePicker.TimeChangedEventArgs e)
        {
            Rebind ();
        }

        void DatePicker.IOnDateChangedListener.OnDateChanged (DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            Rebind ();
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            if (changeDateTimeHandler != null) {
                changeDateTimeHandler.OnChangeDateTime (DateTimeFromPicker - InitialTime, Tag);
            }
        }

        private DateTime DateTimeFromPicker
        {
            get {
                return DateTime.SpecifyKind (datePicker.DateTime
                                             .AddHours (timePicker.CurrentHour.IntValue ())
                                             .AddMinutes (timePicker.CurrentMinute.IntValue ()), DateTimeKind.Unspecified);
            }
        }

        private static DateTime GetTrunkedTime (DateTime time)
        {
            return new DateTime (time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        }
    }
}
