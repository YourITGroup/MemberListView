#if !NET5_0_OR_GREATER
using MemberListView.Controllers;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core.Composing;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

namespace MemberListView.Composing
{
    public class MemberListViewComponent : IComponent
    {
        public void Initialize()
        {
            ServerVariablesParser.Parsing += new EventHandler<Dictionary<string, object>>(ServerVariablesParser_Parsing);
            
        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("HttpContext is null");

            UrlHelper urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            string memberListViewBaseUrl = urlHelper.GetUmbracoApiServiceBaseUrl<ExtendedMemberController>(controller => controller.GetCanExport());

            Dictionary<string, object> umbracoUrls;
            if (e.TryGetValue("umbracoUrls", out object found))
            {
                umbracoUrls = found as Dictionary<string, object>;
            }
            else
            {
                umbracoUrls = new Dictionary<string, object>();
                e["umbracoUrls"] = umbracoUrls;
            }

            if (!umbracoUrls.ContainsKey(nameof(memberListViewBaseUrl)))
            {
                umbracoUrls[nameof(memberListViewBaseUrl)] = memberListViewBaseUrl;
            }
        }

        public void Terminate()
        {
        }
    }
}
#endif
