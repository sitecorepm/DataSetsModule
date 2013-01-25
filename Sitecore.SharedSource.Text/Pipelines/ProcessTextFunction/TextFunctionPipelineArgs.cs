using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Pipelines;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction
{
    public class TextFunctionPipelineArgs : PipelineArgs
    {
        public TextFunctionPipelineArgs() { }

        public bool FunctionHandled { get; set; }
        public string FunctionName { get; set; }
        public object[] Args { get; set; }
        public ITextFunctionHandler[] CustomFunctionHandlers { get; set; }
        public BaseFunctionProcessor HandledBy { get; set; }
        public string Result { get; set; }

        /// <summary>
        /// Sets result AND marks FunctionHandled=True
        /// </summary>
        /// <param name="value"></param>
        public string HandledResult
        {
            set
            {
                this.Result = value;
                this.FunctionHandled = true;
            }
        }
    }
}
