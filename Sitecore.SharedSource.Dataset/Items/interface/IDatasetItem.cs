using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Sitecore.Data.Items;
using Sitecore.SharedSource.Text;

namespace Sitecore.SharedSource.Dataset.Items
{
    public interface IDatasetItem
    {
        Item InnerItem { get; }
        string GetFieldValue(DataRow dr, string fieldname, string before, string after, string parameters);
        string GetItemUrl(DataRow dr);
        Dictionary<string, string> FieldMap { get; }
        DataTable Data { get; }
    }
}
