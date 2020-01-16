using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Web.Security;
using System.Xml.Linq;
using Umbraco.Core.Configuration;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.UnitOfWork;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Umbraco.Core.Security;
using Umbraco.Core.Services;
using Umbraco.Core;

namespace MemberListView.Services
{
    public class MemberExtendedService : MemberService, IMemberExtendedService
    {
        //private 
        public MemberExtendedService(IScopeUnitOfWorkProvider provider, RepositoryFactory repositoryFactory, ILogger logger, IEventMessagesFactory eventMessagesFactory, IMemberGroupService memberGroupService, IDataTypeService dataTypeService)
            : base(provider, repositoryFactory, logger, eventMessagesFactory, memberGroupService, dataTypeService)
        {
            UowProvider = provider;
        }

        internal new IScopeUnitOfWorkProvider UowProvider { get; private set; }

        /// <summary>
        /// Gets a list of paged <see cref="IMember"/> objects
        /// </summary>
        /// <remarks>An <see cref="IMember"/> can be of type <see cref="IMember"/> </remarks>
        /// <param name="pageIndex">Current page index</param>
        /// <param name="pageSize">Size of the page</param>
        /// <param name="totalRecords">Total number of records found (out)</param>
        /// <param name="orderBy">Field to order by</param>
        /// <param name="orderDirection">Direction to order by</param>
        /// <param name="orderBySystemField">Flag to indicate when ordering by system field</param>
        /// <param name="memberTypeAlias"></param>
        /// <param name="filter">Search text filter</param>
        /// <param name="additionalFilters">additional filter conditions</param>
        /// <param name="isApproved">Optional filter on IsApproved state</param>
        /// <param name="isLockedOut">Optional filter on IsLockedOut state</param>
        /// <returns><see cref="IEnumerable{T}"/></returns>
        public IEnumerable<IMember> GetAll(long pageIndex, int pageSize, out long totalRecords,
            string orderBy, Direction orderDirection, bool orderBySystemField, string memberTypeAlias, string filter,
            IDictionary<string, string> additionalFilters = null, bool? isApproved = null, bool? isLockedOut = null)
        {
            using (var uow = UowProvider.GetUnitOfWork(readOnly: true))
            {
                IQuery<IMember> filterQuery = null;
                if (filter.IsNullOrWhiteSpace() == false)
                {
                    filterQuery = Query<IMember>.Builder.Where(x => x.Name.Contains(filter) || x.Username.Contains(filter) || x.Email.Contains(filter));
                }

                if (isApproved.HasValue)
                {
                    filterQuery = filterQuery.Where(x => x.IsApproved == isApproved.Value);
                }
                if (isApproved.HasValue)
                {
                    filterQuery = filterQuery.Where(x => x.IsLockedOut == isLockedOut.Value);
                }
                if (additionalFilters != null)
                {
                    foreach(var f in additionalFilters)
                    {
                        filterQuery = filterQuery.Where(x => x.AdditionalData.ContainsKey(f.Key) && x.AdditionalData[f.Key].ToString() == f.Value);
                    }
                }

                var repository = RepositoryFactory.CreateMemberRepository(uow);
                IEnumerable<IMember> ret;
                if (memberTypeAlias == null)
                {
                    ret = repository.GetPagedResultsByQuery(null, pageIndex, pageSize, out totalRecords, orderBy, orderDirection, orderBySystemField, filterQuery);
                }
                else
                {
                    var query = new Query<IMember>().Where(x => x.ContentTypeAlias == memberTypeAlias);
                    ret = repository.GetPagedResultsByQuery(query, pageIndex, pageSize, out totalRecords, orderBy, orderDirection, orderBySystemField, filterQuery);
                }

                return ret;
            }

        }
    }
}