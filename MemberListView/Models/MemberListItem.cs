using System.Collections.Generic;
using System.Runtime.Serialization;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Mapping;
using Umbraco.Web.Models.ContentEditing;

namespace MemberListView.Models
{
    [DataContract(Name = "content", Namespace = "")]
    public class MemberListItem
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "key")]
        public object Key { get; set; }

        [DataMember(Name = "icon")]
        public string Icon
        {
            get
            {
                return ContentType != null ? ContentType.Icon : "user-icon";
            }
        }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "loginName")]
        public string LoginName { get; set; }

        [DataMember(Name = "isApproved")]
        public bool IsApproved { get; set; }

        [DataMember(Name = "isLockedOut")]
        public bool IsLockedOut { get; set; }

        [DataMember(Name = "memberType")]
        public IMemberType ContentType { get; set; }

        private Dictionary<string, string> properties;
        [DataMember(Name = "properties")]
        public IDictionary<string, string> Properties
        {
            get
            {
                if (properties == null)
                    properties = new Dictionary<string, string>();
                return properties;
            }
        }
    }

    [DataContract(Name = "property", Namespace = "")]
    public class MemberProperty
    {
        [DataMember(Name = "alias")]
        public string Alias { get; set; }

        [DataMember(Name = "value")]
        public object Value { get; set; }
    }
}