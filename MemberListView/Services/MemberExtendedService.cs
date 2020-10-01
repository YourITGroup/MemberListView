using MemberListView.Models;
using NPoco.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Events;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

namespace MemberListView.Services
{
    public class MemberExtendedService : MemberService, IMemberExtendedService
    {
        private readonly IMemberRepository memberRepository;

        //private 
        public MemberExtendedService(IScopeProvider provider, ILogger logger, IEventMessagesFactory eventMessagesFactory, IMemberGroupService memberGroupService, IMediaFileSystem mediaFileSystem,
            IMemberRepository memberRepository, IMemberTypeRepository memberTypeRepository, IMemberGroupRepository memberGroupRepository, IAuditRepository auditRepository)
            : base(provider, logger, eventMessagesFactory, memberGroupService, mediaFileSystem, memberRepository, memberTypeRepository, memberGroupRepository, auditRepository)
        {
            this.memberRepository = memberRepository;
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
                scope.ReadLock(Umbraco.Core.Constants.Locks.MemberTree);
                var query1 = memberTypeAlias == null ? null : Query<IMember>().Where(x => x.ContentTypeAlias == memberTypeAlias);
                var query2 = filter == null ? null : Query<IMember>().Where(x => x.Name.Contains(filter) || x.Username.Contains(filter) || x.Email.Contains(filter));

                if (groups != null && groups.Any())
                {
                    // We neeed to build a sub query and "spoof" it into a values enumeration for WhereIn.
                    var groupList = groups.Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? $"{group}" : $"{list},{group}");
                    var subQuery = $"SELECT Member FROM {Umbraco.Core.Constants.DatabaseSchema.Tables.Member2MemberGroup} WHERE MemberGroup IN ('{groupList}')";
                    query2.WhereIn(x => x.Id, subQuery);
                }

                if (isApproved.HasValue)
                {
                    query2.Where(x => (isApproved.Value && x.IsApproved) || (!isApproved.Value && !x.IsApproved));
                }

                if (isLockedOut.HasValue)
                {
                    query2.Where(x => (isLockedOut.Value && x.IsLockedOut) || (!isLockedOut.Value && !x.IsLockedOut));
                }

                if (additionalFilters != null)
                {
                    foreach (var f in additionalFilters.Where(f => f.Key.StartsWith("f_")))
                    {
                        query2 = query2.Where(x => x.AdditionalData.ContainsKey(f.Key) && x.AdditionalData[f.Key].ToString() == f.Value);
                    }
                }

                return memberRepository.GetPage(query1, pageIndex, pageSize, out totalRecords, query2, Ordering.By(orderBy, orderDirection, isCustomField: !orderBySystemField));
            }

        }

        /// <inheritdoc />
        public IEnumerable<MemberExportModel> GetForExport(string orderBy, Direction orderDirection, bool orderBySystemField,
                                                 string memberTypeAlias, IEnumerable<int> groups, string filter,
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
            dynamic exportedData = exportMethod.Invoke(this, new object[] { record.Key }) as dynamic;

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
                if (record.Properties.IndexOfKey(property) > -1)
                {
                    switch (record.Properties[property].PropertyType.PropertyEditorAlias)
                    {
                        case Umbraco.Core.Constants.PropertyEditors.Aliases.Boolean:
                            propertyValue = record.GetValue<bool>(property);
                            break;
                        case Umbraco.Core.Constants.PropertyEditors.Legacy.Aliases.Date:
                            propertyValue = record.GetValue<DateTime?>(property)?.Date;
                            break;
                        case Umbraco.Core.Constants.PropertyEditors.Aliases.DateTime:
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
    }
}