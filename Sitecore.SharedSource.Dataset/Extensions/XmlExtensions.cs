using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class XmlExtensions
    {
        /// <summary>
        /// SelectNodes method without needing to worry about namespaces...
        /// </summary>
        /// <param name="xn"></param>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public static XmlNodeList SelectNodesEx(this XmlNode xn, string xpath)
        {
            var xd = new XmlDocument();
            xd.LoadXml(xn.OuterXml);

            var xnsm = new XmlNamespaceManager(xd.NameTable);
            xnsm.AddNamespace("x", "http://schemas.microsoft.com/sharepoint/soap/");

            var r = new Regex(@"\w+");
            xpath = r.Replace(xpath, "x:$&");

            return xd.SelectNodes(xpath, xnsm);
        }
    }
}
