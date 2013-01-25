using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Fields;
using Sitecore.SharedSource.Dataset.Items;
using System.Text.RegularExpressions;

namespace Sitecore.SharedSource.Dataset.Fields
{
    public class DatasetRendererTemplateField : CustomField
    {
        public DatasetRendererTemplateField(Field field)
            : base(field)
        {
        }

        public static implicit operator DatasetRendererTemplateField(Field field)
        {
            if (field == null)
                return null;
            return new DatasetRendererTemplateField(field);
        }

        private DatasetRendererItem _contextDatasetRenderer = null;
        private DatasetRendererItem ContextDatasetRenderer
        {
            get
            {
                if (_contextDatasetRenderer == null)
                    _contextDatasetRenderer = this.InnerField.Item;
                return _contextDatasetRenderer;
            }
        }

        public string EditorValue
        {
            get
            {
                return ToEditorValue(this.ContextDatasetRenderer.Dataset.FieldMap, this.InnerField.Value);
            }
            set
            {
                this.InnerField.Value = ToRawValue(this.ContextDatasetRenderer.Dataset.FieldMap, value);
            }
        }

        /// <summary>
        /// Replaces (friendly) fieldname references with fieldkey references for storage
        /// </summary>
        /// <param name="map">A mapping of fieldkey to fieldname</param>
        /// <param name="value">Text to convert</param>
        /// <returns></returns>
        public static string ToRawValue(Dictionary<string, string> map, string value)
        {
            var keyLookup = map.ToDictionary(k => k.Value.ToLower().TrimEnd(new char[] { '?' }), v => v.Key);

            return RegexUtil.rxFieldExpression.Replace(value, delegate(Match m)
            {
                var id = m.Groups["fieldidentifier"].Value.ToLower();
                if (keyLookup.ContainsKey(id))
                {
                    var result = keyLookup[id];
                    if (m.Groups["parameters"].Success && !string.IsNullOrEmpty(m.Groups["parameters"].Value))
                        result += "?" + m.Groups["parameters"].Value;
                    return "[" + result + "]";
                }
                return m.Value;
            });
        }

        /// <summary>
        /// Replaces fieldkey references with (friendly) fieldname references
        /// </summary>
        /// <param name="map">A mapping of fieldkey to fieldname</param>
        /// <param name="value">Text to convert</param>
        /// <returns></returns>
        public static string ToEditorValue(Dictionary<string, string> map, string value)
        {
            // Convert GUID field references to friendly display names
            return RegexUtil.rxFieldExpression.Replace(value, delegate(Match m)
            {
                var id = m.Groups["fieldidentifier"].Value;
                if (map.ContainsKey(id))
                {
                    var result = map[id];
                    if (m.Groups["parameters"].Success && !string.IsNullOrEmpty(m.Groups["parameters"].Value))
                        result += "?" + m.Groups["parameters"].Value;
                    return "[" + result + "]";
                }
                return m.Value;
            });
        }

        /// <summary>
        /// Store field keys (IDs for Sitecore items..) instead of field names in the database
        /// </summary>
        public string RawValue
        {
            get { return this.InnerField.Value; }
            set { this.InnerField.Value = value; }
        }
    }
}
