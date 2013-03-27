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
using Sitecore.Data.Items;

using Sitecore.SharedSource.Dataset.Caching;
using Sitecore.SharedSource.Dataset.Extensions;
using IDs = Sitecore.SharedSource.Dataset.FieldIDs;
using Sitecore.SharedSource.Text;
using System.Web;
using System.Text.RegularExpressions;


namespace Sitecore.SharedSource.Dataset.Items
{
    public abstract class BaseDataset : CustomItem, IDatasetItem
    {
        private static Sitecore.SharedSource.Dataset.KeyLockManager _lockMgr = new Sitecore.SharedSource.Dataset.KeyLockManager();
        protected static Regex _rgxKeyMaker = new Regex(@"[^A-Za-z0-9\-\._\s\@]", RegexOptions.Compiled | RegexOptions.Singleline);
        protected Dictionary<string, string> _fieldMap = null;
        private DataTable _data = null;
        private static object syncRoot = new object();

        public BaseDataset(Item item) : base(item) { }

        protected virtual Dictionary<string, string> RefreshFieldMap()
        {
            var map = new Dictionary<string, string>();
            map.Add("@first", "@first");
            map.Add("@last", "@last");
            map.Add("@index", "@index");
            map.Add("@rowcount", "@rowcount");
            return map;
        }
        protected virtual void BeforeRefreshFieldMap() { }
        protected virtual void AfterRefreshFieldMap() { }

        protected abstract DataTable RefreshData();
        protected virtual void BeforeRefreshData() { }
        protected virtual void AfterRefreshData() { }

        public bool HasCachedException
        {
            get
            {
                try
                {
                    var obj = DsCache.GetExpired<CacheFailure>(CacheKeyData);
                    return (obj != null) && (obj.LastGoodData != null);
                }
                catch
                {
                    return false;
                }
            }
        }

        private string CacheKeyData
        {
            get
            {
                return this.InnerItem.ID.ToString() + ".data";
            }
        }

        private string CacheKeyFieldList
        {
            get
            {
                return this.InnerItem.ID.ToString() + ".fields";
            }
        }

        public virtual string GetFieldValue(DataRow dr, string fieldidentifier, string before, string after, string parameters)
        {
            var s = string.Empty;
            try
            {
                s = dr[fieldidentifier].ToString();
                if (!string.IsNullOrEmpty(s))
                    s = (before ?? string.Empty) + s + (after ?? string.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to resolve DataRow column. [fieldidentifier:{0}][before:{1}][after:{2}][parameters:{3}]",
                                            new string[] { fieldidentifier, before, after, parameters }), ex);
            }
            return s;
        }

        public virtual string GetItemUrl(DataRow dr)
        {
            return null;
        }

        protected virtual double AsyncTimeout
        {
            get
            {
                string s = this.InnerItem[IDs.Dataset.BaseDataset.AsynchTimeout].IfNullOrEmpty("2.0");
                double d;
                if (!double.TryParse(s, out d))
                    d = 2.0;

                return d;
            }
        }

        protected virtual TimeSpan CacheTimeout
        {
            get
            {
                string s = this.InnerItem[IDs.Dataset.BaseDataset.CacheTimeoutMinutes].IfNullOrEmpty("5");
                double d;
                if (!double.TryParse(s, out d))
                    d = 5.0;

                return TimeSpan.FromMinutes(d);
            }
        }

        protected virtual TimeSpan ExceptionCacheTimeout
        {
            get
            {
                string s = this.InnerItem[IDs.Dataset.BaseDataset.ExceptionCacheTimeoutMinutes].IfNullOrEmpty("5");
                double d;
                if (!double.TryParse(s, out d))
                    d = 5.0;

                return TimeSpan.FromMinutes(d);
            }
        }

        protected bool OnErrorUseCachedData
        {
            get
            {
                return this.InnerItem[IDs.Dataset.BaseDataset.OnErrorUseCachedData] == "1";
            }
        }

        private static Dictionary<string, DatasetCache> _cacheInstances = new Dictionary<string, DatasetCache>();
        protected static DatasetCache DsCache
        {
            get
            {
                string key = Context.Site.SiteInfo.Name;

                if (!_cacheInstances.ContainsKey(key))
                {
                    lock (_lockMgr.AcquireKeyLock(key))
                    {
                        if (!_cacheInstances.ContainsKey(key))
                            _cacheInstances[key] = new DatasetCache(Context.Site.SiteInfo, StringUtil.ParseSizeString(Settings.GetSetting("Caching.DefaultDatasetCacheSize", "5MB")));
                    }
                }
                return _cacheInstances[key];
            }
        }

        //protected static DatasetCache DsCache
        //{
        //    get
        //    {
        //        return new DatasetCache(Sitecore.Context.Site.SiteInfo,
        //                StringUtil.ParseSizeString(Settings.GetSetting("Caching.DefaultDatasetCacheSize", "5MB")));
        //    }
        //}

        public Dictionary<string, string> FieldMap
        {
            get
            {
                if (_fieldMap == null)
                {
                    this.BeforeRefreshFieldMap();
                    _fieldMap = DsCache.Get<Dictionary<string, string>>(this.CacheKeyFieldList, this.AsyncTimeout, this.CacheTimeout, this.ExceptionCacheTimeout, this.OnErrorUseCachedData, this.RefreshFieldMap)
                                ?? new Dictionary<string, string>();
                    this.AfterRefreshFieldMap();
                }
                return _fieldMap;
            }
        }

        public DataTable Data
        {
            get
            {
                if (_data == null)
                {
                    this.BeforeRefreshData();
                    _data = DsCache.Get<DataTable>(this.CacheKeyData, this.AsyncTimeout, this.CacheTimeout, this.ExceptionCacheTimeout, this.OnErrorUseCachedData, this.RefreshData);
                    this.AfterRefreshData();
                }
                return _data;
            }
        }

        protected bool IsSpecialField(string fieldidentifier)
        {
            return fieldidentifier.StartsWith("@");
        }
    }
}
