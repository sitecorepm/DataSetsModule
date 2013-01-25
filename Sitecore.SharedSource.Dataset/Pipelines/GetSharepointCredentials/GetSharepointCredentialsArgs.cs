using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Pipelines;
using Sitecore.Data.Items;
using System.Net;

namespace Sitecore.SharedSource.Dataset.Pipelines.GetSharepointCredentials
{
    public class GetSharepointCredentialsArgs : PipelineArgs
    {
        public GetSharepointCredentialsArgs() { }

        public Item SharepointQueryItem { get; set; }
        public ICredentials Credentials { get; set; }
    }
}
