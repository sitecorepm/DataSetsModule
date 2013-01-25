using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Dataset.DataAccess;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using DsIDs = Sitecore.SharedSource.Dataset.FieldIDs.Dataset;

namespace Sitecore.SharedSource.Dataset.Items
{
    public class DatabaseArguementsQueryItem : DatabaseQueryItem
    {
        private string _query = null;

        public DatabaseArguementsQueryItem(Item item) : base(item) { }

        protected override SqlServerDatabase DbInstance
        {
            get
            {
                return new SqlServerDatabase(this[DsIDs.DatabaseArguementsQuery.ConnectionStringName]);
            }
        }

        protected override void BeforeRefreshFieldMap()
        {
            SetQuery();
        }
        protected override void BeforeRefreshData()
        {
            SetQuery();
        }

        private void SetQuery()
        {
            if (_query == null)
                _query = this.ParseQuery(this[DsIDs.DatabaseArguementsQuery.Query], this[DsIDs.DatabaseArguementsQuery.QueryString]);
        }

        protected override string Query { get { return _query; } }

        private string ParseQuery(string query, string querystring)
        {
            NameValueCollection nvcArguements = HttpUtility.ParseQueryString(querystring);

            foreach (String sArguement in nvcArguements.AllKeys)
            {
                if (query.IndexOf("[" + sArguement + "]") != -1)
                    query = query.Replace("[" + sArguement + "]", Sitecore.Context.Request.GetQueryString(sArguement, nvcArguements[sArguement]));
            }
            return query;
        }

        protected override TimeSpan CacheTimeout
        {
            get
            {
                return TimeSpan.Zero;
            }
        }

        private static TimeSpan _exceptionCacheTimeout = new TimeSpan(0, 0, 2);
        protected override TimeSpan ExceptionCacheTimeout
        {
            get
            {
                return _exceptionCacheTimeout;
            }
        }
    }
}

