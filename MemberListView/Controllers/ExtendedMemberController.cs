using MemberListView.Extensions;
using MemberListView.Models;
using MemberListView.Services;
using MemberListView.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;
using Umbraco.Cms.Web.BackOffice.Controllers;
#else
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Security;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Core.Security;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi.Filters;
using static Umbraco.Core.Constants;
#endif

namespace MemberListView.Controllers
{
    [PluginController(Constants.PluginName)]
#if NET5_0_OR_GREATER
    [Authorize(Policy = AuthorizationPolicies.SectionAccessMembers)]
#else
    [UmbracoApplicationAuthorize(Applications.Members)]
#endif
    public class ExtendedMemberController : MemberController
    {
        private readonly PropertyEditorCollection propertyEditors;
        private readonly IMemberExtendedService memberExtendedService;
        private readonly IDataTypeService dataTypeService;
        private readonly IMemberTypeService memberTypeService;
        private readonly IMemberGroupService memberGroupService;
        private readonly Settings settings;


#if NET5_0_OR_GREATER
        private readonly IMemberManager memberManager;
        private readonly IUmbracoMapper umbracoMapper;
        private readonly IBackOfficeSecurityAccessor backOfficeSecurityAccessor;

        public ExtendedMemberController(
            ICultureDictionary cultureDictionary,
            ILoggerFactory loggerFactory,
            IShortStringHelper shortStringHelper,
            IEventMessagesFactory eventMessages,
            ILocalizedTextService localizedTextService,
            PropertyEditorCollection propertyEditors,
            IUmbracoMapper umbracoMapper,
            IMemberService memberService,
            IMemberTypeService memberTypeService,
            IMemberManager memberManager,
            IDataTypeService dataTypeService,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
            IJsonSerializer jsonSerializer,
            IPasswordChanger<MemberIdentityUser> passwordChanger,
            IScopeProvider scopeProvider, 
            IMemberExtendedService memberExtendedService,
            IMemberGroupService memberGroupService,
            IConfiguration configuration) :
            base(cultureDictionary, loggerFactory, shortStringHelper, eventMessages, localizedTextService, 
                propertyEditors, umbracoMapper, memberService, memberTypeService, 
                memberManager, dataTypeService, backOfficeSecurityAccessor, jsonSerializer, passwordChanger, scopeProvider)
#else
        //private readonly MembershipProvider membershipProvider;
        public ExtendedMemberController(PropertyEditorCollection propertyEditors, IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor,
                                   ISqlContext sqlContext, ServiceContext services,
                                   AppCaches appCaches, IProfilingLogger logger, Umbraco.Core.IRuntimeState runtimeState,
                                   UmbracoHelper umbracoHelper, IMemberExtendedService memberExtendedService) : 
            base(propertyEditors, globalSettings, umbracoContextAccessor, sqlContext, services, appCaches, logger, runtimeState, umbracoHelper)
#endif
        {
            this.propertyEditors = propertyEditors;
            this.memberExtendedService = memberExtendedService;
#if NET5_0_OR_GREATER
            this.dataTypeService = dataTypeService;
            this.memberTypeService = memberTypeService;
            this.memberGroupService = memberGroupService;
            this.memberManager = memberManager;
            this.umbracoMapper = umbracoMapper;
            this.backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            settings = new Settings(configuration);
#else
            dataTypeService = services.DataTypeService;
            memberTypeService = services.MemberTypeService;
            memberGroupService = services.MemberGroupService;
            //membershipProvider = MembershipProviderExtensions.GetMembersMembershipProvider();
            settings = new Settings();
#endif
        }

        [HttpGet]
#if NET5_0_OR_GREATER
        [Authorize(Policy = AuthorizationPolicies.SectionAccessForMemberTree)]
#else
        [UmbracoTreeAuthorize(Trees.Members)]
#endif
        public ContentPropertyDisplay GetDashboardControl()
        {
            var dataType = dataTypeService.GetDataType(Constants.PropertyEditors.MemberListView);

            var configuration = dataTypeService.GetDataType(dataType.Id).Configuration;
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
            return memberGroupService
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
#if NET5_0_OR_GREATER
                        .Select(x => umbracoMapper.Map<MemberListItem>(x))
#else
                        .Select(x => Mapper.Map<MemberListItem>(x))
#endif
            };
        }

        [HttpGet]
        public bool GetCanExport()
        {
            return HasAccessToSensitiveData();
        }

        [HttpGet]
        public IEnumerable<MemberColumn> GetMemberColumns(string memberType = null)
        {
            var excludedColumns = settings.ExcludedColumns;
            bool foundType = false;
            if (!string.IsNullOrWhiteSpace(memberType))
            {
                var type = memberTypeService.Get(memberType);
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
                foreach (var type in memberTypeService.GetAll())
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
#if NET5_0_OR_GREATER
        public async Task<IActionResult> GetExportedMembers(
#else
        public async Task<IHttpActionResult> GetExportedMembers(
#endif
            string orderBy = "email",
            Direction orderDirection = Direction.Ascending,
            bool orderBySystemField = false,
            string filter = "",
            ExportFormat format = ExportFormat.Excel)
        {
            // Only export if the current user has access to sensitive data.
            if (!HasAccessToSensitiveData())
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

            var stream = new MemoryStream();

            var name = $"Members - {DateTime.Now:yyyy-MM-dd}";
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

            var filename = $"Members-{DateTime.Now:yyyy-MM-dd}.{ext}";
            stream.Seek(0, SeekOrigin.Begin);
#if NET5_0_OR_GREATER
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filename, out string contentType))
            {
                contentType = "application/octet-stream";
            }

            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, contentType, filename);
#else
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = filename
            };

            return ResponseMessage(response);
#endif

        }

#if NET5_0_OR_GREATER
        public async Task UnlockByKey(Guid key)
        {
            var identityMember = await memberManager.FindByIdAsync(key.ToString());
            if (identityMember == null)
            {
                ModelState.AddModelError("", "Could not unlock the user");
            }

            // Handle unlocking with the member manager (takes care of other nuances)
            else if (identityMember.IsLockedOut)
            {
                var unlockResult = await memberManager.SetLockoutEndDateAsync(identityMember, DateTimeOffset.Now.AddMinutes(-1));
                if (unlockResult.Succeeded == false)
                {
                    ModelState.AddModelError("", $"Could not unlock the user: {unlockResult.Errors.ToErrorMessage()}");
                }
            }
        }
#else
        public void UnlockByKey(Guid key)
        {
            var membershipProvider = MembershipProviderExtensions.GetMembersMembershipProvider();
            var member = membershipProvider.GetUser(key, false);
            // if they were locked but now they are trying to be unlocked
            if (member != null && member.IsLockedOut)
            {
                try
                {
                    var result = membershipProvider.UnlockUser(member.UserName);
                    if (result == false)
                    {
                        // it wasn't successful - but it won't really tell us why.
                        ModelState.AddModelError("", "Could not unlock the user");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex);
                }
            }
        }
#endif

        public void SuspendByKey(Guid key)
        {
            var member = GetMember(key);
            if (member != null)
            {
                member.IsApproved = false;
                memberExtendedService.Save(member);
            }
        }

        public void ApproveByKey(Guid key)
        {
            var member = GetMember(key);
            if (member != null)
            {
                member.IsApproved = true;
                memberExtendedService.Save(member);
            }
        }

        private IMember GetMember(Guid key)
        {
            return memberExtendedService.GetByKey(key);
        }

        private bool HasAccessToSensitiveData()
        {
#if NET5_0_OR_GREATER
            return backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser.HasAccessToSensitiveData();
#else
            return Security.CurrentUser.HasAccessToSensitiveData();
#endif

        }
    }
}
