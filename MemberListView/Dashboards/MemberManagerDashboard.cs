using System;
using Umbraco.Core.Composing;
using Umbraco.Core.Dashboards;

namespace MemberListView.Dashboards
{
    [Weight(-10)]
    public class MemberManagerDashboard : IDashboard
    {
        public string[] Sections => new[]
        {
            Umbraco.Core.Constants.Applications.Members
        };
        public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();

        public string Alias => Constants.Dashboards.MemberManager;

        public string View => "/App_Plugins/MemberListView/dashboard/memberManager.html";
    }
}