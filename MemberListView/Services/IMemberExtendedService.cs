using MemberListView.Models;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
#else
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Services;
#endif

namespace MemberListView.Services
{
    public interface IMemberExtendedService : IMemberService
    {
        /// <summary>
        /// Gets a list of paged <see cref="IMember"/> objects matching the criteria
        /// </summary>
        /// <remarks>An <see cref="IMember"/> can be of type <see cref="IMember"/> </remarks>
        /// <param name="pageIndex">Current page index</param>
        /// <param name="pageSize">Size of the page</param>
        /// <param name="totalRecords">Total number of records found (out)</param>
        /// <param name="orderBy">Field to order by</param>
        /// <param name="orderDirection">Direction to order by</param>
        /// <param name="orderBySystemField">Flag to indicate when ordering by system field</param>
        /// <param name="memberTypeAlias"></param>
        /// <param name="groups"></param>
        /// <param name="filter">Search text filter</param>
        /// <param name="additionalFilters">additional filter conditions</param>
        /// <param name="isApproved">Optional filter on IsApproved state</param>
        /// <param name="isLockedOut">Optional filter on IsLockedOut state</param>
        /// <returns><see cref="IEnumerable{T}"/></returns>
        IEnumerable<IMember> GetPage(long pageIndex, int pageSize, out long totalRecords, string orderBy,
                                     Direction orderDirection, bool orderBySystemField, string memberTypeAlias,
                                     IEnumerable<int> groups, string filter,
                                     IDictionary<string, string> additionalFilters = null, bool? isApproved = null,
                                     bool? isLockedOut = null);

        /// <summary>
        /// Gets all <see cref="MemberExportModel"/> objects matching the criteria
        /// </summary>
        /// <param name="orderBy">Field to order by</param>
        /// <param name="orderDirection">Direction to order by</param>
        /// <param name="orderBySystemField">Flag to indicate when ordering by system field</param>
        /// <param name="memberTypeAlias"></param>
        /// <param name="groups"></param>
        /// <param name="filter">Search text filter</param>
        /// <param name="includedColumns">List of columns to include in the export</param>
        /// <param name="additionalFilters">additional filter conditions</param>
        /// <param name="isApproved">Optional filter on IsApproved state</param>
        /// <param name="isLockedOut">Optional filter on IsLockedOut state</param>
        /// <returns><see cref="IEnumerable{T}"/></returns>
        IEnumerable<MemberExportModel> GetForExport(string orderBy, Direction orderDirection, bool orderBySystemField,
                                          string memberTypeAlias, IEnumerable<int> groups, string filter,
                                          IEnumerable<string> includedColumns,
                                          IDictionary<string, string> additionalFilters = null, bool? isApproved = null,
                                          bool? isLockedOut = null);

    }
}