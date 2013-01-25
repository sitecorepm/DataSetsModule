using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Sitecore.SharedSource.Dataset.Items;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.SharedSource.Dataset.Fields;
using System.Collections.Generic;


namespace Sitecore.SharedSource.Dataset.ContentEditorFieldTypes
{
    public class DatasetRendererTemplate : Frame
    {
        private static Database _contentDB = Sitecore.Context.ContentDatabase;

        public DatasetRendererTemplate()
        {
            this.Class = "scContentControlIFrame";
            base.Activation = true;
            base.AllowTransparency = false;
            this.Style.Add("padding", "5px");
        }

        #region Properties
        private DatasetRendererItem ContextDatasetRendererItem
        {
            get
            {
                return _contentDB.GetItem(this.ItemID);
            }
        }

        public bool TrackModified
        {
            get
            {
                return base.GetViewStateBool("TrackModified", true);
            }
            set
            {
                base.SetViewStateBool("TrackModified", value, true);
            }
        }

        public string FieldID
        {
            get
            {
                return base.GetViewStateString("FieldID");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateString("FieldID", value);
            }
        }

        public string ItemID
        {
            get
            {
                return base.GetViewStateString("ItemID");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateString("ItemID", value);
            }
        }

        public string ItemLanguage
        {
            get
            {
                return base.GetViewStateString("ItemLanguage");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateString("ItemLanguage", value);
            }
        }

        public string Source
        {
            get
            {
                return base.GetViewStateString("Source");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateString("Source", value);
            }
        }

        #endregion

        protected override void DoRender(HtmlTextWriter output)
        {
            string str2 = this.AllowTransparency ? " allowtransparency=\"true\"" : string.Empty;
            output.WriteLine();
            var map = this.ContextDatasetRendererItem.Dataset.FieldMap;
            var s = HttpUtility.HtmlEncode(DatasetRendererTemplateField.ToEditorValue(map, this.Value)).Trim(new char[] { ' ', '\n' });

            s = RegexUtil.rxFieldExpressionWithPreSufFixHtmlEncoded.Replace(s, delegate(Match m) { return MarkFieldWithSpan(map, m); });
            s = RegexUtil.rxFieldExpression.Replace(s, delegate(Match m) { return MarkFieldWithSpan(map, m); });

            output.WriteLine("<div" + base.ControlAttributes + str2 + "><pre>" + s + "</pre></div>");
        }

        private string MarkFieldWithSpan(Dictionary<string, string> fieldmap, Match m)
        {
            if (fieldmap.Values.Any(x => x.TrimEnd(new char[] { '?' }) == m.Groups["fieldidentifier"].Value))
                return "<span style='line-height: normal; padding: 0px 2px; margin: 0px 2px; border: 1px solid #999999; background-color: rgb(222, 231, 248);'>" + m.Value + "</span>";
            else
                return m.Value;
        }

        protected void Edit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!this.Disabled)
            {
                if (args.IsPostBack)
                {
                    var dsr = this.ContextDatasetRendererItem;
                    var value = args.Result;
                    if (value != this.Value && value != "undefined")
                    {
                        this.SetModified();
                        this.Value = DatasetRendererTemplateField.ToRawValue(dsr.Dataset.FieldMap, value);
                        SheerResponse.Refresh(this);
                    }
                }
                else
                {
                    var dsr = this.ContextDatasetRendererItem;
                    var fields = string.Empty;
                    if (dsr.SelectFields.Length > 0)
                        fields = string.Join("|", dsr.SelectFields.Select(delegate(string x)
                        {
                            if (dsr.Dataset.FieldMap.ContainsKey(x))
                                return dsr.Dataset.FieldMap[x];
                            else
                                return "('" + x + "' not in field list)";
                        }).OrderBy(x => x).ToArray());
                    else
                        fields = string.Join("|", dsr.Dataset.FieldMap.Values.OrderBy(x => x).ToArray());

                    var urlString = new UrlString("/sitecore modules/Shell/Datasets/Field/DatasetRendererTemplateEditor.aspx");
                    var handle = new UrlHandle();
                    handle["value"] = DatasetRendererTemplateField.ToEditorValue(dsr.Dataset.FieldMap, this.Value);
                    handle["fields"] = fields;
                    handle.Add(urlString);
                    SheerResponse.ShowModalDialog(urlString.ToString(), "1000px", "500px", string.Empty, true);
                    args.WaitForPostBack();
                }
            }
        }

        protected virtual void SetModified()
        {
            if (this.TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
                SheerResponse.Eval("scContent.startValidators()");
            }
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Arguments["id"] == this.ID)
            {
                switch (message.Name)
                {
                    case "datasetrenderertemplate:edit":
                        Sitecore.Context.ClientPage.Start(this, "Edit");
                        break;
                }
                base.HandleMessage(message);
            }
        }

        private static bool IsDatasetRenderer(Sitecore.Data.Items.Item item)
        {
            return item.Database != null && item.Database.Name == "master" && item.TemplateName == "DatasetRenderer";
        }
    }
}
