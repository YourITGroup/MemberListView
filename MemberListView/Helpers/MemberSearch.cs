using Examine;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using Umbraco.Core;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Web;
using CoreConstants = Umbraco.Core.Constants;

namespace MemberListView.Helpers
{
    public class MemberSearch
    {
        public static IEnumerable<SearchResult> PerformMemberSearch(string filter, IDictionary<string,string> filters, out int totalRecordCount,
            string memberType = "",
            int pageNumber = 0,
            int pageSize = 0,
            string orderBy = "",
            Direction orderDirection = Direction.Ascending)
        {

            var internalSearcher = ExamineManager.Instance.SearchProviderCollection[CoreConstants.Examine.InternalMemberSearcher];
            ISearchCriteria criteria = internalSearcher.CreateSearchCriteria().RawQuery(" +__IndexType:member");

            var basicFields = new List<string>() { "id", "_searchEmail", "email", "loginName" };

            var filterParameters = filters.Where(q => !string.IsNullOrWhiteSpace(q.Value));

            //build a lucene query
            if (string.IsNullOrWhiteSpace(filter) && !filterParameters.Any())
            {
                // Generic get everything...
                criteria.RawQuery("a* b* c* d* e* f* g* h* i* j* k* l* m* n* o* p* q* r* s* t* u* v* w* x* y* z*");
            }
            else
            {
                // the __nodeName will be boosted 10x without wildcards
                // then __nodeName will be matched normally with wildcards
                // the rest will be normal without wildcards
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var sb = new StringBuilder();
                    sb.Append("+(");
                    //node name exactly boost x 10
                    sb.AppendFormat("__nodeName:{0}^10.0 ", filter.ToLower());

                    //node name normally with wildcards
                    sb.AppendFormat(" __nodeName:{0}* ", filter.ToLower());

                    foreach (var field in basicFields)
                    {
                        //additional fields normally
                        sb.AppendFormat("{0}:{1} ", field, filter);
                    }
                    sb.Append(")");
                    criteria.RawQuery(sb.ToString());
                }

                // Now specific field searching. - these should be ANDed and grouped.
                foreach (var qs in filterParameters)
                {
                    string alias = qs.Key;
                    if (alias == Constants.Members.Groups)
                    {
                        // search on list with no commas.
                        alias = $"_{alias}";
                    }
                    else if (alias.StartsWith("f_"))
                    {
                        alias = qs.Key.Substring(2);
                    }

                    var values = filters[qs.Key].Split(',');
                    if (values.Length > 0)
                    {
                        criteria.GroupedOr(new[] { alias }, values);
                    }
                }
            }

            //must match index type
            if (!string.IsNullOrWhiteSpace(memberType))
            {
                criteria.NodeTypeAlias(memberType);
            }

            //// Order the results 
            //// Examine Sorting seems too unreliable, particularly on nodeName
            //if (orderDirection == Direction.Ascending) {
            //    criteria.OrderBy(orderBy.ToLower() == "name" ? "nodeName" : "email");
            //} else {
            //    criteria.OrderByDescending(orderBy.ToLower() == "name" ? "nodeName" : "email");
            //}

            var result = internalSearcher.Search(criteria);
            totalRecordCount = result.TotalItemCount;

            string orderFieldName;
            switch(orderBy.ToLower())
            {
                case "isapproved":
                    orderFieldName = CoreConstants.Conventions.Member.IsApproved;
                    break;
                case "islockedout":
                    orderFieldName = CoreConstants.Conventions.Member.IsLockedOut;
                    break;
                case "name":
                    orderFieldName = "nodeName";
                    break;
                default:
                    orderFieldName = orderBy;
                    break;
            }
            // Order the results 
            var orderedResults = orderDirection == Direction.Ascending
                ? result.OrderBy(o => o.Fields[orderFieldName])
                : result.OrderByDescending(o => o.Fields[orderFieldName]);

            if (pageSize > 0)
            {
                int skipCount = (pageNumber > 0 && pageSize > 0) ? Convert.ToInt32((pageNumber - 1) * pageSize) : 0;
                if (result.TotalItemCount < skipCount)
                {
                    skipCount = result.TotalItemCount / pageSize;
                }

                return orderedResults.Skip(skipCount).Take(pageSize);
            }
            return orderedResults;
        }
    }
}