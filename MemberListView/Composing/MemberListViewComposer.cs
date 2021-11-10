using MemberListView.Indexing;
using MemberListView.Models.Mapping;
using MemberListView.Services;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Extensions;
using Microsoft.Extensions.DependencyInjection;
#else
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Mapping;
#endif

namespace MemberListView.Composing
{
#if NET5_0_OR_GREATER
    public class MemberListViewComposer : IComposer
#else
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class MemberListViewComposer : IUserComposer
#endif
    {
        public MemberListViewComposer()
        {
        }
#if NET5_0_OR_GREATER
        public void Compose(IUmbracoBuilder builder)
        {
            builder.MapDefinitions().Add<MemberListItemMapDefinition>();

            builder.Components().Append<MemberIndexingComponent>();
            builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesParsingHandler>();

            builder.Services.AddUnique<IMemberExtendedService, MemberExtendedService>();

            // Extend the Member Index fieldset.
            builder.Services.ConfigureOptions<ConfigureMemberIndexOptions>();
        }
#else
        public void Compose(Composition composition)
        {
            composition.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
                .Add<MemberListItemMapDefinition>();

            composition.Components().Append<MemberListViewComponent>();
            composition.Components().Append<MemberIndexingComponent>();

            composition.Register<IMemberExtendedService, MemberExtendedService>();
        }
#endif
    }
}