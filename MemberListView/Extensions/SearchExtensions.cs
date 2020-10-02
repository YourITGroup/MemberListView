using Examine.Search;

namespace MemberListView.Extensions
{
    internal static class SearchExtensions
    {

        internal static IQuery And(this IQuery query, IBooleanOperation op)
        {
            return op?.And() ?? query;
        }

        internal static IQuery Or(this IQuery query, IBooleanOperation op)
        {
            return op?.Or() ?? query;
        }

        internal static IQuery Not(this IQuery query, IBooleanOperation op)
        {
            return op?.Not() ?? query;
        }

        internal static IBooleanOperation BooleanField(this IQuery query, string field, bool value)
        {
            return query.GroupedOr(new[] { field }, value.ToString(), value ? "1" : "0");
        }
    }
}