using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Events;
using Sitecore.SecurityModel;
//using Sitecore.SharedSource.Dataset.Extensions;

namespace Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction
{
    public class CoreFunctionProcessor : BaseFunctionProcessor
    {
        public override void ProcessFunction(TextFunctionPipelineArgs pipeArgs)
        {
            var args = pipeArgs.Args;

            switch (pipeArgs.FunctionName)
            {
                case "trim":
                    pipeArgs.HandledResult = ((string)args[0]).Trim();
                    break;
                case "replace":
                    pipeArgs.HandledResult = ((string)args[0]).Replace((string)args[1], (string)args[2]);
                    break;
                case "formatdate":
                    pipeArgs.HandledResult = FormatDate((string)args[0], (string)args[1]);
                    break;
                case "now":
                    if (args.Length == 0)
                        pipeArgs.HandledResult = DateTime.Now.ToString();
                    else
                        pipeArgs.HandledResult = DateTime.Now.ToString((string)args[0]);
                    break;
                case "if":
                    if ((bool)args[0])
                        pipeArgs.HandledResult = (string)args[1];
                    else
                        pipeArgs.HandledResult = (string)args[2];
                    break;
                case "dateadd":
                    pipeArgs.HandledResult = DateAdd((string)args[0], (string)args[1]);
                    break;
                case "substring":
                    var source = (string)args[0];
                    var startIndex = (int)args[1];
                    if (args.Length == 2)
                        pipeArgs.HandledResult = source.Substring(startIndex);
                    else if (args.Length == 3)
                    {
                        var length = (int)args[2];
                        pipeArgs.HandledResult = source.Substring(startIndex, Math.Min(source.Length - startIndex, length));
                    }
                    break;
            }

        }

        private static string FormatDate(string datestring, string formatstring)
        {
            string result = string.Empty;
            DateTime date;

            if (DateTime.TryParse(datestring, out date))
                result = date.ToString(formatstring);

            return result;
        }

        private static string DateAdd(string timespan, string datestring)
        {
            string result = string.Empty;
            DateTime date;
            TimeSpan ts;

            if (DateTime.TryParse(datestring, out date))
            {
                if (TimeSpan.TryParse(timespan, out ts))
                    result = date.Add(ts).ToString();
            }

            return result;
        }
    }
}
