using Examine;
using Examine.Search;
using MemberListView.Extensions;
using MemberListView.Models;
using MemberListView.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Implement;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;
using Microsoft.Extensions.Logging;
#else
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Examine;
using static Umbraco.Core.Constants;
#endif

namespace MemberListView.Services
{
    public class MemberExtendedService : MemberService, IMemberExtendedService
    {
        private readonly Logging<MemberExtendedService> logger;
        private readonly IMemberGroupService memberGroupService;
        private readonly IMemberRepository memberRepository;
        private readonly IExamineManager examineManager;

#if NET5_0_OR_GREATER
        public MemberExtendedService(IScopeProvider provider, ILoggerFactory loggerFactory, IEventMessagesFactory eventMessagesFactory, IMemberGroupService memberGroupService,
            IMemberRepository memberRepository, IMemberTypeRepository memberTypeRepository, IMemberGroupRepository memberGroupRepository, IAuditRepository auditRepository,
            IExamineManager examineManager)
            : base(provider, loggerFactory, eventMessagesFactory, memberGroupService, memberRepository, memberTypeRepository, memberGroupRepository, auditRepository)
        {
            this.logger = new Logging<MemberExtendedService>(loggerFactory.CreateLogger<MemberExtendedService>());
#else
        public MemberExtendedService(IScopeProvider provider, ILogger logger, IEventMessagesFactory eventMessagesFactory,
        IMemberGroupService memberGroupService, IMediaFileSystem mediaFileSystem,
                                     IMemberRepository memberRepository, IMemberTypeRepository memberTypeRepository,
                                     IMemberGroupRepository memberGroupRepository, IAuditRepository auditRepository,
                                     IExamineManager examineManager)
            : base(provider, logger, eventMessagesFactory, memberGroupService, mediaFileSystem, 
                  memberRepository, memberTypeRepository, memberGroupRepository, auditRepository)
        {
            this.logger = new Logging<MemberExtendedService>(logger);
#endif
            this.memberGroupService = memberGroupService;
            this.memberRepository = memberRepository;
            this.examineManager = examineManager;
        }

        /// <inheritdoc />
        public IEnumerable<IMember> GetPage(long pageIndex, int pageSize, out long totalRecords, string orderBy,
                                            Direction orderDirection, bool orderBySystemField, string memberTypeAlias,
                                            IEnumerable<int> groups, string filter,
                                            IDictionary<string, string> additionalFilters = null,
                                            bool? isApproved = null, bool? isLockedOut = null)
        {
            using (var scope = ScopeProvider.CreateScope(autoComplete: true))
            {
                scope.ReadLock(Locks.MemberTree);
                // Use the database method unless we have complex search.
                totalRecords = 0;

                if ((groups?.Any() ?? false) || 
                    isApproved.HasValue || 
                    isLockedOut.HasValue || 
                    (additionalFilters?.Any() ?? false))
                {
                    return PerformExamineSearch(pageIndex, pageSize, out totalRecords, orderBy, orderDirection,
                                                memberTypeAlias, groups, filter, additionalFilters, isApproved,
                                                isLockedOut)
                                    .Select(x => GetById(int.Parse(x.Id)));
                }
                else
                {
                    return PerformRepositorySearch(pageIndex, pageSize, out totalRecords, orderBy, orderDirection,
                                                   orderBySystemField, memberTypeAlias, filter);
                }
            }
        }

        private IEnumerable<IMember> PerformRepositorySearch(long pageIndex, int pageSize, out long totalRecords,
                                                             string orderBy, Direction orderDirection,
                                                             bool orderBySystemField, string memberTypeAlias,
                                                             string filter)
        {
            var query1 = memberTypeAlias == null ? null : Query<IMember>().Where(x => x.ContentTypeAlias == memberTypeAlias);
            var query2 = filter == null ? null : Query<IMember>().Where(x => x.Name.Contains(filter) || x.Username.Contains(filter) || x.Email.Contains(filter));

            return memberRepository.GetPage(query1, pageIndex, pageSize, out totalRecords, query2, Ordering.By(orderBy, orderDirection, isCustomField: !orderBySystemField));
        }

        /// <inheritdoc />
        public IEnumerable<MemberExportModel> GetForExport(string orderBy, Direction orderDirection,
                                                           bool orderBySystemField, string memberTypeAlias,
                                                           IEnumerable<int> groups, string filter,
                                                           IEnumerable<string> includedColumns,
                                                           IDictionary<string, string> additionalFilters = null,
                                                           bool? isApproved = null, bool? isLockedOut = null)
        {
            using (var scope = ScopeProvider.CreateScope(autoComplete: true))
            {

                const int pageSize = 500;
                var page = 0;
                var total = long.MaxValue;
                while (page * pageSize < total)
                {
                    var items = GetPage(page++, pageSize, out total, orderBy, orderDirection, orderBySystemField,
                                        memberTypeAlias, groups, filter, additionalFilters, isApproved, isLockedOut);

                    foreach (var item in items)
                    {
                        yield return MapToExportModel(item, includedColumns);
                    }
                }
            }

        }

        private MemberExportModel MapToExportModel(IMember record, IEnumerable<string> includedColumns)
        {
            // Hack: using the internal ExportMember method on the MemberService as it auto does auditing etc.
            // We don't actually use this data though.
            var exportMethod = typeof(MemberService).GetMethod("ExportMember", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _ = exportMethod.Invoke(this, new object[] { record.Key }) as dynamic;

            var member = new MemberExportModel
            {
                Id = record.Id,
                Key = record.Key,
                Name = record.Name,
                Username = record.Username,
                Email = record.Email,
                Groups = GetAllRoles(record.Id).ToList(),
                MemberType = record.ContentTypeAlias,
                CreateDate = record.CreateDate,
                UpdateDate = record.UpdateDate,
                IsApproved = record.IsApproved,
                IsLockedOut = record.IsLockedOut,
            };

            foreach (var property in includedColumns)
            {
                // Try to work out the type
                object propertyValue;
                if (record.Properties.Contains(property)) //.IndexOfKey(property) > -1)
                {
                    switch (record.Properties[property].PropertyType.PropertyEditorAlias)
                    {
                        case PropertyEditors.Aliases.Boolean:
                            propertyValue = record.GetValue<bool>(property);
                            break;
                        case PropertyEditors.Legacy.Aliases.Date:
                            propertyValue = record.GetValue<DateTime?>(property)?.Date;
                            break;
                        case PropertyEditors.Aliases.DateTime:
                            propertyValue = record.GetValue<DateTime?>(property);
                            break;
                        default:
                            propertyValue = record.GetValue(property);
                            break;
                    }
                    member.Properties.Add(record.Properties[property].PropertyType.Name, propertyValue);
                }
            }

            return member;
        }

        private IEnumerable<ISearchResult> PerformExamineSearch(long pageIndex, int pageSize, out long totalRecords,
                                                                string orderBy, Direction orderDirection,
                                                                string memberTypeAlias, IEnumerable<int> groups,
                                                                string filter,
                                                                IDictionary<string, string> additionalFilters = null,
                                                                bool? isApproved = null, bool? isLockedOut = null)
        {
            if (!(InitialiseMemberQuery() is IQuery query))
            {
                totalRecords = 0;
                return Enumerable.Empty<ISearchResult>();
            }

            IBooleanOperation op = null;
            if (!memberTypeAlias.IsNullOrWhiteSpace())
            {
                op = query.NodeTypeAlias(memberTypeAlias);
            }

            if (groups?.Any() ?? false)
            {
                // Get group names from ids.
                var groupNames = memberGroupService.GetByIds(groups).Select(x => x.Name);
                if (groupNames.Any())
                {
                    op = query.And(op).GroupedOr(new[] { Constants.Members.Groups }, groupNames.ToArray());
                }
            }

            if (isApproved.HasValue)
            {
                op = query.And(op).BooleanField(Conventions.Member.IsApproved, isApproved.Value);
            }

            if (isLockedOut.HasValue)
            {
                op = query.And(op).BooleanField(Conventions.Member.IsLockedOut, isLockedOut.Value);
            }

            var basicFields = new List<string>() { "id", "__NodeId", "__Key", "email", "loginName" };

            var filterParameters = additionalFilters.Where(q => q.Key.StartsWith("f_") && !string.IsNullOrWhiteSpace(q.Value));

            //build a lucene query
            if (op == null && string.IsNullOrWhiteSpace(filter) && !filterParameters.Any())
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
                        sb.AppendFormat("__nodeName:{0}^10.0 ", filter.ToLower());

                        //node name normally with wildcards
                        sb.AppendFormat(" __nodeName:{0}* ", filter.ToLower());

                        foreach (var field in basicFields)
                        {
                            //additional fields normally
                            sb.AppendFormat("{0}:{1} ", field, filter);
                        }
                        sb.Append(")");
                        op = query.And(op).NativeQuery(sb.ToString());
                    }
                }


                // Now specific field searching. - these should be ANDed and grouped.
                foreach (var qs in filterParameters)
                {
                    string alias = qs.Key;
                    if (alias.StartsWith("f_"))
                    {
                        alias = qs.Key.Substring(2);
                    }

                    var values = qs.Value.Split(',');
                    if (values.Length > 0)
                    {
                        op = query.And(op).GroupedOr(new[] { alias }, values);
                    }
                }
            }


            //// Order the results 
            // Examine Sorting seems too unreliable, particularly on nodeName
            IOrdering ordering;
            if (orderDirection == Direction.Ascending)
            {
                ordering = op.OrderBy(new SortableField(orderBy.ToLower() == "name" ? "nodeName" : orderBy, SortType.String));
            }
            else
            {
                ordering = op.OrderByDescending(new SortableField(orderBy.ToLower() == "name" ? "nodeName" : orderBy, SortType.String));
            }

#if NET5_0_OR_GREATER
            QueryOptions options = new(0, (int)(pageSize * pageIndex));
            var results = ordering.Execute(options);
#else
            var results = ordering.Execute((int)(pageSize * pageIndex));
#endif
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

        private IQuery InitialiseMemberQuery(BooleanOperation operation = BooleanOperation.And, string indexType = UmbracoIndexes.MembersIndexName)
        {
            if (examineManager.TryGetIndex(indexType, out var index))
            {
#if NET5_0_OR_GREATER
                var searcher = index.Searcher;
#else
                var searcher = index.GetSearcher();
#endif
                return searcher.CreateQuery(IndexTypes.Member, defaultOperation: operation);
            }
            logger.LogWarning("Could not retrieve index {indexType}", indexType);
            return null;
        }

    }
}