using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Indexing;

// See https://shazwazza.github.io/Examine/configuration for details
internal class ConfigureMemberIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{

    public void Configure(string name, LuceneDirectoryIndexOptions options)
    {
        switch (name)
        {
            case UmbracoIndexes.MembersIndexName:
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.Comments), FieldDefinitionTypes.FullText));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.IsLockedOut), FieldDefinitionTypes.FullTextSortable));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.IsApproved), FieldDefinitionTypes.FullTextSortable));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.FailedPasswordAttempts), FieldDefinitionTypes.Integer));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.LastLoginDate), FieldDefinitionTypes.DateTime));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.LastLockoutDate), FieldDefinitionTypes.DateTime));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(nameof(IMember.LastPasswordChangeDate), FieldDefinitionTypes.DateTime));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Members.Groups, FieldDefinitionTypes.FullTextSortable));
                //// Set the "Price" field to map to the 'Double' value type.
                //options.FieldDefinitions.AddOrUpdate(
                //    new FieldDefinition("Price", FieldDefinitionTypes.Double));
                break;
        }
    }

    public void Configure(LuceneDirectoryIndexOptions options)
        => Configure(string.Empty, options);
}