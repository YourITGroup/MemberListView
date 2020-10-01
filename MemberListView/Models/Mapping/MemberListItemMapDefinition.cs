using System.Collections.Generic;
using System.Web.Security;
using Umbraco.Core;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;

namespace MemberListView.Models.Mapping
{
    internal class MemberListItemMapDefinition : IMapDefinition
    {
        private readonly IUserService userService;
        private readonly IMemberTypeService memberTypeService;

        public MemberListItemMapDefinition(IUserService userService, IMemberTypeService memberTypeService)
        {
            this.userService = userService;
            this.memberTypeService = memberTypeService;
        }

        public void DefineMaps(UmbracoMapper mapper)
        {
            mapper.Define<IMember, MemberListItem>((source, context) => new MemberListItem(), Map);
        }

        private UserProfile GetOwner(IContentBase source, MapperContext context)
        {
            var profile = source.GetCreatorProfile(userService);
            return profile == null ? null : context.Map<IProfile, UserProfile>(profile);
        }

        private static IEnumerable<string> GetMemberGroups(string username)
        {
            var userRoles = username.IsNullOrWhiteSpace() ? null : Roles.GetRolesForUser(username);

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
            target.Properties = context.MapEnumerable<Property, ContentPropertyBasic>(source.Properties);
            target.SortOrder = source.SortOrder;
            target.State = null;
            target.Udi = Udi.Create(Umbraco.Core.Constants.UdiEntityType.Member, source.Key);
            target.UpdateDate = source.UpdateDate;
            target.Username = source.Username;

            target.MemberGroups = GetMemberGroups(source.Username);
            target.IsLockedOut = source.IsLockedOut;
            target.IsApproved = source.IsApproved;
            target.ContentType = memberTypeService.Get(source.ContentType.Alias);
        }
    }
}