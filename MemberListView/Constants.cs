namespace MemberListView
{
    internal static class Constants
    {
        public const string PluginName = "MemberListView";

        //internal static class Dashboards
        //{
        //    public const string MemberManager = "memberManager";
        //}

        internal static class PropertyEditors
        {
            public const string MemberListView = "memberListView";
        }

        internal static class Configuration
        {
            public const string ExportExcludedColumns = "memberListView:Export:ExcludedColumns";
        }

        internal static class Members
        {
            public const string Groups = "memberGroups";
            public const string MemberType = "memberType";
            public const string MemberApproved = "umbracoMemberApproved";
            public const string MemberLockedOut = "umbracoMemberLockedOut";
        }

        internal static class MimeTypes
        {
            public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string CSV = "application/octet-stream";
        }
    }
}