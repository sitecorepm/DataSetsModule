using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.Items;

namespace Sitecore.SharedSource.Dataset.UI
{
    public class DatasetRendererPage : EditorPage
    {
        private DatasetRendererItem _contextDatasetRenderer;
        protected DatasetRendererItem ContextDatasetRenderer
        {
            get
            {
                if (_contextDatasetRenderer == null)
                    _contextDatasetRenderer = (DatasetRendererItem)this.ContextItem;
                return _contextDatasetRenderer;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.AddHeaderJsLink("../js/jquery-1.7.2.min.js");
        }
    }
}
