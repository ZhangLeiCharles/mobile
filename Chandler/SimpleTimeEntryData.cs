using System;

namespace Toggl.Chandler
{
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
