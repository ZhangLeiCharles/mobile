﻿using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class ClientJsonConverter
    {
        public static async Task<ClientJson> ToJsonAsync (this ClientData data)
        {
            var workspaceId = await GetRemoteId<WorkspaceData> (data.WorkspaceId);

            return new ClientJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                WorkspaceId = workspaceId,
            };
        }

        private static async Task<long> GetRemoteId<T> (Guid id)
            where T : CommonData
        {
            throw new NotImplementedException ();
        }

        private static Task<T> GetByRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static Task Put (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task Delete (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task<Guid> ResolveRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static async Task Merge (ClientData data, ClientJson json)
        {
            data.Name = json.Name;
            data.WorkspaceId = await ResolveRemoteId<WorkspaceData> (json.WorkspaceId);
            MergeCommon (data, json);
        }

        private static void MergeCommon (CommonData data, CommonJson json)
        {
            data.RemoteId = json.Id;
            data.RemoteRejected = false;
            data.DeletedAt = null;
            data.ModifiedAt = json.ModifiedAt;
            data.IsDirty = false;
        }

        public static async Task<ClientData> ToDataAsync (this ClientJson json)
        {
            var data = await GetByRemoteId<ClientData> (json.Id.Value);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new ClientData ();
                    await Merge (data, json);
                    await Put (data);
                } else if (data != null) {
                    await Delete (data);
                    data = null;
                }
            }

            return data;
        }
    }
}
