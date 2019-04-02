using Examine;
using Examine.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using UmbracoExamine;

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
                EnsureMembershipFlags(e, ApplicationContext.Current.Services.MemberService.GetById(e.NodeId));
            }
        }
        /// <summary>
        /// Make sure the isApproved and isLockedOut fields are setup properly in the index
        /// </summary>
        /// <param name="e"></param>
        /// <param name="node"></param>
        /// <remarks>
        ///  these fields are not consistently updated in the XML fragment when a member is saved (as they may never get set) so we have to do this.
        ///  </remarks>
        private void EnsureMembershipFlags(IndexingNodeDataEventArgs e, IContentBase node)
        {
            Func<string, bool> valueExists = fieldName => {
                return e.Node.Nodes().Any(n => n is XElement ? (n as XElement).Name == fieldName : false);
            };

            if (!valueExists(Constants.Conventions.Member.IsLockedOut) || !valueExists(Constants.Conventions.Member.IsApproved))
            {
                // We need to augment from the database.
                var member = ApplicationContext.Current.Services.MemberService.GetById(e.NodeId);
                if (!e.Fields.ContainsKey(Constants.Conventions.Member.IsLockedOut) && !valueExists(Constants.Conventions.Member.IsLockedOut))
                {
                    e.Fields.Add(Constants.Conventions.Member.IsLockedOut, member.IsLockedOut.ToString().ToLower());
                }
                if (!e.Fields.ContainsKey(Constants.Conventions.Member.IsApproved) && !valueExists(Constants.Conventions.Member.IsApproved))
                {
                    e.Fields.Add(Constants.Conventions.Member.IsApproved, member.IsApproved.ToString().ToLower());
                }
            }
        }
    }
}
