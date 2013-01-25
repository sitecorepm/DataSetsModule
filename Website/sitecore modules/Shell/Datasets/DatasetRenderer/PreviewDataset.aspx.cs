using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Sitecore.SharedSource.Dataset.Items;

namespace Sitecore.SharedSource.Dataset.UI.DatasetRenderer
{
    public partial class PreviewDataset : DatasetRendererPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Sitecore.Context.Database = Sitecore.Configuration.Factory.GetDatabase("master");
            IDatasetItem dsItem = this.ContextDatasetRenderer.Dataset;

            if (dsItem != null)
            {
                var dt = dsItem.Data;

                // translate column names
                //foreach (DataColumn dc in dt.Columns)
                //    dc.ColumnName = dsItem.FieldMap[dc.ColumnName];

                gvDataset.DataSource = dt;
                if (gvDataset.DataSource != null)
                    gvDataset.DataBind();
            }
        }
    }
}
