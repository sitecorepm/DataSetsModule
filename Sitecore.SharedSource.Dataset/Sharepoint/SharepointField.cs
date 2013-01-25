using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sitecore.SharedSource.Dataset.Sharepoint
{
    public class SharepointField
    {
        private static Regex _rgxSharepointJunk = new Regex(@"(#?)\d+;#", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _rgxSharepointComputedFloat = new Regex(@"float\;\#(?<floatvalue>\d+\.\d+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _rgxSharepointComputedDateTime = new Regex(@"datetime\;\#(?<datetimevalue>[\d\-\s:(AM)(PM)]+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _rgxSharepointUrlField = new Regex(@"(?<url>[^,]*),(?<title>.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _rgxSharepointLookupMulti = new Regex(@"(?:\d+);#(?<value>.*?)(?=(?:;#|$))", RegexOptions.Compiled | RegexOptions.Singleline);
        #region From: http://blogs.msdn.com/b/markarend/archive/2007/05/29/parsing-multi-value-fields-multichoice-lookup-user-url-rules-for-the-delimiter.aspx
        /// <summary>
        /// Regular expression to isolate multi-choice values
        /// </summary>
        /// <example>
        //    System.Text.RegularExpressions.Match choice;
        //    string strField;
        //    foreach (SPListItem item in list.Items)
        //    {
        //        strField = (string)item["MultiChoiceField"];
        //        output.Append(item.Title + ": " + strField + "<BR>");
        //        if (strField != null)
        //        {
        //            choice = Util.rexMultiChoiceField.Match(strField);
        //            while (choice.Success)
        //            {
        //                output.Append("- " + choice.Result("$1") + "<BR>");
        //                choice = choice.NextMatch();
        //            }
        //        }
        //    }
        ///// </example>
        private static Regex rexMultiChoiceField = new Regex(@"#(.+?);", RegexOptions.Compiled);


        /// <summary>
        /// Regular expression to isolate an ID ("$1") or lookup value ("$2")
        /// Use this for "Lookup" fields, or for "Person or Group" fields
        /// </summary>
        /// <example>
        ///    string LookupField, LookupId, LookupValue;
        ///    if (Util.rexLookupField.Match(LookupField).Success)
        ///    {
        ///        LookupId = Util.rexLookupField.Match(LookupField).Result("$1");
        ///        LookupValue = Util.rexLookupField.Match(LookupField).Result("$2");
        ///    }
        /// </example>
        private static Regex rexLookupField = new Regex(@"(\d+);#(.*)$", RegexOptions.Compiled);


        /// <summary>
        /// Regular expression to isolate a URL ("$1") or its Description ("$2")
        /// </summary>
        /// <example>
        ///    string UrlField, UrlPath, UrlName;
        ///    if (Util.rexUrlField.Match(UrlField).Success)
        ///    {
        ///        UrlPath = Util.rexUrlField.Match(UrlField).Result("$1");
        ///        UrlName = Util.rexUrlField.Match(UrlField).Result("$2");
        ///    }
        /// </example>
        private static Regex rexUrlField = new Regex(@"^(.*), +(.*)$", RegexOptions.Compiled);

        #endregion

        public SharepointField(XmlNode xn)
        {
            this.DisplayName = xn.Attributes["DisplayName"].Value;
            this.Name = XmlConvert.DecodeName(xn.Attributes["Name"].Value);
            this.Type = xn.Attributes["Type"].Value;
            this.ReadOnly = xn.Attributes["ReadOnly"] == null ? false : bool.Parse(xn.Attributes["ReadOnly"].Value);
        }

        public bool ReadOnly { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string FriendlyName
        {
            get
            {
                return (string.IsNullOrEmpty(this.DisplayName) ? this.Name : this.DisplayName);
            }
        }
        public string FriendlyValue(string vcRawValue)
        {
            string vcValue = string.Empty;

            vcValue = _rgxSharepointJunk.Replace(vcRawValue, "");

            switch (this.Type.ToLower())
            {
                case "url":
                    Match match = _rgxSharepointUrlField.Match(vcValue);
                    if (match.Success)
                    {
                        string vcUrl = match.Groups["url"].Value.Trim();
                        string vcTitle = match.Groups["title"].Value.Trim();

                        if (!string.IsNullOrEmpty(vcUrl) && !string.IsNullOrEmpty(vcTitle))
                            vcValue = "<a href='" + vcUrl + "'>" + vcTitle + "</a>";
                        else if (!string.IsNullOrEmpty(vcUrl))
                            vcValue = "<a href='" + vcUrl + "'>" + vcUrl + "</a>";
                        else
                            vcValue = vcValue.Replace(",", "").Trim();
                    }
                    break;
                case "datetime":
                    DateTime date;
                    if (DateTime.TryParse(vcValue, out date))
                        vcValue = date.ToString();
                    break;
                case "number":
                    vcValue = FormatNumber(vcValue);
                    break;
                case "currency":
                    Double currency;
                    if (Double.TryParse(vcValue, out currency))
                        vcValue = currency.ToString("C");
                    break;
                case "calculated":
                    vcValue = _rgxSharepointComputedDateTime.Replace(vcValue, delegate(Match m) { return m.Groups["datetimevalue"].Value; });
                    vcValue = _rgxSharepointComputedFloat.Replace(vcValue, delegate(Match m) { return FormatNumber(m.Groups["floatvalue"].Value); });
                    vcValue = vcValue.Replace("string;#", "");
                    break;
                case "lookupmulti":
                    MatchCollection mc = _rgxSharepointLookupMulti.Matches(vcRawValue);
                    if (mc.Count > 0)
                        vcValue = string.Empty;
                    foreach (Match m in mc)
                    {
                        if (!string.IsNullOrEmpty(vcValue)) vcValue += ", ";
                        vcValue += m.Groups["value"].Value;
                    }
                    break;
                case "attachments":
                case "boolean":
                case "choice":
                case "contenttypeid":
                case "counter":
                case "file":
                case "guid":
                case "integer":
                case "lookup":
                case "modstat":
                case "note":
                case "text":
                case "user":
                case "workflowstatus":
                default:
                    break;
            }

            //Logging.Log.Debug("[type:" + this.Type.ToLower() + "][rawvalue:" + vcRawValue + "][value:" + vcValue + "]");

            return vcValue;
        }

        public string ConvertToSharepointValue(string vcRawValue)
        {
            string vcValue = string.Empty;

            switch (this.Type.ToLower())
            {
                case "datetime":
                    DateTime date;
                    if (DateTime.TryParse(vcRawValue, out date))
                        vcValue = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    break;
                case "attachments":
                case "boolean":
                case "calculated":
                case "choice":
                case "contenttypeid":
                case "counter":
                case "currency":
                case "file":
                case "guid":
                case "integer":
                case "lookup":
                case "modstat":
                case "note":
                case "number":
                case "text":
                case "url":
                case "user":
                case "workflowstatus":
                default:
                    vcValue = vcRawValue;
                    break;
            }

            return vcValue;
        }

        private static string FormatNumber(string vcValue)
        {
            Double dbl;
            if (Double.TryParse(vcValue, out dbl))
                vcValue = dbl.ToString("G");
            return vcValue;
        }
    }
}
