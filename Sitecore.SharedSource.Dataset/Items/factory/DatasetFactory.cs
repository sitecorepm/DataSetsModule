using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.Dataset.Items
{
    public static class DatasetFactory
    {
        public static IDatasetItem Create(Item item)
        {
            IDatasetItem q = null;
            switch (item.TemplateName)
            {
                case "SharepointQuery":
                    q = new SharepointQueryItem(item);
                    break;
                case "ItemsQuery":
                    q = new ItemsQueryItem(item);
                    break;
                case "StaticItemSet":
                    q = new StaticItemSetItem(item);
                    break;
                case "DatabaseQuery":
                    q = new DatabaseQueryItem(item);
                    break;
                case "DatabaseArguementsQuery":
                    q = new DatabaseArguementsQueryItem(item);
                    break;
                case "ContextItemQuery":
                    q = new ContextItemQueryItem(item);
                    break;
                case "ItemsSubset":
                    q = new ItemsSubset(item);
                    break;
                case "XmlFeed":
                    q = new XmlFeedItem(item);
                    break;
                default:
                    throw new Exception(string.Format("Invalid dataset type. [{0}][{1}]", item.TemplateName, item.Paths.Path));
            }
            return q;
        }
    }
}
