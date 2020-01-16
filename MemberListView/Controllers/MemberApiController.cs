using AutoMapper;
using MemberListView.Helpers;
using MemberListView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi.Filters;

namespace MemberListView.Controllers
{
    [PluginController("MemberManager")]
    //[OutgoingDateTimeFormat]
    public class MemberApiController : BackOfficeNotificationsController
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MemberApiController()
            : this(UmbracoContext.Current)
        {
            _provider = global::Umbraco.Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();
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

        public PagedResult<MemberListItem> GetMembers(
            int pageNumber = 1,
            int pageSize = 100,
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            string filter = "")
        {
            var queryString = Request.GetQueryNameValuePairs();
            string memberTypeAlias = queryString.Where(q => q.Key == "memberType").Select(q => q.Value).FirstOrDefault();

            if (pageNumber <= 0 || pageSize <= 0)
            {
                throw new NotSupportedException("Both pageNumber and pageSize must be greater than zero");
            }

            // Base Query data

            Dictionary<string, string> filters = new Dictionary<string, string>();

            foreach (var kvp in queryString.Where(q => q.Key.StartsWith("f_") && !string.IsNullOrWhiteSpace(q.Value)))
            {
                filters.Add(kvp.Key, kvp.Value);
            }

            var members = Mapper.Map<IEnumerable<MemberListItem>>(MemberSearch.PerformMemberSearch(filter, filters, out int totalMembers,
                                                                                                    memberTypeAlias, pageNumber, pageSize,
                                                                                                    orderBy, orderDirection));
            if (totalMembers == 0)
                return new PagedResult<MemberListItem>(0, 0, 0);

            var pagedResult = new PagedResult<MemberListItem>(totalMembers, pageNumber, pageSize)
            {
                Items = members
            };

            return pagedResult;
        }

        [HttpGet]
        public HttpResponseMessage GetMembersExport(
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            string filter = "")
        {
            // Base Query data
            var queryString = Request.GetQueryNameValuePairs();
            string memberType = queryString.Where(q => q.Key == "memberType").Select(q => q.Value).FirstOrDefault();

            Dictionary<string, string> filters = new Dictionary<string, string>();

            foreach (var kvp in queryString.Where(q => q.Key.StartsWith("f_") && !string.IsNullOrWhiteSpace(q.Value)))
            {
                filters.Add(kvp.Key, kvp.Value);
            }


            var members = Mapper.Map<IEnumerable<MemberExportModel>>(MemberSearch.PerformMemberSearch(filter, filters, out _,
                                                                                                        memberType,
                                                                                                        orderBy: orderBy,
                                                                                                        orderDirection: orderDirection));

            var content = members.CreateCSV();

            // see http://stackoverflow.com/questions/9541351/returning-binary-file-from-controller-in-asp-net-web-api
            // & http://stackoverflow.com/questions/12975886/how-to-download-a-file-using-web-api-in-asp-net-mvc-4-and-jquery
            // We really should use an async version - the above reference includes an example.
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content,
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, // this is the default; we don't really need to specify it.
                                        throwOnInvalidBytes: true // Recommended for security reasons.
                                        ))
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = string.Format("Members_{0:yyyyMMdd}.csv", DateTime.Now)
            };
            return result;
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
