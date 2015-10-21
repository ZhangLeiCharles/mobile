using System;

namespace Toggl.Joey.Wear
{
    public static class Common
    {
        public static readonly string StartTimeEntryPath = "/toggl/wear/start";
        public static readonly string StopTimeEntryPath = "/toggl/wear/stop";
        public static readonly string RestartTimeEntryPath = "/toggl/wear/restart";
        public static readonly string TimeEntryListPath = "/toggl/wear/data";

        public static readonly string TimeEntryListKey = "time_entry_list_key";
    }

    public class SimpleTimeEntryData
    {
        public Guid Id { get; set; }

        public bool IsRunning { get; set; }

        public string Description { get; set; }

        public string Project { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? StopTime { get; set; }


    }
}

