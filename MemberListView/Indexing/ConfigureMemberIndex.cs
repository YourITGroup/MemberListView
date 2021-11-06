#if NET5_0_OR_GREATER
using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Indexing
{
    // See https://shazwazza.github.io/Examine/configuration for details
    internal class ConfigureMemberIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
    {
        //private readonly ILoggerFactory _loggerFactory;

        //public ConfigureMemberIndexOptions(ILoggerFactory loggerFactory)
        //    => _loggerFactory = loggerFactory;

        public void Configure(string name, LuceneDirectoryIndexOptions options)
        {
            switch (name)
            {
                case UmbracoIndexes.MembersIndexName:
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.Comments, FieldDefinitionTypes.FullText));
                            
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.IsLockedOut, FieldDefinitionTypes.FullTextSortable));
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.IsApproved, FieldDefinitionTypes.FullTextSortable));
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.FailedPasswordAttempts, FieldDefinitionTypes.Integer));
                            
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.LastLoginDate, FieldDefinitionTypes.DateTime));
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.LastLockoutDate, FieldDefinitionTypes.DateTime));
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Conventions.Member.LastPasswordChangeDate, FieldDefinitionTypes.DateTime));
                            
                    options.FieldDefinitions.AddOrUpdate(new FieldDefinition(Constants.Members.Groups, FieldDefinitionTypes.FullTextSortable));
                    // Set the "Price" field to map to the 'Double' value type.
                    options.FieldDefinitions.AddOrUpdate(
                        new FieldDefinition("Price", FieldDefinitionTypes.Double));
                    break;
            }
        }

        public void Configure(LuceneDirectoryIndexOptions options)
            => Configure(string.Empty, options);
    }
}
#endif