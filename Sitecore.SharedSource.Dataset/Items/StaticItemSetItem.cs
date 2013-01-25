using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.UI;

using Sitecore;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.WebControls;

using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;


namespace Sitecore.SharedSource.Dataset.Items
{
    public class StaticItemSetItem : ItemDataset
    {
        public StaticItemSetItem(Item item) : base(item) { }

        protected override Item[] RefreshItems()
        {
            return this.InnerItem.GetMultilistField(IDs.Dataset.StaticItemSet.Items).GetItems();
        }
    }
}
