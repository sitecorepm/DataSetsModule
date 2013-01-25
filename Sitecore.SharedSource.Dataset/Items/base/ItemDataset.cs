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
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Resources.Media;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.WebControls;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;


namespace Sitecore.SharedSource.Dataset.Items
{
    public abstract class ItemDataset : BaseDataset
    {
        public ItemDataset(Item item) : base(item) { }

        private Item[] _items = null;
        public virtual Item[] Items
        {
            set { _items = value; }
            get
            {
                if (_items == null)
                    _items = RefreshItems();
                return _items;
            }
        }

        public virtual TemplateItem ItemTemplate
        {
            get
            {
                var items = this.Items;
                if (items != null && items.Length > 0)
                    return items[0].Template;
                return null;
            }
        }

        protected abstract Item[] RefreshItems();

        public override string GetFieldValue(DataRow dr, string fieldidentifier, string before, string after, string parameters)
        {
            string result = string.Empty;

            // Try to get the value directly from the sitecore item if possible,
            // otherwise just use the value in the DataRow
            if (!IsSpecialField(fieldidentifier) && dr.Table.Columns.Contains("_ItemId"))
            {
                Item item = this.InnerItem.Database.GetItem(new ID((string)dr["_ItemId"]));

                var fieldID = string.Empty;
                if (Sitecore.Data.ID.IsID(fieldidentifier))
                    fieldID = fieldidentifier;
                else if (dr.Table.Columns.Contains(fieldidentifier))
                    fieldID = (string)dr.Table.Columns[fieldidentifier].ExtendedProperties["_FieldKey"];
                else
                {
                    Log.Info(string.Format("ItemDataset does not contain the field. [ds:{0}][field:{1}]", this.InnerItem.Paths.Path, fieldidentifier), this);
                    return string.Empty;
                }

                if (item[fieldID].IsNullOrEmpty())
                    result = item.RenderField(fieldID, parameters);
                else
                {
                    FieldRenderer fr = new FieldRenderer();
                    fr.Before = before;
                    fr.After = after;
                    fr.Parameters = parameters;
                    fr.FieldName = fieldID;
                    fr.Item = item;
                    result = fr.Render();
                }
            }
            else
            {
                result = dr[fieldidentifier].ToString();
            }

            return result;
        }

        private string ResolveSpecialFieldValue(DataRow dr, Item i, string fieldkey)
        {
            var f = fieldkey.ToLower();

            switch (f)
            {
                case "@id":
                    return i.ID.ToString();
                case "@name":
                    return i.Name;
                case "@path":
                    return i.Paths.Path;
                case "@displayname":
                    return string.IsNullOrEmpty(i.DisplayName) ? i.Name : i.DisplayName;
                case "@iconurl":
                    return i.Appearance.Icon;
                case "@icon":
                    if (!string.IsNullOrEmpty(i.Appearance.Icon))
                        return "<img src='" + Sitecore.Resources.Images.GetThemedImageSource(i.Appearance.Icon) + "' alt='icon' />";
                    break;
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns a link to this item
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override string GetItemUrl(DataRow dr)
        {
            Item item = this.InnerItem.Database.GetItem(new ID((string)dr["_ItemId"]));
            if (item.Paths.IsMediaItem)
                return MediaManager.GetMediaUrl(item);
            else
                return LinkManager.GetItemUrl(item);
        }

        protected override Dictionary<string, string> RefreshFieldMap()
        {
            var map = base.RefreshFieldMap();
            if (this.ItemTemplate != null)
            {
                map = CreateFieldMapFromItemTemplate(this.ItemTemplate, map);

                // Add special item fields
                map.Add("@id", "@id");
                map.Add("@name", "@name");
                map.Add("@path", "@path");
                map.Add("@displayname", "@displayname");
                map.Add("@iconurl", "@iconurl");
                map.Add("@icon", "@icon");
            }
            return map;
        }

        protected Dictionary<string, string> CreateFieldMapFromItemTemplate(TemplateItem ti, Dictionary<string, string> map)
        {
            if (map == null)
                map = new Dictionary<string, string>();
            if (ti.ID != Sitecore.TemplateIDs.StandardTemplate)
            {
                foreach (TemplateFieldItem tfi in ti.OwnFields)
                {
                    if (!map.ContainsKey(tfi.ID.ToString()))
                    {
                        var fieldname = tfi.Title.IfNullOrEmpty(tfi.DisplayName).IfNullOrEmpty(tfi.Name);

                        // if duplicate field names, append a "(x)" to the name where x is a number
                        if (map.ContainsValue(fieldname))
                        {
                            var i = 1;
                            var rootname = fieldname;
                            while (map.ContainsValue(rootname))
                            {
                                rootname = fieldname + " (" + i.ToString() + ")";
                                i++;
                            }
                            fieldname = rootname;
                        }
                        map.Add(tfi.ID.ToString(), fieldname);
                    }
                }

                foreach (TemplateItem basetemplate in ti.BaseTemplates)
                    CreateFieldMapFromItemTemplate(basetemplate, map);
            }
            return map;
        }

        protected override DataTable RefreshData()
        {
            var dt = new DataTable();
            var items = this.Items ?? new Item[] { };
            if (items.Length > 0)
            {
                foreach (var entry in this.FieldMap)
                {
                    DataColumn dc = new DataColumn(entry.Value);

                    // Ignore special fields...
                    if (!IsSpecialField(entry.Key))
                    {
                        // Map data types to support sorting...
                        switch (items[0].Fields[entry.Key].TypeKey)
                        {
                            case "date":
                            case "datetime":
                                dc.DataType = typeof(DateTime);
                                break;
                            case "integer":
                                dc.DataType = typeof(Int32);
                                break;
                            case "number":
                                dc.DataType = typeof(Double);
                                break;
                        }

                        dc.ExtendedProperties.Add("_FieldKey", entry.Key);
                    }

                    dt.Columns.Add(dc);
                }

                DataColumn columnkey = dt.Columns.Add("_ItemId");
                dt.PrimaryKey = new DataColumn[] { columnkey };

                // copy item data into the DataTable
                foreach (Item i in items)
                {
                    DataRow dr = dt.NewRow();

                    dr["_ItemId"] = i.ID.ToString();
                    foreach (var entry in this.FieldMap)
                    {
                        if (IsSpecialField(entry.Key))
                            dr[entry.Value] = ResolveSpecialFieldValue(dr, i, entry.Key);
                        else
                        {
                            string value = i[entry.Key];
                            switch (dt.Columns[entry.Value].DataType.Name)
                            {
                                case "DateTime":
                                    dr[entry.Value] = DateUtil.IsoDateToDateTime(value);
                                    break;
                                case "Int32":
                                    dr[entry.Value] = ConvertUtil.ToInt32(value, 0);
                                    break;
                                case "Double":
                                    dr[entry.Value] = ConvertUtil.ToDouble(value, 0.0);
                                    break;
                                default:
                                    dr[entry.Value] = value;
                                    break;
                            }
                        }
                    }


                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }



    }
}
