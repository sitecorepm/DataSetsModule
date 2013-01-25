using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Pipelines;

namespace Sitecore.SharedSource.Text.Pipelines
{
    public interface IPipelineProcessor<T> where T : PipelineArgs
    {
        void Process(T args);
    }
}
