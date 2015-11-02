using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryDurationDialogFragment : BaseDialogFragment
    {
        public interface IChangeDuration
        {
            void OnChangeDuration (TimeSpan newDuration);
        }

        private static readonly string StartTimeId = "com.toggl.timer.start_time";
        private static readonly string StopTimeId = "com.toggl.timer.stop_time";
        private const string NewDurationHoursKey = "com.toggl.timer.new_duration_hours";
        private const string NewDurationMinutesKey = "com.toggl.timer.new_duration_mins";
        private const string DigitsEnteredKey = "com.toggl.timer.digits_entered";

        private TextView durationTextView;
        private ImageButton deleteImageButton;
        private Button add5Button;
        private Button add30Button;
        private Button okButton;

        private readonly Button[] numButtons = new Button[10];
        private Duration oldDuration;
        private Duration newDuration;
        private int digitsEntered;
        private IChangeDuration onChangeDurationHandler;

        private DateTime StartTime
        {
            get { return new DateTime (Arguments.GetLong (StartTimeId)); }
        }

        private DateTime StopTime
        {
            get { return new DateTime (Arguments.GetLong (StopTimeId)); }
        }

        public ChangeTimeEntryDurationDialogFragment ()
        {
        }

        public ChangeTimeEntryDurationDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static ChangeTimeEntryDurationDialogFragment NewInstance (DateTime stopTime, DateTime startTime)
        {
            var fragment = new ChangeTimeEntryDurationDialogFragment ();

            var args = new Bundle ();
            args.PutLong (StopTimeId, stopTime.Ticks);
            args.PutLong (StartTimeId, startTime.Ticks);
            fragment.Arguments = args;

            return fragment;
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (savedInstanceState != null) {
                digitsEntered = savedInstanceState.GetInt (DigitsEnteredKey, digitsEntered);
                newDuration = new Duration (
                    savedInstanceState.GetInt (NewDurationHoursKey, newDuration.Hours),
                    savedInstanceState.GetInt (NewDurationMinutesKey, newDuration.Minutes));
            }

            oldDuration = GetDuration ();
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);

            outState.PutInt (DigitsEnteredKey, digitsEntered);
            outState.PutInt (NewDurationHoursKey, newDuration.Hours);
            outState.PutInt (NewDurationMinutesKey, newDuration.Minutes);
        }

        public ChangeTimeEntryDurationDialogFragment SetChangeDurationHandler (IChangeDuration handler)
        {
            onChangeDurationHandler = handler;
            return this;
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            var view = LayoutInflater.From (Activity)
                       .Inflate (Resource.Layout.ChangeTimeEntryDurationDialogFragment, null);
            durationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.Roboto);
            deleteImageButton = view.FindViewById<ImageButton> (Resource.Id.DeleteImageButton);
            add5Button = view.FindViewById<Button> (Resource.Id.Add5Button).SetFont (Font.RobotoLight);
            add30Button = view.FindViewById<Button> (Resource.Id.Add30Button).SetFont (Font.RobotoLight);
            numButtons [0] = view.FindViewById<Button> (Resource.Id.Num0Button).SetFont (Font.RobotoLight);
            numButtons [1] = view.FindViewById<Button> (Resource.Id.Num1Button).SetFont (Font.RobotoLight);
            numButtons [2] = view.FindViewById<Button> (Resource.Id.Num2Button).SetFont (Font.RobotoLight);
            numButtons [3] = view.FindViewById<Button> (Resource.Id.Num3Button).SetFont (Font.RobotoLight);
            numButtons [4] = view.FindViewById<Button> (Resource.Id.Num4Button).SetFont (Font.RobotoLight);
            numButtons [5] = view.FindViewById<Button> (Resource.Id.Num5Button).SetFont (Font.RobotoLight);
            numButtons [6] = view.FindViewById<Button> (Resource.Id.Num6Button).SetFont (Font.RobotoLight);
            numButtons [7] = view.FindViewById<Button> (Resource.Id.Num7Button).SetFont (Font.RobotoLight);
            numButtons [8] = view.FindViewById<Button> (Resource.Id.Num8Button).SetFont (Font.RobotoLight);
            numButtons [9] = view.FindViewById<Button> (Resource.Id.Num9Button).SetFont (Font.RobotoLight);

            deleteImageButton.Click += OnDeleteImageButtonClick;
            deleteImageButton.LongClick += OnDeleteImageButtonLongClick;
            foreach (var numButton in numButtons) {
                numButton.Click += OnNumButtonClick;
            }
            add5Button.Click += OnAdd5ButtonClick;
            add30Button.Click += OnAdd30ButtonClick;

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.ChangeTimeEntryDurationDialogTitle)
                   .SetView (view)
                   .SetNegativeButton (Resource.String.ChangeTimeEntryDurationDialogCancel, (IDialogInterfaceOnClickListener)null)
                   .SetPositiveButton (Resource.String.ChangeTimeEntryDurationDialogOk, OnOkClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // AlertDialog buttons aren't available earlier:
            var dia = (AlertDialog)Dialog;
            okButton = dia.GetButton ((int)DialogButtonType.Positive);

            Rebind ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        private void Rebind ()
        {
            if (durationTextView == null) {
                return;
            }

            var durationShown = digitsEntered > 0 ? newDuration : oldDuration;
            var durationText = durationShown.ToString ();
            var durationSpannable = new SpannableString (durationText);
            durationSpannable.SetSpan (
                new ForegroundColorSpan (Color.LightGray),
                0, durationSpannable.Length (),
                SpanTypes.InclusiveExclusive);
            if (digitsEntered > 0) {
                // Divider
                var sepIdx = durationText.IndexOf (":", StringComparison.Ordinal);
                if (sepIdx >= 0) {
                    durationSpannable.SetSpan (
                        new ForegroundColorSpan (Color.Black),
                        sepIdx, sepIdx + 1,
                        SpanTypes.InclusiveExclusive);
                }
                // Color entered minutes
                durationSpannable.SetSpan (
                    new ForegroundColorSpan (Color.Black),
                    durationText.Length - (Math.Min (digitsEntered, 2)), durationText.Length,
                    SpanTypes.InclusiveExclusive);
                // Color entered hours
                if (digitsEntered > 2) {
                    durationSpannable.SetSpan (
                        new ForegroundColorSpan (Color.Black),
                        2 - (digitsEntered - 2), 2,
                        SpanTypes.InclusiveExclusive);
                }
            }
            durationTextView.SetText (durationSpannable, TextView.BufferType.Spannable);

            int num = 0;
            foreach (var numButton in numButtons) {
                numButton.Enabled = digitsEntered < 4;
                num += 1;
            }

            deleteImageButton.Enabled = digitsEntered > 0;
            add5Button.Enabled = newDuration.IsValid && newDuration.AddMinutes (5).IsValid;
            add30Button.Enabled = newDuration.IsValid && newDuration.AddMinutes (30).IsValid;
            okButton.Enabled = digitsEntered > 0 && newDuration.IsValid;
        }

        private void OnDeleteImageButtonClick (object sender, EventArgs e)
        {
            if (digitsEntered < 1) {
                return;
            }
            newDuration = newDuration.RemoveDigit ();
            if (newDuration == Duration.Zero) {
                // Clear all entered digits, is no value remains
                digitsEntered = 0;
            } else {
                digitsEntered -= 1;
            }

            Rebind ();
        }

        private void OnDeleteImageButtonLongClick (object sender, View.LongClickEventArgs e)
        {
            newDuration = Duration.Zero;
            digitsEntered = 0;
            Rebind ();
        }

        private void OnNumButtonClick (object sender, EventArgs e)
        {
            if (digitsEntered > 3) {
                return;
            }

            int num = Array.IndexOf (numButtons, sender);
            if (num < 0) {
                return;
            }

            newDuration = newDuration.AppendDigit (num);
            digitsEntered += 1;
            Rebind ();
        }

        private void OnAdd5ButtonClick (object sender, EventArgs e)
        {
            var duration = newDuration.AddMinutes (5);
            if (!duration.IsValid) {
                return;
            }

            newDuration = duration;
            digitsEntered = 4;
            Rebind ();
        }

        private void OnAdd30ButtonClick (object sender, EventArgs e)
        {
            var duration = newDuration.AddMinutes (30);
            if (!duration.IsValid) {
                return;
            }

            newDuration = duration;
            digitsEntered = 4;
            Rebind ();
        }

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
            var duration = new TimeSpan (newDuration.Hours, newDuration.Minutes, 0);
            if (onChangeDurationHandler != null) {
                onChangeDurationHandler.OnChangeDuration (duration);
            }
        }

        private TimeSpan GetDuration ()
        {
            if (StartTime.IsMinValue ()) {
                return TimeSpan.Zero;
            }

            var duration = StopTime - StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }
    }
}

