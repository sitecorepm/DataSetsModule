using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Sitecore.Collections;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.SharedSource.Dataset;
using Sitecore.SharedSource.Dataset.Domain;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.Items;
using Sitecore.SharedSource.Text;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web.UI.WebControls;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using Sitecore.Diagnostics;
using Sitecore.SharedSource.Dataset.Caching;


namespace Sitecore.SharedSource.Dataset.ServerControls
{
    public class DatasetRenderer : BaseSitecoreControl
    {
        private DatasetRendererItem _DatasetRenderer = null;
        private CompiledView _ItemTemplateView = null;
        private CompiledView _AlternateItemTemplateView = null;

        public DatasetRenderer() { }
        public DatasetRenderer(DatasetRendererItem dri)
        {
            _DatasetRenderer = dri;
        }

        #region Properties...

        protected DatasetRendererItem DatasetRendererItem
        {
            get
            {
                try
                {
                    if (_DatasetRenderer == null)
                    {
                        _DatasetRenderer = this.DataSourceItem;
                        foreach (var k in this.ParameterCollection.AllKeys)
                            _DatasetRenderer.Parameters.Add("@param." + k, this.ParameterCollection[k]);
                        for (var i = 0; i < this.WildcardItemNames.Count; i++)
                            _DatasetRenderer.Parameters.Add("@wc." + i, this.WildcardItemNames[i]);
                    }
                }
                catch
                {
                    throw new Exception("Unable to load DatasetRendererItem: " + this.DataSource);
                }
                return _DatasetRenderer;
            }
        }

        public ITextFunctionHandler[] CustomFunctionHandlers
        {
            get
            {
                var cfhList = new List<ITextFunctionHandler>();
                var cfh = this.DatasetRendererItem.CustomFunctionHandler;
                if (cfh != null)
                    cfhList.Add(cfh);
                cfhList.Add(new SubRenderer());

                return cfhList.ToArray();
            }
        }

        #endregion

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

        }
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            //var editframe = new EditFrame() { DataSource = this.DatasetRendererItem.InnerItem.ID.ToString() };
            //editframe.RenderFirstPart(output);
            DoRenderInternal(output, HtmlTextWriterTag.Div);
            //editframe.RenderLastPart(output);
        }

        protected void DoRenderInternal(HtmlTextWriter output, HtmlTextWriterTag containerTag)
        {
            StringBuilder sb = new StringBuilder();
            HtmlTextWriter htw = new HtmlTextWriter(new System.IO.StringWriter(sb));
            var onErrorContent = string.Empty;

            try
            {
                onErrorContent = this.DatasetRendererItem.OnErrorContent;

                if (string.IsNullOrEmpty(this.DatasetRendererItem.ListItemTemplate)
                    && string.IsNullOrEmpty(this.DatasetRendererItem.AltListItemTemplate))
                {
                    throw new Exception("DatasetRenderer requires the ListItemTemplate field be populated.");
                }

                htw.AddAttribute(HtmlTextWriterAttribute.Class, this.DatasetRendererItem.ClassValue);
                htw.RenderBeginTag(containerTag);
                htw.WriteLine();

                AddExceptionDataIndicator(htw);

                htw.Write(this.RenderDataset());

                htw.RenderEndTag(); //Div

                // Send output
                output.Write(sb.ToString());
            }
            catch (Exception ex)
            {
                if (ex is CacheFailureException)
                    Log.Info(ex.Message, this);
                else
                    Log.Error("Dataset renderer error.", ex, this);

                var msg = "<div class='dataset-error-message' style='display:none'>" + (this.Debugging ? HttpUtility.HtmlEncode(ex.ToString()) : ex.Message) + "</div>";

                if (!string.IsNullOrEmpty(onErrorContent))
                    msg = onErrorContent + msg;

                output.Write(msg);
            }
        }

        private void AddExceptionDataIndicator(HtmlTextWriter htw)
        {
            var ds = (BaseDataset)this.DatasetRendererItem.Dataset;
            if (ds.HasCachedException)
            {
                htw.AddAttribute(HtmlTextWriterAttribute.Class, "dataset-cached-data");
                htw.RenderBeginTag(HtmlTextWriterTag.Div);
                htw.WriteLine("[cached data]");
                htw.RenderEndTag();
                htw.WriteLine();
            }
        }

        protected string RenderDataset()
        {
            StringBuilder sb = new StringBuilder();
            HtmlTextWriter htw = new HtmlTextWriter(new System.IO.StringWriter(sb));

            IDatasetItem DSI = this.DatasetRendererItem.Dataset;
            DataTable dt = this.DatasetRendererItem.GetData();

            DataRowCollection rows = dt == null ? null : dt.Rows;

            if (rows != null && rows.Count > 0)
            {
                if (!String.IsNullOrEmpty(this.DatasetRendererItem.HeaderValue))
                {
                    htw.Write(this.DatasetRendererItem.HeaderValue);
                }

                if (this.DatasetRendererItem.ColumnsValue == 0)
                    WriteList(htw, DSI, rows);
                else
                    WriteColumnedList(htw, DSI, rows);

                if (!String.IsNullOrEmpty(this.DatasetRendererItem.FooterValue))
                {
                    htw.Write(this.DatasetRendererItem.FooterValue);
                }
            }
            else
                htw.Write(this.DatasetRendererItem.DefaultContent);


            return sb.ToString();
        }

        protected void WriteList(HtmlTextWriter htw, IDatasetItem DSI, DataRowCollection rows)
        {
            bool bAltFlag = false;
            foreach (DataRow dr in rows)
            {
                WriteListItem(htw, dr, bAltFlag, DSI);
                bAltFlag = !bAltFlag;
            }
        }

        protected void WriteColumnedList(HtmlTextWriter htw, IDatasetItem DSI, DataRowCollection rows)
        {
            int iRowCount = rows.Count;

            for (int i = 1; i <= this.DatasetRendererItem.ColumnsValue; i++)
            {
                string vcClass = "";

                if (i == 1)
                    vcClass += "left";
                else if (i == this.DatasetRendererItem.ColumnsValue)
                    vcClass += "right";
                else
                    vcClass += "middle";

                htw.AddAttribute(HtmlTextWriterAttribute.Class, vcClass.Trim());
                htw.RenderBeginTag(HtmlTextWriterTag.Ul);
                htw.WriteLine();

                int jStart;
                int jMax;
                int jStep;

                if (this.DatasetRendererItem.MulticolumnCollationValue == "Across")
                {
                    jStart = i - 1;
                    jMax = iRowCount;
                    jStep = this.DatasetRendererItem.ColumnsValue;
                }
                else
                {
                    int iColLen = iRowCount / this.DatasetRendererItem.ColumnsValue;
                    int iColRemainder = iRowCount % this.DatasetRendererItem.ColumnsValue;

                    jStart = (iColLen * (i - 1));
                    jMax = (iColLen * i);

                    if (i - 1 <= iColRemainder)
                        jStart += i - 1;
                    if (i <= iColRemainder)
                        jMax++;

                    jStep = 1;
                }

                bool bAltFlag = false;
                for (int j = jStart; j < jMax; j += jStep)
                {
                    htw.RenderBeginTag(HtmlTextWriterTag.Li);
                    htw.WriteLine();

                    this.WriteListItem(htw, rows[j], bAltFlag, DSI);

                    htw.RenderEndTag(); //LI
                    htw.WriteLine("");

                    bAltFlag = !bAltFlag;
                }

                htw.RenderEndTag(); //Ul
            }
        }

        protected void WriteListItem(HtmlTextWriter output, DataRow dr, bool bUseAlt, IDatasetItem DSI)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(this.DatasetRendererItem, "DatasetRendererItem");

            if (_ItemTemplateView == null && !string.IsNullOrEmpty(this.DatasetRendererItem.ListItemTemplate))
                _ItemTemplateView = DatasetViewEngine.Compile(this.DatasetRendererItem.ListItemTemplate);

            if (_AlternateItemTemplateView == null && !string.IsNullOrEmpty(this.DatasetRendererItem.AltListItemTemplate))
                _AlternateItemTemplateView = DatasetViewEngine.Compile(this.DatasetRendererItem.AltListItemTemplate);

            CompiledView cv = null;
            if (!bUseAlt || String.IsNullOrEmpty(this.DatasetRendererItem.AltListItemTemplate))
                cv = _ItemTemplateView;
            else
                cv = _AlternateItemTemplateView;

            if (cv != null)
            {
                var s = cv.ViewText;
                s = DatasetViewEngine.Render(cv, dr, DSI.GetFieldValue, DSI.GetItemUrl, this.CustomFunctionHandlers);
                output.Write(s);
            }
        }

        class SubRenderer : ITextFunctionHandler
        {
            public bool ProcessFunction(string fxName, object[] args, ref string result)
            {
                var fxHandled = false;

                switch (fxName)
                {
                    case "renderer": // renderer("AGUID|AnotherGUID|3GUIDs|4thGuid","GUID-or-path-to-contextitem-renderer")
                        result = Renderer(args[0] as string, args[1] as string);
                        fxHandled = true;
                        break;
                }

                return fxHandled;
            }

            private static string Renderer(string itemIdOrQueryList, string rendererPathOrGuid)
            {
                var output = string.Empty;
                var ids = itemIdOrQueryList.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);


                var items = new List<Item>();

                foreach (var id in ids)
                {
                    if (Sitecore.Data.ID.IsID(id))
                    {
                        var i = Sitecore.Context.Database.GetItem(id);
                        if (i != null)
                            items.Add(i);
                    }
                    else
                    {
                        var results = Sitecore.Context.Database.SelectItems(id);
                        if (results.Length > 0)
                            items.AddRange(results);
                    }
                }

                if (items.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    HtmlTextWriter htw = new HtmlTextWriter(new System.IO.StringWriter(sb));

                    var dri = new DatasetRendererItem(Sitecore.Context.Database.GetItem(rendererPathOrGuid));
                    var dr = new DatasetRenderer(dri);

                    if (dri.Dataset is ItemsSubset)
                    {
                        (dri.Dataset as ItemsSubset).Items = items.ToArray();
                        dr.DoRenderInternal(htw, HtmlTextWriterTag.Span);
                    }
                    else if (dri.Dataset is ContextItemQueryItem && items.Count == 1)
                    {
                        var preservedContextItem = Sitecore.Context.Item;
                        Sitecore.Context.Item = items[0];
                        dr.DoRenderInternal(htw, HtmlTextWriterTag.Span);
                        Sitecore.Context.Item = preservedContextItem;
                    }
                    output = sb.ToString();
                }

                return output;
            }
        }
    }
}