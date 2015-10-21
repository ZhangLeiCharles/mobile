using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Support.Wearable.Views;

namespace Toggl.Chandler
{
    public class TimeEntriesPagerAdapter : FragmentGridPagerAdapter
    {
        private Context mContext;
        private List<SimpleTimeEntryData> timeEntries = new List<SimpleTimeEntryData> ();

        public TimeEntriesPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
        {
            mContext = ctx.ApplicationContext;
            GenerateEntries ();
        }

        private void GenerateEntries()
        {
            var entry1 = new SimpleTimeEntryData();
            entry1.Description = "Dev wear app";
            entry1.Project = "Madrid challenge";
            timeEntries.Add (entry1);

            var entry2 = new SimpleTimeEntryData();
            entry2.Description = "Mobile bug fix";
            entry2.Project = "Mobile";
            timeEntries.Add (entry2);

            var entry3 = new SimpleTimeEntryData();
            entry3.Description = "Mezcal testing";
            entry3.Project = "Madrid challenge";
            timeEntries.Add (entry3);

            var entry4 = new SimpleTimeEntryData();
            entry4.Description = "Breakfast";
            entry4.Project = "Fuencaral";
            timeEntries.Add (entry4);
        }

        #region implemented abstract members of GridPagerAdapter

        public override int GetColumnCount (int p0)
        {
            return timeEntries.Count;
        }

        public override int RowCount
        {
            get {
                return 1;
            }
        }

        #endregion

        #region implemented abstract members of FragmentGridPagerAdapter

        public override Fragment GetFragment (int row, int col)
        {
            var fragment =  new TimeEntryFragment (timeEntries[col]);
            return fragment;
        }

        #endregion
    }
}
