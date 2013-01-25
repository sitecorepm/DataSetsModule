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
    public class ItemsQueryItem : ItemDataset
    {
        public ItemsQueryItem(Item item) : base(item) { }

        protected override Item[] RefreshItems()
        {
            Item[] items;
            var target = this.InnerItem.GetReferenceField(IDs.Dataset.ItemsQuery.Target).TargetItem;
            if (target == null)
                throw new Exception(string.Format("ItemsQueryItem target is NULL or cannot be resolved to an item. [dataset:{0}][target:{1}]", this.InnerItem.Paths.Path, this.InnerItem[IDs.Dataset.ItemsQuery.Target]));

            if (string.IsNullOrEmpty(this.InnerItem[IDs.Dataset.ItemsQuery.Query]))
                items = target.GetChildren().ToArray();
            else
                items = target.Axes.SelectItems(this.InnerItem[IDs.Dataset.ItemsQuery.Query]);
            return items;
        }
    }
}
