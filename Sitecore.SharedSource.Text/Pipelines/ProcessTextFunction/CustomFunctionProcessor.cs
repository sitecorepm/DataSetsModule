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
    public class CustomFunctionProcessor : BaseFunctionProcessor
    {
        public override void ProcessFunction(TextFunctionPipelineArgs pipeArgs)
        {
            var args = pipeArgs.Args;
            var fxHandlers = pipeArgs.CustomFunctionHandlers;
            var result = string.Empty;

            if ( fxHandlers != null && fxHandlers.Length > 0)
            {
                foreach (var fx in fxHandlers)
                {
                    if (fx.ProcessFunction(pipeArgs.FunctionName, args, ref result))
                    {
                        pipeArgs.HandledResult = result;
                        break;
                    }
                }
            }
        }
    }
}
