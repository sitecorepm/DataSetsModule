using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;

namespace Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction
{
	public abstract class BaseFunctionProcessor : IPipelineProcessor<TextFunctionPipelineArgs>
    {
        // Methods
        public void Process(TextFunctionPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(args.FunctionName, "Function Name");
            Assert.ArgumentNotNull(args.Args, "Function Arguments");

            if (!args.FunctionHandled)
            {
                args.HandledBy = this;
                ProcessFunction(args);
            }
        }


        public abstract void ProcessFunction(TextFunctionPipelineArgs args);
	}
}
