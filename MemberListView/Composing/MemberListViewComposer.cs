using MemberListView.Indexing;
using MemberListView.Models.Mapping;
using MemberListView.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace MemberListView.Composing
{
    public class MemberListViewComposer : IComposer
    {
        public MemberListViewComposer()
        {
        }
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddOptions()
                .Configure<Config.MemberListView>(builder.Config.GetSection(nameof(Config.MemberListView)));

            // Extend the Member Index fieldset.
            builder.Services.ConfigureOptions<ConfigureMemberIndexOptions>();

            builder.MapDefinitions().Add<MemberListItemMapDefinition>();

            builder.Components().Append<MemberIndexingComponent>();
            builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesParsingHandler>();

            builder.Services.AddUnique<IMemberExtendedService, MemberExtendedService>();

        }
    }
}