using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Linq;

using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Sitecore.Links;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web.UI.WebControls;

using Sitecore.SharedSource.Dataset.Items;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;
using System.Web;
using Sitecore.Diagnostics;


namespace Sitecore.SharedSource.Dataset.ServerControls
{
    public class DatasetRendererPager : BaseSitecoreControl
    {
        private DatasetRendererPagerItem _DatasetRendererPager = null;

        #region Properties...

        protected DatasetRendererPagerItem DatasetRendererPagerItem
        {
            get
            {
                try
                {
                    _DatasetRendererPager = _DatasetRendererPager ?? this.DataSourceItem;
                }
                catch
                {
                    throw new Exception("Unable to load DatasetRendererPagerItem: " + this.DataSource);
                }
                return _DatasetRendererPager;
            }
        }

        #endregion

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // Add css link here to avoid Viewstate tracking and the dreaded "Failed to load Viewstate" error
            this.PrependHeaderCssLink("/sc_styles/datasetrenderer-paging.css", "datasetrenderer-paging-" + this.ClientID);
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            StringBuilder sb = new StringBuilder();
            HtmlTextWriter htw = new HtmlTextWriter(new System.IO.StringWriter(sb));

            try
            {
                var pager = this.DatasetRendererPagerItem;
                var qsKey = pager.PageQueryString;
                var dt = pager.DatasetRenderer.GetDataView();

                if (dt.Rows.Count > 0 && pager.PageSize < dt.Rows.Count)
                {
                    var firstPageNumber = 0;
                    var currentPageNumber = pager.PageCurrent;
                    var lastPageNumber = dt.Rows.Count / pager.PageSize; // We would subtract 1 since page numbers are zero-based 
                                                                         // but we want to include a page for the remainder value.

                    if (lastPageNumber + 1 > pager.MaxPages)
                    {
                        var midPoint = pager.MaxPages / 2;
                        if (currentPageNumber > midPoint)
                            firstPageNumber = Math.Min(currentPageNumber - midPoint, lastPageNumber - pager.MaxPages);
                    }

                    htw.AddAttribute(HtmlTextWriterAttribute.Class, "paging-controls");
                    htw.RenderBeginTag(HtmlTextWriterTag.Div);

                    htw.RenderBeginTag(HtmlTextWriterTag.Ul);
                    htw.WriteLine();

                    if (pager.ShowFirstLast && !(currentPageNumber <= firstPageNumber))
                        RenderListItem(htw, qsKey, firstPageNumber, false, pager.FirstLinkText, "pager-first");

                    if (pager.ShowPreviousNext && !(currentPageNumber <= firstPageNumber))
                        RenderListItem(htw, qsKey, currentPageNumber - 1, false, pager.PreviousLinkText, "pager-previous");


                    for (var p = firstPageNumber; p <= lastPageNumber; p++)
                        RenderListItem(htw, qsKey, p, currentPageNumber == p);


                    if (pager.ShowPreviousNext && !(currentPageNumber >= lastPageNumber))
                        RenderListItem(htw, qsKey, currentPageNumber + 1, false, pager.NextLinkText, "pager-next");

                    if (pager.ShowFirstLast && !(currentPageNumber >= lastPageNumber))
                        RenderListItem(htw, qsKey, lastPageNumber, false, pager.LastLinkText, "pager-last");


                    htw.RenderEndTag(); //Ul
                    htw.RenderEndTag(); //Div

                    // Send output
                    output.Write(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error("DatasetRendererPager error.", ex, this);
                if (this.Debugging)
                    output.Write("Error: \n" + ex.ToString());
            }
        }

        private static void RenderListItem(HtmlTextWriter htw, string qsKey, int page, bool currentPage)
        {
            RenderListItem(htw, qsKey, page, currentPage, string.Empty, string.Empty);
        }

        private static void RenderListItem(HtmlTextWriter htw, string qsKey, int page, bool currentPage, string linktext, string liCssClass)
        {
            if (currentPage)
                liCssClass += " current-page";

            htw.AddAttribute(HtmlTextWriterAttribute.Class, liCssClass.Trim());
            htw.RenderBeginTag(HtmlTextWriterTag.Li);

            if (!currentPage)
            {
                var qs = string.Empty;
                if (page > 0)
                    qs += "?" + qsKey + "=" + page.ToString();
                htw.AddAttribute(HtmlTextWriterAttribute.Href, LinkManager.GetItemUrl(Sitecore.Context.Item) + qs);
                htw.RenderBeginTag(HtmlTextWriterTag.A);
            }

            if (string.IsNullOrEmpty(linktext))
                htw.Write((page + 1).ToString());
            else
                htw.Write(linktext);

            if (!currentPage)
                htw.RenderEndTag();

            htw.RenderEndTag();
        }

    }
}