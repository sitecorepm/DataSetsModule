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

using Sitecore.SharedSource.Dataset.Caching;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;


namespace Sitecore.SharedSource.Dataset.Items
{
    public class ContextItemQueryItem : ItemDataset
    {
        public ContextItemQueryItem(Item item) : base(item) { }

        public override TemplateItem ItemTemplate
        {
            get
            {
                return this.InnerItem.GetReferenceField(IDs.Dataset.ContextItemQuery.ItemTemplate).TargetItem;
            }
        }

        protected override TimeSpan CacheTimeout
        {
            get
            {
                return TimeSpan.Zero;
            }
        }

        private static TimeSpan _exceptionCacheTimeout = new TimeSpan(0, 0, 2);
        protected override TimeSpan ExceptionCacheTimeout
        {
            get
            {
                return _exceptionCacheTimeout;
            }
        }

        protected override Item[] RefreshItems()
        {
            var items = new Item[] { };
            var target = Sitecore.Context.Item;

            if (target != null)
            {
                if (string.IsNullOrEmpty(this.InnerItem[IDs.Dataset.ItemsQuery.Query]))
                    items = target.GetChildren().ToArray();
                else
                    items = target.Axes.SelectItems(this.InnerItem[IDs.Dataset.ItemsQuery.Query]);

                if (items.Length > 0 &&
                    !items[0].Template.InheritsFrom(this.ItemTemplate))
                {
                    // Only return if items returned match or inherit from the ItemTemplate
                    items = new Item[] { };
                }
            }
            return items;
        }
    }
}
