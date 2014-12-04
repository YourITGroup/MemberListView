using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;

namespace MemberListView.Extensions
{
    public static class SearchExtensions
    {
        public static IEnumerable<EntityBasic> ExamineSearchRaw(this EntityController controller, string query, UmbracoEntityTypes entityType)
        {
            string type;
            var searcher = Constants.Examine.InternalSearcher;            
            var fields = new[] { "id", "bodyText" };
            
            //TODO: WE should really just allow passing in a lucene raw query
            switch (entityType)
            {
                case UmbracoEntityTypes.Member:
                    searcher = Constants.Examine.InternalMemberSearcher;
                    type = "member";
                    fields = new[] { "id", "email", "loginName"};
                    break;
                case UmbracoEntityTypes.Media:
                    type = "media";
                    break;
                case UmbracoEntityTypes.Document:
                    type = "content";
                    break;
                default:
                    throw new NotSupportedException("The " + typeof(EntityController) + " currently does not support searching against object type " + entityType);                    
            }

            var internalSearcher = ExamineManager.Instance.SearchProviderCollection[searcher];

            //build a lucene query:
            // the __nodeName will be boosted 10x without wildcards
            // then __nodeName will be matched normally with wildcards
            // the rest will be normal without wildcards
            var sb = new StringBuilder();
            
            //node name exactly boost x 10
            sb.Append("+(__nodeName:");
            sb.Append(query.ToLower());
            sb.Append("^10.0 ");

            //node name normally with wildcards
            sb.Append(" __nodeName:");            
            sb.Append(query.ToLower());
            sb.Append("* ");

            foreach (var f in fields)
            {
                //additional fields normally
                sb.Append(f);
                sb.Append(":");
                sb.Append(query);
                sb.Append(" ");
            }

            //must match index type
            sb.Append(") +__IndexType:");
            sb.Append(type);

            var raw = internalSearcher.CreateSearchCriteria().RawQuery(sb.ToString());
            
            var result = internalSearcher.Search(raw);

            switch (entityType)
            {
                case UmbracoEntityTypes.Member:
                    return MemberFromSearchResults(result);
                case UmbracoEntityTypes.Media:
                    return MediaFromSearchResults(result);                    
                case UmbracoEntityTypes.Document:
                    return ContentFromSearchResults(result);
                default:
                    throw new NotSupportedException("The " + typeof(EntityController) + " currently does not support searching against object type " + entityType);
            }
        }
    }
}