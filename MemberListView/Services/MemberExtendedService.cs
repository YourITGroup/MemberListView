using Examine;
using Examine.Search;
using MemberListView.Extensions;
using MemberListView.Utility;
using Microsoft.Extensions.Logging;
using System.Text;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Services
{
    public class MemberExtendedService : MemberService, IMemberExtendedService
    {
        private readonly ILogger<MemberExtendedService> logger;
        private readonly IMemberGroupService memberGroupService;
        private readonly IExamineManager examineManager;
        private readonly IUmbracoContextFactory umbracoContextFactory;

        public MemberExtendedService(ICoreScopeProvider provider,
                                     ILoggerFactory loggerFactory,
                                     IEventMessagesFactory eventMessagesFactory,
                                     IMemberGroupService memberGroupService,
                                     IMemberRepository memberRepository,
                                     IMemberTypeRepository memberTypeRepository,
                                     IMemberGroupRepository memberGroupRepository,
                                     IAuditRepository auditRepository,
                                     IExamineManager examineManager,
                                     IUmbracoContextFactory umbracoContextFactory)
            : base(provider,
                   loggerFactory,
                   eventMessagesFactory,
                   memberGroupService,
                   memberRepository,
                   memberTypeRepository,
                   memberGroupRepository,
                   auditRepository)
        {
            logger = loggerFactory.CreateLogger<MemberExtendedService>();
            this.memberGroupService = memberGroupService;
            this.examineManager = examineManager;
            this.umbracoContextFactory = umbracoContextFactory;
        }

        /// <inheritdoc />
        public IEnumerable<IMember> GetPage(long pageIndex, int pageSize, out long totalRecords, string orderBy,
                                     Direction orderDirection, bool orderBySystemField, string? memberTypeAlias,
                                     string filter = "", IEnumerable<string>? ids = null, IEnumerable<int>? groups = null,
                                     IDictionary<string, string>? additionalFilters = null, bool? isApproved = null,
                                     bool? isLockedOut = null)
        {

            // Use the database method unless we have complex search.
            totalRecords = 0;

            //if ((ids?.Any() ?? false) ||
            //    (groups?.Any() ?? false) ||
            //    isApproved.HasValue ||
            //    isLockedOut.HasValue ||
            //    (additionalFilters?.Any() ?? false))
            //{
            return PerformExamineSearch(pageIndex, pageSize, out totalRecords, orderBy, orderDirection,
                                        memberTypeAlias, ids, groups, filter, additionalFilters, isApproved,
                                        isLockedOut)
                            .Select(x => GetById(int.Parse(x.Id))).WhereNotNull();
            //}
            //else
            //{
            //    return GetAll(pageIndex, pageSize, out totalRecords, orderBy, orderDirection, orderBySystemField, memberTypeAlias, filter);
            //}
        }

        /// <inheritdoc />
        public IEnumerable<MemberExportModel> GetForExport(string orderBy, Direction orderDirection, bool orderBySystemField,
                                                    string? memberTypeAlias, string filter = "",
                                                    IEnumerable<string>? ids = null,
                                                    IEnumerable<int>? groups = null,
                                                    IEnumerable<string>? includedColumns = null,
                                                    IDictionary<string, string>? additionalFilters = null,
                                                    bool? isApproved = null, bool? isLockedOut = null)
        {
            using ICoreScope scope = ScopeProvider.CreateCoreScope(autoComplete: true);
            scope.ReadLock(Locks.MemberTree);

            const int pageSize = 500;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                var items = GetPage(page++, pageSize, out total, orderBy, orderDirection, orderBySystemField,
                                    memberTypeAlias, filter, ids, groups, additionalFilters, isApproved, isLockedOut);

                foreach (var item in items)
                {
                    var mapped = MapToExportModel(item, includedColumns);
                    if (mapped is not null)
                    {
                        yield return mapped;
                    }
                }
            }

        }

        private MemberExportModel? MapToExportModel(IMember record, IEnumerable<string>? includedColumns)
        {
            var model = ExportMember(record.Key);
            if (model is null)
            {
                return null;
            }

            //Clean up unwanted properties.
            if (includedColumns is not null)
            {
                var properties = model.Properties.ToArray();
                foreach (var property in properties)
                {
                    if (!includedColumns.Contains(property.Alias))
                    {
                        model.Properties.Remove(property);
                    }
                }
            }

            // Go through the properties looking for UDIs for conversion to their names / urls...
            var umbracoContext = umbracoContextFactory.EnsureUmbracoContext()?.UmbracoContext;
            foreach (var property in model.Properties)
            {
                if (property.Value is string stringVal && stringVal.Contains("umb"))
                {
                    var udis = MemberDataUdiParser.FindUdis(stringVal).ToList();
                    foreach (var udi in udis)
                    {
                        if (udi.EntityType == UdiEntityType.Document)
                        {
                            var node = umbracoContext?.Content?.GetById(udi);
                            if (node != null)
                            {
                                stringVal = stringVal.Replace(udi.ToString(), node.Name);
                            }
                        }
                        else if (udi.EntityType == UdiEntityType.Media)
                        {
                            var media = umbracoContext?.Media?.GetById(udi);
                            if (media != null)
                            {
                                stringVal = stringVal.Replace(udi.ToString(), media.MediaUrl());
                            }
                        }
                    }

                    property.Value = stringVal;
                }
            }
            return model;
        }

        private IEnumerable<ISearchResult> PerformExamineSearch(long pageIndex,
                                                                int pageSize,
                                                                out long totalRecords,
                                                                string orderBy,
                                                                Direction orderDirection,
                                                                string? memberTypeAlias,
                                                                IEnumerable<string>? ids,
                                                                IEnumerable<int>? groups,
                                                                string filter,
                                                                IDictionary<string, string>? additionalFilters,
                                                                bool? isApproved,
                                                                bool? isLockedOut)
        {
            if (InitialiseMemberQuery() is not IQuery query)
            {
                totalRecords = 0;
                return Enumerable.Empty<ISearchResult>();
            }

            IBooleanOperation? op = null;
            if (memberTypeAlias is not null && !memberTypeAlias.IsNullOrWhiteSpace())
            {
                op = query.NodeTypeAlias(memberTypeAlias);
            }

            if (ids is not null && ids.Any())
            {
                op = query.And(op).GroupedOr(new[] { UmbracoExamineFieldNames.NodeKeyFieldName }, ids.ToArray());
            }

            if (groups?.Any() ?? false)
            {
                // Get group names from ids.
                var groupNames = memberGroupService.GetByIds(groups).Select(x => x.Name);
                if (groupNames is not null && groupNames.Any())
                {
                    // Keywords need to be all lowercase for Examine 2.0
                    op = query.And(op).GroupedOr(new[] { Constants.Members.Groups }, groupNames.WhereNotNull().Select(g => new ExamineValue(Examineness.Escaped, g.ToLower())).Cast<IExamineValue>().ToArray());
                }
            }

            if (isApproved.HasValue)
            {
                op = query.And(op).BooleanField(nameof(Member.IsApproved), isApproved.Value);
            }

            if (isLockedOut.HasValue)
            {
                op = query.And(op).BooleanField(nameof(IMember.IsLockedOut), isLockedOut.Value);
            }

            var basicFields = new List<string>() { MemberExamineIndexFieldNames.Id, UmbracoExamineFieldNames.ItemIdFieldName, UmbracoExamineFieldNames.NodeKeyFieldName, MemberExamineIndexFieldNames.Email, MemberExamineIndexFieldNames.LoginName };

            var filterParameters = additionalFilters?.Where(q => q.Key.StartsWith("f_") && !string.IsNullOrWhiteSpace(q.Value));

            //build a lucene query
            if (op is null && string.IsNullOrWhiteSpace(filter) && !(filterParameters?.Any() ?? false))
            {
                // Generic get everything (theoretically we shouldn't even get here)...
                op = query.NativeQuery("a* b* c* d* e* f* g* h* i* j* k* l* m* n* o* p* q* r* s* t* u* v* w* x* y* z*");
            }
            else
            {
                if (!filter.IsNullOrWhiteSpace())
                {
                    // the __nodeName will be boosted 10x without wildcards
                    // then __nodeName will be matched normally with wildcards
                    // the rest will be normal without wildcards
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        var sb = new StringBuilder();
                        sb.Append("+(");
                        //node name exactly boost x 10
                        sb.Append($"{UmbracoExamineFieldNames.NodeNameFieldName}:{filter.ToLower()}^10.0 ");

                        //node name normally with wildcards
                        sb.Append($"{UmbracoExamineFieldNames.NodeNameFieldName}:{filter.ToLower()}* ");

                        foreach (var field in basicFields)
                        {
                            //additional fields normally
                            sb.Append($"{field}:{filter} ");
                        }
                        sb.Append(')');
                        op = query.And(op).NativeQuery(sb.ToString());
                    }
                }


                // Now specific field searching. - these should be ANDed and grouped.
                if (filterParameters is not null)
                {
                    foreach (var qs in filterParameters)
                    {
                        string alias = qs.Key;
                        if (alias.StartsWith("f_"))
                        {
                            alias = qs.Key[2..];
                        }

                        var values = qs.Value.Split(',');
                        if (values.Length > 0)
                        {
                            op = query.And(op).GroupedOr(new[] { alias }, values);
                        }
                    }
                }
            }


            //// Order the results 
            IOrdering ordering;
            if (orderDirection == Direction.Ascending)
            {
                ordering = op!.OrderBy(new SortableField(orderBy.ToLower() == "name" ? UmbracoExamineFieldNames.NodeNameFieldName : orderBy, SortType.String));
            }
            else
            {
                ordering = op!.OrderByDescending(new SortableField(orderBy.ToLower() == "name" ? UmbracoExamineFieldNames.NodeNameFieldName : orderBy, SortType.String));
            }

            QueryOptions options = new(0, (int)(pageSize * (pageIndex + 1)));
            var results = ordering.Execute(options);
            totalRecords = results.TotalItemCount;


            if (pageSize > 0)
            {
                int skipCount = (pageIndex > 0 && pageSize > 0) ? Convert.ToInt32((pageIndex - 1) * pageSize) : 0;
                if (totalRecords < skipCount)
                {
                    skipCount = (int)totalRecords / pageSize;
                }

                return results.Skip(skipCount).Take(pageSize);
            }

            return results;
        }

        private IQuery? InitialiseMemberQuery(BooleanOperation operation = BooleanOperation.And, string indexType = UmbracoIndexes.MembersIndexName)
        {
            if (examineManager.TryGetIndex(indexType, out var index))
            {
                var searcher = index.Searcher;
                return searcher.CreateQuery(IndexTypes.Member, defaultOperation: operation);
            }
            logger.LogWarning("Could not retrieve index {indexType}", indexType);
            return null;
        }

    }
}