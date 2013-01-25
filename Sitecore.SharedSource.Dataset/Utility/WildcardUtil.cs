using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Sitecore.Data.Items;
using Sitecore.Links;

namespace Sitecore.SharedSource.Dataset
{
    public class WildcardUtil
    {
        /// <summary>
        /// Gets a mapping of ItemID to dynamic name for wildcard items.
        /// So if a path is: /site/page/*/*
        /// And the incoming URL was: /site/page/foo/bar
        /// This would return the following (ID - urlpart):
        ///                     (GUID for the LAST * in path) = "bar"
        ///                     (GUID for the next * to the left) = "foo"
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection GetWildcardItemNames()
        {
            var result = new NameValueCollection();
            var ci = Sitecore.Context.Item;

            if (ci != null && ci.Paths.Path.Contains("*"))
            {
                var item = ci;
                var urlParts = Sitecore.Context.RawUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray();

                for (var i = 0; i < urlParts.Length; i++)
                {
                    if (item.Name == "*")
                        result.Add(item.ID.ToString(), urlParts[i]);
                    item = item.Parent;
                }

            }
            return result;
        }

        public static string GetWildcardItemUrl(Item item)
        {
            return ResolveWildcardItemUrl(item, GetWildcardItemNames());
        }

        private static string ResolveWildcardItemUrl(Item item, NameValueCollection wcItemNames)
        {
            if (item.Name != "*")
                return LinkManager.GetItemUrl(item);

            if (wcItemNames.AllKeys.Contains(item.ID.ToString()))
                return ResolveWildcardItemUrl(item.Parent, wcItemNames).TrimEnd('/') + '/' + wcItemNames[item.ID.ToString()].TrimEnd('/') + '/';

            return string.Empty;
        }

    }
}
