using System;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore.SharedSource.Dataset.Items;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System.Web;
using Sitecore;
using System.Collections.Generic;

namespace Sitecore.SharedSource.Dataset.UI.DatasetRenderer
{
    public partial class DatasetRendererTemplateEditor : System.Web.UI.Page
    {
        private string[] AvailableFields
        {
            get { return (string[])ViewState["AvailableFields"]; }
            set { ViewState["AvailableFields"] = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsPostBack)
            {
                var handle = UrlHandle.Get();
                txtTemplate.Text = handle["value"];
                rptrFields.DataSource = handle["fields"].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (rptrFields.DataSource != null)
                    rptrFields.DataBind();
            }
        }

        protected void btnOK_Click(object sender, EventArgs e)
        {
            var value = StringUtil.EscapeJavascriptString(txtTemplate.Text);
            //if (string.IsNullOrEmpty(value))
            //    value = "__#!$No value$!#__";
            this.ClientScript.RegisterStartupScript(this.GetType(), "SetDialogValue", "window.returnValue=" + value + ";", true);
            CloseWindow();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            this.ClientScript.RegisterStartupScript(this.GetType(), "CloseWindow", "window.close();", true);
        }
    }
}
