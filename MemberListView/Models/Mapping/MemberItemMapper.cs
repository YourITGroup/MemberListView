using AutoMapper;
using Examine;
using MemberListView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Mapping;

namespace MemberListView.Models.Mapping
{
    public class MemberItemMapper : MapperConfiguration
    {
        public override void ConfigureMappings(AutoMapper.IConfiguration config, Umbraco.Core.ApplicationContext applicationContext)
        {
            //FROM SearchResult TO MemberListItem - used when searching for members.
            config.CreateMap<SearchResult, MemberListItem>()
                //.ForMember(member => member.Id, expression => expression.MapFrom(result => result.Id))
                .ForMember(member => member.Key, expression => expression.Ignore())

                .ForMember(member => member.LoginName,
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
                        Guid key;
                        if (Guid.TryParse(searchResult.Fields["__key"], out key))
                        {
                            member.Key = key;
                        }
                    }

                    if (searchResult.Fields.ContainsKey("__Key") && searchResult.Fields["__Key"] != null)
                    {
                        Guid key;
                        if (Guid.TryParse(searchResult.Fields["__Key"], out key))
                        {
                            member.Key = key;
                        }
                    }

                    bool val = true;
                    // We assume not approved for this property.
                    member.IsApproved = false;
                    // We assume not locked out for this property.
                    member.IsLockedOut = false;

                    if (!searchResult.Fields.ContainsKey(Constants.Conventions.Member.IsApproved) ||
                        !searchResult.Fields.ContainsKey(Constants.Conventions.Member.IsLockedOut))
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
                        if (searchResult.Fields[Constants.Conventions.Member.IsApproved] == "1")
                            member.IsApproved = true;
                        else if (bool.TryParse(searchResult.Fields[Constants.Conventions.Member.IsApproved], out val))
                            member.IsApproved = val;

                        if (searchResult.Fields[Constants.Conventions.Member.IsLockedOut] == "1")
                            member.IsLockedOut = true;
                        else if (bool.TryParse(searchResult.Fields[Constants.Conventions.Member.IsLockedOut], out val))
                            member.IsLockedOut = val;
                    }

                    // Get any other properties available from the fields.
                    foreach (var field in searchResult.Fields)
                    {
                        if (!field.Key.StartsWith("_") && !field.Key.StartsWith("umbraco") && !field.Key.EndsWith("_searchable") &&
                            field.Key != "id" && field.Key != "key" && 
                            field.Key != "updateDate" && field.Key != "writerName" &&
                            field.Key != "loginName" && field.Key != "email" &&
                            field.Key != Constants.Conventions.Member.IsApproved && 
                            field.Key != Constants.Conventions.Member.IsLockedOut &&
                            field.Key != "nodeName" && field.Key != "nodeTypeAlias")
                        {
                            member.Properties.Add(field.Key, field.Value);
                        }

                    }
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
                            member.Properties.Add(p.Key, p.Value);
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