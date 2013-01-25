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
    public class ItemsSubset : ItemDataset
    {
        public ItemsSubset(Item item) : base(item) { }


        public override TemplateItem ItemTemplate
        {
            get
            {
                return this.InnerItem.GetReferenceField(IDs.Dataset.ItemsSubset.ItemTemplate).TargetItem;
            }
        }

        protected override Dictionary<string, string> RefreshFieldMap()
        {
            var map = base.RefreshFieldMap();
            return CreateFieldMapFromItemTemplate(this.ItemTemplate, map);
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
            return null;
        }
    }
}
