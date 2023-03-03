using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Indexing;

// See https://shazwazza.github.io/Examine/configuration for details
internal class ConfigureMemberIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
    {
        switch (name)
        {
            case UmbracoIndexes.MembersIndexName:
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.Comments, FieldDefinitionTypes.FullText));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.IsLockedOut, FieldDefinitionTypes.FullTextSortable));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.IsApproved, FieldDefinitionTypes.FullTextSortable));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.FailedPasswordAttempts, FieldDefinitionTypes.Integer));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.LastLoginDate, FieldDefinitionTypes.DateTime));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.LastLockoutDate, FieldDefinitionTypes.DateTime));
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Indexing.LastPasswordChangeDate, FieldDefinitionTypes.DateTime));

                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Members.Groups, FieldDefinitionTypes.FullTextSortable));

                break;
        }
    }

    public void Configure(LuceneDirectoryIndexOptions options)
        => Configure(null, options);
}