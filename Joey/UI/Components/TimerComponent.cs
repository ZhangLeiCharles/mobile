using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Activity = Android.Support.V4.App.FragmentActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Components
{
    public class TimerComponent
    {
        private static readonly string LogTag = "TimerComponent";

        protected TextView DurationTextView { get; private set; }
        protected TextView ProjectTextView { get; private set; }
        protected TextView DescriptionTextView { get; private set; }
        protected TextView TimerTitleTextView { get; private set; }

        public View Root { get; private set; }
        public ImageButton AddManualEntry { get; private set; }
        public LogTimeEntriesViewModel ViewModel { get; private set; }

        private Binding<bool, bool> isRunningBinding;
        private Binding<string, string> descBinding, durationBinding, projectBinding;

        private Activity activity;
        private bool hide;
        private bool isRunning;

        public bool IsRunning
        {
            get {
                return isRunning;
            } set {
                isRunning = value;
                AddManualEntry.Visibility = isRunning ? ViewStates.Gone : ViewStates.Visible;
                TimerTitleTextView.Visibility = isRunning ? ViewStates.Gone : ViewStates.Visible;
                ProjectTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
                DescriptionTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
                DurationTextView.Visibility = isRunning ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        public bool Hide
        {
            get { return hide; }
            set {
                hide = value;
                Root.Visibility = Hide ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        public void OnCreate (Activity activity)
        {
            this.activity = activity;
            Root = LayoutInflater.From (activity).Inflate (Resource.Layout.TimerComponent, null);

            DurationTextView = Root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.RobotoLight);
            TimerTitleTextView = Root.FindViewById<TextView> (Resource.Id.TimerTitleTextView);
            ProjectTextView = Root.FindViewById<TextView> (Resource.Id.ProjectTextView);
            DescriptionTextView = Root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.RobotoLight);
            AddManualEntry = Root.FindViewById<ImageButton> (Resource.Id.AddManuallyButton);

            AddManualEntry.Click += CreateTimeEntryManually;
            IsRunning = false;
        }

        public void SetViewModel (LogTimeEntriesViewModel viewModel)
        {
            ViewModel = viewModel;

            // TODO: investigate why WhenSourceChanges doesn't work. :(
            isRunningBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning, () => IsRunning);
            durationBinding = this.SetBinding (() => ViewModel.Duration, () => DurationTextView.Text);
            descBinding = this.SetBinding (() => ViewModel.Description, () => DescriptionTextView.Text)
                          .ConvertSourceToTarget (desc => desc != string.Empty ? desc : activity.ApplicationContext.Resources.GetText (Resource.String.TimerComponentNoDescription));
            projectBinding = this.SetBinding (() => ViewModel.ProjectName, () => ProjectTextView.Text)
                             .ConvertSourceToTarget (proj => proj != string.Empty ? proj : activity.ApplicationContext.Resources.GetText (Resource.String.TimerComponentNoProject));
        }

        public void DetachBindind ()
        {
            isRunningBinding.Detach ();
            durationBinding.Detach ();
            descBinding.Detach ();
            projectBinding.Detach ();
        }

        private void CreateTimeEntryManually (object sender, EventArgs e)
        {
            OpenTimeEntryEdit (new TimeEntryModel());
        }

        private void OpenTimeEntryEdit (ITimeEntryModel model)
        {
            var i = new Intent (activity, typeof (EditTimeEntryActivity));
            i.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, new List<string> {model.Id.ToString ()});
            activity.StartActivity (i);
        }
    }
}