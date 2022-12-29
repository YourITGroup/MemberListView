using MemberListView.Extensions;
using MemberListView.Models;
using MemberListView.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;

namespace MemberListView.Controllers
{
    [PluginController(Constants.PluginName)]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessMembers)]
    public class ExtendedMemberController : MemberController
    {
        private readonly PropertyEditorCollection propertyEditors;
        private readonly IMemberExtendedService memberExtendedService;
        private readonly IDataTypeService dataTypeService;
        private readonly IMemberTypeService memberTypeService;
        private readonly IMemberGroupService memberGroupService;
        private readonly IOptions<Config.MemberListView> options;
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
            ICoreScopeProvider scopeProvider,
            IMemberExtendedService memberExtendedService,
            IMemberGroupService memberGroupService,
            IOptions<Config.MemberListView> options) :
            base(cultureDictionary, loggerFactory, shortStringHelper, eventMessages, localizedTextService,
                propertyEditors, umbracoMapper, memberService, memberTypeService,
                memberManager, dataTypeService, backOfficeSecurityAccessor, jsonSerializer, passwordChanger, scopeProvider)
        {
            this.propertyEditors = propertyEditors;
            this.memberExtendedService = memberExtendedService;
            this.dataTypeService = dataTypeService;
            this.memberTypeService = memberTypeService;
            this.memberGroupService = memberGroupService;
            this.options = options;
            this.memberManager = memberManager;
            this.umbracoMapper = umbracoMapper;
            this.backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.SectionAccessForMemberTree)]
        public ContentPropertyDisplay? GetDashboardControl()
        {
            var dataType = dataTypeService.GetDataType(Constants.PropertyEditors.MemberListView);

            if (dataType is null)
            {
                return default;
            }
            var configuration = dataTypeService.GetDataType(dataType.Id)?.Configuration;
            var editor = propertyEditors[dataType.EditorAlias];

            return new ContentPropertyDisplay()
            {
                Editor = dataType.EditorAlias,
                Validation = new PropertyTypeValidation(),
                View = editor?.GetValueEditor().View,
                //#if NET7_0_OR_GREATER
                Config = editor?.GetConfigurationEditor().ToConfigurationEditor(configuration)
                //#else
                //                Config = editor?.GetConfigurationEditor().ToConfigurationEditor(configuration)
                //#endif
            };
        }


        [HttpGet]
        public IDictionary<int, string?> GetMemberGroups()
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

            var members = memberExtendedService.GetPage(pageNumber - 1, pageSize, out long totalRecords, orderBy,
                                                        orderDirection, orderBySystemField, typeAlias, filter, groups,
                                                        filters, isApproved, isLockedOut);
            if (totalRecords == 0)
            {
                return new PagedResult<MemberListItem>(0, 0, 0);
            }

            return new PagedResult<MemberListItem>(totalRecords, pageNumber, pageSize)
            {
                Items = members.Select(x => umbracoMapper.Map<MemberListItem>(x)).WhereNotNull()
            };
        }

        [HttpGet]
        public bool GetCanExport()
        {
            return HasAccessToSensitiveData();
        }

        [HttpGet]
        public IEnumerable<MemberColumn> GetMemberColumns(string? memberType = null)
        {
            var excludedColumns = options.Value.ExportExcludedColumns ?? Array.Empty<string>();
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
                        if (!columns.Contains(col.Alias!))
                        {
                            columns.Add(col.Alias!);
                            yield return col;
                        }
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExportedMembers(
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

            var members = memberExtendedService.GetForExport(orderBy, orderDirection, orderBySystemField, typeAlias,
                                                             filter, groups, columns, filters, isApproved, isLockedOut);

            var stream = new MemoryStream();

            var name = $"Members - {DateTime.Now:yyyy-MM-dd}";
            string ext = "csv";
            switch (format)
            {
                case ExportFormat.CSV:
                    await members.ToList().CreateCSVAsync(stream);
                    break;
                case ExportFormat.Excel:
                    ext = "xlsx";
                    await members.CreateExcelAsync(stream, name);
                    break;
            }

            var filename = $"Members-{DateTime.Now:yyyy-MM-dd}.{ext}";
            stream.Seek(0, SeekOrigin.Begin);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filename, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, contentType, filename);

        }

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

        private IMember? GetMember(Guid key)
        {
            return memberExtendedService.GetByKey(key);
        }

        private bool HasAccessToSensitiveData()
        {
            return backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.HasAccessToSensitiveData() ?? false;
        }
    }
}
