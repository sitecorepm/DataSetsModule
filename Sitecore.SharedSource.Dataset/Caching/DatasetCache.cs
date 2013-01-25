using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Sitecore;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines.DataCommands;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Diagnostics.PerformanceCounters;
using Sitecore.Jobs;
using Sitecore.Web;
using Sitecore.Reflection;
using System.Threading;

namespace Sitecore.SharedSource.Dataset.Caching
{
    public class DatasetCache : CustomCache
    {
        private static object _syncRoot = new object();
        private static Dictionary<string, object> _keyLocks = new Dictionary<string, object>();

        #region Diagnostic Counters
        private static readonly PerformanceCounter _datasetCacheClearings = new PerformanceCounter("DatasetCacheClearings", "Sitecore.Caching");
        private static readonly PerformanceCounter _datasetCacheHits = new PerformanceCounter("DatasetCacheHits", "Sitecore.Caching");
        private static readonly PerformanceCounter _datasetCacheMisses = new PerformanceCounter("DatasetCacheMisses", "Sitecore.Caching");

        public static PerformanceCounter DatasetCacheClearings
        {
            get
            {
                return _datasetCacheClearings;
            }
        }

        public static PerformanceCounter DatasetCacheHits
        {
            get
            {
                return _datasetCacheHits;
            }
        }

        public static PerformanceCounter DatasetCacheMisses
        {
            get
            {
                return _datasetCacheMisses;
            }
        }
        #endregion

        // Fields
        private TimeSpan _clearLatency;
        private readonly TimeSpan _defaultTimeout;
        private readonly SiteInfo _site;
        private readonly Database _database;


        // Methods
        public DatasetCache(SiteInfo site, long maxSize)
            : base(site.Name + "[dataset]", maxSize)
        {
            this._defaultTimeout = DateUtil.ParseTimeSpan(Settings.GetSetting("Caching.DatasetLifetime"), TimeSpan.Zero);
            this._site = site;
            this._clearLatency = TimeSpan.Zero;
            this._database = Factory.GetDatabase(site.Database);
            SetupDatabaseEventHandlers();
        }

        private string _shortName = null;
        private string ShortName
        {
            get
            {
                if (_shortName == null)
                    _shortName = this.Name.Replace("[dataset]", string.Empty);
                return _shortName;
            }
        }

        public object AcquireKeyLock(string key)
        {
            lock (_syncRoot)
            {
                var obj = (object)null;
                if (_keyLocks.ContainsKey(key))
                    obj = _keyLocks[key];

                if (obj == null)
                {
                    obj = new object();
                    _keyLocks.Add(key, obj);
                }

                return obj;
            }
        }

        private void SetupDatabaseEventHandlers()
        {
            this._database.Engines.DataEngine.AddedVersion += new EventHandler<ExecutedEventArgs<AddVersionCommand>>(this.DataEngine_AddedVersion);
            this._database.Engines.DataEngine.DeletedItem += new EventHandler<ExecutedEventArgs<DeleteItemCommand>>(this.DataEngine_DeletedItem);
            this._database.Engines.DataEngine.RemovedData += new EventHandler<ExecutedEventArgs<RemoveDataCommand>>(this.DataEngine_RemovedData);
            this._database.Engines.DataEngine.RemovedVersion += new EventHandler<ExecutedEventArgs<RemoveVersionCommand>>(this.DataEngine_RemovedVersion);
            this._database.Engines.DataEngine.SavedItem += new EventHandler<ExecutedEventArgs<SaveItemCommand>>(this.DataEngine_SavedItem);
        }

        private void DataEngine_AddedVersion(object sender, ExecutedEventArgs<AddVersionCommand> e)
        {
            this.RemoveItem(e.Command.Item.ID);
        }

        private void DataEngine_DeletedItem(object sender, ExecutedEventArgs<DeleteItemCommand> e)
        {
            this.RemoveItem(e.Command.Item.ID);
        }

        private void DataEngine_RemovedData(object sender, ExecutedEventArgs<RemoveDataCommand> e)
        {
            this.RemoveItem(e.Command.ItemId);
        }

        private void DataEngine_RemovedVersion(object sender, ExecutedEventArgs<RemoveVersionCommand> e)
        {
            this.RemoveItem(e.Command.Item.ID);
        }

        private void DataEngine_SavedItem(object sender, ExecutedEventArgs<SaveItemCommand> e)
        {
            this.RemoveItem(e.Command.Item.ID);
        }

        public override void Clear()
        {
            this.Clear(false);
        }

        public virtual void Clear(bool force)
        {
            if (force || (this.ClearLatency == TimeSpan.Zero))
            {
                this.DoClear();
            }
            else
            {
                string jobName = this.Name + "_CacheClear";
                if (!JobManager.IsJobQueued(jobName))
                {
                    JobOptions options = new JobOptions(jobName, "caching", this._site.Name, this, "DoClear");
                    options.InitialDelay = this.ClearLatency;
                    Job job = new Job(options);
                    JobManager.Start(job);
                }
            }
        }

        public virtual void RemoveItem(ID itemId)
        {
            Assert.ArgumentNotNull(itemId, "itemId");
            var keys = base.InnerCache.GetCacheKeys(itemId.ToString());
            foreach (string k in keys)
            {
                var obj = GetObject(k, true);
                if (obj != null && !(obj is CacheFailure))
                    base.Remove(k);
            }
        }

        private void DoClear()
        {
            base.Clear();
            DatasetCache.DatasetCacheClearings.Increment();
        }

        private object GetObject(string key)
        {
            return GetObject(key, false);
        }
        private object GetObject(string key, bool includeExpired)
        {
            var value = base.InnerCache.GetValue(key);
            if (value != null)
            {
                DatasetCache.DatasetCacheHits.Increment();
                if (value is CacheContainer)
                {
                    var container = (CacheContainer)value;
                    if (!container.IsExpired || includeExpired)
                        return container.Data;
                }
            }
            DatasetCache.DatasetCacheMisses.Increment();
            return null;
        }

        public void SetObject(string key, object obj, TimeSpan cacheduration)
        {
            Assert.ArgumentNotNull(key, "key");
            Assert.ArgumentNotNull(obj, "obj");
            if (base.Enabled)
            {
                var container = new CacheContainer(obj, cacheduration);
                base.InnerCache.Add(key, container, TypeUtil.SizeOfObject());
            }
        }

        public T Get<T>(string key, double asyncTimeout, TimeSpan cacheTimeout, TimeSpan exceptionTimeout, bool onErrorUseCachedData, Func<T> fn) where T : class
        {
            // Get the (un-expired) object from the cached..
            // It could be: DataTable, CacheFailure or null
            var obj = GetObject(key);

            // If the type requested is "CacheFailure" simply return what we have...
            if (typeof(T) == typeof(CacheFailure))
                return obj as T;

            // If its a CacheFailure.. handle it (raise cached error or use last good data)
            obj = HandleCachedFailure(key, onErrorUseCachedData, obj);

            // If its null, we need to try to retrieve new data...
            if (obj == null || cacheTimeout.TotalSeconds == 0.0)
            {
                var cachefailuretimeout = exceptionTimeout;
                lock (this.AcquireKeyLock(key))
                {
                    obj = GetObject(key) as T;
                    if (obj == null || cacheTimeout.TotalSeconds == 0.0)
                    {
                        // Refresh the data and cache it on a separate thread...
                        var crt = new CacheRefreshThread<T>()
                        {
                            DsCache = this,
                            CacheKey = key,
                            ContextItem = Sitecore.Context.Item,
                            ContextSite = Sitecore.Context.Site,
                            CacheSuccessTimeout = cacheTimeout,
                            CacheExceptionTimeout = exceptionTimeout,
                            RefreshData = fn,
                            RequestUri = WebUtil.GetRawUrl() ?? string.Empty
                        };

                        var t = new System.Threading.Thread(new ThreadStart(crt.ThreadProc));

                        var started = DateTime.Now;
                        t.Start();

                        // Wait X second(s), checking for status update each 100ms
                        var endtime = DateTime.Now.AddSeconds(asyncTimeout);
                        var timeoutexpired = false;
                        while (!timeoutexpired)
                        {
                            Thread.Sleep(50);
                            if (!t.IsAlive)
                                break;
                            timeoutexpired = DateTime.Now > endtime;
                        }

                        var cost = DateTime.Now - started;

                        // First try grabbing result from worker thread result..
                        obj = crt.Result as T;

                        if (obj == null)
                        {
                            // The worker thread completed (successfully or otherwise).
                            // Re-try getting the object from the cache..
                            obj = GetObject(key);
                            obj = HandleCachedFailure(key, onErrorUseCachedData, obj);

                            if ((obj as T) == null)
                            {
                                if (onErrorUseCachedData)
                                {
                                    obj = GetLastGoodData(key) as T;
                                    if (obj != null)
                                        Log.Debug(string.Format("Dataset refresh did not finish in {0} seconds. Using LastGoodData. [key:{1}][cost:{2}]", asyncTimeout, key, cost));
                                }

                                if ((obj as T) == null)
                                {
                                    if (timeoutexpired)
                                        throw new Exception(string.Format("Dataset refresh did not finish in {0} seconds. Consider changing the 'AsynchTimeout' value on this dataset if a longer refresh time is normal. [key:{1}][cost:{2}]", asyncTimeout, key, cost));
                                    else
                                        throw new Exception(string.Format("Dataset refresh failed. [key:{0}][cost:{1}]", key, cost));
                                }

                            }
                        }

                        //if ((obj as T) == null)
                        //{
                        //    throw new Exception(string.Format("Dataset refresh failed. [key:{0}][cost:{1}]", key, cost));
                        //}
                    }
                }
            }

            return obj as T;
        }

        /// <summary>
        /// If the incoming object is of type CacheFailure, return the last good cached data 
        /// if no good data, raise the inner CacheFailure error.
        /// </summary>
        private object HandleCachedFailure(string key, bool onErrorUseCachedData, object obj)
        {
            if (obj is CacheFailure)
            {
                var fail = (CacheFailure)obj;

                if (onErrorUseCachedData && fail.LastGoodData as DataTable != null)
                {
                    obj = fail.LastGoodData;
                    Log.Info("CACHED FAILURE ---------> Using last good cached data. [key:" + key + "]", this);
                }
                else
                    throw fail.GetException();

            }
            return obj;
        }

        internal void HandleCacheRefreshSuccess(string key, TimeSpan duration, TimeSpan cacheTimeout, object obj, string url)
        {
            var msg = "GetData: data refreshed. [key:" + key + "][cost:" + duration.TotalSeconds.ToString() + "s][cache for:" + cacheTimeout.TotalMinutes + " min][cache:" + this.ShortName + "]";
            if (!string.IsNullOrEmpty(url))
                msg += "[url:" + url + "]";
            Log.Info(msg, this);
            SetObject(key, obj, cacheTimeout);
        }

        internal void HandleCacheRefreshException(string key, TimeSpan duration, TimeSpan cachefailuretimeout, Exception ex)
        {
            // Error while retrieving data. 
            // Store a "CacheFailure" object in place of the cached data for "cachefailuretimeoutminutes" minutes. Subsequent
            // calls will see the CacheFailure and automatically throw the same exception back to the client until it expires.
            Log.Error("FAILED to refresh cache for: [key:" + key + "][cost:" + duration.TotalSeconds + "s][retry in: " + cachefailuretimeout.TotalMinutes + " min]", ex, this);

            // try to store the most recent good data.. 
            var expireddata = GetLastGoodData(key);
            var cacheFail = new CacheFailure()
            {
                Timestamp = DateTime.Now,
                InnerException = ex,
                Duration = cachefailuretimeout,
                Key = key,
                LastGoodData = expireddata
            };
            SetObject(key, cacheFail, cachefailuretimeout);
        }

        private DataTable GetLastGoodData(string key)
        {
            var expireddata = GetExpired<DataTable>(key) as DataTable;
            if (expireddata == null)
            {
                var priorFailure = GetExpired<CacheFailure>(key) as CacheFailure;
                if (priorFailure != null)
                    expireddata = priorFailure.LastGoodData as DataTable;
            }
            return expireddata;
        }

        public T GetExpired<T>(string key) where T : class
        {
            return GetObject(key, true) as T;
        }

        //public DataTable GetDataset(ID dsItemID)
        //{
        //    return GetDataset(dsItemID, false);
        //}
        //public DataTable GetDataset(ID dsItemID, bool includeExpired)
        //{
        //    var xml = GetObject(dsItemID, includeExpired) as string;
        //    if (xml != null)
        //        return DataTableFromXml(xml);
        //    return null;
        //}

        //public void SetDataset(ID dsItemID, DataTable dt, TimeSpan cacheduration)
        //{
        //    SetObject(dsItemID, DataTableToXml(dt), cacheduration);
        //}

        // Properties
        public virtual TimeSpan ClearLatency
        {
            get
            {
                return this._clearLatency;
            }
            set
            {
                this._clearLatency = value;
            }
        }

        //private static string DataTableToXml(DataTable dt)
        //{
        //    var output = string.Empty;
        //    using (System.IO.StringWriter sw = new System.IO.StringWriter())
        //    {
        //        DataSet tmp = CreateTempDataSet(dt);
        //        tmp.WriteXml(sw, XmlWriteMode.WriteSchema);
        //        output = sw.ToString();
        //    }
        //    return output;
        //}

        //private static DataTable DataTableFromXml(string xml)
        //{
        //    DataSet tmp = new DataSet("DataTable");
        //    using (System.IO.StringReader sr = new System.IO.StringReader(xml))
        //    {
        //        tmp.ReadXml(sr, XmlReadMode.ReadSchema);
        //    }
        //    return tmp.Tables["tbl"];
        //}

        //private static DataSet CreateTempDataSet(DataTable dt)
        //{
        //    // Create a temporary DataSet
        //    DataSet ds = new DataSet("DataTable");
        //    dt.TableName = "tbl";

        //    // Make sure the DataTable does not already belong to a DataSet
        //    if (dt.DataSet == null)
        //        ds.Tables.Add(dt);
        //    else
        //        ds.Tables.Add(dt.Copy());
        //    return ds;
        //}
    }

}