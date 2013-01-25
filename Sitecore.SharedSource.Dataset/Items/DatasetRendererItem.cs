using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Reflection;
using Sitecore.SharedSource.Dataset;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using Sitecore.Web.UI.WebControls;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;


namespace Sitecore.SharedSource.Dataset.Items
{
    public class DatasetRendererItem : CustomItem
    {
        private string[] _selectFields = null;


        public DatasetRendererItem(Item item)
            : base(item)
        {
            this.Parameters = new NameValueCollection();
        }

        public static implicit operator DatasetRendererItem(Item item)
        {
            if (item == null)
                return null;
            return new DatasetRendererItem(item);
        }

        public NameValueCollection Parameters { get; set; }

        private DatasetRendererPagerItem _pager = null;
        public DatasetRendererPagerItem Pager
        {
            get
            {
                if (_pager == null && this.InnerItem.HasChildren)
                {
                    var item = this.InnerItem.GetChildren().InnerChildren.SingleOrDefault(x => x.TemplateName == "DatasetRendererPager");
                    _pager = (DatasetRendererPagerItem)item;
                }
                return _pager;
            }
        }


        private IDatasetItem _dataset = null;
        public IDatasetItem Dataset
        {
            get
            {
                if (_dataset == null)
                    _dataset = DatasetFactory.Create(this.InnerItem.Parent);
                return _dataset;
            }
        }

        public DataTable GetData()
        {
            IEnumerable<DataRow> rows = new DataRow[] { };
            DataTable dtResult = null;
            DataTable dt = GetDataView();

            if (dt != null)
            {
                int iRowCount = dt.Rows.Count;

                if (iRowCount > 0)
                {
                    rows = dt.Rows.Cast<DataRow>();

                    // handle paging
                    if (this.Pager != null && this.Pager.PageSize > 0)
                    {
                        if (this.Pager.PageCurrent > 0)
                        {
                            var skipN = this.Pager.PageCurrent * this.Pager.PageSize;
                            if (skipN > rows.Count())
                                skipN = rows.Count();
                            rows = rows.Skip(skipN);
                        }
                        if (this.Pager.PageSize < rows.Count())
                            rows = rows.Take(this.Pager.PageSize);
                    }

                    // handle SkipN
                    if (this.SkipN > 0 && this.SkipN < rows.Count())
                        rows = rows.Skip(this.SkipN);

                    // handle TopN
                    if (this.TopN > 0 && this.TopN < rows.Count())
                        rows = rows.Take(this.TopN);

                    // handle RandomN
                    if (this.RandomN > 0 && this.RandomN < rows.Count())
                        rows = rows.Random(this.RandomN);

                    // handle EveryNth
                    if (this.EveryNth > 0 && this.EveryNth < rows.Count())
                        rows = rows.Where((dr, index) => index % this.EveryNth == 0);
                }

                dtResult = dt.Clone();
                foreach (DataRow dr in rows)
                    dtResult.ImportRow(dr);

                // Set the assign special field values
                AssignSpecialFieldValues(dtResult);

            }
            return dtResult;
        }

        private void AssignSpecialFieldValues(DataTable dt)
        {
            var rowcount = dt.Rows.Count;

            if (rowcount > 0)
            {
                for (var i = 0; i < rowcount; i++)
                {
                    if (dt.Columns.Contains("@rowcount"))
                        dt.Rows[i]["@rowcount"] = rowcount;
                    if (dt.Columns.Contains("@index"))
                        dt.Rows[i]["@index"] = i + 1;
                    if (dt.Columns.Contains("@first"))
                        dt.Rows[i]["@first"] = (i == 0 ? 1 : 0);
                    if (dt.Columns.Contains("@last"))
                        dt.Rows[i]["@last"] = (i == (rowcount - 1) ? 1 : 0);
                }
                dt.AcceptChanges();
            }
        }

        /// <summary>
        /// Returns data that has been filtered and sorted but not yet had the following functions applied: PageN, SkipN, TopN, RandomN, EveryNth
        /// </summary>
        public DataTable GetDataView()
        {
            return ApplyDataviewCriteria(this.Dataset.Data);
        }

        private DataTable ApplyDataviewCriteria(DataTable dtRawData)
        {
            DataTable dt = null;

            if (dtRawData != null && dtRawData.Rows.Count > 0)
            {
                DataView dv = new DataView(dtRawData, this.Filter, this.SortBy, DataViewRowState.CurrentRows);

                if (this.SelectFields.Length > 0)
                {
                    var fields = this.SelectFields
                                        .Where(x => this.Dataset.FieldMap.ContainsKey(x))
                                        .Select(x => this.Dataset.FieldMap[x]).ToList();

                    if (!this.Distinct)
                    {
                        // Keep the PrimaryKey columns as long as DISTINCT is NOT specified
                        foreach (DataColumn dc in dtRawData.PrimaryKey)
                            fields.Add(dc.ColumnName);
                    }


                    // Ensure ONLY select fields exist in the table, if not remove them
                    List<string> columns = dtRawData.Columns
                                            .Cast<DataColumn>()
                                            .Select<DataColumn, string>(x => x.ColumnName)
                                            .ToList();

                    fields = fields.Where(s => columns.Contains(s)).ToList();

                    dt = dv.ToTable(this.Distinct, fields.ToArray());
                }
                else
                    dt = dv.ToTable();

            }

            return dt;
        }


        #region Item Fields...

        public string ClassValue
        {
            get
            {

                string vcClass = this.InnerItem[CpnIDs.DatasetRenderer.Class];
                vcClass = "datasetrenderer " + (vcClass ?? "");
                return vcClass.Trim();
            }
        }

        public string HeaderValue
        {
            get
            {
                return this.InnerItem.RenderField(CpnIDs.DatasetRenderer.Header, "cancel-linebreaks=true");
                //return this.InnerItem[CpnIDs.DatasetRenderer.Header];
            }
        }

        public string FooterValue
        {
            get
            {
                return this.InnerItem.RenderField(CpnIDs.DatasetRenderer.Footer, "cancel-linebreaks=true");
                //return this.InnerItem[CpnIDs.DatasetRenderer.Footer];
            }
        }

        public string[] SelectFields
        {
            get
            {
                if (_selectFields == null)
                {
                    _selectFields = this.InnerItem[CpnIDs.DatasetRenderer.Select].IfNullOrEmpty("")
                                            .Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return _selectFields;
            }
        }

        public bool Distinct
        {
            get
            {
                return ((CheckboxField)this.InnerItem.Fields[CpnIDs.DatasetRenderer.Distinct]).Checked;
            }
        }


        public string SortBy
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRenderer.SortBy];
            }
        }

        public string Filter
        {
            get
            {
                var filter = this.InnerItem[CpnIDs.DatasetRenderer.Filter];
                // Process parameters & functions
                DatasetViewEngine.Render(filter, this.Parameters, FilterGetItemValue, delegate(NameValueCollection dr) { return string.Empty; });
                return filter;
            }
        }
        public string FilterGetItemValue(NameValueCollection dr, string fieldname, string before, string after, string parameters)
        {
            if (dr.AllKeys.Contains(fieldname))
                return dr[fieldname];
            else
                return "[" + fieldname + "]"; // Preserve fields in the Filter
        }

        public int TopN
        {
            get
            {
                string vcTopN = this.InnerItem[CpnIDs.DatasetRenderer.TopN];
                return String.IsNullOrEmpty(vcTopN) ? 0 : int.Parse(vcTopN);
            }
        }

        public int RandomN
        {
            get
            {
                string vcRandomN = this.InnerItem[CpnIDs.DatasetRenderer.RandomN];
                return String.IsNullOrEmpty(vcRandomN) ? 0 : int.Parse(vcRandomN);
            }
        }

        public int EveryNth
        {
            get
            {
                string vcEveryNth = this.InnerItem[CpnIDs.DatasetRenderer.EveryNth];
                return String.IsNullOrEmpty(vcEveryNth) ? 0 : int.Parse(vcEveryNth);
            }
        }

        public int SkipN
        {
            get
            {
                string vcSkipN = this.InnerItem[CpnIDs.DatasetRenderer.SkipN];
                return String.IsNullOrEmpty(vcSkipN) ? 0 : int.Parse(vcSkipN);
            }
        }

        public string ListItemTemplate
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRenderer.ListItemTemplate];
            }
        }

        public string AltListItemTemplate
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRenderer.AltListItemTemplate];
            }
        }

        public string DefaultContent
        {
            get
            {
                return this.InnerItem.RenderField(CpnIDs.DatasetRenderer.DefaultContent, "cancel-linebreaks=true") ?? string.Empty;
            }
        }

        public string OnErrorContent
        {
            get
            {
                return this.InnerItem.RenderField(CpnIDs.DatasetRenderer.OnErrorContent, "cancel-linebreaks=true") ?? string.Empty;
            }
        }


        public int ColumnsValue
        {
            get
            {

                string vcColumns = this.InnerItem[CpnIDs.DatasetRenderer.Columns];
                int iColumns = String.IsNullOrEmpty(vcColumns) ? 1 : int.Parse(vcColumns);
                return iColumns;
            }
        }

        public string MulticolumnCollationValue
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRenderer.MulticolumnCollation];
            }
        }

        public ITextFunctionHandler CustomFunctionHandler
        {
            get
            {
                var typeString = this.InnerItem[CpnIDs.DatasetRenderer.CustomFunctionHandler];
                if (!string.IsNullOrEmpty(typeString))
                    return ReflectionUtil.CreateObject(typeString, new object[0]) as ITextFunctionHandler;
                return null;
            }
        }

        #endregion

    }
}
