using AutoMapper;
using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.Mapping;
using Umbraco.Web.Models.ContentEditing;
using CoreConstants = Umbraco.Core.Constants;

namespace MemberListView.Models.Mapping
{
    public class MemberItemMapper : MapperConfiguration
    {
        public override void ConfigureMappings(AutoMapper.IConfiguration config, Umbraco.Core.ApplicationContext applicationContext)
        {
            //FROM SearchResult TO MemberListItem - used when searching for members.
            config.CreateMap<SearchResult, MemberListItem>()
                .ForMember(member => member.Id, expression => expression.MapFrom(user => int.MaxValue))
                .ForMember(display => display.Udi, expression => expression.Ignore())
                .ForMember(member => member.CreateDate, expression => expression.Ignore())//.MapFrom(user => user.CreationDate))
                .ForMember(member => member.UpdateDate, expression => expression.MapFrom(result => result["updateDate"]))
                .ForMember(member => member.Owner, expression => expression.UseValue(new UserProfile { Name = "Admin", UserId = 0 }))
                .ForMember(member => member.Icon, expression => expression.UseValue("icon-user"))
                .ForMember(member => member.Properties, expression => expression.Ignore())
                .ForMember(member => member.ParentId, expression => expression.Ignore())
                .ForMember(member => member.Path, expression => expression.Ignore())
                .ForMember(member => member.SortOrder, expression => expression.Ignore())
                .ForMember(member => member.AdditionalData, expression => expression.Ignore())
                .ForMember(member => member.Published, expression => expression.Ignore())
                .ForMember(member => member.Updater, expression => expression.Ignore())
                .ForMember(member => member.Trashed, expression => expression.Ignore())
                .ForMember(member => member.Alias, expression => expression.Ignore())
                .ForMember(member => member.ContentTypeAlias, expression => expression.Ignore())
                .ForMember(member => member.HasPublishedVersion, expression => expression.Ignore())
                .ForMember(member => member.MemberGroups, expression => expression.MapFrom(result => result[Constants.Members.Groups]
                                                                                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                                                        .Select(g => g.Trim())))

                .ForMember(member => member.Key, expression => expression.Ignore())

                .ForMember(member => member.Username,
                    expression => expression.MapFrom(result => result.Fields["loginName"]))

                .ForMember(member => member.Name,
                    expression => expression.MapFrom(result => result.Fields["nodeName"]))

                .ForMember(member => member.ContentType,
                    expression => expression.MapFrom(result => ApplicationContext.Current.Services.MemberTypeService.Get(result.Fields["nodeTypeAlias"])))

                .ForMember(member => member.Email,
                    expression => expression.MapFrom(result =>
                        result.Fields.ContainsKey("email") ? result.Fields["email"] : string.Empty))

                .ForMember(member => member.IsApproved, expression => expression.Ignore())

                .ForMember(member => member.IsLockedOut, expression => expression.Ignore())

                .ForMember(member => member.Icon, expression => expression.Ignore())
                .ForMember(member => member.Properties, expression => expression.Ignore())

                .AfterMap((searchResult, member) =>
                {
                    if (searchResult.Fields.ContainsKey("__key") && searchResult.Fields["__key"] != null)
                    {
                        if (Guid.TryParse(searchResult.Fields["__key"], out Guid key))
                        {
                            member.Key = key;
                        }
                    }

                    if (searchResult.Fields.ContainsKey("__Key") && searchResult.Fields["__Key"] != null)
                    {
                        if (Guid.TryParse(searchResult.Fields["__Key"], out Guid key))
                        {
                            member.Key = key;
                        }
                    }

                    bool val = true;
                    // We assume not approved for this property.
                    member.IsApproved = false;
                    // We assume not locked out for this property.
                    member.IsLockedOut = false;

                    if (!searchResult.Fields.ContainsKey(CoreConstants.Conventions.Member.IsApproved) ||
                        !searchResult.Fields.ContainsKey(CoreConstants.Conventions.Member.IsLockedOut))
                    {
                        // We need to get a member back from the database as these values aren't indexed reliably for some reason.
                        var m = ApplicationContext.Current.Services.MemberService.GetByKey((Guid)member.Key);
                        if (m != null)
                        {
                            member.IsApproved = m.IsApproved;
                            member.IsLockedOut = m.IsLockedOut;
                        }
                    }
                    else
                    {
                        if (searchResult.Fields[CoreConstants.Conventions.Member.IsApproved] == "1")
                            member.IsApproved = true;
                        else if (bool.TryParse(searchResult.Fields[CoreConstants.Conventions.Member.IsApproved], out val))
                            member.IsApproved = val;

                        if (searchResult.Fields[CoreConstants.Conventions.Member.IsLockedOut] == "1")
                            member.IsLockedOut = true;
                        else if (bool.TryParse(searchResult.Fields[CoreConstants.Conventions.Member.IsLockedOut], out val))
                            member.IsLockedOut = val;
                    }

                    // Get any other properties available from the fields.
                    member.Properties = searchResult.Fields.Where(field => !field.Key.StartsWith("_") && !field.Key.StartsWith("umbraco") && !field.Key.EndsWith("_searchable") &&
                                                                            field.Key != "id" && field.Key != "key" &&
                                                                            field.Key != "updateDate" && field.Key != "writerName" &&
                                                                            field.Key != "loginName" && field.Key != "email" &&
                                                                            field.Key != CoreConstants.Conventions.Member.IsApproved &&
                                                                            field.Key != CoreConstants.Conventions.Member.IsLockedOut &&
                                                                            field.Key != "nodeName" && field.Key != "nodeTypeAlias")
                                                            .Select(field => new ContentPropertyBasic { Alias = field.Key, Value = field.Value });
                });

            config.CreateMap<ISearchResults, IEnumerable<MemberListItem>>()
                  .ConvertUsing(results => results.Select(Mapper.Map<MemberListItem>));

            //FROM MemberListItem to MemberExportModel.
            config.CreateMap<MemberListItem, MemberExportModel>()
                .ForMember(member => member.MemberType,
                    expression => expression.MapFrom(item => item.ContentType.Name))
                .ForMember(member => member.Properties, expression => expression.Ignore())
                .AfterMap((listItem, member) =>
                    {
                        // Get any other properties available from the fields.
                        foreach (var p in listItem.Properties)
                        {
                            member.Properties.Add(p.Alias, p.Value.ToString());
                        }

                        // Resolve groups into a comma-delimited string.
                        var roles = ApplicationContext.Current.Services.MemberService.GetAllRoles(member.Id);
                        if (roles != null)
                        {
                            member.Groups = roles.Aggregate("", (a, b) => (a == "" ? a : a + ",") + b);
                        }

                    });

            config.CreateMap<IEnumerable<MemberListItem>, IEnumerable<MemberExportModel>>()
                  .ConvertUsing(results => results.Select(Mapper.Map<MemberExportModel>));

            //FROM SearchResult to MemberExportModel.
            config.CreateMap<SearchResult, MemberExportModel>()
                .ConvertUsing(result => Mapper.Map<MemberExportModel>(Mapper.Map<MemberListItem>(result)));

            config.CreateMap<IEnumerable<MemberListItem>, IEnumerable<MemberExportModel>>()
                  .ConvertUsing(items => items.Select(Mapper.Map<MemberExportModel>));
        }
    }
}