using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.Sharepoint;
using Sitecore.SharedSource.Text;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using Sitecore.SharedSource.Dataset.Pipelines.GetSharepointCredentials;
using Sitecore.Pipelines;

namespace Sitecore.SharedSource.Dataset.Items
{
    public class SharepointQueryItem : BaseDataset
    {
        public SharepointQueryItem(Item item) : base(item) { }

        private static Regex _rgxColumnName = new Regex(@"\[(?<fieldname>[^\]]+)\]", RegexOptions.Compiled | RegexOptions.Singleline);

        private Dictionary<string, SharepointField> _dictListFields = null;
        private Dictionary<string, SharepointField> FieldDictionary
        {
            get
            {
                if (_dictListFields == null)
                    _dictListFields = this.Service.GetFields(this.ListName, this.ViewName);
                return _dictListFields;
            }
        }

        private SharepointService _service = null;
        private SharepointService Service
        {
            get
            {
                if (_service == null)
                {
                    var args = new GetSharepointCredentialsArgs() { SharepointQueryItem = this.InnerItem };
                    CorePipeline.Run("GetSharepointCredentials", args);
                    _service = new SharepointService(this.WebServiceAsmxUrl, args.Credentials);
                }
                return _service;
            }
        }


        private void AddSharepointItemUrlColumn(DataTable dt)
        {
            dt.Columns.Add("_ItemUrl", typeof(string));
            foreach (DataRow dr in dt.Rows)
            {
                string ows_EncodedAbsUrl = dr["ows_EncodedAbsUrl"].ToString();
                string ows_LinkFilename = dr["ows_LinkFilename"].ToString();
                string ows_ID = dr["ows_ID"].ToString();
                if (ows_LinkFilename.EndsWith(".000"))
                    dr["_ItemUrl"] = ows_EncodedAbsUrl.Replace(ows_LinkFilename, "DispForm.aspx?ID=" + ows_ID);
                else
                    dr["_ItemUrl"] = ows_EncodedAbsUrl;
            }

            dt.PrimaryKey = new DataColumn[] { dt.Columns["_ItemUrl"] };
        }


        #region IDatasetItem


        /// <summary>
        /// Returns a link to this Sharepoint item
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override string GetItemUrl(DataRow dr)
        {
            return (string)dr["_ItemUrl"];
        }

        protected override Dictionary<string, string> RefreshFieldMap()
        {
            var map = base.RefreshFieldMap();
            foreach (var entry in this.FieldDictionary)
                map.Add(_rgxKeyMaker.Replace(entry.Key, string.Empty), entry.Key);
            return map;
        }
        public string[] DebugFieldDictionary()
        {
            return this.FieldDictionary.Values.Select(x => "[" + x.DisplayName + "][" + x.FriendlyName + "][" + x.Name + "][" + x.Type + "]").ToArray();
        }

        public override string GetFieldValue(DataRow dr, string fieldidentifier, string before, string after, string parameters)
        {
            if (!this.FieldMap.ContainsKey(fieldidentifier))
                throw new Exception(string.Format("Failed to resolve field map. [fieldidentifier:{0}]", fieldidentifier));
            return base.GetFieldValue(dr, this.FieldMap[fieldidentifier], before, after, parameters);
        }

        protected override DataTable RefreshData()
        {
            var dt = this.Service.GetListItems(this.ListName, this.ViewName, this.RowLimit, this.Query, this.ViewFields, this.QueryOptions);

            if (dt == null)
            {
                Log.Debug("Sharepoint query response was NULL. Creating empty dataset table... [" + this.InnerItem.Paths.Path + "]");

                // Create an empty datatable
                dt = new DataTable();
                foreach (var k in this.FieldDictionary.Keys)
                    dt.Columns.Add(k, typeof(string));
            }

            if (dt != null)
            {
                int i = 1;

                // Compute the ItemUrl for each row
                AddSharepointItemUrlColumn(dt);

                // Create a reverse lookup dictionary so we can lookup what field name was displayed
                // in the field list in Content Editor UI.
                Dictionary<string, string> reverselookup = new Dictionary<string, string>();
                foreach (string key in this.FieldDictionary.Keys)
                {
                    reverselookup.Add("ows_" + this.FieldDictionary[key].Name, key);
                }

                // Rename columns (prepend column index to avoid name collisions)
                foreach (DataColumn dc in dt.Columns)
                {
                    if (reverselookup.ContainsKey(dc.ColumnName))
                    {
                        dc.ColumnName = i.ToString() + "_" + reverselookup[dc.ColumnName];
                        i++;
                    }
                    else
                    {
                        dc.ColumnName = "_" + dc.ColumnName;
                    }
                }

                // Remove column prefixes
                foreach (DataColumn dc in dt.Columns)
                {
                    int j = dc.ColumnName.IndexOf('_');
                    dc.ColumnName = dc.ColumnName.Substring(j + 1);
                }

                // Clean up data
                foreach (DataRow dr in dt.Rows)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (this.FieldDictionary.ContainsKey(dc.ColumnName))
                            dr[dc] = this.FieldDictionary[dc.ColumnName].FriendlyValue(dr[dc].ToString());
                    }
                }

                // Attempt to convert column datatypes to sharepoint types
                dt = ConvertColumnDataTypes(dt);

            }

            return dt;
        }

        private DataTable ConvertColumnDataTypes(DataTable dt)
        {
            DataTable dtNew = dt.Clone();

            foreach (DataColumn dc in dtNew.Columns)
            {
                if (this.FieldDictionary.ContainsKey(dc.ColumnName))
                {
                    switch (this.FieldDictionary[dc.ColumnName].Type)
                    {
                        case "Boolean":
                            dc.DataType = typeof(Boolean);
                            break;
                        case "DateTime":
                            dc.DataType = typeof(DateTime);
                            break;
                        case "Integer":
                            dc.DataType = typeof(Int32);
                            break;
                        case "Number":
                            dc.DataType = typeof(Double);
                            break;
                    }
                }
            }

            // Copy data to new table structure
            foreach (DataRow dr in dt.Rows)
            {
                DataRow drNew = dtNew.NewRow();

                foreach (DataColumn dc in dt.Columns)
                {
                    switch (dtNew.Columns[dc.ColumnName].DataType.Name)
                    {
                        case "Boolean":
                            drNew[dc.ColumnName] = dr[dc].ToString() == "1" ? true : false;
                            break;
                        case "DateTime":
                            DateTime date;
                            if (DateTime.TryParse(dr[dc].ToString(), out date))
                                drNew[dc.ColumnName] = date;
                            break;
                        case "Int32":
                            drNew[dc.ColumnName] = ConvertUtil.ToInt32(dr[dc].ToString(), 0);
                            break;
                        case "Double":
                            drNew[dc.ColumnName] = ConvertUtil.ToDouble(dr[dc].ToString(), 0.0);
                            break;
                        default:
                            drNew[dc.ColumnName] = dr[dc].ToString();
                            break;
                    }
                }
                dtNew.Rows.Add(drNew);
            }

            return dtNew;
        }

        #endregion



        public string WebServiceAsmxUrl
        {
            get
            {
                return this.InnerItem.GetReferenceField(IDs.Dataset.SharepointQuery.SharepointSite).TargetItem[IDs.Core.SharepointSite.WebServiceAsmxUrl];
            }
        }

        public string ListName { get { return this.InnerItem[IDs.Dataset.SharepointQuery.ListName]; } }
        public string ViewName { get { return this.InnerItem[IDs.Dataset.SharepointQuery.ViewName]; } }
        public string RowLimit { get { return this.InnerItem[IDs.Dataset.SharepointQuery.RowLimit]; } }
        public string Query { get { return this.InnerItem[IDs.Dataset.SharepointQuery.Query]; } }
        public string ViewFields { get { return this.InnerItem[IDs.Dataset.SharepointQuery.ViewFields]; } }
        public string QueryOptions { get { return this.InnerItem[IDs.Dataset.SharepointQuery.QueryOptions]; } }

    }
}
