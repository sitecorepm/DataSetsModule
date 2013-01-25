using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.WebControls;

using Sitecore.SharedSource.Text;
using Sitecore.SharedSource.Dataset.Domain;

namespace Sitecore.SharedSource.Dataset
{
    public static class DatasetViewEngine
    {
        private static KeyLockManager _lockMgr = new KeyLockManager();
        private static Dictionary<string, string> _variableCache = new Dictionary<string, string>();

        public static string Render<T>(string template, T dataitem, GetDataItemFieldValue<T> gfv, GetDataItemUrl<T> gu)
        {
            return Render(template, dataitem, gfv, gu, null);
        }
        public static string Render<T>(string template, T dataitem, GetDataItemFieldValue<T> gfv, GetDataItemUrl<T> gu, ITextFunctionHandler[] tfh)
        {
            string s = template;

            if (gu != null)
            {
                // Replace <a>xyz</a> with real link tags
                s = RegexUtil.rxHtmlLink.Replace(s, delegate(Match m)
                {
                    return BuildAnchorTag(gu(dataitem), m.Groups["linkcontent"].Value);
                });
            }

            // store variables to enable better function parsing
            var variables = new Dictionary<string, string>();

            // Sample match: {"some prefix", [my field?param=1], "some suffix"} 
            s = RegexUtil.rxFieldExpressionWithPreSufFix.Replace(s, delegate(Match m)
            {
                var varID = GetVariableGuid(m.Value);
                variables.Add(varID, gfv(dataitem, m.Groups["fieldidentifier"].Value, m.Groups["before"].Value, m.Groups["after"].Value, m.Groups["parameters"].Value));
                return varID;
            });

            // Sample match: [my field?param=1]
            s = RegexUtil.rxFieldExpression.Replace(s, delegate(Match m)
            {
                var varID = GetVariableGuid(m.Value);
                variables.Add(varID, gfv(dataitem, m.Groups["fieldidentifier"].Value, string.Empty, string.Empty, m.Groups["parameters"].Value));
                return varID;
            });

            s = FunctionInterpreter.Process(s, tfh, variables);

            // Resolve any embedded variables to their field values...
            foreach (var entry in variables)
                s = s.Replace((string)entry.Key, (string)entry.Value);

            return s;
        }

        public static string Render<T>(CompiledView view, T dataitem, GetDataItemFieldValue<T> gfv, GetDataItemUrl<T> gu, ITextFunctionHandler[] tfh)
        {
            var variables = view.ResolveVariables(dataitem, gfv);

            var s = FunctionInterpreter.Process(view.ViewText, tfh, variables);

            if (gu != null)
            {
                // Replace <a>xyz</a> with real link tags
                s = RegexUtil.rxHtmlLink.Replace(s, delegate(Match m)
                {
                    return BuildAnchorTag(gu(dataitem), m.Groups["linkcontent"].Value);
                });
            }

            // Resolve any variables to their values...
            foreach (var entry in variables)
                s = s.Replace((string)entry.Key, (string)entry.Value);

            return s;
        }

        /// <summary>
        /// Offers better performance. 
        /// Using this method, variables are only parsed out just once.
        /// Returns the "compiled" text to pass to the subsequent Render method
        /// </summary>
        public static CompiledView Compile(string view)
        {
            var declarations = new Dictionary<string, FieldDeclaration>();

            // Sample match: {"some prefix", [my field?param=1], "some suffix"} 
            var result = RegexUtil.rxFieldExpressionWithPreSufFix.Replace(view, delegate(Match m)
            {
                var varID = GetVariableGuid(m.Value);
                if (!declarations.ContainsKey(varID))
                {
                    declarations.Add(varID, new FieldDeclaration()
                    {
                        FieldName = m.Groups["fieldidentifier"].Value,
                        Before = m.Groups["before"].Value,
                        After = m.Groups["after"].Value,
                        Parameters = m.Groups["parameters"].Value
                    });
                }
                return varID;
            });

            // Sample match: [my field?param=1]
            result = RegexUtil.rxFieldExpression.Replace(result, delegate(Match m)
            {
                var varID = GetVariableGuid(m.Value);
                if (!declarations.ContainsKey(varID))
                {
                    declarations.Add(varID, new FieldDeclaration()
                    {
                        FieldName = m.Groups["fieldidentifier"].Value,
                        Before = string.Empty,
                        After = string.Empty,
                        Parameters = m.Groups["parameters"].Value
                    });
                }
                return varID;
            });

            return new CompiledView(result, declarations);
        }

        /// <summary>
        /// Store variable name/id map so we can cache any parsed @functions.. 
        /// by keeping the same guid we can cache the function parse tree and re-use it. 
        /// </summary>
        private static string GetVariableGuid(string name)
        {
            var newid = "@{" + Guid.NewGuid().ToString() + "}";
            if (_variableCache.ContainsKey(name))
                newid = _variableCache[name];
            else
            {
                lock (_lockMgr.AcquireKeyLock(name))
                {
                    if (_variableCache.ContainsKey(name))
                        newid = _variableCache[name];
                    else
                        _variableCache.Add(name, newid);
                }
            }
            return newid;
        }

        public static string Render(string template, Item item)
        {
            return Render(template, item, DefaultGetFieldValue, DefaultGetDataItemUrl);
        }
        public static string Render(string template, DataRow dr)
        {
            return Render(template, dr, DefaultGetFieldValue, null);
        }
        public static string Render(string template, NameValueCollection nvc)
        {
            return Render(template, nvc, DefaultGetFieldValue, null);
        }
        public static string Render(string template, IDictionary dictionary)
        {
            return Render(template, dictionary, DefaultGetFieldValue, null);
        }

        private static string DefaultGetFieldValue(Item item, string fieldname, string before, string after, string parameters)
        {
            Field f = item.Fields[fieldname];
            if (f != null)
            {
                if (item[f.ID].IsNullOrEmpty())
                    return Sitecore.Web.UI.WebControls.FieldRenderer.Render(item, f.ID.ToString(), parameters);
                else
                {
                    FieldRenderer fr = new FieldRenderer();
                    fr.Before = before;
                    fr.After = after;
                    fr.Parameters = parameters;
                    fr.FieldName = f.ID.ToString();
                    fr.Item = item;
                    return fr.Render();
                }
            }
            return string.Empty;
        }
        private static string DefaultGetFieldValue(DataRow dr, string fieldname, string before, string after, string parameters)
        {
            return ApplyBeforeAfterValues(before, dr[fieldname].ToString(), after);
        }
        private static string DefaultGetFieldValue(NameValueCollection nvc, string fieldname, string before, string after, string parameters)
        {
            return ApplyBeforeAfterValues(before, nvc[fieldname], after);
        }
        private static string DefaultGetFieldValue(IDictionary dictionary, string fieldname, string before, string after, string parameters)
        {
            return ApplyBeforeAfterValues(before, dictionary[fieldname].ToString(), after);
        }
        private static string DefaultGetDataItemUrl(Item item)
        {
            return LinkManager.GetItemUrl(item);
        }

        private static string ApplyBeforeAfterValues(string before, string value, string after)
        {
            if (!string.IsNullOrEmpty(value))
                value = before + value + after;
            return value ?? string.Empty;
        }

        private static string BuildAnchorTag(string vcLinkHref, string vcLinkText)
        {
            StringBuilder sbOutput = new StringBuilder();
            using (StringWriter sw = new StringWriter(sbOutput))
            {
                using (HtmlTextWriter output = new HtmlTextWriter(sw))
                {
                    output.AddAttribute(HtmlTextWriterAttribute.Href, vcLinkHref);
                    output.RenderBeginTag(HtmlTextWriterTag.A);
                    output.Write(vcLinkText);
                    output.RenderEndTag(); //A
                }
            }
            return sbOutput.ToString();
        }
    }
}
