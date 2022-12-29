namespace MemberListView
{
    internal static class Constants
    {
        internal const string PluginName = "MemberListView";

        //internal static class Dashboards
        //{
        //    internal const string MemberManager = "memberManager";
        //}

        internal static class PropertyEditors
        {
            internal const string MemberListView = "memberListView";
        }

        internal static class Configuration
        {
            internal const string SectionName = "MemberListView";
            internal const string ExportExcludedColumns = "ExportExcludedColumns";
        }

        internal static class Members
        {
            internal const string Ids = "ids";
            internal const string Groups = "memberGroups";
            internal const string MemberType = "memberType";
            internal const string MemberApproved = "umbracoMemberApproved";
            internal const string MemberLockedOut = "umbracoMemberLockedOut";
        }

        internal static class MimeTypes
        {
            internal const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            internal const string CSV = "application/octet-stream";
        }
    }
}