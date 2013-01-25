using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class PageExtensions
    {
        private static bool HeaderAccessible(Page p)
        {
            return p.Header != null;
        }

        private static HtmlLink CreateHtmlLink(string vcCssFile, string id)
        {
            HtmlLink link = new HtmlLink();
            link.ID = id;
            link.Attributes.Add("type", "text/css");
            link.Attributes.Add("rel", "stylesheet");
            link.Attributes.Add("href", vcCssFile);
            return link;
        }


        public static void AddHeaderLiteral(this Page p, string vcHtml)
        {
            LiteralControl lc = new LiteralControl();
            lc.Text = vcHtml;
            p.AddHeaderControl(lc, null);
        }

        /// <summary>
        /// You can use relative paths such as: ~/js/myscript.js
        /// </summary>
        /// <param name="vcJsFile"></param>
        public static void AddHeaderJsLink(this Page p, string vcJsFile)
        {
            if (HeaderAccessible(p))
            {
                ClientScriptManager csm = p.ClientScript;
                string key = System.IO.Path.GetFileName(vcJsFile);
                csm.RegisterClientScriptInclude(key, p.ResolveUrl(vcJsFile));
            }
            else
            {
                // Add the JS script tag inline
                string filepath = p.ResolveUrl(vcJsFile);
                p.Response.Write("<script language='javascript' type='text/javascript' src='" + filepath + "'></script>");
            }
        }

        /// <summary>
        /// You can use relative paths such as: ~/css/mystyles.css
        /// </summary>
        /// <param name="vcCssFile"></param>
        /// <param name="id"></param>
        public static void PrependHeaderCssLink(this Page p, string vcCssFile, string id)
        {
            p.AddHeaderCssLink(vcCssFile, id, 0);
        }
        public static void AddHeaderCssLink(this Page p, string vcCssFile, string id)
        {
            p.AddHeaderCssLink(vcCssFile, id, null);
        }
        public static void AddHeaderCssLink(this Page p, string vcCssFile, string id, int? iAddAt)
        {
            HtmlLink link = CreateHtmlLink(vcCssFile, id);
            if (HeaderAccessible(p))
                AddHeaderControl(p, link, iAddAt);
            else
            {
                // Add the CSS link inline
                string filepath = p.ResolveUrl(vcCssFile);
                p.Response.Write("<link href='" + filepath + "' rel='stylesheet' type='text/css' />");
            }
        }

        private static void AddHeaderControl(this Page p, Control ctl, int? iAddAt)
        {
            ctl.EnableViewState = false;
            // Don't add the control multiple times if we can help it...
            HtmlHead hh = p.Header;

            int len = hh.Controls.Cast<Control>()
                                    .Where<Control>(f => f.ClientID == ctl.ClientID)
                                    .ToArray<Control>().Length;
            if (len == 0)
            {
                if (iAddAt.HasValue)
                    p.Header.Controls.AddAt(iAddAt.Value, ctl);
                else
                    p.Header.Controls.Add(ctl);
            }

        }

        /// <summary>
        /// Appends the script to the bottom of the calling page wrapped inside a 
        /// jQuery ready() function.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="script"></param>
        public static void jQueryReadyScript(this Page p, string key, string script)
        {
            p.AddStartupScript(key, "$j(function(){" + script + "});");
        }

        public static void AddStartupScript(this Page p, string key, string script)
        {
            p.ClientScript.RegisterStartupScript(p.GetType(), key, script, true);
        }

        public static void AddClientScriptBlock(this Page p, string key, string script)
        {
            p.ClientScript.RegisterClientScriptBlock(p.GetType(), key, script, true);
        }
    }
}
