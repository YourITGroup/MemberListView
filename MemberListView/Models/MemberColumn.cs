using System.Runtime.Serialization;

namespace MemberListView.Models
{
    [DataContract]
    public class MemberColumn
    {
        [DataMember(Name = "id")]
        public string? Id { get; set; }

        [DataMember(Name = "alias")]
        public string? Alias { get; set; }

        [DataMember(Name = "name")]
        public string? Name { get; set; }
    }
}