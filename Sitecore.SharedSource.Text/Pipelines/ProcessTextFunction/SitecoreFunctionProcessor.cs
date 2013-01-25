using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Events;
using Sitecore.SecurityModel;
using System.Collections.Specialized;
//using Sitecore.SharedSource.Dataset.Extensions;

namespace Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction
{
    public class SitecoreFunctionProcessor : BaseFunctionProcessor
    {
        public override void ProcessFunction(TextFunctionPipelineArgs pipeArgs)
        {
            var args = pipeArgs.Args;

            switch (pipeArgs.FunctionName)
            {
                case "lookup":
                    pipeArgs.HandledResult = GetLookupValue((string)args[0], (string)args[1]);
                    break;
                case "lookupforeach": // lookupforeach("AGUID|AnotherGUID|3GUIDs|4thGuid","LookupItemFieldName","|","<br/>")
                    pipeArgs.HandledResult = LookupForEachValue((string)args[0], (string)args[1], (string)args[2], (string)args[3]);
                    break;
                case "wildcard":
                    pipeArgs.HandledResult = GetWildcardValue((int)args[0]);
                    break;
            }

        }

        private static string LookupForEachValue(string vcItemIdList, string vcFieldName, string vcInputDelimiter, string vcOutputDelimiter)
        {
            string[] ids = vcItemIdList.Split(vcInputDelimiter.ToCharArray());
            string vcOutput = string.Empty;
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(vcOutput))
                    vcOutput += vcOutputDelimiter;
                vcOutput += GetLookupValue(id, vcFieldName);
            }
            return vcOutput;
        }

        private static string GetLookupValue(string vcItemPathOrID, string fieldname)
        {
            string key = vcItemPathOrID + "." + fieldname;
            var value = (string)System.Web.HttpContext.Current.Cache[key] ?? string.Empty;

            if (string.IsNullOrEmpty(value))
            {
                Item i = null;
                if (Sitecore.Data.ID.IsID(vcItemPathOrID))
                    i = Sitecore.Context.Database.GetItem(Sitecore.Data.ID.Parse(vcItemPathOrID));
                else
                    i = Sitecore.Context.Database.GetItem(vcItemPathOrID);

                if (i != null)
                    value = i[fieldname] ?? string.Empty;

                // Cache this data for 5 minutes... so we're not always hitting the DB
                System.Web.HttpContext.Current.Cache.Insert(key
                                    , value, null
                                    , DateTime.Now.AddMinutes(1.0)
                                    , System.Web.Caching.Cache.NoSlidingExpiration);
            }

            return value;
        }

        private static string GetWildcardValue(int index)
        {
            var wcItemNames = GetWildcardItemNames();
            if (wcItemNames.Count - 1 >= index)
                return wcItemNames[index];
            return string.Empty;
        }

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

    }
}
