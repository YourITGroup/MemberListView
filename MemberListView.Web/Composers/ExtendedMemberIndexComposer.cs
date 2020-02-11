using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;
using static Umbraco.Core.Constants;

namespace MemberListView.Web.Composers
{
    class ExtendedMemberIndexComposer
    {
    }

    public class ExtendedMemberIndexComposer : ComponentComposer<ExtendedMemberIndexComponent> { }

    public class ExtendedMemberIndexComponent : IComponent
    {
        private readonly IExamineManager _examineManager;

        public ExtendedMemberIndexComponent(IExamineManager examineManager)
        {
            _examineManager = examineManager;
        }

        public void Initialize()
        {
            // get the member index
            if (!_examineManager.TryGetIndex(UmbracoIndexes.MembersIndexName, out IIndex index))
                return;

            index.
            // add a custom field type
            index.FieldDefinitionCollection.TryAdd(new FieldDefinition("price", FieldDefinitionTypes.Double));

            // modify an existing field type (not recommended)
            index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition("parentID", FieldDefinitionTypes.FullText));
        }

        public void Terminate()
        {
        }

        private void AddGroups(IndexingNodeDataEventArgs e, IMember member)
        {
            var groups = Roles.GetRolesForUser(member.Username);
            e.Fields.Add(Constants.Members.Groups, groups.Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list}, {group}"));
            e.Fields.Add($"_{Constants.Members.Groups}", groups.Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list}, {group}"));
        }

    }
}
