using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Toggl.Chandler
{
    public class TimeEntryFragment : Fragment
    {
        private readonly SimpleTimeEntryData dataObject;
        private TextView DurationTextView;
        private TextView DescriptionTextView;
        private TextView ProjectTextView;

        public TimeEntryFragment (SimpleTimeEntryData data)
        {
            dataObject = data;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntryFragment, container, false);

            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            DescriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            ProjectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);

            DurationTextView.Text =  "2h 30 min";
            DescriptionTextView.Text = dataObject.Description;
            ProjectTextView.Text = dataObject.Project;

            return view;
        }
    }
}

