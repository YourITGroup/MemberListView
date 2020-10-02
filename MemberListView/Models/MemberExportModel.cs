using System;
using System.Collections.Generic;

namespace MemberListView.Models
{
    public class MemberExportModel
    {
        public int Id { get; set; }

        public object Key { get; set; }

        public string Name { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool IsApproved { get; set; }

        public bool IsLockedOut { get; set; }

        public string MemberType { get; set; }

        public List<string> Groups { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        private Dictionary<string, object> properties;
        public IDictionary<string, object> Properties
        {
            get
            {
                if (properties == null)
                    properties = new Dictionary<string, object>();
                return properties;
            }
        }

    }
}