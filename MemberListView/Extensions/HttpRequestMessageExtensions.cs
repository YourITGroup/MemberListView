using Microsoft.AspNetCore.Http;

namespace MemberListView.Extensions;

internal static partial class HttpRequestMessageExtensions
{
    internal static IEnumerable<string>? GetColumns(this HttpRequest request)
    {
        return request.Query["columns"]
                    .FirstOrDefault()
                    ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }


    internal static string? GetMemberType(this HttpRequest request)
    {
        return request.Query[Constants.Members.MemberType].FirstOrDefault();
    }

    internal static bool? GetIsLockedOut(this HttpRequest request)
    {
        return int.TryParse(request.Query[Constants.Members.MemberLockedOut].FirstOrDefault(), out int boolValue) ? boolValue == 1 : null;
    }

    internal static bool? GetIsApproved(this HttpRequest request)
    {
        return int.TryParse(request.Query[Constants.Members.MemberApproved].FirstOrDefault(), out int boolValue) ? boolValue == 1 : null;
    }

    internal static IEnumerable<int> GetGroups(this HttpRequest request)
    {
        var groups = request.Query[Constants.Members.Groups].FirstOrDefault();
        if (groups is null)
        {
            yield break;
        }
        foreach (var id in groups.Split(new[] { ',' }))
        {
            if (int.TryParse(id, out int groupId))
            {
                yield return groupId;
            }
        }
    }

    internal static IEnumerable<string> GetMemberIds(this HttpRequest request)
    {

        var ids = request.Query[Constants.Members.Ids].FirstOrDefault();
        if (ids is null)
        {
            yield break;
        }
        foreach (var id in ids.Split(new[] { ',' }))
        {
            yield return id;
        }
    }

    internal static Dictionary<string, string> GetFilters(this HttpRequest request)
    {

        Dictionary<string, string> filters = new();

        foreach (var kvp in request.Query
                    .Where(q => (q.Key.StartsWith("f_") || q.Key == Constants.Members.Groups) && !string.IsNullOrWhiteSpace(q.Value)))
        {
            filters.Add(kvp.Key, kvp.Value!);
        }

        return filters;
    }
}