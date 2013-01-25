using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.Items;

namespace Sitecore.SharedSource.Dataset.UI.DatasetRenderer
{
    public partial class ListItemFields : DatasetRendererPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var dataset = this.ContextDatasetRenderer.Dataset;
            if (dataset != null)
            {
                if (this.ContextDatasetRenderer.SelectFields.Length > 0)
                    rptrFields.DataSource = this.ContextDatasetRenderer.SelectFields.Select(delegate(string x){
                        if (dataset.FieldMap.ContainsKey(x))
                            return dataset.FieldMap[x];
                        else
                            return "('" + x + "' not in field list)";
                    }).OrderBy(x => x);
                else
                    rptrFields.DataSource = dataset.FieldMap.Values.OrderBy(x => x);
            }

            if (rptrFields.DataSource != null)
                rptrFields.DataBind();
        }
    }
}
