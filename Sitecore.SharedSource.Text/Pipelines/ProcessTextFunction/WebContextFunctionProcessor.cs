using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Events;
using Sitecore.SecurityModel;
using System.Web;
//using Sitecore.SharedSource.Dataset.Extensions;

namespace Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction
{
    public class WebContextFunctionProcessor : BaseFunctionProcessor
    {
        public override void ProcessFunction(TextFunctionPipelineArgs pipeArgs)
        {
            var args = pipeArgs.Args;

            switch (pipeArgs.FunctionName)
            {
                case "querystring": // querystring("key"), querystring("key","defaultvalue")
                    if (args.Length > 1)
                        pipeArgs.HandledResult = GetQueryStringValue((string)args[0], (string)args[1]);
                    else
                        pipeArgs.HandledResult = GetQueryStringValue((string)args[0], null);
                    break;
                case "htmlencode": // htmlencode("<p>htmlstuff</p>")
                    pipeArgs.HandledResult = HttpUtility.HtmlEncode((string)args[0]);
                    break;
                case "htmldecode": // htmldecode("<p>htmlstuff</p>")
                    pipeArgs.HandledResult = HttpUtility.HtmlDecode((string)args[0]);
                    break;
                case "urlencode": // urlencode("<p>urlstuff</p>")
                    pipeArgs.HandledResult = HttpUtility.UrlEncode((string)args[0]);
                    break;
                case "urldecode": // urldecode("<p>urlstuff</p>")
                    pipeArgs.HandledResult = HttpUtility.UrlDecode((string)args[0]);
                    break;
            }
        }

        private static string GetQueryStringValue(string key, string defaultvalue)
        {
            var result = string.Empty;
            var ctx = System.Web.HttpContext.Current;
            if (ctx != null &&
                ctx.Request != null &&
                ctx.Request.Params.Count > 0 &&
                !string.IsNullOrEmpty(key))
            {
                if (ctx.Request.Params.AllKeys.Any(x => x.ToLower() == key.ToLower()))
                    result = ctx.Request.Params[key];
                else if (defaultvalue != null)
                    result = defaultvalue;
            }
            return result;
        }

    }
}
