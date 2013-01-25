using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Text;
using DsIDs = Sitecore.SharedSource.Dataset.FieldIDs.Dataset;
using Sitecore.SharedSource.Dataset.DataAccess;

namespace Sitecore.SharedSource.Dataset.Items
{
    public class DatabaseQueryItem : BaseDataset
    {

        public DatabaseQueryItem(Item item) : base(item) { }

        protected virtual SqlServerDatabase DbInstance
        {
            get
            {
                return new SqlServerDatabase(this[DsIDs.DatabaseQuery.ConnectionStringName]);
            }
        }

        protected virtual string Query
        {
            get
            {
                return this[DsIDs.DatabaseQuery.Query];
            }
        }

        #region IDatasetItem

        protected override Dictionary<string, string> RefreshFieldMap()
        {
            var map = base.RefreshFieldMap();
            string sql = "select * from (" + this.Query + ") x where 1=0";
            DataTable dt = this.DbInstance.SelectDataTable(sql);
            if (dt != null && dt.Columns.Count > 0)
            {
                foreach (DataColumn c in dt.Columns)
                    map.Add(c.ColumnName, c.ColumnName);
            }
            return map;
        }

        protected override DataTable RefreshData()
        {
            DataTable dt = this.DbInstance.SelectDataTable(this.Query);
            return dt;
        }

        #endregion

    }
}
