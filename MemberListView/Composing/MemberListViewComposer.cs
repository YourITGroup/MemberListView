using MemberListView.Models.Mapping;
using MemberListView.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Mapping;

namespace MemberListView.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class MemberListViewComposer : IUserComposer, IComposer, IDiscoverable
    {
        public MemberListViewComposer()
        {
        }

        public void Compose(Composition composition)
        {
            composition.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
                .Add<MemberListItemMapDefinition>();

            composition.Components().Append<MemberListViewComponent>();

            composition.Register<IMemberExtendedService, MemberExtendedService>();
        }
    }
}