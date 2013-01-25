using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Sites;

namespace Sitecore.SharedSource.Dataset.Caching
{
    public class CacheRefreshThread<T>
    {
        public Func<T> RefreshData;
        public DatasetCache DsCache { get; set; }
        public SiteContext ContextSite { get; set; }
        public Item ContextItem { get; set; }
        public string CacheKey { get; set; }
        public string RequestUri { get; set; }
        public TimeSpan CacheSuccessTimeout { get; set; }
        public TimeSpan CacheExceptionTimeout { get; set; }
        public T Result { get; private set; }

        public void ThreadProc()
        {
            var started = DateTime.Now;

            try
            {
                // Preserve the context properties... 
                Sitecore.Context.Item = ContextItem;
                Sitecore.Context.Site = ContextSite;

                var data = RefreshData();
                if (data != null)
                {
                    DsCache.HandleCacheRefreshSuccess(CacheKey, DateTime.Now - started, CacheSuccessTimeout, data, RequestUri);
                    this.Result = data;
                }

            }
            catch (Exception ex)
            {
                if (ex is System.Web.Services.Protocols.SoapException)
                {
                    var detail = (ex as System.Web.Services.Protocols.SoapException).Detail.OuterXml;
                    ex = new Exception(detail, ex);
                }
                Log.Debug(string.Format("CacheRefreshThread [{0}] error... ", this.CacheKey) + ex.Message);
                DsCache.HandleCacheRefreshException(CacheKey, DateTime.Now - started, CacheExceptionTimeout, ex);
            }

        }
    }
}
