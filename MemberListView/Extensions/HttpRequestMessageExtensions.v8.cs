using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
namespace MemberListView.Extensions
{
    internal static partial class HttpRequestMessageExtensions
    {
#if !NET5_0_OR_GREATER
        internal static IEnumerable<string> GetColumns(this HttpRequestMessage request)
        {
            return request.GetQueryNameValuePairs()
                        .Where(q => q.Key == "columns")
                        .Select(q => q.Value)
                        .FirstOrDefault()
                        ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }


        internal static string GetMemberTypeFromQuery(this HttpRequestMessage request)
        {
            return request.GetQueryNameValuePairs()
                        .FirstOrDefault(q => q.Key == Constants.Members.MemberType).Value;
        }

        internal static bool? GetIsLockedOut(this HttpRequestMessage request)
        {
            return int.TryParse(request.GetQueryNameValuePairs()
                        .FirstOrDefault(q => q.Key == Constants.Members.MemberLockedOut).Value, out int boolValue) ? boolValue == 1 : (bool?)null;
        }

        internal static bool? GetIsApproved(this HttpRequestMessage request)
        {
            return int.TryParse(request.GetQueryNameValuePairs()
                        .FirstOrDefault(q => q.Key == Constants.Members.MemberApproved).Value, out int boolValue) ? boolValue == 1 : (bool?)null;
        }

        internal static IEnumerable<int> GetGroupsFromQuery(this HttpRequestMessage request)
        {
            var groups = request.GetQueryNameValuePairs()
                        .FirstOrDefault(q => q.Key == Constants.Members.Groups).Value;
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

        internal static Dictionary<string, string> GetFilters(this HttpRequestMessage request)
        {

            Dictionary<string, string> filters = new Dictionary<string, string>();

            foreach (var kvp in request.GetQueryNameValuePairs()
                        .Where(q => (q.Key.StartsWith("f_") || q.Key == Constants.Members.Groups) && !string.IsNullOrWhiteSpace(q.Value)))
            {
                filters.Add(kvp.Key, kvp.Value);
            }

            return filters;
        }
#endif
    }
}