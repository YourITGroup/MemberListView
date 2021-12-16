using System.Collections.Generic;
using System.Runtime.Serialization;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
#else
using Umbraco.Core.Models;
using Umbraco.Web.Models.ContentEditing;
#endif

namespace MemberListView.Models
{
    public class MemberListItem : MemberBasic
    {
        [DataMember(Name = "isApproved")]
        public bool IsApproved { get; set; }

        [DataMember(Name = "isLockedOut")]
        public bool IsLockedOut { get; set; }

        [DataMember(Name = "memberType")]
        public IMemberType ContentType { get; set; }

        [DataMember(Name = "memberGroups")]
        public IEnumerable<string> MemberGroups { get; set; }

    }
}