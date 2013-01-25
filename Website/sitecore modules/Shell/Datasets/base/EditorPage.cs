using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Dataset.Extensions;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;

namespace Sitecore.SharedSource.Dataset.UI
{
    public abstract class EditorPage : System.Web.UI.Page
    {
        private Item _contextItem;
        protected Item ContextItem
        {
            get
            {
                if (_contextItem == null)
                {
                    Database db = Factory.GetDatabase("master");
                    _contextItem = db.GetItem(Request.Params["id"]);
                }
                return _contextItem;
            }
        }

        private Field _contextField;
        protected Field ContextField
        {
            get
            {
                if (_contextField == null)
                    _contextField = this.ContextItem.Fields[Request.Params["field"]];
                return _contextField;
            }
        }

        /// <summary>
        /// Set this in your Page_Load event with javascript to retrieve the current value of this field/item.
        /// </summary>
        /// <param name="script"></param>
        protected void GetValueJavascript(string script)
        {
            this.AddClientScriptBlock("scGetFrameValue", "function scGetFrameValue(value, request) {\n " + script + "\n }");
        }
    }
}
