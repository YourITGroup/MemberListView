using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif
namespace MemberListView.Extensions
{
    internal static partial class HttpRequestMessageExtensions
    {
#if NET5_0_OR_GREATER
        internal static IEnumerable<string> GetColumns(this HttpRequest request)
        {
            return request.Query["columns"]
                        .FirstOrDefault()
                        ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }


        internal static string GetMemberTypeFromQuery(this HttpRequest request)
        {
            return request.Query[Constants.Members.MemberType].FirstOrDefault();
        }

        internal static bool? GetIsLockedOut(this HttpRequest request)
        {
            return int.TryParse(request.Query[Constants.Members.MemberType].FirstOrDefault(), out int boolValue) ? boolValue == 1 : null;
        }

        internal static bool? GetIsApproved(this HttpRequest request)
        {
            return int.TryParse(request.Query[Constants.Members.MemberApproved].FirstOrDefault(), out int boolValue) ? boolValue == 1 : null;
        }

        internal static IEnumerable<int> GetGroupsFromQuery(this HttpRequest request)
        {
            var groups = request.Query[Constants.Members.Groups].FirstOrDefault();
            if (groups == null)
            {
                yield break;
            }
            foreach(var id in groups?.Split(new[] { ','}))
            {
                if (int.TryParse(id, out int groupId))
                {
                    yield return groupId;
                }
            }
        }

        internal static Dictionary<string, string> GetFilters(this HttpRequest request)
        {

            Dictionary<string, string> filters = new();

            foreach (var kvp in request.Query
                        .Where(q => (q.Key.StartsWith("f_") || q.Key == Constants.Members.Groups) && !string.IsNullOrWhiteSpace(q.Value)))
            {
                filters.Add(kvp.Key, kvp.Value);
            }

            return filters;
        }
#endif
    }
}