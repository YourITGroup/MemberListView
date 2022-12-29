using Examine;
using Examine.Search;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Extensions;

internal static class SearchExtensions
{

    internal static IQuery And(this IQuery query, IBooleanOperation? op)
    {
        return op?.And() ?? query;
    }

    internal static IQuery Or(this IQuery query, IBooleanOperation? op)
    {
        return op?.Or() ?? query;
    }

    internal static IQuery Not(this IQuery query, IBooleanOperation? op)
    {
        return op?.Not() ?? query;
    }

    internal static IBooleanOperation BooleanField(this IQuery query, string field, bool value)
    {
        return query.GroupedOr(new[] { field }, value.ToString(), value ? "1" : "0");
    }

    internal static IQuery InitialiseQuery(this IExamineManager examineManager, string indexName = UmbracoIndexes.ExternalIndexName, string? category = null, BooleanOperation operation = BooleanOperation.And)
    {
        if (examineManager.TryGetIndex(indexName, out var index))
        {
            var searcher = index.Searcher;
            return searcher.CreateQuery(category, defaultOperation: operation);
        }
        throw new ApplicationException("The search provider is not configured correctly.");
    }

    internal static List<object> GetObjList(this object obj)
    {
        return new List<object> { obj };
    }
}