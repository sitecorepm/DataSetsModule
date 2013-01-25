using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SharedSource.Text;
using DsIDs = Sitecore.SharedSource.Dataset.FieldIDs.Dataset;

namespace Sitecore.SharedSource.Dataset.Items
{
    public class XmlFeedItem : BaseDataset
    {

        public XmlFeedItem(Item item) : base(item) { }

        private Uri FeedUrl
        {
            get
            {
                try
                {

                    return new Uri(this[DsIDs.XmlFeed.FeedUrl]);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to create a URI from the string: '" + this[DsIDs.XmlFeed.FeedUrl] + "'", ex);
                }
            }
        }

        private string XPathQuery
        {
            get
            {
                return this[DsIDs.XmlFeed.XPathQuery];
            }
        }

        private string Username
        {
            get
            {
                return this[DsIDs.XmlFeed.Username];
            }
        }

        private string Password
        {
            get
            {
                return this[DsIDs.XmlFeed.Password];
            }
        }

        #region IDatasetItem

        protected override Dictionary<string, string> RefreshFieldMap()
        {
            var map = base.RefreshFieldMap();
            var dt = RefreshData();
            foreach (DataColumn c in dt.Columns)
                map.Add(c.ColumnName, c.ColumnName);
            return map;
        }

        protected override DataTable RefreshData()
        {
            Assert.ArgumentNotNull(this.FeedUrl, "Feed Url");
            Assert.ArgumentNotNull(this.XPathQuery, "XPath Query");

            var dt = new DataTable();
            var url = this.FeedUrl.AbsoluteUri;
            var settings = (XmlReaderSettings)null;
            XDocument feed = null;

            if (!string.IsNullOrEmpty(this.Username) && !string.IsNullOrEmpty(this.Password))
            {
                var resolver = new XmlUrlResolver();
                resolver.Credentials = new System.Net.NetworkCredential(this.Username, this.Password);
                settings.XmlResolver = resolver;

                feed = XDocument.Load(XmlReader.Create(url, settings));
            }
            else
            {
                feed = XDocument.Load(url);
            }

            var doc = new XmlDocument();
            using (var xmlReader = feed.CreateReader())
            {
                doc.Load(xmlReader);
            }
            var dataitems = doc.SelectNodes(this.XPathQuery);

            if (dataitems.Count > 0)
            {
                // Build the table from the first 5 rows..
                for (var i = 0; i < Math.Min(dataitems.Count, 6); i++)
                {
                    foreach (XmlNode n in dataitems[i].ChildNodes)
                    {
                        if (!dt.Columns.Contains(n.Name))
                        {
                            DateTime date;
                            if (DateTime.TryParse(n.Value, out date))
                                dt.Columns.Add(n.Name, typeof(DateTime));
                            else
                                dt.Columns.Add(n.Name, typeof(string));
                        }
                    }
                }

                // Populate it
                foreach (XmlNode item in dataitems)
                {
                    var dr = dt.NewRow();

                    foreach (DataColumn dc in dt.Columns)
                    {
                        var xmlCol = item.ChildNodes.Cast<XmlNode>().SingleOrDefault(x => x.Name.ToLower() == dc.ColumnName.ToLower());
                        if (xmlCol != null)
                        {
                            var rawvalue = xmlCol.InnerText;

                            switch (dc.DataType.Name)
                            {
                                case "DataTime":
                                    DateTime date;
                                    if (DateTime.TryParse(rawvalue, out date))
                                        dr[dc] = date;
                                    break;
                                default:
                                    dr[dc] = rawvalue;
                                    break;
                            }
                        }
                    }

                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        #endregion

    }
}
