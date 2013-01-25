using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Dataset;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Text;
using CpnIDs = Sitecore.SharedSource.Dataset.FieldIDs.Component;

namespace Sitecore.SharedSource.Dataset.Sharepoint
{
    public class SharepointService
    {
        private object _ListsProxy = null;
        private object _ViewsProxy = null;
        private ICredentials _credentials = null;
        private Dictionary<string, Dictionary<string, SharepointField>> _GetFieldsCache = new Dictionary<string, Dictionary<string, SharepointField>>();

        public SharepointService(string webServiceAsmxUrl)
            : this(webServiceAsmxUrl, null) { }

        public SharepointService(string webServiceAsmxUrl, ICredentials creds)
        {
            if (creds != null)
            {
                _ListsProxy = WebServiceUtil.GetWebServiceClientProxyClass(webServiceAsmxUrl, "Lists", creds);
                _ViewsProxy = WebServiceUtil.GetWebServiceClientProxyClass(webServiceAsmxUrl, "Views", creds);
                _credentials = creds;
            }
            else
            {
                _ListsProxy = WebServiceUtil.GetWebServiceClientProxyClass(webServiceAsmxUrl, "Lists");
                _ViewsProxy = WebServiceUtil.GetWebServiceClientProxyClass(webServiceAsmxUrl, "Views");
            }
        }

        /// <summary>
        /// Get field name dictionary. 
        /// Key: DisplayName
        /// Value: (system) Name
        /// </summary>
        public Dictionary<string, SharepointField> GetFields(string vcListName, string vcViewName)
        {
            string cachekey = vcListName + vcViewName;

            var dict = new Dictionary<string, SharepointField>();

            if (_GetFieldsCache.ContainsKey(cachekey))
                dict = _GetFieldsCache[cachekey];
            else
            {
                if (String.IsNullOrEmpty(vcViewName))
                {
                    var xnList = CallListMethod<XmlNode>("GetList", new object[] { vcListName });
                    var xnl = ExcludeHiddenFields(xnList);

                    foreach (XmlNode xn in xnl)
                        AddFieldToDictionary(dict, xn);
                }
                else
                {
                    string viewGUID = GetViewGuid(vcListName, vcViewName);
                    var xnResults = CallListMethod<XmlNode>("GetListAndView", new object[] { vcListName, viewGUID });

                    var xnlViewFields = xnResults.SelectNodesEx("ListAndView/View/ViewFields/FieldRef");
                    var xnlFields = xnResults.SelectNodesEx("ListAndView/List/Fields/Field");

                    foreach (XmlNode xn in xnlViewFields)
                    {
                        var xnField = xnlFields.Cast<XmlNode>().Single(x => x.Attributes["Name"].Value == xn.Attributes["Name"].Value);
                        AddFieldToDictionary(dict, xnField);
                    }
                }
                _GetFieldsCache.Add(cachekey, dict);
            }

            return dict;
        }

        private static IEnumerable<XmlNode> ExcludeHiddenFields(XmlNode xnList)
        {
            IEnumerable<XmlNode> xnl;
            xnl = xnList.SelectNodesEx("List/Fields/Field").Cast<XmlNode>()
                                    .Where(x => x.Attributes["Hidden"] == null ||
                                              x.Attributes["Hidden"].Value.ToLower() == "false");
            return xnl;
        }

        public DataTable GetLists()
        {
            List<XmlNode> xnl = CallListMethod<XmlNode>("GetListCollection", new object[] { })
                                            .SelectNodesEx("Lists/List")
                                            .Cast<XmlNode>()
                                            .OrderBy(x => x.Attributes["Title"].Value)
                                            .ToList();
            return XmlNodeListToDataTable(xnl);
        }

        public DataTable GetViews(string vcListName)
        {
            List<XmlNode> xnl = CallViewMethod<XmlNode>("GetViewCollection", new object[] { vcListName })
                                            .SelectNodesEx("Views/View")
                                            .Cast<XmlNode>()
                                            .OrderBy(x => x.Attributes["DisplayName"].Value)
                                            .ToList();
            return XmlNodeListToDataTable(xnl);
        }

        private DataTable XmlNodeListToDataTable(List<XmlNode> xnl)
        {
            DataTable dt = null;

            if (xnl.Count > 0)
            {
                dt = new DataTable();

                // Build table structure
                foreach (XmlAttribute attr in xnl[0].Attributes)
                    dt.Columns.Add(attr.Name);

                // Populate table
                foreach (XmlNode xn in xnl)
                {
                    DataRow dr = dt.NewRow();
                    foreach (DataColumn dc in dt.Columns)
                    {
                        XmlAttribute a = xn.Attributes[dc.ColumnName];
                        if (a != null)
                            dr[dc] = a.Value;
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }

        public DataTable GetListItems(string listName, string viewName, string rowLimit, string query, string viewFields, string queryOptions)
        {
            XmlNode nodeListItems = null;
            DataTable dt = null;

            /* Assign values to the string parameters of the GetListItems method, using GUIDs for the listName and viewName variables. 
             * For listName, using the list display name will also work, but using the list GUID is recommended. 
             * For viewName, only the view GUID can be used. Using an empty string for viewName causes the default view to be used.*/
            if (!string.IsNullOrEmpty(viewName))
                viewName = GetViewGuid(listName, viewName);

            /*Use the CreateElement method of the document object to create elements for the parameters that use XML.*/
            var xmlDoc = new XmlDocument();
            var xmlQuery = xmlDoc.CreateElement("Query");
            var xmlViewFields = xmlDoc.CreateElement("ViewFields");
            var xmlQueryOptions = xmlDoc.CreateElement("QueryOptions");
            var webID = (string)null;

            /*To specify values for the parameter elements (optional), assign CAML fragments to the InnerXml property of each element.*/
            xmlQuery.InnerXml = query;               //"<Where><Gt><FieldRef Name=\"ID\" /><Value Type=\"Counter\">3</Value></Gt></Where>";
            xmlViewFields.InnerXml = viewFields;     //"<FieldRef Name=\"Title\" />";
            xmlQueryOptions.InnerXml = queryOptions;

            nodeListItems = CallListMethod<XmlNode>("GetListItems", new object[] { listName, viewName, xmlQuery, xmlViewFields, rowLimit, xmlQueryOptions, webID });

            using (DataSet ds = new DataSet())
            {
                ds.ReadXml(new XmlNodeReader(nodeListItems));
                if (ds.Tables.Count > 1)
                    dt = ds.Tables[1];
            }

            return dt;
        }

        private T CallListMethod<T>(string methodName, object[] parameters)
        {
            return (T)ExecuteWebMethod(_ListsProxy, methodName, parameters);
        }

        private T CallViewMethod<T>(string methodName, object[] parameters)
        {
            return (T)ExecuteWebMethod(_ViewsProxy, methodName, parameters);
        }


        private object ExecuteWebMethod(object wsProxy, string methodName, object[] parameters)
        {
            object result = null;


            var wsType = wsProxy.GetType();

            if (_credentials != null)
                wsType.GetProperty("Credentials").SetValue(wsProxy, _credentials, null);

            //wsType.GetProperty("Url").SetValue(wsProxy, webServiceAsmxUrl, null);
            wsType.GetProperty("Timeout").SetValue(wsProxy, System.Convert.ToInt32(Sitecore.Configuration.Settings.GetTimeSpanSetting("SharepointServiceProxy.Timeout", "00:00:03").TotalMilliseconds), null);

            try
            {
                result = wsType.GetMethod(methodName).Invoke(wsProxy, parameters);
            }
            catch (Exception ex)
            {
                var creds = string.Empty;
                var nc = (NetworkCredential)_credentials;
                if (nc != null)
                    creds = nc.Domain + @"\" + nc.UserName;
                var msg = string.Format("ExecuteWebMethod failed. [{0}][{1}]", new object[] { methodName, creds });

                try
                {
                    if (ex.GetBaseException() is SoapException)
                        msg += "\nSoapException.Detail: " + ((SoapException)ex.GetBaseException()).Detail.OuterXml;
                }
                catch
                {
                    msg += "[failed to extract SoapException.Detail]";
                }

                throw new Exception(msg, ex);
            }

            return result;
        }

        public XmlNode UpdateListItems<T>(string listName, string viewName, T dataitem, GetDataItemFieldValue<T> gfv)
        {
            XmlNode xnResults = null;

            var spFieldLookup = this.GetFields(listName, viewName).Values.ToDictionary(x => x.Name);

            string vcBatch = "<Method ID='1' Cmd='New'>" +
                                    "<Field Name='ID'>New</Field>";

            //foreach (Mapping m in info.FieldMappings)
            foreach (var f in spFieldLookup.Keys)
            {
                string fieldvalue = string.Empty;
                //if (m.FieldExpression.Contains("["))
                //    // Expression processing...
                //    fieldvalue = DatasetViewEngine_v1.Render(m.FieldExpression, dataitem, gfv, null);
                //else
                //    fieldvalue = gfv(dataitem, m.FieldExpression, null, null, null);
                fieldvalue = gfv(dataitem, f, null, null, null);

                vcBatch += "<Field Name='" + f + "'>" +
                                    System.Web.HttpUtility.HtmlEncode(spFieldLookup[f].ConvertToSharepointValue(fieldvalue)) +
                           "</Field>";
            }

            vcBatch += "</Method>";


            var xmlDoc = new XmlDocument();
            var e = xmlDoc.CreateElement("Batch");

            e.SetAttribute("OnError", "Continue");

            if (!string.IsNullOrEmpty(viewName))
                e.SetAttribute("ViewName", viewName);

            e.InnerXml = vcBatch;

            try
            {
                xnResults = CallListMethod<XmlNode>("UpdateListItems", new object[] { listName, e });

                // Check response for error text
                XmlNode xn = xnResults.SelectNodesEx("Results/Result")[0];
                if (xn.InnerXml.Contains("<ErrorText"))
                    throw new Exception(xn.OuterXml);
                //else
                //    Logging.Log.Debug("Update Batch Xml: \n " + e.OuterXml);
            }
            catch (Exception ex)
            {
                string message = "Failed to save form data to sharepoint. \n\n";
                message += "Sharepoint Connection: [ListName:" + listName + "][ViewName:" + viewName + "]\n\n";
                message += "Update Batch Xml:\n" + e.OuterXml + "\n\n";
                if (xnResults != null)
                    message += "Results Xml:\n" + xnResults.OuterXml;
                throw new Exception(message, ex);
            }

            return xnResults;
        }

        public string AddAttachment(string vcListName, string vcListItemId, string vcFileName, byte[] contents)
        {
            return CallListMethod<string>("AddAttachment", new object[] { vcListName, vcListItemId, vcFileName, contents });
        }

        private static void AddFieldToDictionary(Dictionary<string, SharepointField> dict, XmlNode xn)
        {
            SharepointField sf = new SharepointField(xn);
            if (dict.Keys.Contains(sf.FriendlyName))
            {
                if (dict.Keys.Contains(sf.Name))
                    throw new Exception(string.Format("This SharepointView has duplicate column names. {0} and {1} already exist in the field list.", new object[] { sf.FriendlyName, sf.Name }));
                else
                    dict.Add(sf.Name, sf);
            }
            else
                dict.Add(sf.FriendlyName, sf);
        }

        /// <summary>
        /// Get the view GUID for the specified view name
        /// </summary>
        /// <param name="item"></param>
        /// <param name="listName"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        private string GetViewGuid(string listName, string viewName)
        {
            string viewGUID = string.Empty;

            // Make sure viewName isn't already a GUID
            try
            {
                Guid g = new Guid(viewName);
                viewGUID = viewName;
            }
            catch { }

            if (string.IsNullOrEmpty(viewGUID))
            {
                XmlNode nView = null;

                nView = CallViewMethod<XmlNode>("GetViewCollection", new object[] { listName })
                                .SelectNodesEx("Views/View")
                                .Cast<XmlNode>()
                                .SingleOrDefault(x => x.Attributes["DisplayName"].Value == viewName);

                if (nView != null)
                    viewGUID = nView.Attributes["Name"].Value;

                if (string.IsNullOrEmpty(viewGUID))
                    throw new Exception("The sharepoint view name '" + viewName + "' could not be found.");
            }

            return viewGUID;
        }
    }

}
