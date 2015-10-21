using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe;
using System.Linq;

namespace Toggl.Joey.Wear
{
    public static class WearDataProvider
    {
        public static async Task StartStopTimeEntry ()
        {
            var manager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            if (manager.Active == null) {
                return;
            }

            var active = new TimeEntryModel (manager.Active);
            if (manager.Running) {
                await active.StopAsync ();
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.Watch);
            } else {
                await active.StartAsync ();
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.WatchStart);
            }
        }

        public static async Task ContinueTimeEntry (Guid timeEntryId)
        {
            var entryModel = new TimeEntryModel (timeEntryId);
            await entryModel.StartAsync ();
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.WatchContinue);
        }

        public static async Task<List<SimpleTimeEntryData>> ContinueTimeEntry (Guid timeEntryId)
        {
            var itemCount = 5;
            //var items = new List<SimpleTimeEntryData> (itemCount);

            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var endTime = Time.UtcNow;
            var startTime = endTime - TimeSpan.FromDays (9);


            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<Toggl.Phoebe.Net.AuthManager> ().GetUserId ();

            var baseQuery = store.Table<TimeEntryData> ()
                            .OrderBy (r => r.StartTime, false)
                            .Where (r => r.State != TimeEntryState.New
                                    && r.DeletedAt == null
                                    && r.UserId == userId);

            var entries = await baseQuery
                          .QueryAsync (r => r.StartTime <= endTime
                                       && r.StartTime > startTime);

            var groupedList =
                from entry in entries
            group entry by new {
                entry.Description,
                entry.ProjectId,
                entry.WorkspaceId
            } into newGroup
            select newGroup;



            Console.WriteLine (groupedList);
            return null;
        }

        public static bool IsGroupableWith (TimeEntryData data, TimeEntryData other)
        {
            return data.ProjectId == other.ProjectId &&
                   string.Compare (data.Description, other.Description, StringComparison.Ordinal) == 0 &&
                   data.TaskId == other.TaskId &&
                   data.UserId == other.UserId &&
                   data.WorkspaceId == other.WorkspaceId;
        }


    }
}

