using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Services;

namespace MemberListView.Services
{
    public interface IMemberExtendedService : IMemberService
    {
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
        IEnumerable<Umbraco.Core.Models.IMember> GetAll(long pageIndex, int pageSize, out long totalRecords, string orderBy, Umbraco.Core.Persistence.DatabaseModelDefinitions.Direction orderDirection, bool orderBySystemField, string memberTypeAlias, string filter, IDictionary<string, string> additionalFilters = null, bool? isApproved = null, bool? isLockedOut = null);
    }
}