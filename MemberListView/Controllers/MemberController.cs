using DocumentFormat.OpenXml.Packaging;
using MemberListView.Extensions;
using MemberListView.Models;
using MemberListView.Services;
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
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi.Filters;

namespace MemberListView.Controllers
{
    [PluginController(Constants.PluginName)]
    [UmbracoApplicationAuthorize(global::Umbraco.Core.Constants.Applications.Members)]
    public class ExtendedMemberController : Umbraco.Web.Editors.MemberController
    {
        private readonly PropertyEditorCollection propertyEditors;
        private readonly IMemberExtendedService memberExtendedService;
        private readonly MembershipProvider provider = global::Umbraco.Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();

        public ExtendedMemberController(PropertyEditorCollection propertyEditors, IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor,
                                   Umbraco.Core.Persistence.ISqlContext sqlContext, ServiceContext services,
                                   AppCaches appCaches, IProfilingLogger logger, Umbraco.Core.IRuntimeState runtimeState,
                                   UmbracoHelper umbracoHelper, IMemberExtendedService memberExtendedService) : 
            base(propertyEditors, globalSettings, umbracoContextAccessor, sqlContext, services, appCaches, logger, runtimeState, umbracoHelper)
        {
            this.propertyEditors = propertyEditors;
            this.memberExtendedService = memberExtendedService;
        }

        [HttpGet]
        [UmbracoTreeAuthorize(global::Umbraco.Core.Constants.Trees.Members)]
        public ContentPropertyDisplay GetDashboardControl()
        {
            var dataType = Services.DataTypeService.GetDataType(Constants.PropertyEditors.MemberListView);

            var configuration = Services.DataTypeService.GetDataType(dataType.Id).Configuration;
            var editor = propertyEditors[dataType.EditorAlias];

            return new ContentPropertyDisplay()
            {
                Editor = dataType.EditorAlias,
                Validation = new PropertyTypeValidation(),
                View = editor.GetValueEditor().View,
                Config = editor.GetConfigurationEditor().ToConfigurationEditor(configuration)
            };
        }


        [HttpGet]
        public IDictionary<int, string> GetMemberGroups()
        {
            return Services.MemberGroupService
                            .GetAll()
                            .OrderBy(g => g.Id)
                            .ToDictionary(g => g.Id, g => g.Name);
        }

        [HttpGet]
        public PagedResult<MemberListItem> GetPagedMembers(
            int pageNumber = 1,
            int pageSize = 100,
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            bool orderBySystemField = false,
            string filter = "")
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                throw new NotSupportedException("Both pageNumber and pageSize must be greater than zero");
            }

            var typeAlias = Request.GetMemberTypeFromQuery();
            var groups = Request.GetGroupsFromQuery();
            var filters = Request.GetFilters();
            var isLockedOut = Request.GetIsLockedOut();
            var isApproved = Request.GetIsApproved();

            var members = memberExtendedService.GetPage(pageNumber - 1, pageSize, out long totalRecords, orderBy, orderDirection,
                                                        orderBySystemField, typeAlias, groups, filter, filters, isApproved, isLockedOut);
            if (totalRecords == 0)
            {
                return new PagedResult<MemberListItem>(0, 0, 0);
            }

            return new PagedResult<MemberListItem>(totalRecords, pageNumber, pageSize)
            {
                Items = members
                        .Select(x => Mapper.Map<MemberListItem>(x))
            };
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
        public async Task<IHttpActionResult> GetExportedMembers(
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            bool orderBySystemField = false,
            string filter = "",
            ExportFormat format = ExportFormat.Excel)
        {
            // Only export if the current user has access to sensitive data.
            if (!Security.CurrentUser.HasAccessToSensitiveData())
            {
                return Ok();
            }

            var typeAlias = Request.GetMemberTypeFromQuery();
            var groups = Request.GetGroupsFromQuery();
            var filters = Request.GetFilters();
            var isLockedOut = Request.GetIsLockedOut();
            var isApproved = Request.GetIsApproved();
            var columns = Request.GetColumns();

            var members = memberExtendedService.GetForExport(orderBy, orderDirection, orderBySystemField, typeAlias, groups,
                                                       filter, columns, filters, isApproved, isLockedOut);

            var name = $"Members - {DateTime.Now:yyyy-MM-dd}";
            var filename = $"Members-{DateTime.Now:yyyy-MM-dd}";
            var stream = new MemoryStream();

            string ext = "csv";
            string mimeType = Constants.MimeTypes.CSV;
            switch (format)
            {
                case ExportFormat.CSV:
                    await members.ToList().CreateCSVAsync(stream);
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


        public void UnlockByKey(Guid key)
        {
            var member = GetMember(key);
            //if they were locked but now they are trying to be unlocked
            if (member != null && member.IsLockedOut)
            {
                try
                {
                    var result = provider.UnlockUser(member.UserName);
                    if (result == false)
                    {
                        //it wasn't successful - but it won't really tell us why.
                        ModelState.AddModelError("", "Could not unlock the user");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex);
                }
            }
        }

        public void SuspendByKey(Guid key)
        {
            var member = GetMember(key);
            if (member != null)
            {
                member.IsApproved = false;
                provider.UpdateUser(member);
            }
        }

        public void ApproveByKey(Guid key)
        {
            var member = GetMember(key);
            if (member != null)
            {
                member.IsApproved = true;
                provider.UpdateUser(member);
            }
        }

        private MembershipUser GetMember(Guid key)
        {
            //var member = Services.MemberService.GetByKey(key);
            //key = member.Key;
            return provider.GetUser(key, false);
        }
    }
}
