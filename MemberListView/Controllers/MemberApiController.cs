using AutoMapper;
using MemberListView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Security;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Umbraco.Web;
using System.Net.Http.Formatting;
using System.Net.Http;
using System.Web.Mvc;
using System.Net.Http.Headers;
using System.Net;
using MemberListView.Helpers;
using System.Text;
using Umbraco.Core.Logging;

namespace MemberListView.Controllers
{
    [Umbraco.Web.Mvc.PluginController("MemberManager")]
    public class MemberApiController : UmbracoAuthorizedApiController
    {
        private MembershipProvider provider;

        /// <summary>
        /// Returns the currently used Membership Provider.
        /// </summary>
        private MembershipProvider MembershipProvider
        {
            get
            {
                if (provider == null)
                {
                    if (Membership.Providers[Constants.Conventions.Member.UmbracoMemberProviderName] == null)
                    {
                        throw new InvalidOperationException("No membership provider found with name " + Constants.Conventions.Member.UmbracoMemberProviderName);
                    }

                    provider = Membership.Providers[Constants.Conventions.Member.UmbracoMemberProviderName];
                }
                return provider;
            }
        }
        [HttpGet]
        [HttpQueryStringFilter("queryStrings")]
        public PagedResult<MemberListItem> GetMembers(FormDataCollection queryStrings)
        {
            // Base Query data
            int pageNumber = queryStrings.HasKey("pageNumber") ? queryStrings.GetValue<int>("pageNumber") : 1;
            int pageSize = queryStrings.HasKey("pageSize") ? queryStrings.GetValue<int>("pageSize") : 10;
            string orderBy = queryStrings.HasKey("orderBy") ? queryStrings.GetValue<string>("orderBy") : "email";
            Direction orderDirection = queryStrings.HasKey("orderDirection") ? queryStrings.GetValue<Direction>("orderDirection") : Direction.Ascending;
            string memberType = queryStrings.HasKey("memberType") ? queryStrings.GetValue<string>("memberType") : "";

            string filter = queryStrings.HasKey("filter") ? queryStrings.GetValue<string>("filter") : "";

            int totalMembers = 0;
            var members = Mapper.Map<IEnumerable<MemberListItem>>(MemberSearch.PerformMemberSearch(filter, queryStrings, out totalMembers,
                                                                                                    memberType, pageNumber, pageSize,
                                                                                                    orderBy, orderDirection));
            if (totalMembers == 0)
                return new PagedResult<MemberListItem>(0, 0, 0);

            var pagedResult = new PagedResult<MemberListItem>(
               totalMembers,
               pageNumber,
               pageSize);

            pagedResult.Items = members;

            return pagedResult;
        }

        [HttpGet]
        [HttpQueryStringFilter("queryStrings")]
        public HttpResponseMessage GetMembersExport(FormDataCollection queryStrings)
        {
            // Base Query data
            string memberType = queryStrings.HasKey("memberType") ? queryStrings.GetValue<string>("memberType") : "";
            string orderBy = queryStrings.HasKey("orderBy") ? queryStrings.GetValue<string>("orderBy") : "email";
            Direction orderDirection = queryStrings.HasKey("orderDirection") ? queryStrings.GetValue<Direction>("orderDirection") : Direction.Ascending;

            string filter = queryStrings.HasKey("filter") ? queryStrings.GetValue<string>("filter") : "";

            int totalMembers = 0;

            var members = Mapper.Map<IEnumerable<MemberExportModel>>(MemberSearch.PerformMemberSearch(filter, queryStrings, out totalMembers,
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
                    var result = MembershipProvider.UnlockUser(member.UserName);
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
                MembershipProvider.UpdateUser(member);
            }
        }

        public void PostApprove(string id)
        {
            var member = GetMember(id);
            if (member != null)
            {
                member.IsApproved = true;
                MembershipProvider.UpdateUser(member);
            }
        }

        private MembershipUser GetMember(string id)
        {
            Guid key = Guid.Empty;

            int iid;
            if (int.TryParse(id, out iid))
            {
                var member = Services.MemberService.GetById(iid);
                key = member.Key;
            }
            else if (!Guid.TryParse(id, out key))
            {
                return null;
            }
            return MembershipProvider.GetUser(key, false);
        }
    }
}
