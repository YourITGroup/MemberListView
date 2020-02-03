using AutoMapper;
using Examine;
using MemberListView.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Mapping;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;
using CoreConstants = Umbraco.Core.Constants;

namespace MemberListView.Models.Mapping
{
    public class MemberItemMapper : MapperConfiguration
    {
        public override void ConfigureMappings(IConfiguration config, ApplicationContext applicationContext)
        {
            //FROM SearchResult TO MemberListItem - used when searching for members.
            config.CreateMap<SearchResult, MemberListItem>()
                .ForMember(member => member.Id, expression => expression.MapFrom(user => user.Id))
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
                .ForMember(dto => dto.Properties, expression => expression.ResolveUsing(new MemberSearchPropertiesResolver()))
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
                });

            config.CreateMap<ISearchResults, IEnumerable<MemberListItem>>()
                  .ConvertUsing(results => results.Select(Mapper.Map<MemberListItem>));
        }


        /// <summary>
        /// A resolver to map <see cref="IMember"/> properties to a collection of <see cref="ContentPropertyBasic"/>
        /// </summary>
        internal class MemberSearchPropertiesResolver : IValueResolver
        {
            public ResolutionResult Resolve(ResolutionResult source)
            {
                if (source.Value != null && (source.Value is SearchResult) == false)
                    throw new AutoMapperMappingException(string.Format("Value supplied is of type {0} but expected {1}.\nChange the value resolver source type, or redirect the source value supplied to the value resolver using FromMember.", new object[]
                    {
                    source.Value.GetType(),
                    typeof (SearchResult)
                    }));
                return source.New(
                    //perform the mapping with the current umbraco context
                    ResolveCore(source.Context.GetUmbracoContext(), (SearchResult)source.Value), typeof(IEnumerable<ContentPropertyDisplay>));
            }

            private IEnumerable<ContentPropertyBasic> ResolveCore(UmbracoContext umbracoContext, SearchResult content)
            {
                var memberType = ApplicationContext.Current.Services.MemberTypeService.Get(content.Fields["nodeTypeAlias"]);

                var fields = content.Fields.Where(field => !field.Key.StartsWith("_") && !field.Key.StartsWith("umbraco") && !field.Key.EndsWith("_searchable") &&
                                                                            field.Key != "id" && field.Key != "key" &&
                                                                            field.Key != "updateDate" && field.Key != "writerName" &&
                                                                            field.Key != "loginName" && field.Key != "email" &&
                                                                            field.Key != CoreConstants.Conventions.Member.IsApproved &&
                                                                            field.Key != CoreConstants.Conventions.Member.IsLockedOut &&
                                                                            field.Key != "nodeName" && field.Key != "nodeTypeAlias")
                                    .Select(field => new ContentPropertyBasic { Alias = field.Key, Value = field.Value });

                //now update the IsSensitive value
                foreach (var prop in fields)
                {
                    //check if this property is flagged as sensitive
                    var isSensitiveProperty = memberType.IsSensitiveProperty(prop.Alias);
                    //check permissions for viewing sensitive data
                    if (isSensitiveProperty && umbracoContext.Security.CurrentUser.HasAccessToSensitiveData() == false)
                    {
                        //mark this property as sensitive
                        prop.IsSensitive = true;
                        //clear the value
                        prop.Value = null;
                    }
                }
                return fields;
            }
        }
    }
}