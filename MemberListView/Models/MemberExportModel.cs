using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Umbraco.Core.Models;

namespace MemberListView.Models
{
    public class MemberExportModel
    {
        public int Id { get; set; }

        public object Key { get; set; }

        public string Name { get; set; }

        public string LoginName { get; set; }

        public string Email { get; set; }

        public bool IsApproved { get; set; }

        public bool IsLockedOut { get; set; }

        public string MemberType { get; set; }

        public string Groups { get; set; }

        private Dictionary<string, string> properties;
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
}