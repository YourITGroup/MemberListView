using System.Collections.Generic;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;
using Umbraco.Cms.Core;
using UserProfile = Umbraco.Cms.Core.Models.ContentEditing.UserProfile;
#else
using Umbraco.Core;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;
using static Umbraco.Core.Constants;
#endif

namespace MemberListView.Models.Mapping
{
    internal class MemberListItemMapDefinition : IMapDefinition
    {
        private readonly IUserService userService;
        private readonly IMemberTypeService memberTypeService;
        private readonly IMemberService memberService;

        public MemberListItemMapDefinition(IUserService userService, IMemberTypeService memberTypeService, IMemberService memberService)
        {
            this.userService = userService;
            this.memberTypeService = memberTypeService;
            this.memberService = memberService;
        }

#if NET5_0_OR_GREATER
        public void DefineMaps(IUmbracoMapper mapper) => mapper.Define<IMember, MemberListItem>(Map);
#else
        public void DefineMaps(UmbracoMapper mapper) =>  mapper.Define<IMember, MemberListItem>(Map);
#endif

        private UserProfile GetOwner(IContentBase source, MapperContext context)
        {
            var profile = source.GetCreatorProfile(userService);
            return profile == null ? null : context.Map<IProfile, UserProfile>(profile);
        }

        private IEnumerable<string> GetMemberGroups(string username)
        {
            var userRoles = username.IsNullOrWhiteSpace() ? null : memberService.GetAllRoles(username);

            return userRoles;
        }

        private void Map(IMember source, MemberListItem target, MapperContext context)
        {
            target.ContentTypeId = source.ContentType.Id;
            target.ContentTypeAlias = source.ContentType.Alias;
            target.CreateDate = source.CreateDate;
            target.Email = source.Email;
            target.Icon = source.ContentType.Icon;
            target.Id = int.MaxValue;
            target.Key = source.Key;
            target.Name = source.Name;
            target.Owner = GetOwner(source, context);
            target.ParentId = source.ParentId;
            target.Path = source.Path;
            target.Properties = context.MapEnumerable<Property, ContentPropertyBasic>((IEnumerable<Property>)source.Properties);
            target.SortOrder = source.SortOrder;
            target.State = null;
            target.Udi = Udi.Create(UdiEntityType.Member, source.Key);
            target.UpdateDate = source.UpdateDate;
            target.Username = source.Username;

            target.MemberGroups = GetMemberGroups(source.Username);
            target.IsLockedOut = source.IsLockedOut;
            target.IsApproved = source.IsApproved;
            target.ContentType = memberTypeService.Get(source.ContentType.Alias);
        }
    }
}