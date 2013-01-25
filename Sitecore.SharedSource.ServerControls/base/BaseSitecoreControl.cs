using System;
using System.Web;
using System.Web.UI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Web.UI.WebControls;
using Sitecore.Collections;
using Sitecore.Resources.Media;
using System.Web.UI.HtmlControls;
using System.Linq;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.Diagnostics;


namespace Sitecore.SharedSource.Dataset.ServerControls
{
    public class DefaultableControlBuilder : ControlBuilder
    {

        public override Type GetChildControlType(string tagName, IDictionary attribs)
        {
            // Allows TableRow without "runat=server" attribute to be added to the collection.
            if (String.Compare(tagName.ToLower(), "defaultvalue", true) == 0)
                return typeof(System.Web.UI.HtmlControls.HtmlGenericControl);
            return null;
        }

        public override void AppendLiteralString(string s)
        {
            // Ignores literals between rows.
        }
    }

    [ControlBuilderAttribute(typeof(DefaultableControlBuilder)), ParseChildren(false)]
    public abstract class BaseSitecoreControl : Sitecore.Web.UI.WebControl
    {
        private static readonly string _defaultRootTemplateName = "Site";

        private string _rootTemplateName = null;
        private Item _homeItem;
        private Item _dataSourceItem = null;
        private NameValueCollection _parameterCollection = null;
        private List<Item> _ancestorItemList = null;
        private bool _debugging = false;

        /// <summary>
        /// Use to pass "Debug" parameter (in sublayout/rendering properties) to a control.
        /// Use this to perhaps return more verbose debuggin info on the page.
        /// </summary>
        public bool Debugging
        {
            set
            {
                _debugging = value;
            }
            get
            {
                if (!bool.TryParse(this.SubLayoutParameter("Debug", "False"), out _debugging))
                    _debugging = false;
                return _debugging;
            }
        }

        protected HttpResponse Response
        {
            get
            {
                return Sitecore.Context.Page.Page.Response;
            }
        }

        protected HttpRequest Request
        {
            get
            {
                return Sitecore.Context.Page.Page.Request;
            }
        }

        protected virtual MediaUrlOptions DefaultMediaUrlOptions
        {
            get
            {
                return new MediaUrlOptions();
            }
        }

        public Item DataSourceItem
        {
            set
            {
                _dataSourceItem = value;
            }
            get
            {
                if (_dataSourceItem == null)
                {
                    if (!string.IsNullOrEmpty(this.DataSource))
                        _dataSourceItem = Sitecore.Context.Database.SelectSingleItem(this.DataSource);
                    else if (!string.IsNullOrEmpty(this.Attributes["source"]))
                        _dataSourceItem = Sitecore.Context.Database.GetItem(this.Attributes["source"]);

                    if (_dataSourceItem == null)
                        throw new Exception(string.Format("Unable to resolve data source item. [DataSource:][SourceAttribute:]", this.DataSource, this.Attributes["source"]));
                }
                return _dataSourceItem;
            }
        }

        protected ClientScriptManager ClientScriptManager
        {
            get
            {
                return Sitecore.Context.Page.Page.ClientScript;
            }
        }

        protected NameValueCollection ParameterCollection
        {
            get
            {
                if (_parameterCollection == null)
                    _parameterCollection = Sitecore.Web.WebUtil.ParseUrlParameters(this.Parameters);
                return _parameterCollection;
            }
        }

        private NameValueCollection _wildcardItemNames = null;
        protected NameValueCollection WildcardItemNames
        {
            get
            {
                if (_wildcardItemNames == null)
                    _wildcardItemNames = WildcardUtil.GetWildcardItemNames();
                return _wildcardItemNames;
            }
        }

        protected string SubLayoutParameter(string key, string vcDefaultValue)
        {
            NameValueCollection nvc = this.ParameterCollection;
            if (nvc.AllKeys.Contains(key))
                return nvc[key];
            else
                return vcDefaultValue;
        }

        private int _cacheTimeoutMinutes = 2;
        protected int CacheTimeoutMinutes
        {
            get
            {
                return _cacheTimeoutMinutes;
            }
            set
            {
                _cacheTimeoutMinutes = value;
            }
        }

        protected Item HomeItem
        {
            get
            {
                if (_homeItem == null)
                    _homeItem = this.ContextItem.ParentSiteItem(); //GetHomeItem(this.ContextItem);
                return _homeItem;
            }
        }

        protected List<Item> AncestorItemList
        {
            get
            {
                if (_ancestorItemList == null)
                {
                    _ancestorItemList = GetAncestorItemList(this.ContextItem, null);
                    _ancestorItemList.Reverse(); // So the Site item is first and context item is last
                }
                return _ancestorItemList;
            }
        }

        protected Item ContextItem
        {
            get { return Sitecore.Context.Item; }
        }

        public string RootTemplateName
        {
            get
            {
                if (_rootTemplateName == null)
                    _rootTemplateName = _defaultRootTemplateName;
                return _rootTemplateName;
            }
            set
            {
                _rootTemplateName = value;
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.CacheTimeout = new TimeSpan(0, this.CacheTimeoutMinutes, 0);
        }

        protected override string GetCachingID()
        {
            string cachingid = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(this.DataSource))
                    cachingid = "WebControlCacheID." + this.DataSourceItem.ID.ToString() + this.Parameters;
                //else if (this.ContextItem != null)
                //    cachingid = "WebControlCacheID." + this.ContextItem.ID.ToString() + this.Parameters;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to get cachingid. Control will not be htmlcached: " + this.GetType().FullName, ex, this);
            }

            if (string.IsNullOrEmpty(cachingid))
                Log.Debug("WebControl will not be cached: " + this.GetType().FullName, this);

            return cachingid;
        }

        private List<Item> GetAncestorItemList(Item item, List<Item> list)
        {
            if (list == null)
                list = new List<Item>();

            if (item == null)
                return list;

            list.Add(item);

            // See if this item is part of a SiteSectionGroup
            if (item.Parent.TemplateName == this.RootTemplateName)
            {
                Item ssg = item.Parent.Axes.SelectSingleItem("*[@@templatename = 'SiteSectionGroup' and contains(@SiteSections,'" + item.ID.ToString() + "')]");
                if (ssg != null)
                    list.Add(ssg);

            }

            if (item.ID == this.HomeItem.ID)
            {
                return list;
            }
            else
            {
                return GetAncestorItemList(item.Parent, list);
            }
        }

        protected string GetMediaItemURL(Item sdiItem, Sitecore.Data.ID idField)
        {
            Sitecore.Data.Fields.ImageField imgfImage = sdiItem.Fields[idField];
            return GetMediaItemURL(imgfImage);
        }
        /// <summary>
        /// Retrieve the URL for the current link's image
        /// </summary>
        /// <param name="imgf">this is the image field that the current link wants to display</param>
        /// <returns>string containing the link's image's url</returns>
        protected string GetMediaItemURL(Sitecore.Data.Fields.ImageField imgf)
        {
            return GetMediaItemURL(imgf.MediaItem, int.Parse(imgf.Height), int.Parse(imgf.Width));
        }
        protected string GetMediaItemURL(MediaItem mi)
        {
            return GetMediaItemURL(mi, int.Parse(mi.InnerItem["Height"]), int.Parse(mi.InnerItem["Width"]));
        }

        protected string GetMediaItemURL(MediaItem mi, int iHeight, int iWidth)
        {
            try
            {
                MediaUrlOptions muoOptions = this.DefaultMediaUrlOptions;

                muoOptions.UseItemPath = true;
                muoOptions.AbsolutePath = true;

                if (mi != null)
                {
                    muoOptions.Height = iHeight;
                    muoOptions.Width = iWidth;
                    return Sitecore.Resources.Media.MediaManager.GetMediaUrl(mi, muoOptions);
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// The DefaultValue for a control may be set by adding <defaultvalue></defaultvalue> tags
        /// inside the control declaration. It is then up to the programmer to decide whether to 
        /// render the DefaultValue to the output buffer from this property.
        /// 
        /// Sample:
        /// <du:RecentItems id="myid" runat="server">
        ///     <defaultvalue>
        ///         <p>There are no recent items! Go away!</p>
        ///     </defaultvalue>
        /// </du:RecentItems>
        /// </summary>
        protected string DefaultValue
        {
            get
            {
                System.Web.UI.HtmlControls.HtmlGenericControl hgc = (System.Web.UI.HtmlControls.HtmlGenericControl)this.Controls[0];
                if (hgc != null)
                    return hgc.InnerHtml;
                return string.Empty;
            }
        }

        #region Add Header controls
        /// <summary>
        /// This only works if you call it from the OnLoad event and the .ASPX page's (layout) 
        /// HEAD element has runat="server" set.
        /// </summary>
        /// 
        private void AddHeaderControl(Control ctl)
        {
            AddHeaderControl(ctl, null);
        }
        private void AddHeaderControl(Control ctl, int? iAddAt)
        {
            ctl.EnableViewState = false;
            // Don't add the control multiple times if we can help it...
            HtmlHead hh = Sitecore.Context.Page.Page.Header;
            int len = hh.Controls.Cast<Control>()
                                    .Where<Control>(f => f.ClientID == ctl.ClientID)
                                    .ToArray<Control>().Length;
            if (len == 0)
            {
                if (iAddAt.HasValue)
                    Sitecore.Context.Page.Page.Header.Controls.AddAt(iAddAt.Value, ctl);
                else
                    Sitecore.Context.Page.Page.Header.Controls.Add(ctl);
            }
        }

        protected void AddHeaderLiteral(string vcHtml, string id)
        {
            LiteralControl lc = new LiteralControl();
            lc.ID = id;
            lc.Text = vcHtml;
            AddHeaderControl(lc);
        }
        /// <summary>
        /// This only works if you call it from the OnLoad event and the .ASPX page's (layout) 
        /// HEAD element has runat="server" set.
        /// </summary>
        protected void AddHeaderCssLink(string vcCssFile, string id)
        {
            HtmlLink link = CreateHtmlLink(vcCssFile, id);
            AddHeaderControl(link);
        }


        protected void PrependHeaderCssLink(string vcCssFile, string id)
        {
            HtmlLink link = CreateHtmlLink(vcCssFile, id);
            AddHeaderControl(link, 0);
        }

        private static HtmlLink CreateHtmlLink(string vcCssFile, string id)
        {
            HtmlLink link = new HtmlLink();
            link.ID = id;
            link.Attributes.Add("type", "text/css");
            link.Attributes.Add("rel", "stylesheet");
            link.Attributes.Add("href", vcCssFile);
            return link;
        }
        #endregion Add Header controls

    }
}
