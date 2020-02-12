using Examine;
using Examine.Providers;
using System.Linq;
using System.Web.Security;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using UmbracoExamine;
using CoreConstants = Umbraco.Core.Constants;

namespace MemberListView.EventHandlers
{
    public class MembershipIndexEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            foreach (var provider in ExamineManager.Instance.IndexProviderCollection.AsEnumerable<BaseIndexProvider>())
            {
                if (provider.Name == "InternalMemberIndexer")
                    provider.GatheringNodeData += provider_GatheringNodeData;
            }
        }

        void provider_GatheringNodeData(object sender, IndexingNodeDataEventArgs e)
        {
            if (e.IndexType == IndexTypes.Member)
            {
                var member = ApplicationContext.Current.Services.MemberService.GetById(e.NodeId);
                EnsureMembershipFlags(e, member);
                AddGroups(e, member);
            }
        }

        private void AddGroups(IndexingNodeDataEventArgs e, IMember member)
        {
            var groups = Roles.GetRolesForUser(member.Username);
            e.Fields.Add(Constants.Members.Groups, groups.Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list}, {group}"));
            e.Fields.Add($"_{Constants.Members.Groups}", groups.Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list}  {group}"));
        }

        /// <summary>
        /// Make sure the isApproved and isLockedOut fields are setup properly in the index
        /// </summary>
        /// <param name="e"></param>
        /// <param name="member"></param>
        /// <remarks>
        ///  these fields are not consistently updated in the XML fragment when a member is saved (as they may never get set) so we have to do this.
        ///  </remarks>
        private void EnsureMembershipFlags(IndexingNodeDataEventArgs e, IMember member)
        {
            bool valueExists(string fieldName)
            {
                return e.Node.Nodes().Any(n => n is XElement ? (n as XElement).Name == fieldName : false);
            }

            if (!valueExists(CoreConstants.Conventions.Member.IsLockedOut) || !valueExists(CoreConstants.Conventions.Member.IsApproved))
            {
                // We need to augment from the database.
                if (!e.Fields.ContainsKey(CoreConstants.Conventions.Member.IsLockedOut) && !valueExists(CoreConstants.Conventions.Member.IsLockedOut))
                {
                    e.Fields.Add(CoreConstants.Conventions.Member.IsLockedOut, member.IsLockedOut.ToString().ToLower());
                }
                if (!e.Fields.ContainsKey(CoreConstants.Conventions.Member.IsApproved) && !valueExists(CoreConstants.Conventions.Member.IsApproved))
                {
                    e.Fields.Add(CoreConstants.Conventions.Member.IsApproved, member.IsApproved.ToString().ToLower());
                }
            }
        }
    }
}
