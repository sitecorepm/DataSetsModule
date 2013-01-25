using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Web.UI.Sheer;

using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.Items;

namespace Sitecore.SharedSource.Dataset.UI.DatasetRenderer
{
    public partial class Select : DatasetRendererPage
    {
        const string _CHECKBOXITEM = "<td><input id='{0}' value='{1}' type=checkbox><label for='{0}'>{2}</label></td>";

        protected void Page_Load(object sender, EventArgs e)
        {
            this.jQueryReadyScript("Call-CheckSelectedFields", "CheckSelectedFields();");
            var dataset = this.ContextDatasetRenderer.Dataset;

            if (dataset != null)
                GetValueJavascript("return GetValue();");
            else
                GetValueJavascript("return $j('#" + txtValue.ClientID + "').text();");

            if (!IsPostBack)
            {
                var selectedFields = this.ContextDatasetRenderer.SelectFields;

                hdnValue.Value = this.ContextItem[CpnIDs.DatasetRenderer.Select].IfNullOrEmpty("");

                if (dataset != null)
                {
                    var rows = dataset.FieldMap.OrderBy(x => x.Value)
                                    .Select((map, index) => new
                                    {
                                        row = string.Format(_CHECKBOXITEM, map.Key, selectedFields.Any(x => x == map.Key) ? "checked" : string.Empty, map.Value),
                                        index
                                    })
                                    .GroupBy(g => g.index / 5, x => x.row)
                                    .Select(g => string.Join(string.Empty, g.ToArray()));

                    rptrList.DataSource = rows;
                    rptrList.DataBind();
                }
                else
                {
                    pnlDatasetComponentSource.Visible = true;
                    pnlDatasetRenderer.Visible = false;

                    txtValue.Text = this.ContextItem[CpnIDs.DatasetRenderer.Select].IfNullOrEmpty("");
                }
            }
        }
    }
}
