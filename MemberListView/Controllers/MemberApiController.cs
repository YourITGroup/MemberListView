using AutoMapper;
using Examine;
using MemberListView.Extensions;
using MemberListView.Helpers;
using MemberListView.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Security;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace MemberListView.Controllers
{
    [PluginController("MemberManager")]
    public class MemberApiController : BackOfficeNotificationsController
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MemberApiController()
            : this(UmbracoContext.Current)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="umbracoContext"></param>
        public MemberApiController(UmbracoContext umbracoContext)
            : base(umbracoContext)
        {
            _provider = global::Umbraco.Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();
        }

        private readonly MembershipProvider _provider;

        [HttpGet]
        public IEnumerable<string> GetMemberGroups()
        {
            return Services.MemberGroupService
                            .GetAll()
                            .Select(g => g.Name)
                            .OrderBy(g => g);
        }

        [HttpGet]
        public PagedResult<MemberListItem> GetMembers(
            int pageNumber = 1,
            int pageSize = 100,
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            string filter = "")
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                throw new NotSupportedException("Both pageNumber and pageSize must be greater than zero");
            }

            var queryString = Request.GetQueryNameValuePairs();
            var memberTypeAlias = GetMemberType(queryString);
            var filters = GetFilters(queryString);

            var members = MemberSearch.PerformMemberSearch(filter, filters, out int totalRecords,
                                                            memberTypeAlias, pageNumber, pageSize,
                                                            orderBy, orderDirection);
            if (totalRecords == 0)
            {
                return new PagedResult<MemberListItem>(0, 0, 0);
            }

            var pagedResult = new PagedResult<MemberListItem>(totalRecords, pageNumber, pageSize)
            {
                Items = members
                    .Select(x => AutoMapperExtensions.MapWithUmbracoContext<SearchResult, MemberListItem>(x, UmbracoContext))
            };
            return pagedResult;
        }

        [HttpGet]
        public bool GetCanExport()
        {
            return Security.CurrentUser.HasAccessToSensitiveData();
        }

        [HttpGet]
        public IEnumerable<MemberColumn> GetMemberColumns(string memberType = null)
        {
            var excludedColumns = ConfigurationManager.AppSettings[Constants.Configuration.ExportExcludedColumns]
                                                        ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    ?? Array.Empty<string>();
            bool foundType = false;
            if (!string.IsNullOrWhiteSpace(memberType))
            {
                var type = Services.MemberTypeService.Get(memberType);
                if (type != null)
                {
                    foundType = true;
                    foreach (var col in type.GetColumns(excludedColumns))
                    {
                        yield return col;
                    }
                }
            }
            if (!foundType)
            {
                // This is only used to track columns we already have added.
                var columns = new List<string>();
                foreach (var type in Services.MemberTypeService.GetAll())
                {
                    foreach (var col in type.GetColumns(excludedColumns))
                    {
                        if (!columns.Contains(col.Alias))
                        {
                            columns.Add(col.Alias);
                            yield return col;
                        }
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetMembersExport(
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            string filter = "",
            ExportFormat format = ExportFormat.Excel)
        {
            // Only export if the current user has access to sensitive data.
            if (!Security.CurrentUser.HasAccessToSensitiveData())
            {
                return Ok();
            }

            var queryString = Request.GetQueryNameValuePairs();
            var memberTypeAlias = GetMemberType(queryString);
            var filters = GetFilters(queryString);
            var columns = GetColumns(queryString);

            var members = MemberSearch.PerformMemberSearch(filter, filters, out _,
                                                            memberTypeAlias,
                                                            orderBy: orderBy,
                                                            orderDirection: orderDirection)
                                        .ToExportModel(columns);

            var name = $"Members - {DateTime.Now:yyyy-MM-dd}";
            var filename = $"Members-{DateTime.Now:yyyy-MM-dd}";
            var stream = new MemoryStream();

            string ext = "csv";
            string mimeType = Constants.MimeTypes.CSV;
            switch (format)
            {
                case ExportFormat.CSV:
                    await members.CreateCSVAsync(stream);
                    break;
                case ExportFormat.Excel:
                    ext = "xlsx";
                    mimeType = Constants.MimeTypes.Excel;
                    await members.CreateExcelAsync(stream, name);
                    break;
            }

            stream.Seek(0, SeekOrigin.Begin);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"{filename}.{ext}"
            };

            return ResponseMessage(response);
        }


        private IEnumerable<string> GetColumns(IEnumerable<KeyValuePair<string, string>> query)
        {
            return query.Where(q => q.Key == "columns")
                        .Select(q => q.Value)
                        .FirstOrDefault()
                        ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string GetMemberType(IEnumerable<KeyValuePair<string, string>> query)
        {
            return query.Where(q => q.Key == "memberType").Select(q => q.Value).FirstOrDefault();
        }

        private static Dictionary<string, string> GetFilters(IEnumerable<KeyValuePair<string, string>> query)
        {

            Dictionary<string, string> filters = new Dictionary<string, string>();

            foreach (var kvp in query.Where(q => (q.Key.StartsWith("f_") || q.Key == Constants.Members.Groups) && !string.IsNullOrWhiteSpace(q.Value)))
            {
                filters.Add(kvp.Key, kvp.Value);
            }

            return filters;
        }

        public void PostUnlock(string id)
        {
            var member = GetMember(id);
            //if they were locked but now they are trying to be unlocked
            if (member != null && member.IsLockedOut)
            {
                try
                {
                    var result = _provider.UnlockUser(member.UserName);
                    if (result == false)
                    {
                        //it wasn't successful - but it won't really tell us why.
                        ModelState.AddModelError("custom", "Could not unlock the user");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("custom", ex);
                }
            }
        }

        public void PostSuspend(string id)
        {
            var member = GetMember(id);
            if (member != null)
            {
                member.IsApproved = false;
                _provider.UpdateUser(member);
            }
        }

        public void PostApprove(string id)
        {
            var member = GetMember(id);
            if (member != null)
            {
                member.IsApproved = true;
                _provider.UpdateUser(member);
            }
        }

        private MembershipUser GetMember(string id)
        {
            Guid key;
            if (int.TryParse(id, out int iid))
            {
                var member = Services.MemberService.GetById(iid);
                key = member.Key;
            }
            else if (!Guid.TryParse(id, out key))
            {
                return null;
            }
            return _provider.GetUser(key, false);
        }
    }
}
