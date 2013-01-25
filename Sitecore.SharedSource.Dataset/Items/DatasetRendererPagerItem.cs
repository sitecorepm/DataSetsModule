using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Sitecore.Reflection;
using Sitecore.Web.UI.WebControls;

using Sitecore.SharedSource.Dataset.Extensions;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;


namespace Sitecore.SharedSource.Dataset.Items
{
    public class DatasetRendererPagerItem : CustomItem
    {
        public DatasetRendererPagerItem(Item item) : base(item) { }

        public static implicit operator DatasetRendererPagerItem(Item item)
        {
            if (item == null)
                return null;
            return new DatasetRendererPagerItem(item);
        }

        private DatasetRendererItem _datasetrenderer = null;
        public DatasetRendererItem DatasetRenderer
        {
            get
            {
                if (_datasetrenderer == null)
                    _datasetrenderer = (DatasetRendererItem)this.InnerItem.Parent;
                return _datasetrenderer;
            }
        }

        public int PageCurrent
        {
            get
            {
                var result = string.Empty;
                if (System.Web.HttpContext.Current != null &&
                            System.Web.HttpContext.Current.Request != null &&
                            System.Web.HttpContext.Current.Request.Params.Count > 0)
                {
                    result = System.Web.HttpContext.Current.Request.Params[this.PageQueryString];
                }
                return String.IsNullOrEmpty(result) ? 0 : int.Parse(result);
            }
        }

        #region Item Fields...

        public string PageQueryString
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRendererPager.PageQueryString];
            }
        }

        /// <summary>
        /// For use with paging datasets
        /// </summary>
        public int PageSize
        {
            get
            {
                var s = this.InnerItem[CpnIDs.DatasetRendererPager.PageSize];
                return String.IsNullOrEmpty(s) ? 0 : int.Parse(s);
            }
        }

        public int MaxPages
        {
            get
            {
                var i = this.InnerItem[CpnIDs.DatasetRendererPager.MaxPages];
                return String.IsNullOrEmpty(i) ? 0 : int.Parse(i);
            }
        }
       
        public bool ShowFirstLast
        {
            get
            {
                return ((CheckboxField)this.InnerItem.Fields[CpnIDs.DatasetRendererPager.ShowFirstLast]).Checked;
            }
        }

        public string FirstLinkText
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRendererPager.FirstLinkText];
            }
        }
        public string LastLinkText
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRendererPager.LastLinkText];
            }
        }

        public bool ShowPreviousNext
        {
            get
            {
                return ((CheckboxField)this.InnerItem.Fields[CpnIDs.DatasetRendererPager.ShowPreviousNext]).Checked;
            }
        }

        public string PreviousLinkText
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRendererPager.PreviousLinkText];
            }
        }
        public string NextLinkText
        {
            get
            {
                return this.InnerItem[CpnIDs.DatasetRendererPager.NextLinkText];
            }
        }
        

        

        #endregion

    }
}
